using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using ElectricityOffNotifier.Data.Models;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TimeZoneConverter;

namespace ElectricityOffNotifier.AppHost.Services;

public sealed class TelegramNotifier : ITelegramNotifier
{
	private readonly ITelegramBotClient _botClient;
	private readonly ILogger<TelegramNotifier> _logger;

	private static readonly ConcurrentDictionary<string, Lazy<TimeZoneInfo>> TimeZones = new();
	private static readonly ConcurrentDictionary<string, Lazy<IFormatProvider>> Cultures = new();

	public TelegramNotifier(ITelegramBotClient botClient, ILogger<TelegramNotifier> logger)
	{
		_botClient = botClient;
		_logger = logger;
	}
	
	public async Task NotifyElectricityIsDownAsync(SentNotification? upSince, Address address, Subscriber subscriber,
		CancellationToken cancellationToken)
	{
		DateTime localTime = GetLocalTime(DateTime.UtcNow, subscriber.TimeZone);
		IFormatProvider localCulture = GetCulture(subscriber.Culture);
		
		var builder = new StringBuilder();
		AppendAddressBlockTo(builder, address);
		builder.AppendFormat("<b>Електропостачання відсутнє!</b> Час початку відключення: <b>{0}</b>",
			localTime.ToString("g", localCulture));

		var messageToSend = builder.ToString();
		await SendMessageAsync(subscriber.TelegramId, subscriber.TelegramThreadId, messageToSend, cancellationToken);
	}

	public async Task NotifyElectricityIsUpAsync(SentNotification? downSince, Address address, Subscriber subscriber,
		CancellationToken cancellationToken)
	{
		var builder = new StringBuilder();
		AppendAddressBlockTo(builder, address);
		builder.Append("<b>Електропостачання відновлено!</b>");

		if (downSince != null)
		{
			DateTime localTime = GetLocalTime(downSince.DateTime, subscriber.TimeZone);
            IFormatProvider localCulture = GetCulture(subscriber.Culture);

			builder.AppendFormat("\nБуло відсутнє з {0}\n",
				localTime.ToString("g", localCulture));
			
			TimeSpan downDuration = DateTime.UtcNow - downSince.DateTime;
			builder.AppendFormat("Загальна тривалість відключення: {0} год. {1:D2} хв.",
				(int) downDuration.TotalHours, downDuration.Minutes);
		}

		var messageToSend = builder.ToString();
		await SendMessageAsync(subscriber.TelegramId, subscriber.TelegramThreadId, messageToSend, cancellationToken);
	}

	private static void AppendAddressBlockTo(StringBuilder builder, Address address)
	{
		builder.AppendFormat("Повідомлення за адресою <b>{0}, {1} {2}</b>:\n\n",
			$"{address.City.Name}, {address.City.Region}".TrimEnd(' ', ','),
			address.Street,
			address.BuildingNo);
	}

	private async Task SendMessageAsync(long userId, int? messageThreadId, string message, CancellationToken cancellationToken)
	{
		try
		{
			await _botClient.SendTextMessageAsync(
				userId,
				message,
				messageThreadId,
				ParseMode.Html,
				cancellationToken: cancellationToken);
		}
		catch (Exception ex)
		{
			_logger.LogDebug(ex,
				"Error occurred while sending a message to Telegram. Chat id: {ChatId}, msg thread id: {MsgThread}",
				userId,
				messageThreadId);
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