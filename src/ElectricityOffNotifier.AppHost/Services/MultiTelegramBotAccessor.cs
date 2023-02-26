using System.Collections.Concurrent;
using System.Text;
using Telegram.Bot;

namespace ElectricityOffNotifier.AppHost.Services;

internal sealed class MultiTelegramBotAccessor : ITelegramBotAccessor
{
    private readonly HttpClient _httpClient;
    private readonly string _defaultToken;
    private readonly ConcurrentDictionary<string, ITelegramBotClient> _clients = new();

    public MultiTelegramBotAccessor(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _defaultToken = configuration["Bot:Token"]
                        ?? throw new ArgumentException("Bot:Token variable is not set in the env vars.");
        _httpClient = httpClientFactory.CreateClient("BotHttpClient");
    }

    public async ValueTask<ITelegramBotClient> GetBotClientAsync(byte[]? tokenBytes, CancellationToken cancellationToken)
    {
        if (tokenBytes == null)
            return _clients.GetOrAdd(_defaultToken, BotClientFactory);

        string userDefinedToken = Encoding.UTF8.GetString(tokenBytes);
        return _clients.GetOrAdd(userDefinedToken, BotClientFactory);
    }

    private ITelegramBotClient BotClientFactory(string token) => new TelegramBotClient(token, _httpClient);
}