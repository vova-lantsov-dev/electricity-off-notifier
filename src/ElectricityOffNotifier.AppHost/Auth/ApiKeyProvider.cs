using System.Security.Claims;
using AspNetCore.Authentication.ApiKey;
using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectricityOffNotifier.AppHost.Auth;

internal sealed class ApiKeyProvider : IApiKeyProvider
{
	private readonly ElectricityDbContext _context;
	
	public ApiKeyProvider(ElectricityDbContext context)
	{
		_context = context;
	}

	public async Task<IApiKey?> ProvideAsync(string key)
	{
		Producer? producer = await _context.Producers
			.AsNoTracking()
			.FirstOrDefaultAsync(p => p.AccessToken == key);

		if (producer == null)
			return null;
		
		var claims = new List<Claim>
		{
			new(ClaimTypes.NameIdentifier, producer.Id.ToString(), ClaimValueTypes.Integer),
			new(CustomClaimTypes.CheckerId, producer.CheckerId.ToString(), ClaimValueTypes.Integer)
		};
		return new ApiKey(key, claims, "ElectricityChecker");
	}
}