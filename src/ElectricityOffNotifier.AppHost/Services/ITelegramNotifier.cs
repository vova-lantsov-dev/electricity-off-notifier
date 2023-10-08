using ElectricityOffNotifier.Data.Models;
using Telegram.Bot;

namespace ElectricityOffNotifier.AppHost.Services;

public interface ITelegramNotifier
{
	Task NotifyElectricityIsDownAsync(ITelegramBotClient botClient, Location location,
		Subscriber subscriber, CancellationToken cancellationToken);

	Task NotifyElectricityIsUpAsync(ITelegramBotClient botClient, Location location,
		Subscriber subscriber, CancellationToken cancellationToken);
}