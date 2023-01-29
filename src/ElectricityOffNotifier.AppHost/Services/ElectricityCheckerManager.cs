using System.Text.Json;
using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ElectricityOffNotifier.AppHost.Services;

public sealed class ElectricityCheckerManager : IElectricityCheckerManager
{
	private readonly IRecurringJobManager _recurringJobManager;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ITelegramNotifier _telegramNotifier;
	private readonly HttpClient _httpClient;
	private readonly ILogger<ElectricityCheckerManager> _logger;

	// A time of startup that is used to postpone the checks after startup
	private static readonly DateTime StartupTime = DateTime.Now;

	public ElectricityCheckerManager(
		IRecurringJobManager recurringJobManager,
		IServiceScopeFactory scopeFactory,
		ITelegramNotifier telegramNotifier,
		HttpClient httpClient,
		ILogger<ElectricityCheckerManager> logger)
	{
		_recurringJobManager = recurringJobManager;
		_scopeFactory = scopeFactory;
		_telegramNotifier = telegramNotifier;
		_httpClient = httpClient;
		_logger = logger;
	}

	public void StartChecker(int checkerId, int[] webhookProducerIds)
	{
		_recurringJobManager.AddOrUpdate(
			$"checker-{checkerId}",
			() => CheckAsync(checkerId, CancellationToken.None),
			"*/10 * * * * *");

		foreach (int webhookProducerId in webhookProducerIds)
		{
			_recurringJobManager.AddOrUpdate(
				$"c{checkerId}-p{webhookProducerId}-webhook",
				() => ProcessWebhookAsync(webhookProducerId, CancellationToken.None),
				"*/15 * * * * *");
		}
	}

	public void AddWebhookProducer(int checkerId, int webhookProducerId)
	{
		_recurringJobManager.AddOrUpdate(
			$"c{checkerId}-p{webhookProducerId}-webhook",
			() => ProcessWebhookAsync(webhookProducerId, CancellationToken.None),
			"*/15 * * * * *");
	}

	public async Task CheckAsync(int checkerId, CancellationToken cancellationToken)
	{
		if (DateTime.Now - StartupTime < TimeSpan.FromMinutes(1))
			return;

		await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<ElectricityDbContext>();

		var checker = await dbContext.Checkers
			.AsNoTracking()
			.Include(c => c.Entries.OrderByDescending(e => e.DateTime).Take(1))
			.Include(c => c.Address)
			.ThenInclude(a => a.City)
			.FirstAsync(c => c.Id == checkerId, cancellationToken);

		_logger.LogDebug("Checker {CheckerId} has {EntriesCount} entries", checkerId, checker.Entries.Count);

		if (checker.Entries.Count < 1)
			return;

		async Task LoadSubscribersAsync()
		{
			checker.Subscribers = await dbContext.Subscribers
				.AsNoTracking()
				.Include(s => s.ChatInfo)
				.Where(s => s.CheckerId == checkerId)
				.ToListAsync(cancellationToken);
		}

		// Find a last notification sent to the Telegram chat
		SentNotification? lastNotification =
			await GetLastNotificationAsync(dbContext, checkerId, cancellationToken);

		if (_logger.IsEnabled(LogLevel.Trace))
		{
			string lastNotificationJson = JsonSerializer.Serialize(lastNotification);
			_logger.LogTrace("Last notification for checker {CheckerId} is:\n{JsonValue}", checkerId,
				lastNotificationJson);
		}

		// Did we already sent a Telegram notification about current status?
		bool isDown = DateTime.UtcNow - checker.Entries[0].DateTime > TimeSpan.FromSeconds(45);
		_logger.LogDebug("Current status is {Status} for checker {CheckerId}",
			isDown ? "Down" : "Up", checkerId);

		if (lastNotification is { IsUpNotification: true } && !isDown)
		{
			_logger.LogDebug(
				"Last notification and current statuses are both Up for checker {CheckerId}, skipping...",
				checkerId);
			return;
		}

		if (lastNotification is { IsUpNotification: false } && isDown)
		{
			_logger.LogDebug(
				"Last notification and current statuses are both Down for checker {CheckerId}, skipping...",
				checkerId);
			return;
		}

		// Insert a new sent notification entry
		await SetLastNotificationAsync(dbContext, checkerId, !isDown, cancellationToken);

		// Load the subscribers that need to be notified about electricity status
		await LoadSubscribersAsync();
		if (checker.Subscribers.Count == 0)
		{
			_logger.LogDebug("There are no subscribers for checker {CheckerId}, skipping...", checkerId);
			return;
		}

		// Get a method delegate that should be invoked to notify about electricity status
		Func<SentNotification?, Address, Subscriber, CancellationToken, Task> action = isDown
			? _telegramNotifier.NotifyElectricityIsDownAsync
			: _telegramNotifier.NotifyElectricityIsUpAsync;

		// Notify every subscriber about electricity status
		foreach (Subscriber subscriber in checker.Subscribers)
		{
			_logger.LogDebug("Sending a Telegram notification about {Status} status to subscriber {SubscriberId}",
				isDown ? "Down" : "Up", subscriber.Id);
			await action(lastNotification, checker.Address, subscriber, cancellationToken);
		}
	}

	public async Task ProcessWebhookAsync(int producerId, CancellationToken cancellationToken)
	{
		await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<ElectricityDbContext>();

		Producer producer = await dbContext.Producers
			.AsNoTracking()
			.FirstAsync(p => p.Id == producerId, cancellationToken);
		
		if (!producer.IsEnabled)
			return;

		using HttpResponseMessage response = await _httpClient.GetAsync(producer.WebhookUrl!,
			HttpCompletionOption.ResponseHeadersRead, cancellationToken);
		
		if (response.IsSuccessStatusCode)
		{
			var checkerEntry = new CheckerEntry
			{
				DateTime = DateTime.UtcNow,
				CheckerId = producer.CheckerId
			};
			dbContext.CheckerEntries.Add(checkerEntry);
	
			await dbContext.SaveChangesAsync(cancellationToken);
		}
	}
	
	private static async Task<SentNotification?> GetLastNotificationAsync(
		ElectricityDbContext context,
		int checkerId,
		CancellationToken cancellationToken)
	{
		SentNotification? sentNotification = await context.SentNotifications
			.AsNoTracking()
			.OrderByDescending(sn => sn.DateTime)
			.FirstOrDefaultAsync(sn => sn.CheckerId == checkerId, cancellationToken);
		return sentNotification;
	}

	private static async Task SetLastNotificationAsync(
		ElectricityDbContext context,
		int checkerId,
		bool isUpNotification,
		CancellationToken cancellationToken)
	{
		var lastNotification = new SentNotification
		{
			CheckerId = checkerId,
			DateTime = DateTime.UtcNow,
			IsUpNotification = isUpNotification
		};
		context.SentNotifications.Add(lastNotification);
		await context.SaveChangesAsync(cancellationToken);
	}
}