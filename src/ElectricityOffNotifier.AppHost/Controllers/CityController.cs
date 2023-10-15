using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectricityOffNotifier.AppHost.Controllers;

[ApiController]
[Route("v2/[controller]")]
public sealed class CityController : ControllerBase
{
	private readonly ElectricityDbContext _context;

	public CityController(ElectricityDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<ActionResult<List<City>>> ListCities(CancellationToken cancellationToken)
	{
		return await _context.Cities.AsNoTracking().ToListAsync(cancellationToken);
	}
}