using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectricityOffNotifier.Data.Models;

[Table("chat_info", Schema = "public")]
public sealed class ChatInfo
{
    [Key, Column("telegram_id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long TelegramId { get; set; }
    
    [Required, Column("name")]
    public string Name { get; set; }
    [Required, Column("message_up_template")]
    public string MessageUpTemplate { get; set; }
    [Required, Column("message_down_template")]
    public string MessageDownTemplate { get; set; }
    
    public List<Subscriber> Subscribers { get; set; }
}