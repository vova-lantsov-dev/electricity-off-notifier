using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectricityOffNotifier.Data.Models;

[Table("cities", Schema = "public")]
public sealed class City
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("id")]
	public int Id { get; set; }
	
	[Column("name"), Required]
	public string Name { get; set; }
	[Column("region")]
	public string? Region { get; set; }
	
	public List<Address> Addresses { get; set; }
}