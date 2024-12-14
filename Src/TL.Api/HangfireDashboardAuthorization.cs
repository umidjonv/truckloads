using Hangfire.Dashboard;

public class HangfireDashboardAuthorization : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // TODO: need add auth
        return true;
    }
}