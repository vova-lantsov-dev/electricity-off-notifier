using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace ElectricityOffNotifier.AppHost.Services;

public sealed class BotManager : IBotManager
{
    private readonly ITelegramBotAccessor _botAccessor;
    private readonly ILogger<BotManager> _logger;
    private readonly ConcurrentDictionary<long, Task> _receivingTasks = new();

    public BotManager(ITelegramBotAccessor botAccessor, ILogger<BotManager> logger)
    {
        _botAccessor = botAccessor;
        _logger = logger;
    }
    
    public async Task StartBotIfNeededAsync(IUpdateHandler updateHandler, byte[]? botTokenBytes, CancellationToken cancellationToken)
    {
        await AddReceivingTaskAsync(updateHandler, botTokenBytes, CancellationToken.None);
    }

    public IEnumerable<Task> ReceivingTasks => _receivingTasks.Values;

    private async Task AddReceivingTaskAsync(IUpdateHandler updateHandler, byte[]? tokenBytes, CancellationToken cancellationToken)
    {
        ITelegramBotClient botClient = await _botAccessor.GetBotClientAsync(tokenBytes, cancellationToken);
        
        _logger.LogDebug("Created a bot client for bot {BotId}", botClient.BotId);

        if (!_receivingTasks.ContainsKey(botClient.BotId!.Value))
        {
            Task receivingTask = botClient.ReceiveAsync(updateHandler, cancellationToken: cancellationToken);
            _receivingTasks.TryAdd(botClient.BotId.Value, receivingTask);
        }
    }
}