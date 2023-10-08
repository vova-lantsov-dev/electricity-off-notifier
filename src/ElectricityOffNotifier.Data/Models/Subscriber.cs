using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectricityOffNotifier.Data.Models;

[Table("subscribers", Schema = "public")]
public sealed class Subscriber
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("id")]
	public int Id { get; set; }
	
	[Column("telegram_id")]
	public long TelegramId { get; set; }
	[Column("telegram_thread_id")]
	public int? TelegramThreadId { get; set; }
	[Column("time_zone"), Required]
	public string TimeZone { get; set; }
	[Column("culture"), Required]
	public string Culture { get; set; }
	
	[Column("location_id")]
	public int LocationId { get; set; }
	[ForeignKey(nameof(LocationId))]
	public Location Location { get; set; }

	[ForeignKey(nameof(TelegramId))]
	public ChatInfo ChatInfo { get; set; }
}