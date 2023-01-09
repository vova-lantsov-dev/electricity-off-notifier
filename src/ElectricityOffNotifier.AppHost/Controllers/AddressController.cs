using ElectricityOffNotifier.AppHost.Models;
using ElectricityOffNotifier.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectricityOffNotifier.AppHost.Controllers;

[ApiController]
[Route("v1/address")]
public sealed class AddressController : ControllerBase
{
	private readonly ElectricityDbContext _context;

	public AddressController(ElectricityDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<List<AddressModel>>> FindAddressesInCity(
		[FromQuery] FindAddressesModel model,
		CancellationToken cancellationToken)
	{
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