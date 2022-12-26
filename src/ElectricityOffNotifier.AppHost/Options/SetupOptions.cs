using ElectricityOffNotifier.AppHost.Models;

namespace ElectricityOffNotifier.AppHost.Options;

public sealed class SetupOptions
{
	public List<CityModel>? Cities { get; set; }
}