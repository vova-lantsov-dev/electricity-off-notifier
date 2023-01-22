using ElectricityOffNotifier.Data.Models;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace ElectricityOffNotifier.AppHost.Services;

public sealed class TelegramNotifier : ITelegramNotifier
{
	private readonly ITelegramBotClient _botClient;
	private readonly ILogger<TelegramNotifier> _logger;
	private readonly ITemplateService _templateService;

	public TelegramNotifier(ITelegramBotClient botClient, ILogger<TelegramNotifier> logger,
		ITemplateService templateService)
	{
		_botClient = botClient;
		_logger = logger;
		_templateService = templateService;
	}

	public async Task NotifyElectricityIsDownAsync(SentNotification? upSince, Address address, Subscriber subscriber,
		CancellationToken cancellationToken)
	{
		string messageToSend = _templateService.ReplaceMessageTemplate(
			subscriber.ChatInfo.MessageDownTemplate, address, subscriber, upSince);
		await SendMessageAsync(subscriber.TelegramId, subscriber.TelegramThreadId, messageToSend, cancellationToken);
	}

	public async Task NotifyElectricityIsUpAsync(SentNotification? downSince, Address address, Subscriber subscriber,
		CancellationToken cancellationToken)
	{
		string messageToSend = _templateService.ReplaceMessageTemplate(
			subscriber.ChatInfo.MessageUpTemplate, address, subscriber, downSince);
		await SendMessageAsync(subscriber.TelegramId, subscriber.TelegramThreadId, messageToSend, cancellationToken);
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
}