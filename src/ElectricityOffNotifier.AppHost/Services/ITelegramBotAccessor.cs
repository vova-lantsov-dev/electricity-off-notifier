using Telegram.Bot;

namespace ElectricityOffNotifier.AppHost.Services;

public interface ITelegramBotAccessor
{
    ValueTask<ITelegramBotClient> GetBotClientAsync(byte[]? tokenBytes, CancellationToken cancellationToken);
}