using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectricityOffNotifier.Data.Models;

[Table("addresses", Schema = "public")]
public sealed class Address
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("id")]
	public int Id { get; set; }
	
	[Column("street"), Required]
	public string Street { get; set; }
	[Column("building_no"), Required]
	public string BuildingNo { get; set; }
	
	[Column("city_id")]
	public int CityId { get; set; }
	[ForeignKey(nameof(CityId))]
	public City City { get; set; }
	
	public Checker Checker { get; set; }
}