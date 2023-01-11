using ElectricityOffNotifier.AppHost.Auth;
using ElectricityOffNotifier.AppHost.Models;
using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models;
using ElectricityOffNotifier.Data.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PasswordGenerator;

namespace ElectricityOffNotifier.AppHost.Controllers;

[ApiController]
[Route("v1/[controller]")]
public sealed class ProducerController : ControllerBase
{
	private readonly ElectricityDbContext _context;
	private readonly IConfiguration _configuration;

	private readonly Password _accessTokenGenerator = new(
		includeLowercase: true,
		includeUppercase: true,
		includeNumeric: true,
		includeSpecial: false,
		passwordLength: 20);

	public ProducerController(ElectricityDbContext context, IConfiguration configuration)
	{
		_context = context;
		_configuration = configuration;
	}
	
	[HttpPost]
	public async Task<ActionResult> Register([FromBody] ProducerRegisterModel model, CancellationToken cancellationToken)
	{
		if (!await _context.Addresses.AnyAsync(a => a.Id == model.AddressId, cancellationToken))
		{
			ModelState.AddModelError(nameof(model.AddressId), "Address with specified id was not found.");
			return BadRequest(ModelState);
		}

		switch (model)
		{
			case { Mode: ProducerMode.Webhook, WebhookUrl: null }:
				ModelState.AddModelError(nameof(model.WebhookUrl),
					$"Webhook URL must be set when '{nameof(ProducerMode.Webhook)}' mode is specified.");
				return BadRequest(ModelState);
			case { Mode: not ProducerMode.Webhook, WebhookUrl: not null }:
				ModelState.AddModelError(nameof(model.WebhookUrl),
					$"Webhook URL is allowed only when '{nameof(ProducerMode.Webhook)}' mode is specified.");
				return BadRequest(ModelState);
		}

		string accessToken = _accessTokenGenerator.Next();
		byte[] accessTokenSha256Hash = accessToken.ToHmacSha256ByteArray(_configuration["Auth:SecretKey"]);

		// Producer is disabled by default
		// Need to be activated manually at the moment
		var producer = new Producer
		{
			AccessTokenHash = accessTokenSha256Hash,
			IsEnabled = false,
			Mode = model.Mode,
			WebhookUrl = model.WebhookUrl
		};
		
		Checker? checker = await _context.Checkers
			.AsNoTracking()
			.Where(c => c.AddressId == model.AddressId)
			.Select(c => new Checker {Id = c.Id})
			.FirstOrDefaultAsync(cancellationToken);
		if (checker == null)
		{
			checker = new Checker
			{
				AddressId = model.AddressId,
				Producers = new List<Producer>
				{
					producer
				}
			};
			_context.Checkers.Add(checker);
		}
		else
		{
			producer.CheckerId = checker.Id;
			_context.Producers.Add(producer);
		}

		await _context.SaveChangesAsync(CancellationToken.None);

		return Ok(new { accessToken });
	}
}