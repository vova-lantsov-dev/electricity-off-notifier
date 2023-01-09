using Telegram.Bot;
using Telegram.Bot.Polling;

namespace ElectricityOffNotifier.AppHost.Services;

public sealed class BotBackgroundRunner : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUpdateHandler _updateHandler;

    public BotBackgroundRunner(ITelegramBotClient botClient, IUpdateHandler updateHandler)
    {
        _botClient = botClient;
        _updateHandler = updateHandler;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _botClient.ReceiveAsync(_updateHandler, cancellationToken: stoppingToken);
    }
}