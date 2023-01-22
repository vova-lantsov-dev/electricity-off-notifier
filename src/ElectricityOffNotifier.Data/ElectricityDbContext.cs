using ElectricityOffNotifier.Data.Models;
using ElectricityOffNotifier.Data.Models.Enums;
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
	public DbSet<ChatInfo> ChatInfo => Set<ChatInfo>();

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

		modelBuilder.Entity<Producer>()
			.Property(p => p.Mode)
			.HasDefaultValue(ProducerMode.Polling);

		modelBuilder.Entity<ChatInfo>(entity =>
		{
			entity.Property(ci => ci.Name)
				.HasDefaultValue("NAME");
			entity.Property(ci => ci.MessageUpTemplate)
				.HasDefaultValue("Повідомлення за адресою <b>{{Address}}</b>:\n\n<b>Електропостачання відновлено!</b>\n{{#SinceRegion}}\nБуло відсутнє з {{SinceDate}}\nЗагальна тривалість відключення: {{DurationHours}} год. {{DurationMinutes}} хв.\n{{/SinceRegion}}");
			entity.Property(ci => ci.MessageDownTemplate)
				.HasDefaultValue("Повідомлення за адресою <b>{{Address}}</b>:\n\n<b>Електропостачання відсутнє!</b>\n{{#SinceRegion}}\nЧас початку відключення: {{SinceDate}}\nСвітло було протягом {{DurationHours}} год. {{DurationMinutes}} хв.\n{{/SinceRegion}}");
		});
	}
}