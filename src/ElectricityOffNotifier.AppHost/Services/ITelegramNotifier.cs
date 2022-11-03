using ElectricityOffNotifier.Data.Models;

namespace ElectricityOffNotifier.AppHost.Services;

public interface ITelegramNotifier
{
	Task NotifyElectricityIsDownAsync(CheckerEntry lastCheckerEntry, Address address, Subscriber subscriber,
		CancellationToken cancellationToken);

	Task NotifyElectricityIsUpAsync(CheckerEntry downSince, Address address, Subscriber subscriber,
		CancellationToken cancellationToken);
}