using System.Security.Claims;
using ElectricityOffNotifier.AppHost.Auth;
using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectricityOffNotifier.AppHost.Controllers;

[ApiController]
[Route("v1/[controller]")]
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
		int checkerId = int.Parse(User.FindFirstValue(CustomClaimTypes.CheckerId));

		var checkerEntry = new CheckerEntry
		{
			DateTime = DateTime.UtcNow,
			CheckerId = checkerId
		};
		_context.CheckerEntries.Add(checkerEntry);
	
		await _context.SaveChangesAsync();

		return Ok();
	}
}