using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Azure.Identity;
using Azure.Core;

namespace LogAnalyticsToAppInsightsFunction;

public class LogAnalyticsToAppInsights
{
    private readonly ILogger _logger;
    private readonly TelemetryClient _telemetryClient;
    private static readonly string WorkspaceId = Environment.GetEnvironmentVariable("LogAnalyticsWorkspaceId");
    private static readonly HttpClient httpClient = new HttpClient();

    public LogAnalyticsToAppInsights(ILoggerFactory loggerFactory, TelemetryClient telemetryClient)
    {
        _logger = loggerFactory.CreateLogger<LogAnalyticsToAppInsights>();
        _telemetryClient = telemetryClient;
    }

    [Function("LogAnalyticsToAppInsights")]
    public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("Function executed at: {executionTime}", DateTime.Now);

        try
        {
           // var kql = "SignInLogs | project AADTenantID";
            var jsonResult = await RunQueryWithRetriesAsync(Constants.KustoQuery, _logger);
            var rows = jsonResult?["tables"]?[0]?["rows"];

            if (rows != null)
            {
                foreach (var row in rows)
                {
                    string message = string.Join(" | ", row);
                    _telemetryClient.TrackEvent("LogAnalyticsEvent", new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "Message", message }
                    });
                }

                _telemetryClient.Flush();
                await Task.Delay(5000); // Ensure telemetry is sent before function exits
                _logger.LogInformation("Successfully processed rows from Log Analytics query.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing Log Analytics data.");
        }
    }

    private static async Task<JObject> RunQueryWithRetriesAsync(string kustoQuery, ILogger logger)
    {
        const int maxRetries = 3;
        int delay = 2000;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await RunQueryAsync(kustoQuery);
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning("Retry attempt {Attempt} failed: {Message}", attempt, ex.Message);

                if (attempt == maxRetries)
                {
                    logger.LogError("Max retry attempts reached.");
                    throw;
                }

                await Task.Delay(delay);
                delay *= 2; // Exponential backoff
            }
        }

        // This line is technically unreachable, but required to satisfy the compiler
        throw new InvalidOperationException("Unexpected error in retry logic.");
    }

    private static async Task<JObject> RunQueryAsync(string kustoQuery)
    {
        var uri = $"https://api.loganalytics.io/v1/workspaces/{WorkspaceId}/query";
        var requestBody = new { query = kustoQuery };
        var jsonContent = JsonConvert.SerializeObject(requestBody);

        using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var credential = new DefaultAzureCredential();
        var token = await credential.GetTokenAsync(
            new TokenRequestContext(new[] { "https://api.loganalytics.io/.default" })
        );

        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        var response = await httpClient.PostAsync(uri, content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        return JObject.Parse(responseContent);
    }
}
