using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ElectricityOffNotifier.Data.Models;

[Table("cities", Schema = "public")]
public sealed class City
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("id")]
	public int Id { get; set; }
	
	[Column("name"), Required]
	public string Name { get; set; }
	[Column("region"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Region { get; set; }
	
	[JsonIgnore]
	public List<Address> Addresses { get; set; }
}