using System.Security.Claims;
using AspNetCore.Authentication.ApiKey;
using ElectricityOffNotifier.AppHost.Helpers;
using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectricityOffNotifier.AppHost.Auth;

internal sealed class ApiKeyProvider : IApiKeyProvider
{
	private readonly ElectricityDbContext _context;
	private readonly IConfiguration _configuration;

	public ApiKeyProvider(ElectricityDbContext context, IConfiguration configuration)
	{
		_context = context;
		_configuration = configuration;
	}

	public async Task<IApiKey?> ProvideAsync(string key)
	{
		byte[] accessTokenHash = key.ToHmacSha256ByteArray(_configuration.GetRequiredValue("Auth:SecretKey"));
		
		Producer? producer = await _context.Producers
			.AsNoTracking()
			.FirstOrDefaultAsync(p => p.AccessTokenHash == accessTokenHash);

		if (producer == null)
			return null;
		
		var claims = new List<Claim>
		{
			new(ClaimTypes.NameIdentifier, producer.Id.ToString(), ClaimValueTypes.Integer),
			new(CustomClaimTypes.LocationId, producer.LocationId.ToString(), ClaimValueTypes.Integer)
		};
		return new ApiKey(key, claims, "ElectricityChecker");
	}
}