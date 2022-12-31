using ElectricityOffNotifier.AppHost.Auth;
using ElectricityOffNotifier.AppHost.Models;
using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models;
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
	
	public async Task<ActionResult> Register([FromBody] ProducerRegisterModel model, CancellationToken cancellationToken)
	{
		if (!await _context.Addresses.AnyAsync(a => a.Id == model.AddressId, cancellationToken))
		{
			ModelState.AddModelError(nameof(model.AddressId), "Address with specified id was not found.");
			return BadRequest(ModelState);
		}

		string accessToken = _accessTokenGenerator.Next();
		byte[] accessTokenSha256Hash = accessToken.ToHmacSha256ByteArray(_configuration["Auth:SecretKey"]);

		// Producer is disabled by default
		// Need to be activated manually at the moment
		var producer = new Producer
		{
			AccessTokenHash = accessTokenSha256Hash,
			IsEnabled = false
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