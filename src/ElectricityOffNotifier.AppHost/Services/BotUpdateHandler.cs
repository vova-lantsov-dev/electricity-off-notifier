using System.Text;
using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ElectricityOffNotifier.AppHost.Services;

internal sealed partial class BotUpdateHandler : IUpdateHandler
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ITemplateService _templateService;
    private readonly ILogger<BotUpdateHandler> _logger;
    private readonly IConfiguration _configuration;
    private readonly ITelegramBotAccessor _botAccessor;
    private readonly IBotManager _botManager;

    public BotUpdateHandler(IServiceScopeFactory serviceScopeFactory, ITemplateService templateService,
        ILogger<BotUpdateHandler> logger, IConfiguration configuration, ITelegramBotAccessor botAccessor,
        IBotManager botManager)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _templateService = templateService;
        _logger = logger;
        _configuration = configuration;
        _botAccessor = botAccessor;
        _botManager = botManager;
    }
    
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Only message updates are supported at the moment
        if (update is not { Message.Chat.Id: var chatId })
            return;

        await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ElectricityDbContext>();

        ChatInfo? currentChat = null;
        
        _logger.LogDebug("Incoming request for bot {BotId} with text '{MessageText}'",
            botClient.BotId, update.Message.Text);
        
        string? botTokenById = _botAccessor.GetTokenByBotId(botClient.BotId.GetValueOrDefault());
        if (botTokenById != null)
        {
            _logger.LogDebug("Non-default token is registered for bot {BotId} in chat {ChatId}",
                botClient.BotId, chatId);
            
            // If we get here - it means that this bot is registered to be used only in specific chats
            byte[] tokenBytes = Encoding.UTF8.GetBytes(botTokenById);

            // Ensure that this chat is expected to be used with current bot client
            currentChat = await context.ChatInfo
                .AsNoTracking()
                .FirstOrDefaultAsync(ci => ci.BotTokenOverride == tokenBytes && ci.TelegramId == chatId,
                    cancellationToken);

            if (currentChat == null)
            {
                _logger.LogDebug("Bot {BotId} is not supposed to be used in chat {ChatId}, skipping...",
                    botClient.BotId, chatId);
                return;
            }
        }

        currentChat ??= await context.ChatInfo
            .AsNoTracking()
            .FirstOrDefaultAsync(ci => ci.TelegramId == chatId, cancellationToken);

        var isAdmin = true;
        {
            // Verify user rights in group to call the commands
            if (update is
                {
                    Message: { Chat.Type: ChatType.Group or ChatType.Supergroup, From.Id: var fromId }
                })
            {
                ChatMember chatMember = await botClient.GetChatMemberAsync(chatId, fromId, cancellationToken);
                isAdmin = chatMember is { Status: ChatMemberStatus.Administrator or ChatMemberStatus.Creator };
            }
        }
        
        _logger.LogDebug("Does user {UserId} have admin rights in chat {ChatId}: {IsAdmin}",
            update.Message.From?.Id, chatId, isAdmin);

        // Handle the commands
        switch (update)
        {
            case
            {
                Message:
                {
                    Text: "!getid",
                    MessageId: var messageId,
                    MessageThreadId: var messageThreadId
                }
            }
            when isAdmin:
            {
                await HandleGetIdCommand(botClient, chatId, messageThreadId, messageId, cancellationToken);
                break;
            }

            case
            {
                Message:
                {
                    Text: var text and ("!down_template" or "!up_template"),
                    MessageId: var messageId,
                    ReplyToMessage: null,
                    MessageThreadId: var messageThreadId
                }
            }
            when isAdmin:
            {
                await HandleTemplateCommand(botClient, currentChat, chatId, messageThreadId, messageId, text,
                    cancellationToken);
                break;
            }

            case
            {
                Message:
                {
                    Text: var text and ("!down_template" or "!up_template"),
                    MessageId: var messageId,
                    ReplyToMessage.Text: { } replyMessageText,
                    MessageThreadId: var messageThreadId
                }
            }
            when isAdmin:
            {
                await HandleTemplateCommandWithReply(botClient, replyMessageText, chatId, messageThreadId, messageId,
                    currentChat, context, text, cancellationToken);
                break;
            }

            case
            {
                Message:
                {
                    Text: "!info",
                    MessageId: var messageId,
                    MessageThreadId: var messageThreadId
                }
            }:
            {
                await HandleInfoCommand(botClient, currentChat, chatId, messageThreadId, messageId, context, isAdmin,
                    cancellationToken);
                break;
            }

            case
            {
                Message:
                {
                    Text: { Length: > 7 } text,
                    MessageId: var messageId,
                    MessageThreadId: var messageThreadId
                }
            }
            when text.StartsWith("!skip ") && isAdmin:
            {
                await HandleSkipCommand(botClient, text, chatId, messageThreadId, messageId, context,
                    cancellationToken);
                break;
            }

            case
            {
                Message:
                {
                    Text: { Length: > 8 } text,
                    MessageId: var messageId,
                    MessageThreadId: var messageThreadId,
                    From.Id: var userId
                }
            }
            when text.StartsWith("!token ") && isAdmin:
            {
                await HandleTokenCommand(botClient, text, chatId, messageThreadId, messageId, context, isAdmin, userId,
                    cancellationToken);
                break;
            }
        }
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Error occurred while running a polling for bot {BotId}", botClient.BotId);
        return Task.CompletedTask;
    }
}