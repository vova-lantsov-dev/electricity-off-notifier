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

	public ElectricityCheckerManager(
		IRecurringJobManager recurringJobManager,
		IServiceScopeFactory scopeFactory,
		ITelegramNotifier telegramNotifier)
	{
		_recurringJobManager = recurringJobManager;
		_scopeFactory = scopeFactory;
		_telegramNotifier = telegramNotifier;
	}

	public void StartChecker(int checkerId)
	{
		_recurringJobManager.AddOrUpdate(
			$"checker-{checkerId}",
			() => CheckAsync(checkerId, CancellationToken.None),
			"*/30 * * * * *");
	}

	public async Task CheckAsync(int checkerId, CancellationToken cancellationToken)
	{
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
		
		SentNotification? lastNotification =
			await GetLastNotificationAsync(dbContext, checkerId, cancellationToken);
		
		if (DateTime.UtcNow - checker.Entries[0].DateTime > TimeSpan.FromMinutes(2d))
		{
			// If we got here - it seems like the electricity is down
			if (lastNotification is not { IsUpNotification: false })
			{
				await LoadSubscribersAsync();
				await SetLastNotificationAsync(dbContext, checkerId, false, cancellationToken);

				foreach (var subscriber in checker.Subscribers)
				{
					await _telegramNotifier.NotifyElectricityIsDownAsync(checker.Entries[0], checker.Address,
						subscriber,
						cancellationToken);
				}
			}
		}
		else
		{
			// Otherwise, if the electricity is up again, check if we need to notify about that
			if (lastNotification is not { IsUpNotification: true })
			{
				await LoadSubscribersAsync();
				await SetLastNotificationAsync(dbContext, checkerId, true, cancellationToken);

				foreach (var subscriber in checker.Subscribers)
				{
					await _telegramNotifier.NotifyElectricityIsUpAsync(checker.Entries[0], checker.Address, subscriber,
						cancellationToken);
				}
			}
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