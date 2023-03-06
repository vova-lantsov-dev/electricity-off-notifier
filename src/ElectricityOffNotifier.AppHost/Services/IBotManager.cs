namespace ElectricityOffNotifier.AppHost.Services;

public interface IBotManager
{
    Task StartBotIfNeededAsync(byte[] botTokenBytes);
}