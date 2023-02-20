using ElectricityOffNotifier.Data.Models;
using Telegram.Bot;

namespace ElectricityOffNotifier.AppHost.Services;

public interface ITelegramBotAccessor
{
    ValueTask<ITelegramBotClient> GetBotClientAsync(ChatInfo chatInfo, CancellationToken cancellationToken);
}