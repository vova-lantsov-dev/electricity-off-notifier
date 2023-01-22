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
    
    public string ReplaceMessageTemplate(string input, Address address, Subscriber subscriber, SentNotification? since)
    {
        IFormatProvider culture = GetCulture(subscriber.Culture);
        
        var renderData = new Dictionary<string, object>
        {
            ["Address"] = $"{address.City.Name}, {address.City.Region}".TrimEnd(' ', ',') +
                          $", {address.Street} {address.BuildingNo}",
            ["NowDate"] = GetLocalTime(DateTime.UtcNow, subscriber.TimeZone).ToString("g", culture)
        };

        if (since != null)
        {
            renderData["SinceRegion"] = true;
            renderData["SinceDate"] = GetLocalTime(since.DateTime, subscriber.TimeZone).ToString("g", culture);
            
            TimeSpan duration = DateTime.UtcNow - since.DateTime;
            renderData["DurationHours"] = (int)duration.TotalHours;
            renderData["DurationMinutes"] = duration.Minutes.ToString("D2");
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
        static Lazy<TimeZoneInfo> CreateTimeZoneFactory(string timeZone) =>
            new(() => TZConvert.GetTimeZoneInfo(timeZone));

        TimeZoneInfo tz = TimeZones.GetOrAdd(timeZone, CreateTimeZoneFactory).Value;

        return TimeZoneInfo.ConvertTime(utcTime, tz);
    }

    private static IFormatProvider GetCulture(string cultureName)
    {
        return Cultures
            .GetOrAdd(cultureName, c => new Lazy<IFormatProvider>(() => new CultureInfo(c)))
            .Value;
    }
}