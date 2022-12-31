using ElectricityOffNotifier.AppHost.Models;
using ElectricityOffNotifier.AppHost.Options;
using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ElectricityOffNotifier.AppHost.Services;

public sealed class SetupStartupService : IHostedService
{
	private readonly IServiceScopeFactory _serviceScopeFactory;
	private readonly SetupOptions _setupOptions;

	public SetupStartupService(IOptions<SetupOptions> setupOptions, IServiceScopeFactory serviceScopeFactory)
	{
		_serviceScopeFactory = serviceScopeFactory;
		_setupOptions = setupOptions.Value;
	}
	
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<ElectricityDbContext>();
		
		if (_setupOptions is { Cities: { Count: > 0 } cities })
		{
			var citiesToAdd = new List<City>();
			
			foreach (CityModel city in cities)
			{
				// Add a city if it doesn't exist yet
				if (!await context.Cities.AnyAsync(
					    c => c.Name == city.Name && c.Region == city.Region,
					    cancellationToken: cancellationToken))
				{
					citiesToAdd.Add(new City
					{
						Name = city.Name,
						Region = city.Region
					});
				}
			}

			if (citiesToAdd.Count > 0)
			{
				context.Cities.AddRange(citiesToAdd);
				await context.SaveChangesAsync(cancellationToken);
			}
		}
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}
}