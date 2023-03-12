using Telegram.Bot;

namespace ElectricityOffNotifier.AppHost.Services;

public interface ITelegramBotAccessor
{
    string? GetTokenByBotId(long botId);
    
    ValueTask<ITelegramBotClient> GetBotClientAsync(byte[]? tokenBytes, CancellationToken cancellationToken);
}