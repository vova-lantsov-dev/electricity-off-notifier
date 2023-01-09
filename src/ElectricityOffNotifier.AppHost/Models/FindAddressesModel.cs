using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace ElectricityOffNotifier.AppHost.Models;

public sealed class FindAddressesModel
{
	[Range(1, int.MaxValue), FromQuery(Name = "cityId")]
	public int CityId { get; set; }
	[Required, RegularExpression("^[а-яА-Яa-zA-ZїЇєЄґҐ0-9- .']+$"), FromQuery(Name = "street")]
	public string Street { get; set; }
	[Required, RegularExpression("^[0-9a-zA-Zа-яА-Я- /]+$"), FromQuery(Name = "buildingNo")]
	public string BuildingNo { get; set; }
}