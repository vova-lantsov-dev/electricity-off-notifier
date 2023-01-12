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

	// A timer that postpones the checks after startup
	private readonly Task _startupDelay = Task.Delay(TimeSpan.FromMinutes(1d));

	public ElectricityCheckerManager(
		IRecurringJobManager recurringJobManager,
		IServiceScopeFactory scopeFactory,
		ITelegramNotifier telegramNotifier,
		HttpClient httpClient)
	{
		_recurringJobManager = recurringJobManager;
		_scopeFactory = scopeFactory;
		_telegramNotifier = telegramNotifier;
		_httpClient = httpClient;
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

	public async Task CheckAsync(int checkerId, CancellationToken cancellationToken)
	{
		if (!_startupDelay.IsCompleted)
			return;
		
		await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<ElectricityDbContext>();

		var checker = await dbContext.Checkers
			.AsNoTracking()
			.Include(c => c.Entries.OrderByDescending(e => e.DateTime).Take(1))
			.Include(c => c.Address)
			.ThenInclude(a => a.City)
			.FirstAsync(c => c.Id == checkerId, cancellationToken);
		
		if (checker.Entries.Count < 1)
			return;

		async Task LoadSubscribersAsync()
		{
			checker.Subscribers = await dbContext.Subscribers
				.AsNoTracking()
				.Where(s => s.CheckerId == checkerId)
				.ToListAsync(cancellationToken);
		}
		
		// Find a last notification sent to the Telegram chat
		SentNotification? lastNotification =
			await GetLastNotificationAsync(dbContext, checkerId, cancellationToken);
		
		// Did we already sent a Telegram notification about current status?
		bool isDown = DateTime.UtcNow - checker.Entries[0].DateTime > TimeSpan.FromSeconds(45);
		
		if (lastNotification is { IsUpNotification: true } && !isDown)
			return;
		
		if (lastNotification is { IsUpNotification: false } && isDown)
			return;

		// Insert a new sent notification entry
		await SetLastNotificationAsync(dbContext, checkerId, !isDown, cancellationToken);

		// Load the subscribers that need to be notified about electricity status
		await LoadSubscribersAsync();
		if (checker.Subscribers.Count == 0)
		{
			// There are no subscribers, skipping...
			return;
		}

		// Get a method delegate that should be invoked to notify about electricity status
		Func<SentNotification?, Address, Subscriber, CancellationToken, Task> action = isDown
			? _telegramNotifier.NotifyElectricityIsDownAsync
			: _telegramNotifier.NotifyElectricityIsUpAsync;
		
		// Notify every subscriber about electricity status
		foreach (Subscriber subscriber in checker.Subscribers)
		{
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