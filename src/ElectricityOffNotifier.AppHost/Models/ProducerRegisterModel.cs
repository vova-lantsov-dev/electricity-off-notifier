using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ElectricityOffNotifier.Data.Models.Enums;

namespace ElectricityOffNotifier.AppHost.Models;

public sealed record ProducerRegisterModel(
    [Range(1, int.MaxValue)]
    int AddressId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    [Required]
    ProducerMode Mode,
    [Url]
    string? WebhookUrl);