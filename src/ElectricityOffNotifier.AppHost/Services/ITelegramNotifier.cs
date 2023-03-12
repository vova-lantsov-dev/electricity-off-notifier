using ElectricityOffNotifier.Data.Models;
using Telegram.Bot;

namespace ElectricityOffNotifier.AppHost.Services;

public interface ITelegramNotifier
{
	Task NotifyElectricityIsDownAsync(ITelegramBotClient botClient, SentNotification? upSince, Address address,
		Subscriber subscriber, CancellationToken cancellationToken);

	Task NotifyElectricityIsUpAsync(ITelegramBotClient botClient, SentNotification? downSince, Address address,
		Subscriber subscriber, CancellationToken cancellationToken);
}