using System.Security.Claims;
using AspNetCore.Authentication.ApiKey;
using ElectricityOffNotifier.AppHost.Auth;
using ElectricityOffNotifier.AppHost.Services;
using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Configure services
string hangfireConnStr = builder.Configuration.GetConnectionString("HangfireConnectionString");
builder.Services.AddHangfire(configuration => configuration
	.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
	.UseSimpleAssemblyNameTypeSerializer()
	.UseRecommendedSerializerSettings()
	.UsePostgreSqlStorage(hangfireConnStr));
builder.Services.AddHangfireServer();

builder.Services.AddDbServices();

builder.Services.AddAuthentication(ApiKeyDefaults.AuthenticationScheme)
	.AddApiKeyInAuthorizationHeader<ApiKeyProvider>(options =>
	{
		options.Realm = "Electricity Checker";
		options.SuppressWWWAuthenticateHeader = true;
	});

builder.Services.AddSingleton<IElectricityCheckerManager, ElectricityCheckerManager>();
builder.Services.AddSingleton<ITelegramNotifier, TelegramNotifier>();
builder.Services.AddHostedService<BackgroundStartupService>();

// Build and run the application
var app = builder.Build();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/ping", [Authorize] async (HttpContext context, ElectricityDbContext dbContext) =>
{
	int checkerId = int.Parse(context.User.FindFirstValue(CustomClaimTypes.CheckerId));

	var checkerEntry = new CheckerEntry
	{
		DateTime = DateTime.UtcNow,
		CheckerId = checkerId
	};
	dbContext.CheckerEntries.Add(checkerEntry);
	await dbContext.SaveChangesAsync();
});

app.MapHangfireDashboard(new DashboardOptions { IsReadOnlyFunc = _ => true });

await app.RunAsync();
