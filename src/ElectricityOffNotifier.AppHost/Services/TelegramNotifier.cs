using ElectricityOffNotifier.Data.Models;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace ElectricityOffNotifier.AppHost.Services;

public sealed class TelegramNotifier : ITelegramNotifier
{
	private readonly ILogger<TelegramNotifier> _logger;
	private readonly ITemplateService _templateService;

	public TelegramNotifier(ILogger<TelegramNotifier> logger, ITemplateService templateService)
	{
		_logger = logger;
		_templateService = templateService;
	}

	public async Task NotifyElectricityIsDownAsync(ITelegramBotClient botClient,
		Location location, Subscriber subscriber, CancellationToken cancellationToken)
	{
		string messageToSend = _templateService.ReplaceMessageTemplate(
			subscriber.ChatInfo.MessageDownTemplate, location, subscriber);
		await SendMessageAsync(botClient, subscriber.TelegramId, subscriber.TelegramThreadId, messageToSend,
			cancellationToken);
	}

	public async Task NotifyElectricityIsUpAsync(ITelegramBotClient botClient,
		Location location, Subscriber subscriber, CancellationToken cancellationToken)
	{
		string messageToSend = _templateService.ReplaceMessageTemplate(
			subscriber.ChatInfo.MessageUpTemplate, location, subscriber);
		await SendMessageAsync(botClient, subscriber.TelegramId, subscriber.TelegramThreadId, messageToSend,
			cancellationToken);
	}

	private async Task SendMessageAsync(ITelegramBotClient botClient, long userId, int? messageThreadId, string message, CancellationToken cancellationToken)
	{
		try
		{
			await botClient.SendTextMessageAsync(
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
}