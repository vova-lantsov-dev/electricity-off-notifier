using System.Security.Claims;
using ElectricityOffNotifier.AppHost.Auth;
using ElectricityOffNotifier.AppHost.Models;
using ElectricityOffNotifier.Data;
using ElectricityOffNotifier.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElectricityOffNotifier.AppHost.Controllers;

[ApiController]
[Route("v1/[controller]")]
[Authorize]
public sealed class SubscriberController : ControllerBase
{
    private readonly ElectricityDbContext _context;

    public SubscriberController(ElectricityDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<SubscriberModel>> Register([FromBody] SubscriberRegisterModel model,
        CancellationToken cancellationToken)
    {
        int checkerId = int.Parse(User.FindFirstValue(CustomClaimTypes.CheckerId));
        int producerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        if (await _context.Subscribers.AnyAsync(s => s.CheckerId == checkerId && s.TelegramId == model.TelegramId,
                cancellationToken))
        {
            ModelState.AddModelError(nameof(model.TelegramId),
                "Subscriber with specified telegram id is already registered.");
            return BadRequest(ModelState);
        }

        var subscriber = new Subscriber
        {
            CheckerId = checkerId,
            TelegramId = model.TelegramId,
            ProducerId = producerId
        };
        _context.Subscribers.Add(subscriber);

        await _context.SaveChangesAsync(CancellationToken.None);

        return new SubscriberModel
        {
            SubscriberId = subscriber.Id
        };
    }

    [HttpDelete("{subscriberId:int:required}")]
    public async Task<ActionResult> Remove(int subscriberId, CancellationToken cancellationToken)
    {
        Subscriber? subscriber = await _context.Subscribers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == subscriberId, cancellationToken);

        if (subscriber == null)
            return NotFound();

        int producerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        if (subscriber.ProducerId != producerId)
        {
            return StatusCode(403, new
            {
                reason = "You are not the owner of specified subscriber so you can't delete it."
            });
        }

        _context.Subscribers.Remove(subscriber);
        await _context.SaveChangesAsync(CancellationToken.None);

        return Ok();
    }
}