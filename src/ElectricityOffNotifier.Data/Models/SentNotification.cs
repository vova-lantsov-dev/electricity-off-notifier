using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectricityOffNotifier.Data.Models;

[Table("sent_notifications", Schema = "public")]
public sealed class SentNotification
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("id")]
	public int Id { get; set; }
	
	[Column("date_time")]
	public DateTime DateTime { get; set; }
	[Column("is_up_notification")]
	public bool IsUpNotification { get; set; }
	
	[Column("checker_id")]
	public int CheckerId { get; set; }
	[ForeignKey(nameof(CheckerId))]
	public Checker Checker { get; set; }
}