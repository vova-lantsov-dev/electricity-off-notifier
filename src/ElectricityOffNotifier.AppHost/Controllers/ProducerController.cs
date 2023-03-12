using ElectricityOffNotifier.AppHost.Auth;
using ElectricityOffNotifier.AppHost.Helpers;
using ElectricityOffNotifier.AppHost.Models;
using ElectricityOffNotifier.AppHost.Services;
using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models;
using ElectricityOffNotifier.Data.Models.Enums;
using FluentValidation;
using FluentValidation.Results;
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
	private readonly IValidator<ProducerRegisterModel> _producerRegisterModelValidator;
	private readonly IElectricityCheckerManager _electricityCheckerManager;

	private readonly Password _accessTokenGenerator = new(
		includeLowercase: true,
		includeUppercase: true,
		includeNumeric: true,
		includeSpecial: false,
		passwordLength: 20);

	public ProducerController(
		ElectricityDbContext context,
		IConfiguration configuration,
		IValidator<ProducerRegisterModel> producerRegisterModelValidator,
		IElectricityCheckerManager electricityCheckerManager)
	{
		_context = context;
		_configuration = configuration;
		_producerRegisterModelValidator = producerRegisterModelValidator;
		_electricityCheckerManager = electricityCheckerManager;
	}
	
	[HttpPost]
	public async Task<ActionResult> Register([FromBody] ProducerRegisterModel model, CancellationToken cancellationToken)
	{
		ValidationResult validationResult = await _producerRegisterModelValidator.ValidateAsync(model, cancellationToken);

		if (!validationResult.IsValid)
			return this.BadRequestExt(validationResult);

		string accessToken = _accessTokenGenerator.Next();
		byte[] accessTokenSha256Hash = accessToken.ToHmacSha256ByteArray(_configuration["Auth:SecretKey"]);

		// Producer is disabled by default
		// Need to be activated manually at the moment
		var producer = new Producer
		{
			AccessTokenHash = accessTokenSha256Hash,
			IsEnabled = false,
			Mode = model.Mode,
			WebhookUrl = model.WebhookUrl,
			Name = model.Name
		};
		
		Checker? checker = await _context.Checkers
			.AsNoTracking()
			.Where(c => c.AddressId == model.AddressId)
			.Select(c => new Checker {Id = c.Id})
			.FirstOrDefaultAsync(cancellationToken);
		bool existingChecker = checker != null;
		
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
		
		if (!existingChecker)
		{
			_electricityCheckerManager.StartChecker(checker.Id, model.Mode switch
			{
				ProducerMode.Webhook => new[] { producer.Id },
				_ => Array.Empty<int>()
			});
		}
		else if (model.Mode == ProducerMode.Webhook)
		{
			_electricityCheckerManager.AddWebhookProducer(checker.Id, producer.Id);
		}

		return Ok(new { accessToken });
	}
}