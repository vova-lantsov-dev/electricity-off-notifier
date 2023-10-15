using System.Text.Json.Serialization;
using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ElectricityOffNotifier.AppHost.Models;

public sealed record ProducerRegisterModel(
    int LocationId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    ProducerMode Mode,
    string? WebhookUrl,
    string? Name);

public sealed class ProducerRegisterModelValidator : AbstractValidator<ProducerRegisterModel>
{
    public ProducerRegisterModelValidator(ElectricityDbContext context)
    {
        // Validate location identifier
        RuleFor(m => m.LocationId)
            .Cascade(CascadeMode.Stop)
            .GreaterThanOrEqualTo(1)
            .MustAsync((locationId, cancellationToken) =>
                context.Locations.AnyAsync(c => c.Id == locationId, cancellationToken))
            .WithMessage("Location with specified id was not found");
        
        // Validate producer mode
        RuleFor(m => m.Mode).IsInEnum();
        
        // Validate webhook URL
        When(m => m.Mode == ProducerMode.Webhook,
            () =>
            {
                RuleFor(m => m.WebhookUrl)
                    .NotNull()
                    .WithMessage($"'{{PropertyName}}' must be set when '{nameof(ProducerMode.Webhook)}' mode is specified")
                    .Must(webhookUrl =>
                        Uri.TryCreate(webhookUrl, UriKind.Absolute, out Uri? uri) && uri.Scheme is "http" or "https")
                    .WithMessage("'{PropertyName}' must be a valid URL");
            })
            .Otherwise(() =>
            {
                RuleFor(m => m.WebhookUrl)
                    .Null()
                    .WithMessage($"'{{PropertyName}}' must be set ONLY when '{nameof(ProducerMode.Webhook)}' mode is specified");
            });
    }
}