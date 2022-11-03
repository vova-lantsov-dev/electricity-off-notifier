using System.Globalization;
using System.Text;
using ElectricityOffNotifier.Data.Models;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace ElectricityOffNotifier.AppHost.Services;

public sealed class TelegramNotifier : ITelegramNotifier
{
	private readonly ITelegramBotClient _botClient;

	private static readonly IFormatProvider UkrainianFormatProvider = new CultureInfo("uk-UA");

	public TelegramNotifier(ITelegramBotClient botClient)
	{
		_botClient = botClient;
	}
	
	public async Task NotifyElectricityIsDownAsync(CheckerEntry lastCheckerEntry, Address address, Subscriber subscriber,
		CancellationToken cancellationToken)
	{
		var builder = new StringBuilder();
		AppendAddressBlockTo(builder, address);
		builder.AppendFormat("Електроенергія відсутня починаючи з <b>{0}</b>",
			lastCheckerEntry.DateTime.ToString("g", UkrainianFormatProvider));

		var messageToSend = builder.ToString();
		await SendMessageAsync(subscriber.TelegramId, messageToSend, cancellationToken);
	}

	public async Task NotifyElectricityIsUpAsync(CheckerEntry downSince, Address address, Subscriber subscriber,
		CancellationToken cancellationToken)
	{
		var builder = new StringBuilder();
		AppendAddressBlockTo(builder, address);
		builder.AppendFormat("<b>Електроенергія відновлена!</b>\nВона була відсутня протягом {0}",
			(DateTime.UtcNow - downSince.DateTime).ToString("g", UkrainianFormatProvider));

		var messageToSend = builder.ToString();
		await SendMessageAsync(subscriber.TelegramId, messageToSend, cancellationToken);
	}

	private static void AppendAddressBlockTo(StringBuilder builder, Address address)
	{
		builder.AppendFormat("Повідомлення за адресою <b>{0}, {1} {2}</b>:\n\n",
			$"{address.City.Name}, {address.City.Region}".TrimEnd(' ', ','),
			address.Street,
			address.BuildingNo);
	}

	private async Task SendMessageAsync(long userId, string message, CancellationToken cancellationToken)
	{
		try
		{
			await _botClient.SendTextMessageAsync(
				userId,
				message,
				ParseMode.Html,
				cancellationToken: cancellationToken);
		}
		catch
		{
			// silent
		}
	}
}