using System.Globalization;
using System.Security.Claims;
using ElectricityOffNotifier.AppHost.Auth;
using ElectricityOffNotifier.AppHost.Helpers;
using ElectricityOffNotifier.AppHost.Models;
using ElectricityOffNotifier.AppHost.Services;
using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using TimeZoneConverter;

namespace ElectricityOffNotifier.AppHost.Controllers;

[ApiController]
[Route("v2/[controller]")]
[Authorize]
public sealed class SubscriberController : ControllerBase
{
    private readonly ElectricityDbContext _context;
    private readonly ITelegramBotAccessor _botAccessor;
    private readonly ILogger<SubscriberController> _logger;
    private readonly IValidator<SubscriberRegisterModel> _subscriberRegisterModelValidator;

    public SubscriberController(ElectricityDbContext context, ITelegramBotAccessor botAccessor,
        ILogger<SubscriberController> logger, IValidator<SubscriberRegisterModel> subscriberRegisterModelValidator)
    {
        _context = context;
        _botAccessor = botAccessor;
        _logger = logger;
        _subscriberRegisterModelValidator = subscriberRegisterModelValidator;
    }

    [HttpPost]
    public async Task<ActionResult<SubscriberModel>> Register([FromBody] SubscriberRegisterModel model,
        CancellationToken cancellationToken)
    {
        ValidationResult validationResult = _subscriberRegisterModelValidator.Validate(model);

        if (!validationResult.IsValid)
            return this.BadRequestExt(validationResult);
        
        int locationId = int.Parse(User.FindFirstValue(CustomClaimTypes.LocationId)!);
        int producerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        model.TimeZone ??= "Europe/Kiev";
        model.Culture ??= "uk-UA";
        
        if (!TZConvert.TryGetTimeZoneInfo(model.TimeZone, out _))
        {
            ModelState.AddModelError(nameof(model.TimeZone),
                "Invalid time zone name. Consider using any valid time zone in IANA or Windows format.");
            return BadRequest(ModelState);
        }

        try
        {
            _ = CultureInfo.GetCultureInfo(model.Culture);
        }
        catch
        {
            ModelState.AddModelError(nameof(model.Culture),
                "Culture name is not valid. Consider using 'uk-UA' or any similar culture.");
            return BadRequest(ModelState);
        }

        if (await _context.Subscribers.AnyAsync(s => s.LocationId == locationId && s.TelegramId == model.TelegramId,
                cancellationToken))
        {
            ModelState.AddModelError(nameof(model.TelegramId),
                "Subscriber with specified telegram id is already registered.");
            return BadRequest(ModelState);
        }

        var subscriber = new Subscriber
        {
            LocationId = locationId,
            TelegramId = model.TelegramId,
            Culture = model.Culture,
            TimeZone = model.TimeZone,
            TelegramThreadId = model.TelegramThreadId
        };
        
        ChatInfo? chatInfo = await _context.ChatInfo
            .AsNoTracking()
            .FirstOrDefaultAsync(ci => ci.TelegramId == model.TelegramId, cancellationToken);
        
        if (chatInfo == null)
        {
            Chat targetChat;
            try
            {
                ITelegramBotClient botClient = await _botAccessor.GetBotClientAsync(null, cancellationToken);
                targetChat = await botClient.GetChatAsync(model.TelegramId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Unable to get a Telegram chat {TelegramId}", model.TelegramId);
            
                ModelState.AddModelError(nameof(model.TelegramId),
                    "Unable to find the specified chat. Maybe bot was not added to it.");
                return BadRequest(ModelState);
            }
            
            subscriber.ChatInfo = new ChatInfo
            {
                TelegramId = model.TelegramId,
                Name = $"{targetChat.FirstName} {targetChat.LastName}".TrimEnd()
            };
        }
        
        _context.Subscribers.Add(subscriber);

        await _context.SaveChangesAsync(CancellationToken.None);

        return new SubscriberModel
        {
            SubscriberId = subscriber.Id
        };
    }

    [HttpDelete("{subscriberId:int:min(1):required}")]
    public async Task<ActionResult> Remove(int subscriberId, CancellationToken cancellationToken)
    {
        Subscriber? subscriber = await _context.Subscribers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == subscriberId, cancellationToken);

        if (subscriber == null)
            return NotFound();

        int locationId = int.Parse(User.FindFirstValue(CustomClaimTypes.LocationId)!);

        if (subscriber.LocationId != locationId)
        {
            return StatusCode(403, new
            {
                reason = "You are not the owner of specified subscriber, so you can't delete it."
            });
        }

        _context.Subscribers.Remove(subscriber);
        await _context.SaveChangesAsync(CancellationToken.None);

        return Ok();
    }
}