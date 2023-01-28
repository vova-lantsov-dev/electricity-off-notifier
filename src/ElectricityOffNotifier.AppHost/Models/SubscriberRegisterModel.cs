using System.Globalization;
using FluentValidation;
using TimeZoneConverter;

namespace ElectricityOffNotifier.AppHost.Models;

public sealed class SubscriberRegisterModel
{
    public long TelegramId { get; set; }
    public int? TelegramThreadId { get; set; }
    public string? TimeZone { get; set; }
    public string? Culture { get; set; }
}

public sealed class SubscriberRegisterModelValidator : AbstractValidator<SubscriberRegisterModel>
{
    public SubscriberRegisterModelValidator()
    {
        // Validate telegram chat identifier
        RuleFor(m => m.TelegramId).NotEqual(0L)
            .WithMessage("'{PropertyName}' must be a valid Telegram chat identifier");
        
        // Validate telegram chat thread's identifier if set
        RuleFor(m => m.TelegramThreadId)
            .GreaterThanOrEqualTo(1)
            .When(m => m.TelegramThreadId.HasValue)
            .WithMessage("'{PropertyName}' must be a valid Telegram chat thread's identifier");
        
        // Validate RFC 4646 culture name
        RuleFor(m => m.Culture)
            .Must(culture =>
            {
                try
                {
                    _ = new CultureInfo(culture!);
                }
                catch (CultureNotFoundException)
                {
                    return false;
                }

                return true;
            })
            .When(m => m.Culture != null)
            .WithMessage("'{PropertyName}' must be a valid RFC 4646 culture name");
        
        // Validate time zone identifier
        RuleFor(m => m.TimeZone)
            .Must(timeZone => TZConvert.TryGetTimeZoneInfo(timeZone!, out _))
            .When(m => m.TimeZone != null)
            .WithMessage("'{PropertyName}' must be a valid Windows or IANA time zone identifier");
    }
}