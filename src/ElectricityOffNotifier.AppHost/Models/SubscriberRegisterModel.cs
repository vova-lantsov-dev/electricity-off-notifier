namespace ElectricityOffNotifier.AppHost.Models;

public sealed class SubscriberRegisterModel
{
    public long TelegramId { get; set; }
    public string? TimeZone { get; set; }
    public string? Culture { get; set; }
}