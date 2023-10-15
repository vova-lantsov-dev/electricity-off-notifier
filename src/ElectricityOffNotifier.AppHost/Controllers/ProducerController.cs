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

namespace ElectricityOffNotifier.AppHost.Controllers;

[ApiController]
[Route("v2/[controller]")]
public sealed class ProducerController : ControllerBase
{
	private readonly ElectricityDbContext _context;
	private readonly IConfiguration _configuration;
	private readonly IValidator<ProducerRegisterModel> _producerRegisterModelValidator;
	private readonly IElectricityCheckerManager _electricityCheckerManager;

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

		string accessToken = AuthHelper.GenerateApiKey();
		byte[] accessTokenSha256Hash = accessToken.ToHmacSha256ByteArray(_configuration.GetRequiredValue("Auth:SecretKey"));

		var producer = new Producer
		{
			AccessTokenHash = accessTokenSha256Hash,
			Mode = model.Mode,
			WebhookUrl = model.WebhookUrl,
			Name = model.Name,
			LocationId = model.LocationId
		};
		_context.Producers.Add(producer);

		await _context.SaveChangesAsync(HttpContext.RequestAborted);
		
		if (model.Mode == ProducerMode.Webhook)
		{
			_electricityCheckerManager.AddWebhookProducer(producer.LocationId, producer.Id);
		}

		return Ok(new { accessToken });
	}
}