using ElectricityOffNotifier.Data.Models;

namespace ElectricityOffNotifier.AppHost.Services;

public interface ITelegramNotifier
{
	Task NotifyElectricityIsDownAsync(SentNotification? upSince, Address address, Subscriber subscriber,
		CancellationToken cancellationToken);

	Task NotifyElectricityIsUpAsync(SentNotification? downSince, Address address, Subscriber subscriber,
		CancellationToken cancellationToken);
}