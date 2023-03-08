using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Polling;

namespace ElectricityOffNotifier.AppHost.Services;

public sealed class BotBackgroundRunner : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<BotBackgroundRunner> _logger;
    private readonly IBotManager _botManager;
    private readonly IUpdateHandler _updateHandler;
    private readonly TaskCompletionSource _completionSource = new();
    private readonly Dictionary<long, Task> _receivingTasks = new();

    public BotBackgroundRunner(IServiceScopeFactory serviceScopeFactory, ILogger<BotBackgroundRunner> logger,
        IBotManager botManager, IUpdateHandler updateHandler)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _botManager = botManager;
        _updateHandler = updateHandler;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        List<ChatInfo> chats;

        await using (AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ElectricityDbContext>();
            chats = await context.ChatInfo
                .AsNoTracking()
                .Where(c => c.BotTokenOverride != null)
                .ToListAsync(stoppingToken);
        }
        
        _logger.LogInformation("Found {ChatsCount} chats on startup, registering the clients...",
            chats.Count);
        
        _logger.LogInformation("Starting a default bot client");
        await _botManager.StartBotIfNeededAsync(_updateHandler, null, stoppingToken);
        
        _logger.LogInformation("Starting custom bot clients");
        foreach (byte[] tokenBytes in chats
                     .GroupBy(ci => ci.BotTokenOverride)
                     .Select(it => it.Key!))
        {
            await _botManager.StartBotIfNeededAsync(_updateHandler, tokenBytes, stoppingToken);
        }
        
        stoppingToken.ThrowIfCancellationRequested();
        
        stoppingToken.Register(() => _completionSource.SetResult());
        await _completionSource.Task;

        await Task.WhenAll(_receivingTasks.Values);
    }
}