namespace Altinn.AccessMgmt.Core.Appsettings;

public class CoreAppsettings
{
    public CoreAppsettings()
    {
    }

    public CoreAppsettings(Action<CoreAppsettings> configureAppsettings)
    {
        if (configureAppsettings is { })
        {
            configureAppsettings(this);
        }
    }

    public RequestOptions Request { get; set; } = new();

    public ConnectionsOptions Connections { get; set; } = new();

    public class ConnectionsOptions
    {
        public int NotifyAddRightholderPendingInSeconds { get; set; } = 60 * 2;
    }

    public class RequestOptions
    {
        public int NotifyRequestApprovedInSeconds { get; set; } = 60 * 15;

        public int NotifyRequestPendingInSeconds { get; set; } = 60 * 15;
    }
}
