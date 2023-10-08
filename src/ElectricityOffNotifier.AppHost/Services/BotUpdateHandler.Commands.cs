using System.Text;
using ElectricityOffNotifier.AppHost.Auth;
using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ElectricityOffNotifier.AppHost.Services;

internal sealed partial class BotUpdateHandler
{
    private async Task HandleGetIdCommand(ITelegramBotClient botClient, long chatId, int? messageThreadId,
        int messageId, CancellationToken cancellationToken)
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
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error occurred while sending Telegram message");
        }
    }

    private async Task HandleTemplateCommand(ITelegramBotClient botClient, ChatInfo? currentChat, long chatId,
        int? messageThreadId, int messageId, string text, CancellationToken cancellationToken)
    {
        if (currentChat == null)
        {
            try
            {
                await botClient.SendTextMessageAsync(chatId, "Цей чат ще не зареєстровано!",
                    messageThreadId, replyToMessageId: messageId, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error occurred while sending Telegram message");
            }

            return;
        }

        // escaping html symbols to prevent HTML parse mode errors
        string escapedTemplate = (text switch
            {
                "!up_template" => currentChat.MessageUpTemplate,
                _ => currentChat.MessageDownTemplate
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
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error occurred while sending Telegram message");
        }
    }

    private async Task HandleTemplateCommandWithReply(ITelegramBotClient botClient, string replyMessageText,
        long chatId,
        int? messageThreadId, int messageId, ChatInfo? currentChat, ElectricityDbContext context, string text,
        CancellationToken cancellationToken)
    {
        if (!_templateService.ValidateMessageTemplate(replyMessageText))
        {
            try
            {
                await botClient.SendTextMessageAsync(chatId,
                    "Надісланий шаблон є некоректним, виправте помилку та повторіть спробу.",
                    messageThreadId, replyToMessageId: messageId, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error occurred while sending Telegram message");
            }

            return;
        }

        if (currentChat == null)
        {
            try
            {
                await botClient.SendTextMessageAsync(chatId, "Цей чат ще не зареєстровано!",
                    messageThreadId, replyToMessageId: messageId, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error occurred while sending Telegram message");
            }

            return;
        }

        context.ChatInfo.Attach(currentChat);

        switch (text)
        {
            case "!up_template":
                currentChat.MessageUpTemplate = replyMessageText;
                break;

            case "!down_template":
                currentChat.MessageDownTemplate = replyMessageText;
                break;
        }

        await context.SaveChangesAsync(CancellationToken.None);

        try
        {
            await botClient.SendTextMessageAsync(chatId, "Новий шаблон було зареєстровано!", messageThreadId,
                replyToMessageId: messageId, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error occurred while sending Telegram message");
        }
    }

    private async Task HandleInfoCommand(ITelegramBotClient botClient, ChatInfo? currentChat, long chatId,
        int? messageThreadId, int messageId, ElectricityDbContext context, bool isAdmin,
        CancellationToken cancellationToken)
    {
        if (currentChat == null)
        {
            try
            {
                await botClient.SendTextMessageAsync(chatId, "Цей чат ще не зареєстровано!",
                    messageThreadId, replyToMessageId: messageId, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error occurred while sending Telegram message");
            }

            return;
        }

        currentChat = await context.ChatInfo
            .AsNoTracking()
            .Include(ci => ci.Subscribers)
            .FirstAsync(ci => ci.TelegramId == chatId, cancellationToken);

        StringBuilder msgToSend = new();
        msgToSend.Append($"Chat name: {currentChat.Name}\n");
        msgToSend.Append($"Is admin: {isAdmin}\n\n");
        msgToSend.Append("Subscribers:\n");
        msgToSend.AppendJoin("\n", currentChat.Subscribers.Select((s, i) =>
            $"{i + 1}) Subscriber id: {s.Id}\nCulture: {s.Culture}\nTime zone: {s.TimeZone}"));

        try
        {
            await botClient.SendTextMessageAsync(chatId, msgToSend.ToString(), messageThreadId,
                replyToMessageId: messageId, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error occurred while sending Telegram message");
        }
    }

    private async Task HandleTokenCommand(ITelegramBotClient botClient, string text, long chatId, int? messageThreadId,
        int messageId, ElectricityDbContext context, bool isAdmin, long userId,
        CancellationToken cancellationToken)
    {
        string[] separated = text[7..].Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (separated.Length != 2 || !long.TryParse(separated[0], out long targetChatId))
        {
            try
            {
                await botClient.SendTextMessageAsync(chatId, "Неправильний формат повідомлення.",
                    messageThreadId, replyToMessageId: messageId, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error occurred while sending Telegram message");
            }

            return;
        }

        string token = separated[1];
        byte[] botTokenBytes = Encoding.UTF8.GetBytes(token);
        ITelegramBotClient newBotClient =
            await _botAccessor.GetBotClientAsync(botTokenBytes, cancellationToken);
        
        ChatInfo? targetChatInfo =
            await context.ChatInfo.FirstOrDefaultAsync(ci => ci.TelegramId == targetChatId, cancellationToken);
        
        if (targetChatInfo == null)
        {
            Chat targetChat;
            try
            {
                targetChat = await newBotClient.GetChatAsync(targetChatId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Unable to get a Telegram chat {TelegramId}", chatId);

                try
                {
                    await botClient.SendTextMessageAsync(chatId,
                        "Не вдалося знайти вказаний чат. Можливо, бот не був доданий до чату.",
                        messageThreadId, replyToMessageId: messageId, cancellationToken: cancellationToken);
                }
                catch (Exception ex2)
                {
                    _logger.LogDebug(ex2, "Error occurred while sending Telegram message");
                }

                return;
            }
            
            targetChatInfo = new ChatInfo
            {
                TelegramId = targetChatId,
                Name = $"{targetChat.FirstName} {targetChat.LastName}".TrimEnd(),
                BotTokenOverride = botTokenBytes
            };
            
            context.ChatInfo.Add(targetChatInfo);
        }
        else
        {
            targetChatInfo.BotTokenOverride = botTokenBytes;
        }

        if (chatId == targetChatId)
        {
            if (!isAdmin)
                return;
            
            await context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            try
            {
                ChatMember chatMember = await newBotClient.GetChatMemberAsync(targetChatId, userId, cancellationToken);
                if (chatMember is not { Status: ChatMemberStatus.Administrator or ChatMemberStatus.Creator })
                    return;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error occurred while fetching the chat member");
                return;
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        await _botManager.StartBotIfNeededAsync(this, targetChatInfo.BotTokenOverride, cancellationToken);
    }
}