using ElectricityOffNotifier.Data.Models;
using ElectricityOffNotifier.Data.Models.Enums;
using ElectricityOffNotifier.Data.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.DataEncryption;
using Microsoft.EntityFrameworkCore.DataEncryption.Providers;
using Microsoft.Extensions.Options;

namespace ElectricityOffNotifier.Data;

public sealed class ElectricityDbContext : DbContext
{
	private readonly DatabaseEncryptionOptions _encryptionOptions;

	public ElectricityDbContext(
		DbContextOptions<ElectricityDbContext> options,
		IOptions<DatabaseEncryptionOptions> encryptionOptions)
		: base(options)
	{
		_encryptionOptions = encryptionOptions.Value;
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

		byte[] encryptionKey = Convert.FromBase64String(_encryptionOptions.EncryptionKey);
		byte[] encryptionIV = Convert.FromBase64String(_encryptionOptions.EncryptionIV);
		modelBuilder.UseEncryption(new AesProvider(encryptionKey, encryptionIV));

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
		
		modelBuilder
			.Entity<Producer>()
			.Property(ce => ce.SkippedUntil)
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

		modelBuilder.Entity<Producer>()
			.Property(p => p.SkippedUntil)
			.HasDefaultValue(DateTime.UnixEpoch);
	}
}