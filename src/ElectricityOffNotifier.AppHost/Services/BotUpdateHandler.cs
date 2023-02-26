using System.Globalization;
using System.Text;
using ElectricityOffNotifier.AppHost.Auth;
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
    private readonly ILogger<BotUpdateHandler> _logger;
    private readonly IConfiguration _configuration;

    public BotUpdateHandler(IServiceProvider serviceProvider, ITemplateService templateService,
        ILogger<BotUpdateHandler> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _templateService = templateService;
        _logger = logger;
        _configuration = configuration;
    }
    
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var isAdmin = true;
        {
            // Verify user rights in group to call the commands
            if (update is
                {
                    Message: { Chat.Type: ChatType.Group or ChatType.Supergroup, Chat.Id: var chatId, From.Id: var fromId }
                })
            {
                ChatMember chatMember = await botClient.GetChatMemberAsync(chatId, fromId, cancellationToken);
                isAdmin = chatMember is { Status: ChatMemberStatus.Administrator or ChatMemberStatus.Creator };
            }
        }

        // Handle the commands
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
            }
            when isAdmin:
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
            }
            when isAdmin:
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
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error occurred while sending Telegram message");
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
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error occurred while sending Telegram message");
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
                    Chat.Id: var chatId,
                    MessageThreadId: var messageThreadId
                }
            }
            when isAdmin:
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
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error occurred while sending Telegram message");
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
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error occurred while sending Telegram message");
                }

                break;
            }

            case
            {
                Message:
                {
                    Text: "!info",
                    MessageId: var messageId,
                    Chat.Id: var chatId,
                    MessageThreadId: var messageThreadId
                }
            }:
            {
                await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<ElectricityDbContext>();
                
                ChatInfo? chatInfo = await context.ChatInfo
                    .Include(ci => ci.Subscribers)
                    .ThenInclude(s => s.Producer)
                    .FirstOrDefaultAsync(ci => ci.TelegramId == chatId, cancellationToken);
                if (chatInfo == null)
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

                StringBuilder msgToSend = new();
                msgToSend.Append($"Chat name: {chatInfo.Name}\n");
                msgToSend.Append($"Is admin: {isAdmin}\n\n");
                msgToSend.Append("Subscribers:\n");
                msgToSend.AppendJoin("\n", chatInfo.Subscribers.Select((s, i) =>
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

                break;
            }

            case
            {
                Message:
                {
                    Text: { Length: > 7 } text,
                    MessageId: var messageId,
                    Chat.Id: var chatId,
                    MessageThreadId: var messageThreadId
                }
            }
            when text.StartsWith("!skip ") && isAdmin:
            {
                string[] separated = text[6..].Split(' ');
                if (separated.Length != 2)
                {
                    try
                    {
                        await botClient.SendTextMessageAsync(chatId,"Неправильний формат повідомлення.",
                            messageThreadId, replyToMessageId: messageId, cancellationToken: cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error occurred while sending Telegram message");
                    }
                    
                    return;
                }
                
                await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<ElectricityDbContext>();

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
                
                break;
            }

            case
            {
                Message:
                {
                    Text: { Length: > 8 } text,
                    MessageId: var messageId,
                    Chat.Id: var chatId,
                    MessageThreadId: var messageThreadId,
                    From.Id: var userId
                }
            }
            when text.StartsWith("!token ") && isAdmin:
            {
                string[] separated = text[7..].Split(' ');
                if (separated.Length != 2 || !long.TryParse(separated[0], out long targetChatId))
                {
                    try
                    {
                        await botClient.SendTextMessageAsync(chatId,"Неправильний формат повідомлення.",
                            messageThreadId, replyToMessageId: messageId, cancellationToken: cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error occurred while sending Telegram message");
                    }
                    
                    return;
                }
                
                await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<ElectricityDbContext>();

                ChatInfo? chatInfo = await context.ChatInfo
                    .FirstOrDefaultAsync(ci => ci.TelegramId == targetChatId, cancellationToken);
                if (chatInfo == null)
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

                static async Task UpdateTokenAsync(ElectricityDbContext context, ChatInfo chatInfo, string token,
                    CancellationToken cancellationToken)
                {
                    chatInfo.BotTokenOverride = Encoding.UTF8.GetBytes(token);
                    await context.SaveChangesAsync(cancellationToken);
                }

                if (chatId == targetChatId && isAdmin)
                {
                    await UpdateTokenAsync(context, chatInfo, separated[1], cancellationToken);
                }
                else
                {
                    ChatMember chatMember = await botClient.GetChatMemberAsync(targetChatId, userId, cancellationToken);
                    if (chatMember is not { Status: ChatMemberStatus.Administrator or ChatMemberStatus.Creator })
                        break;

                    await UpdateTokenAsync(context, chatInfo, separated[1], cancellationToken);
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