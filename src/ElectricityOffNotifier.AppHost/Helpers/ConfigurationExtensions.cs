namespace ElectricityOffNotifier.AppHost.Helpers;

public static class ConfigurationExtensions
{
    public static string GetRequiredValue(this IConfiguration configuration, string key)
    {
        return configuration[key] ?? throw new InvalidOperationException($"Configuration entry '{key}' is not set.");
    }
}