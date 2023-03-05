using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace ElectricityOffNotifier.AppHost.Services;

public sealed class BotBackgroundRunner : BackgroundService
{
    private readonly ITelegramBotAccessor _botAccessor;
    private readonly IUpdateHandler _updateHandler;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<BotBackgroundRunner> _logger;

    public BotBackgroundRunner(ITelegramBotAccessor botAccessor, IUpdateHandler updateHandler,
        IServiceScopeFactory serviceScopeFactory, ILogger<BotBackgroundRunner> logger)
    {
        _botAccessor = botAccessor;
        _updateHandler = updateHandler;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        List<ChatInfo> chats;

        await using (AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ElectricityDbContext>();
            chats = await context.ChatInfo.AsNoTracking().ToListAsync(stoppingToken);
        }
        
        _logger.LogInformation("Found {ChatsCount} chats on startup, registering the clients...",
            chats.Count);

        var receivingTasks = new Dictionary<long, Task>();
        
        _logger.LogInformation("Starting a default bot client");
        await AddReceivingTaskAsync(receivingTasks, null, stoppingToken);
        
        _logger.LogInformation("Starting custom bot clients");
        foreach (byte[]? tokenBytes in chats.GroupBy(ci => ci.BotTokenOverride).Select(it => it.Key))
        {
            await AddReceivingTaskAsync(receivingTasks, tokenBytes, stoppingToken);
        }

        await Task.WhenAll(receivingTasks.Values);
    }

    private async Task AddReceivingTaskAsync(Dictionary<long, Task> receivingTasks, byte[]? tokenBytes, CancellationToken cancellationToken)
    {
        ITelegramBotClient botClient = await _botAccessor.GetBotClientAsync(tokenBytes, cancellationToken);
        
        _logger.LogDebug("Created a bot client for bot {BotId}", botClient.BotId);

        if (!receivingTasks.ContainsKey(botClient.BotId!.Value))
        {
            Task receivingTask = botClient.ReceiveAsync(_updateHandler, cancellationToken: cancellationToken);
            receivingTasks.Add(botClient.BotId.Value, receivingTask);
        }
    }
}