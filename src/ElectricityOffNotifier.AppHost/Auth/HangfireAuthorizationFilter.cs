using Hangfire.Dashboard;

namespace ElectricityOffNotifier.AppHost.Auth;

public sealed class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}