using System.Text;
using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ElectricityOffNotifier.AppHost.Services;

internal sealed class BotUpdateHandler : IUpdateHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITemplateService _templateService;

    public BotUpdateHandler(IServiceProvider serviceProvider, ITemplateService templateService)
    {
        _serviceProvider = serviceProvider;
        _templateService = templateService;
    }
    
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        switch (update)
        {
            case
            {
                Message:
                {
                    Text: "!getid",
                    MessageId: var messageId,
                    Chat.Id: var chatId,
                    MessageThreadId: var messageThreadId
                }
            }:
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

            case
            {
                Message:
                {
                    Text: var text and ("!down_template" or "!up_template"),
                    MessageId: var messageId,
                    ReplyToMessage: null,
                    Chat.Id: var chatId,
                    MessageThreadId: var messageThreadId
                }
            }:
            {
                await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<ElectricityDbContext>();

                ChatInfo? chatInfo = await context.ChatInfo
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ci => ci.TelegramId == chatId, cancellationToken);
                if (chatInfo == null)
                {
                    try
                    {
                        await botClient.SendTextMessageAsync(chatId, "Цей чат ще не зареєстровано!",
                            messageThreadId, replyToMessageId: messageId, cancellationToken: cancellationToken);
                    }
                    catch
                    {
                        // silent
                    }

                    return;
                }
                
                // escaping html symbols to prevent HTML parse mode errors
                string escapedTemplate = (text switch
                    {
                        "!up_template" => chatInfo.MessageUpTemplate,
                        _ => chatInfo.MessageDownTemplate
                    })
                    .Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;");

                StringBuilder msgToSend = new();
                msgToSend.AppendFormat("Поточний шаблон повідомлення:\n\n<pre>{0}</pre>\n\n", escapedTemplate);
                msgToSend.AppendFormat(
                    "Щоб задати новий шаблон - надішліть його у цей чат та зробіть " +
                    "<b>Reply</b> (Відповісти) з текстом <code>{0}</code> на надісланому повідомленні.", text);

                try
                {
                    await botClient.SendTextMessageAsync(chatId, msgToSend.ToString(), messageThreadId,
                        ParseMode.Html, replyToMessageId: messageId, cancellationToken: cancellationToken);
                }
                catch
                {
                    // silent
                }

                break;
            }

            case
            {
                Message:
                {
                    Text: var text and ("!down_template" or "!up_template"),
                    MessageId: var messageId,
                    ReplyToMessage.Text: { } replyMessageText,
                    Chat: { Id: var chatId, Type: var chatType },
                    From.Id: var fromId,
                    MessageThreadId: var messageThreadId
                }
            }:
            {
                if (chatType is ChatType.Group or ChatType.Supergroup)
                {
                    ChatMember chatMember = await botClient.GetChatMemberAsync(chatId, fromId, cancellationToken);
                    if (chatMember is not { Status: ChatMemberStatus.Administrator or ChatMemberStatus.Creator })
                        return;
                }
                
                if (!_templateService.ValidateMessageTemplate(replyMessageText))
                {
                    try
                    {
                        await botClient.SendTextMessageAsync(chatId,
                            "Надісланий шаблон є некоректним, виправте помилку та повторіть спробу.",
                            messageThreadId, replyToMessageId: messageId, cancellationToken: cancellationToken);
                    }
                    catch
                    {
                        // silent
                    }
                    
                    return;
                }
                
                await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<ElectricityDbContext>();
                
                ChatInfo? chatInfo = await context.ChatInfo
                    .FirstOrDefaultAsync(ci => ci.TelegramId == chatId, cancellationToken);
                if (chatInfo == null)
                {
                    try
                    {
                        await botClient.SendTextMessageAsync(chatId, "Цей чат ще не зареєстровано!",
                            messageThreadId, replyToMessageId: messageId, cancellationToken: cancellationToken);
                    }
                    catch
                    {
                        // silent
                    }

                    return;
                }

                switch (text)
                {
                    case "!up_template":
                        chatInfo.MessageUpTemplate = replyMessageText;
                        break;
                    
                    case "!down_template":
                        chatInfo.MessageDownTemplate = replyMessageText;
                        break;
                }

                await context.SaveChangesAsync(CancellationToken.None);

                try
                {
                    await botClient.SendTextMessageAsync(chatId, "Новий шаблон було зареєстровано!", messageThreadId,
                        replyToMessageId: messageId, cancellationToken: cancellationToken);
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