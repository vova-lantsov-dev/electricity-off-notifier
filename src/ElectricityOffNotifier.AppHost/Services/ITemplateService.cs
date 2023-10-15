using ElectricityOffNotifier.Data.Models;

namespace ElectricityOffNotifier.AppHost.Services;

public interface ITemplateService
{
    string ReplaceMessageTemplate(string input, Location location, Subscriber subscriber);

    bool ValidateMessageTemplate(string input);
}