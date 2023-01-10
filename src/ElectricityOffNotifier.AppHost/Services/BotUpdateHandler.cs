using System.Text;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace ElectricityOffNotifier.AppHost.Services;

internal sealed class BotUpdateHandler : IUpdateHandler
{
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        switch (update)
        {
            case { Message: { Text: "!getid", MessageId: var messageId, Chat.Id: var chatId, MessageThreadId: var messageThreadId } }:
            {
                var messageBuilder = new StringBuilder();
                messageBuilder.Append($"Current chat id: {chatId}");
                if (messageThreadId != null)
                {
                    messageBuilder.Append($"\nCurrent thread id: {messageThreadId}");
                }

                try
                {
                    await botClient.SendTextMessageAsync(
                        chatId,
                        messageBuilder.ToString(),
                        messageThreadId,
                        replyToMessageId: messageId,
                        cancellationToken: cancellationToken);
                }
                catch
                {
                    // silent
                }

                break;
            }
        }
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}