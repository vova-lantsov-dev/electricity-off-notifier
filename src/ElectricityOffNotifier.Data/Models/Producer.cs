using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectricityOffNotifier.Data.Models;

[Table("producers", Schema = "public")]
public sealed class Producer
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("id")]
	public int Id { get; set; }
	
	[Column("access_token_hash"), Required]
	public byte[] AccessTokenHash { get; set; }
	[Column("is_enabled")]
	public bool IsEnabled { get; set; }

	[Column("checker_id")]
	public int CheckerId { get; set; }
	[ForeignKey(nameof(CheckerId))]
	public Checker Checker { get; set; }
	
	public List<Subscriber> Subscribers { get; set; }
}