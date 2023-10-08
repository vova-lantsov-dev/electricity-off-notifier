using System.Security.Claims;
using ElectricityOffNotifier.AppHost.Auth;
using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models;
using ElectricityOffNotifier.Data.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectricityOffNotifier.AppHost.Controllers;

[ApiController]
[Route("v2/[controller]")]
[Authorize]
public sealed class PingController : ControllerBase
{
	private readonly ElectricityDbContext _context;

	public PingController(ElectricityDbContext context)
	{
		_context = context;
	}
	
	[HttpPost]
	public async Task<ActionResult> Ping()
	{
		int producerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
		int locationId = int.Parse(User.FindFirstValue(CustomClaimTypes.LocationId)!);

		Producer producer = await _context.Producers
			.Select(c => new Producer {Id = c.Id, Mode = c.Mode})
			.FirstAsync(c => c.Id == producerId);

		if (producer.Mode != ProducerMode.Polling)
		{
			ModelState.AddModelError($"{nameof(Producer)}.{nameof(Producer.Mode)}",
				$"Producer mode must be '{nameof(ProducerMode.Polling)}' to use this endpoint. " +
				"Please, register another producer.");
			return BadRequest(ModelState);
		}

		var location = new Location
		{
			Id = locationId
		};
		_context.Locations.Attach(location);
		
		location.LastSeenAt = DateTime.UtcNow;
	
		await _context.SaveChangesAsync();

		return Ok();
	}
}