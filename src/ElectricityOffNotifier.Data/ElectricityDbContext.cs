using ElectricityOffNotifier.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectricityOffNotifier.Data;

public class ElectricityDbContext : DbContext
{
	public ElectricityDbContext(DbContextOptions<ElectricityDbContext> options) : base(options)
	{
	}

	public DbSet<Address> Addresses => Set<Address>();
	public DbSet<City> Cities => Set<City>();
	public DbSet<Subscriber> Subscribers => Set<Subscriber>();
	public DbSet<Producer> Producers => Set<Producer>();
	public DbSet<Checker> Checkers => Set<Checker>();
	public DbSet<CheckerEntry> CheckerEntries => Set<CheckerEntry>();
	public DbSet<SentNotification> SentNotifications => Set<SentNotification>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder
			.Entity<SentNotification>()
			.Property(sn => sn.DateTime)
			.HasConversion(
				dt => DateTime.SpecifyKind(dt, DateTimeKind.Unspecified),
				dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc));
		
		modelBuilder
			.Entity<CheckerEntry>()
			.Property(ce => ce.DateTime)
			.HasConversion(
				dt => DateTime.SpecifyKind(dt, DateTimeKind.Unspecified),
				dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc));
	}
}