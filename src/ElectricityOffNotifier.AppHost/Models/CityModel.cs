using System.ComponentModel.DataAnnotations;

namespace ElectricityOffNotifier.AppHost.Models;

public sealed class CityModel
{
	[Required]
	public string Name { get; set; }
	public string? Region { get; set; }
}