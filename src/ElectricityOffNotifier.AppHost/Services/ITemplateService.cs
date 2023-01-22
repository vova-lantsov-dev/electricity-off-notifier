using ElectricityOffNotifier.Data.Models;

namespace ElectricityOffNotifier.AppHost.Services;

public interface ITemplateService
{
    string ReplaceMessageTemplate(string input, Address address, Subscriber subscriber,
        SentNotification? since);

    bool ValidateMessageTemplate(string input);
}