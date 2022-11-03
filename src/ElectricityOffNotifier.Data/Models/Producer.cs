using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectricityOffNotifier.Data.Models;

[Table("producers", Schema = "public")]
public sealed class Producer
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("id")]
	public int Id { get; set; }
	
	[Column("access_token"), Required]
	public string AccessToken { get; set; }

	[Column("checker_id")]
	public int CheckerId { get; set; }
	[ForeignKey(nameof(CheckerId))]
	public Checker Checker { get; set; }
}