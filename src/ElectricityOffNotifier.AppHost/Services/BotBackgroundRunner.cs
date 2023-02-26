using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace ElectricityOffNotifier.AppHost.Services;

public sealed class BotBackgroundRunner : BackgroundService
{
    private readonly ITelegramBotAccessor _botAccessor;
    private readonly IUpdateHandler _updateHandler;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public BotBackgroundRunner(ITelegramBotAccessor botAccessor, IUpdateHandler updateHandler,
        IServiceScopeFactory serviceScopeFactory)
    {
        _botAccessor = botAccessor;
        _updateHandler = updateHandler;
        _serviceScopeFactory = serviceScopeFactory;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        List<ChatInfo> chats;

        await using (AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ElectricityDbContext>();
            chats = await context.ChatInfo.AsNoTracking().ToListAsync(stoppingToken);
        }

        var receivingTasks = new List<Task>(chats.Count);
        foreach (ChatInfo chat in chats)
        {
            ITelegramBotClient botClient = await _botAccessor.GetBotClientAsync(chat, stoppingToken);
            receivingTasks.Add(
                botClient.ReceiveAsync(_updateHandler, cancellationToken: stoppingToken));
        }

        await Task.WhenAll(receivingTasks);
    }
}