using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models;
using ElectricityOffNotifier.Data.Models.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace ElectricityOffNotifier.AppHost.Services;

public sealed class ElectricityCheckerManager : IElectricityCheckerManager
{
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITelegramNotifier _telegramNotifier;
    private readonly HttpClient _httpClient;
    private readonly ITelegramBotAccessor _telegramBotAccessor;
    private readonly ILogger<ElectricityCheckerManager> _logger;

    // A time of startup that is used to postpone the checks after startup
    private static readonly DateTime StartupTime = DateTime.Now;

    public ElectricityCheckerManager(
        IRecurringJobManager recurringJobManager,
        IServiceScopeFactory scopeFactory,
        ITelegramNotifier telegramNotifier,
        HttpClient httpClient,
        ITelegramBotAccessor telegramBotAccessor,
        ILogger<ElectricityCheckerManager> logger)
    {
        _recurringJobManager = recurringJobManager;
        _scopeFactory = scopeFactory;
        _telegramNotifier = telegramNotifier;
        _httpClient = httpClient;
        _telegramBotAccessor = telegramBotAccessor;
        _logger = logger;
    }

    public void StartChecker(int locationId)
    {
        _recurringJobManager.AddOrUpdate(
            $"checker-{locationId}",
            () => CheckAsync(locationId, CancellationToken.None),
            "*/10 * * * * *");
    }

    public void AddWebhookProducer(int locationId, int webhookProducerId)
    {
        _recurringJobManager.AddOrUpdate(
            $"c{locationId}-p{webhookProducerId}-webhook",
            () => ProcessWebhookAsync(webhookProducerId, CancellationToken.None),
            "*/15 * * * * *");
    }

    public async Task CheckAsync(int locationId, CancellationToken cancellationToken)
    {
        if (DateTime.Now - StartupTime < TimeSpan.FromMinutes(1))
            return;

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ElectricityDbContext>();

        Location location = await dbContext.Locations
            .AsNoTracking()
            .Include(loc => loc.City)
            .Include(loc => loc.Producers)
            .Include(loc => loc.Subscribers)
            .ThenInclude(subscriber => subscriber.ChatInfo)
            .FirstAsync(c => c.Id == locationId, cancellationToken);

        DateTime now = DateTime.UtcNow;
        bool alreadySent = true;

        if (alreadySent)
        {
            _logger.LogDebug("Nothing changed for location {LocationId}, skipping...", locationId);
            
            return;
        }

        var updatedLocation = new Location
        {
            Id = locationId,
            LastNotifiedAt = DateTime.UtcNow
        };
        dbContext.Locations.Attach(updatedLocation).Property(loc => loc.LastNotifiedAt).IsModified = true;

        await dbContext.SaveChangesAsync(CancellationToken.None);

        // Load the subscribers that need to be notified about availability status change
        if (location.Subscribers.Count == 0)
        {
            _logger.LogDebug("There are no subscribers for location {LocationId}, skipping broadcasting...", locationId);
            return;
        }

        // Get a method delegate that should be invoked to notify about electricity status
        Func<ITelegramBotClient, Location, Subscriber, CancellationToken, Task> action = location.CurrentStatus == LocationStatus.Offline
            ? _telegramNotifier.NotifyElectricityIsDownAsync
            : _telegramNotifier.NotifyElectricityIsUpAsync;

        // Notify every subscriber about electricity status
        foreach (Subscriber subscriber in location.Subscribers)
        {
            _logger.LogDebug("Sending a Telegram notification about {Status:G} status to subscriber {SubscriberId}",
                location.CurrentStatus, subscriber.Id);

            ITelegramBotClient botClient =
                await _telegramBotAccessor.GetBotClientAsync(subscriber.ChatInfo.BotTokenOverride, cancellationToken);
            await action(botClient, location, subscriber, cancellationToken);
        }
    }

    public async Task ProcessWebhookAsync(int producerId, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ElectricityDbContext>();

        Producer producer = await dbContext.Producers
            .AsNoTracking()
            .FirstAsync(p => p.Id == producerId, cancellationToken);

        using HttpResponseMessage response = await _httpClient.GetAsync(producer.WebhookUrl!,
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var location = new Location
            {
                Id = producer.LocationId,
                LastSeenAt = DateTime.UtcNow
            };
            dbContext.Locations.Attach(location).Property(loc => loc.LastSeenAt).IsModified = true;

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}