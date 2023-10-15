using System.Collections.Concurrent;
using System.Globalization;
using ElectricityOffNotifier.Data.Models;
using Stubble.Core.Builders;
using Stubble.Core.Interfaces;
using TimeZoneConverter;

namespace ElectricityOffNotifier.AppHost.Services;

internal sealed class TemplateService : ITemplateService
{
    private static readonly ConcurrentDictionary<string, Lazy<TimeZoneInfo>> TimeZones = new();
    private static readonly ConcurrentDictionary<string, Lazy<IFormatProvider>> Cultures = new();

    internal static readonly IStubbleRenderer Stubble = new StubbleBuilder()
        .Configure(opts =>
        {
            opts.SetIgnoreCaseOnKeyLookup(true);
        })
        .Build();
    
    public string ReplaceMessageTemplate(string input, Location location, Subscriber subscriber)
    {
        IFormatProvider culture = GetCulture(subscriber.Culture);
        
        var renderData = new Dictionary<string, object>
        {
            ["NowDate"] = GetLocalTime(DateTime.UtcNow, subscriber.TimeZone).ToString("g", culture)
        };

        if (location.LastSeenAt != default)
        {
            renderData["SinceRegion"] = true;
            renderData["SinceDate"] = GetLocalTime(location.LastNotifiedAt, subscriber.TimeZone).ToString("g", culture);
            
            TimeSpan duration = DateTime.UtcNow - location.LastNotifiedAt;
            renderData["DurationHours"] = (int)duration.TotalHours;
            renderData["DurationMinutes"] = duration.Minutes.ToString("D2");
        }

        if (location.FullAddress != null)
        {
            renderData["Address"] = location.FullAddress;
        }

        return Stubble.Render(input, renderData);
    }

    public bool ValidateMessageTemplate(string input)
    {
        var renderData = new Dictionary<string, object>
        {
            ["Address"] = "ADDRESS",
            ["SinceDate"] = DateTime.Now.AddHours(-2).ToString("g"),
            ["DurationHours"] = 1,
            ["DurationMinutes"] = "03",
            ["NowDate"] = DateTime.Now.ToString("g"),
            ["SinceRegion"] = true
        };

        try
        {
            _ = Stubble.Render(input, renderData);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static DateTime GetLocalTime(DateTime utcTime, string timeZone)
    {
        TimeZoneInfo tz = TimeZones.GetOrAdd(timeZone, CreateTimeZoneFactory).Value;

        return TimeZoneInfo.ConvertTime(utcTime, tz);
    }

    internal static IFormatProvider GetCulture(string cultureName)
    {
        return Cultures
            .GetOrAdd(cultureName, c => new Lazy<IFormatProvider>(() => new CultureInfo(c)))
            .Value;
    }
    
    private static Lazy<TimeZoneInfo> CreateTimeZoneFactory(string timeZone) =>
        new(() => TZConvert.GetTimeZoneInfo(timeZone));
}