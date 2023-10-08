using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectricityOffNotifier.Data.Models.Enums;

namespace ElectricityOffNotifier.Data.Models;

[Table("locations", Schema = "public")]
public sealed class Location
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("id")]
	public int Id { get; set; }
	
	[Column("full_address"), Required]
	public string FullAddress { get; set; }
	
	[Column("name")]
	public string? Name { get; set; }
	
	[Column("current_status")]
	public LocationStatus CurrentStatus { get; }
	
	[Column("last_seen_at")]
	public DateTime LastSeenAt { get; set; }
	
	[Column("last_notified_at")]
	public DateTime LastNotifiedAt { get; set; }
	
	[Column("access_token_hash"), Required]
	public byte[] AccessTokenHash { get; set; }
	
	[Column("is_disabled")]
	public bool IsDisabled { get; set; }
	
	public List<Subscriber> Subscribers { get; set; }
	public List<Producer> Producers { get; set; }

	[Column("city_id")]
	public int CityId { get; set; }
	[ForeignKey(nameof(CityId))]
	public City City { get; set; }
}