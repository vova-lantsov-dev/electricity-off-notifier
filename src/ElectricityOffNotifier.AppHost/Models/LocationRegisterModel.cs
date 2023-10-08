using ElectricityOffNotifier.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ElectricityOffNotifier.AppHost.Models;

public sealed record LocationRegisterModel(
    string FullAddress,
    int CityId);
    
public sealed class LocationRegisterModelValidator : AbstractValidator<LocationRegisterModel>
{
    public LocationRegisterModelValidator(ElectricityDbContext context)
    {
        // Validate address identifier
        RuleFor(m => m.CityId)
            .Cascade(CascadeMode.Stop)
            .GreaterThanOrEqualTo(1)
            .MustAsync((cityId, cancellationToken) =>
                context.Cities.AnyAsync(c => c.Id == cityId, cancellationToken))
            .WithMessage("City with specified id was not found");

        // Validate full address
        RuleFor(m => m.FullAddress).MinimumLength(3).MaximumLength(200);
    }
}