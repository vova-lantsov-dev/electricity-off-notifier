using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ElectricityOffNotifier.AppHost.Models;

public sealed class FindAddressesModel
{
	[FromQuery(Name = "cityId")]
	public int CityId { get; set; }
	[FromQuery(Name = "street")]
	public string Street { get; set; }
	[FromQuery(Name = "buildingNo")]
	public string? BuildingNo { get; set; }
}

public sealed class FindAddressesModelValidator : AbstractValidator<FindAddressesModel>
{
	public FindAddressesModelValidator()
	{
		RuleFor(m => m.CityId).GreaterThanOrEqualTo(1);
		RuleFor(m => m.Street).NotNull().Matches("^[а-яА-Яa-zA-ZїЇєЄґҐ0-9- .']+$");
		RuleFor(m => m.BuildingNo).Matches("^[0-9a-zA-Zа-яА-Я-\\/]+$");
	}
}