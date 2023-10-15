namespace ElectricityOffNotifier.AppHost.Services;

public interface IElectricityCheckerManager
{
	void StartChecker(int locationId);

	void AddWebhookProducer(int locationId, int webhookProducerId);
}