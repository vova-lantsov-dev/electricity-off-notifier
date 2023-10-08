using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectricityOffNotifier.Data.Models.Enums;

namespace ElectricityOffNotifier.Data.Models;

[Table("producers", Schema = "public")]
public sealed class Producer
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("id")]
	public int Id { get; set; }
	
	[Column("name")]
	public string? Name { get; set; }
	
	[Column("access_token_hash"), Required]
	public byte[] AccessTokenHash { get; set; }
	
	[Column("mode"), Required]
	public ProducerMode Mode { get; set; }
	
	[Column("webhook_url")]
	public string? WebhookUrl { get; set; }

	[Column("location_id")]
	public int LocationId { get; set; }
	[ForeignKey(nameof(LocationId))]
	public Location Location { get; set; }
}