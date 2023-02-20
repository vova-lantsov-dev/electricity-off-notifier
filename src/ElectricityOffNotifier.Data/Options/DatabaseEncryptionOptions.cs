using System.ComponentModel.DataAnnotations;

namespace ElectricityOffNotifier.Data.Options;

public sealed class DatabaseEncryptionOptions
{
    [Required]
    public string EncryptionKey { get; set; }
    [Required]
    public string EncryptionIV { get; set; }
}