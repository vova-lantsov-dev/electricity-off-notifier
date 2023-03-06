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
    private static async Task HandleGetIdCommand(ITelegramBotClient botClient, long chatId, int? messageThreadId,
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
        catch
        {
            // silent
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
            .ThenInclude(s => s.Producer)
            .FirstAsync(ci => ci.TelegramId == chatId, cancellationToken);

        StringBuilder msgToSend = new();
        msgToSend.Append($"Chat name: {currentChat.Name}\n");
        msgToSend.Append($"Is admin: {isAdmin}\n\n");
        msgToSend.Append("Subscribers:\n");
        msgToSend.AppendJoin("\n", currentChat.Subscribers.Select((s, i) =>
            $"{i + 1}) Subscriber id: {s.Id}\nCulture: {s.Culture}\nTime zone: {s.TimeZone}\nProducer mode: {s.Producer.Mode:G}\nProducer id: {s.Producer.Id}\nProducer enabled: {s.Producer.IsEnabled}"));

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

    private async Task HandleSkipCommand(ITelegramBotClient botClient, string text, long chatId, int? messageThreadId,
        int messageId, ElectricityDbContext context, CancellationToken cancellationToken)
    {
        string[] separated = text[6..].Split(' ');
        if (separated.Length != 2)
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

        Producer? producer = await context.Producers
            .AsNoTracking()
            .Include(ci => ci.Subscribers.OrderByDescending(s => s.Id).Take(1))
            .FirstOrDefaultAsync(
                ci => ci.AccessTokenHash ==
                      separated[0].ToHmacSha256ByteArray(_configuration["Auth:SecretKey"]!), cancellationToken);
        if (producer == null)
        {
            try
            {
                await botClient.SendTextMessageAsync(chatId, "Неправильний API ключ.",
                    messageThreadId, replyToMessageId: messageId, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error occurred while sending Telegram message");
            }

            return;
        }

        string cultureName = producer.Subscribers.FirstOrDefault() is { } subscriber
            ? subscriber.Culture
            : "uk-UA";
        IFormatProvider culture = TemplateService.GetCulture(cultureName);

        if (!TimeSpan.TryParse(separated[1], culture, out TimeSpan timeSpan))
        {
            try
            {
                await botClient.SendTextMessageAsync(chatId, "Не вдається прочитати тривалість з повідомлення",
                    messageThreadId, replyToMessageId: messageId, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error occurred while sending Telegram message");
            }

            return;
        }

        var updatedProducer = new Producer
        {
            Id = producer.Id,
            SkippedUntil = DateTime.UtcNow + timeSpan
        };
        context.Attach(updatedProducer).Property(p => p.SkippedUntil).IsModified = true;

        await context.SaveChangesAsync(CancellationToken.None);
    }

    private async Task HandleTokenCommand(ITelegramBotClient botClient, string text, long chatId, int? messageThreadId,
        int messageId, ChatInfo? currentChat, ElectricityDbContext context, bool isAdmin, long userId,
        CancellationToken cancellationToken)
    {
        string[] separated = text[7..].Split(' ');
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

        if (currentChat == null)
        {
            try
            {
                await botClient.SendTextMessageAsync(chatId, "Вказаний чат ще не було зареєстровано.",
                    messageThreadId, replyToMessageId: messageId, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error occurred while sending Telegram message");
            }

            return;
        }

        context.ChatInfo.Attach(currentChat);

        static async Task UpdateTokenAsync(ElectricityDbContext context, ChatInfo chatInfo, string token,
            CancellationToken cancellationToken)
        {
            chatInfo.BotTokenOverride = Encoding.UTF8.GetBytes(token);
            await context.SaveChangesAsync(cancellationToken);
        }

        if (chatId == targetChatId && isAdmin)
        {
            await UpdateTokenAsync(context, currentChat, separated[1], cancellationToken);
        }
        else
        {
            ChatMember chatMember = await botClient.GetChatMemberAsync(targetChatId, userId, cancellationToken);
            if (chatMember is not { Status: ChatMemberStatus.Administrator or ChatMemberStatus.Creator })
                return;

            await UpdateTokenAsync(context, currentChat, separated[1], cancellationToken);
        }

        await _botManager.StartBotIfNeededAsync(currentChat.BotTokenOverride!);
    }
}