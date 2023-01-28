using ElectricityOffNotifier.AppHost.Helpers;
using ElectricityOffNotifier.AppHost.Models;
using ElectricityOffNotifier.Data;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectricityOffNotifier.AppHost.Controllers;

[ApiController]
[Route("v1/address")]
public sealed class AddressController : ControllerBase
{
	private readonly ElectricityDbContext _context;
	private readonly IValidator<FindAddressesModel> _findAddressesModelValidator;

	public AddressController(ElectricityDbContext context, IValidator<FindAddressesModel> findAddressesModelValidator)
	{
		_context = context;
		_findAddressesModelValidator = findAddressesModelValidator;
	}

	[HttpGet]
	public async Task<ActionResult<List<AddressModel>>> FindAddressesInCity(
		[FromQuery] FindAddressesModel model,
		CancellationToken cancellationToken)
	{
		ValidationResult validationResult = _findAddressesModelValidator.Validate(model);
		
		if (!validationResult.IsValid)
			return this.BadRequestExt(validationResult);

		return await _context.Addresses
			.AsNoTracking()
			.Where(a => EF.Functions.ILike(a.Street, $"%{model.Street}%") &&
						EF.Functions.ILike(a.BuildingNo, $"%{model.BuildingNo}%") &&
						a.CityId == model.CityId)
			.Take(200)
			.Select(a => new AddressModel
			{
				Id = a.Id,
				Street = a.Street,
				BuildingNo = a.BuildingNo
			})
			.ToListAsync(cancellationToken);
	}
}