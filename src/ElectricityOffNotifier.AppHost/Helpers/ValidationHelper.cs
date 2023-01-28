using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace ElectricityOffNotifier.AppHost.Helpers;

public static class ValidationHelper
{
    public static ActionResult BadRequestExt(this ControllerBase controller, ValidationResult validationResult)
    {
        foreach (ValidationFailure validationFailure in validationResult.Errors)
        {
            controller.ModelState.TryAddModelError(validationFailure.PropertyName, validationFailure.ErrorMessage);
        }

        return controller.BadRequest(controller.ModelState);
    }
}