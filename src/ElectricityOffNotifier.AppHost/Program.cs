using AspNetCore.Authentication.ApiKey;
using ElectricityOffNotifier.AppHost.Auth;
using ElectricityOffNotifier.AppHost.Options;
using ElectricityOffNotifier.AppHost.Services;
using ElectricityOffNotifier.Data;
using FluentValidation;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
	builder.Services.AddApplicationInsightsTelemetryWorkerService();
}

builder.Configuration.AddJsonFile("setup.json", optional: true, reloadOnChange: false)
	.AddJsonFile($"setup.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);

// Configure services
builder.Services.AddControllers();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

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
		options.KeyName = "Authorization";
		options.SuppressWWWAuthenticateHeader = true;
		options.IgnoreAuthenticationIfAllowAnonymous = true;
	});
builder.Services.AddAuthorization();

builder.Services.AddSingleton<ITelegramBotClient>(provider =>
{
	var configuration = provider.GetRequiredService<IConfiguration>();
	return new TelegramBotClient(configuration["Bot:Token"]);
});

builder.Services.AddOptions<SetupOptions>()
	.BindConfiguration("Setup")
	.ValidateDataAnnotations();
builder.Services.AddHostedService<SetupStartupService>();

builder.Services.AddSingleton<IUpdateHandler, BotUpdateHandler>();
builder.Services.AddHostedService<BotBackgroundRunner>();

builder.Services.AddSingleton<ITemplateService, TemplateService>();
builder.Services.AddSingleton<ITelegramNotifier, TelegramNotifier>();
builder.Services.AddSingleton<IElectricityCheckerManager, ElectricityCheckerManager>();
builder.Services.AddHttpClient<IElectricityCheckerManager, ElectricityCheckerManager>(opts =>
{
	opts.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHostedService<HangfireStartupService>();

// Build and run the application
var app = builder.Build();

await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<ElectricityDbContext>();

	if ((await dbContext.Database.GetPendingMigrationsAsync()).Any())
	{
		await dbContext.Database.MigrateAsync();
	}
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHangfireDashboard(new DashboardOptions
{
	IsReadOnlyFunc = _ => true,
	Authorization = new IDashboardAuthorizationFilter[]
	{
		new HangfireAuthorizationFilter()
	},
	DisplayStorageConnectionString = false
});

await app.RunAsync();
