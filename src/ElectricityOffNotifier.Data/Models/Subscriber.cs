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
	
	[Column("checker_id")]
	public int CheckerId { get; set; }
	[ForeignKey(nameof(CheckerId))]
	public Checker Checker { get; set; }
}