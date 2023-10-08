using ElectricityOffNotifier.AppHost.Auth;
using ElectricityOffNotifier.AppHost.Helpers;
using ElectricityOffNotifier.AppHost.Models;
using ElectricityOffNotifier.AppHost.Services;
using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace ElectricityOffNotifier.AppHost.Controllers;

[ApiController]
[Route("v2/[controller]")]
public sealed class LocationController : ControllerBase
{
    private readonly ElectricityDbContext _context;
    private readonly IValidator<LocationRegisterModel> _locationRegisterModelValidator;
    private readonly IConfiguration _configuration;
    private readonly IElectricityCheckerManager _electricityCheckerManager;

    public LocationController(
        ElectricityDbContext context,
        IValidator<LocationRegisterModel> locationRegisterModelValidator,
        IConfiguration configuration,
        IElectricityCheckerManager electricityCheckerManager)
    {
        _context = context;
        _locationRegisterModelValidator = locationRegisterModelValidator;
        _configuration = configuration;
        _electricityCheckerManager = electricityCheckerManager;
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] LocationRegisterModel model, CancellationToken cancellationToken)
    {
        ValidationResult validationResult =
            await _locationRegisterModelValidator.ValidateAsync(model, cancellationToken);

        if (!validationResult.IsValid)
            return this.BadRequestExt(validationResult);

        string accessToken = AuthHelper.GenerateApiKey();
        byte[] accessTokenSha256Hash = accessToken.ToHmacSha256ByteArray(_configuration.GetRequiredValue("Auth:SecretKey"));

        var location = new Location
        {
            AccessTokenHash = accessTokenSha256Hash,
            CityId = model.CityId,
            FullAddress = model.FullAddress,
            LastSeenAt = DateTime.UnixEpoch,
            LastNotifiedAt = DateTime.UnixEpoch
        };
        _context.Locations.Add(location);

        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        
        _electricityCheckerManager.StartChecker(location.Id);

        return Ok(new { accessToken });
    }
}