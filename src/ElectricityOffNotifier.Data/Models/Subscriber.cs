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
	
	[Column("producer_id")]
	public int ProducerId { get; set; }
	[ForeignKey(nameof(ProducerId))]
	public Producer Producer { get; set; }
	
	[Column("checker_id")]
	public int CheckerId { get; set; }
	[ForeignKey(nameof(CheckerId))]
	public Checker Checker { get; set; }
}