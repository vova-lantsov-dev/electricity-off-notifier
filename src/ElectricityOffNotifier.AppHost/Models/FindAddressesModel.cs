using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ElectricityOffNotifier.AppHost.Models;

public sealed class FindAddressesModel
{
	[Required, FromQuery(Name = "street")]
	public string Street { get; set; }
	[Required, FromQuery(Name = "buildingNo")]
	public string BuildingNo { get; set; }
}