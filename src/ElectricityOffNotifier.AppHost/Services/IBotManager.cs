using Telegram.Bot.Polling;

namespace ElectricityOffNotifier.AppHost.Services;

public interface IBotManager
{
    Task StartBotIfNeededAsync(IUpdateHandler updateHandler, byte[]? botTokenBytes, CancellationToken cancellationToken);
}