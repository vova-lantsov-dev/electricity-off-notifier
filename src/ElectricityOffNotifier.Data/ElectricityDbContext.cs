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
}