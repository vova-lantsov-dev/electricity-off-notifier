﻿using System.Security.Claims;
using ElectricityOffNotifier.AppHost.Auth;
using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models;
using ElectricityOffNotifier.Data.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
		int producerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
		int checkerId = int.Parse(User.FindFirstValue(CustomClaimTypes.CheckerId));

		Producer producer = await _context.Producers
			.Select(c => new Producer {Id = c.Id, IsEnabled = c.IsEnabled, Mode = c.Mode})
			.FirstAsync(c => c.Id == producerId);

		if (!producer.IsEnabled)
		{
			return StatusCode(403, new
			{
				reason = "Your API key is disabled. Please contact @vova_lantsov to enable it."
			});
		}

		if (producer.Mode != ProducerMode.Polling)
		{
			ModelState.AddModelError($"{nameof(Producer)}.{nameof(Producer.Mode)}",
				$"Producer mode must be '{nameof(ProducerMode.Polling)}' to use this endpoint. " +
				"Please, register another producer.");
			return BadRequest(ModelState);
		}

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