namespace ElectricityOffNotifier.AppHost.Services;

public interface IElectricityCheckerManager
{
	void StartChecker(int checkerId, int[] webhookProducerIds);
}