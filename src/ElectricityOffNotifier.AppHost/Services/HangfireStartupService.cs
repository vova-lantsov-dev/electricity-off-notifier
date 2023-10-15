using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ElectricityOffNotifier.AppHost.Services;

public sealed class HangfireStartupService : IHostedService
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IElectricityCheckerManager _checkerManager;

	public HangfireStartupService(
		IServiceScopeFactory scopeFactory,
		IElectricityCheckerManager checkerManager)
	{
		_scopeFactory = scopeFactory;
		_checkerManager = checkerManager;
	}
	
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<ElectricityDbContext>();

		var checkers = await dbContext.Locations
			.Include(c => c.Producers)
			.Select(c => new { c.Id, c.Producers })
			.ToListAsync(cancellationToken: cancellationToken);

		foreach (var checker in checkers)
		{
			_checkerManager.StartChecker(checker.Id);
		}
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}
}