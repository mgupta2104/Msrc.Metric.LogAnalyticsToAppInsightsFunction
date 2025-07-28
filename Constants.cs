namespace LogAnalyticsToAppInsightsFunction;

public static class Constants
{
    //public const string WorkspaceId = "LogAnalyticsWorkspaceId";
    public const string KustoQuery = "SignInLogs | project AADTenantID";
}
