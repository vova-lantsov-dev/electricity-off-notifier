using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace ElectricityOffNotifier.AppHost.Services;

public sealed class BotBackgroundRunner : BackgroundService, IBotManager
{
    private readonly ITelegramBotAccessor _botAccessor;
    private readonly IUpdateHandler _updateHandler;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<BotBackgroundRunner> _logger;
    private readonly TaskCompletionSource _completionSource = new(TaskCreationOptions.LongRunning);
    private readonly Dictionary<long, Task> _receivingTasks = new();

    private CancellationToken _cancellationToken;

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
        _cancellationToken = stoppingToken;
        List<ChatInfo> chats;

        await using (AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ElectricityDbContext>();
            chats = await context.ChatInfo.AsNoTracking().ToListAsync(stoppingToken);
        }
        
        _logger.LogInformation("Found {ChatsCount} chats on startup, registering the clients...",
            chats.Count);
        
        _logger.LogInformation("Starting a default bot client");
        await AddReceivingTaskAsync(null, stoppingToken);
        
        _logger.LogInformation("Starting custom bot clients");
        foreach (byte[]? tokenBytes in chats.GroupBy(ci => ci.BotTokenOverride).Select(it => it.Key))
        {
            await AddReceivingTaskAsync(tokenBytes, stoppingToken);
        }
        
        stoppingToken.ThrowIfCancellationRequested();
        
        stoppingToken.Register(() => _completionSource.SetResult());
        await _completionSource.Task;

        await Task.WhenAll(_receivingTasks.Values);
    }

    private async Task AddReceivingTaskAsync(byte[]? tokenBytes, CancellationToken cancellationToken)
    {
        ITelegramBotClient botClient = await _botAccessor.GetBotClientAsync(tokenBytes, cancellationToken);
        
        _logger.LogDebug("Created a bot client for bot {BotId}", botClient.BotId);

        if (!_receivingTasks.ContainsKey(botClient.BotId!.Value))
        {
            Task receivingTask = botClient.ReceiveAsync(_updateHandler, cancellationToken: cancellationToken);
            _receivingTasks.Add(botClient.BotId.Value, receivingTask);
        }
    }

    public async Task StartBotIfNeededAsync(byte[] botTokenBytes)
    {
        await AddReceivingTaskAsync(botTokenBytes, _cancellationToken);
    }
}