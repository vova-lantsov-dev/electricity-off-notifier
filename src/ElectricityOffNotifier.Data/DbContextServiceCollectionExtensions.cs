using ElectricityOffNotifier.Data.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElectricityOffNotifier.Data;

public static class DbContextServiceCollectionExtensions
{
	public static IServiceCollection AddDbServices(this IServiceCollection services)
	{
		services.AddOptions<DatabaseEncryptionOptions>()
			.BindConfiguration(nameof(DatabaseEncryptionOptions))
			.ValidateDataAnnotations();
		
		services.AddDbContext<ElectricityDbContext>((provider, options) =>
		{
			var config = provider.GetRequiredService<IConfiguration>();
			options.UseNpgsql(config.GetConnectionString("ElectricityConnectionString"), npgsql =>
				npgsql.EnableRetryOnFailure()
					.MigrationsAssembly("ElectricityOffNotifier.Data"));
		});
		
		return services;
	}
}