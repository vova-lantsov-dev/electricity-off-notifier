using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectricityOffNotifier.Data.Models;

[Table("checkers", Schema = "public")]
public sealed class Checker
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("id")]
	public int Id { get; set; }
	
	[Column("is_enabled")]
	public bool IsEnabled { get; set; }
	
	public List<Subscriber> Subscribers { get; set; }
	public List<Producer> Producers { get; set; }
	public List<CheckerEntry> Entries { get; set; }
	public List<SentNotification> SentNotifications { get; set; }

	[Column("address_id")]
	public int AddressId { get; set; }
	[ForeignKey(nameof(AddressId))]
	public Address Address { get; set; }
}