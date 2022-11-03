using System.Security.Claims;
using AspNetCore.Authentication.ApiKey;

namespace ElectricityOffNotifier.AppHost.Auth;

internal sealed record ApiKey(
	string Key,
	IReadOnlyCollection<Claim> Claims,
	string OwnerName)
	: IApiKey;