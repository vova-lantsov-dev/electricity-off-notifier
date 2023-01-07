using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectricityOffNotifier.Data.Models;

[Table("checker_entries", Schema = "public")]
public sealed class CheckerEntry
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("id")]
	public long Id { get; set; }
	
	[Column("date_time", TypeName = "timestamp")]
	public DateTime DateTime { get; set; }
	
	[Column("checker_id")]
	public int CheckerId { get; set; }
	[ForeignKey(nameof(CheckerId))]
	public Checker Checker { get; set; }
}