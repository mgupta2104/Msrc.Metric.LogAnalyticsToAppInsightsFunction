# LogAnalyticsToAppInsightsFunction

This Azure Function queries Azure Log Analytics using Kusto Query Language (KQL), processes the results, and sends telemetry events to Application Insights. It is triggered every 5 minutes using a TimerTrigger and includes robust retry logic and secure authentication using Azure.Identity.

## Features

- Scheduled execution every 5 minutes via TimerTrigger
- Queries Azure Log Analytics using KQL
- Sends telemetry events to Application Insights
- Implements retry logic with exponential backoff for HTTP requests
- Uses Azure.Identity for secure token-based authentication

## Prerequisites

- Azure subscription with:
  - Log Analytics Workspace
  - Application Insights resource
- Azure Function App configured with:
  - `LogAnalyticsWorkspaceId` environment variable
  - Managed Identity enabled for authentication

## Configuration

Set the following environment variable in your Azure Function App:

- `LogAnalyticsWorkspaceId`: The Workspace ID of your Log Analytics workspace

## How It Works

1. The function is triggered every 5 minutes.
2. It runs a KQL query against the specified Log Analytics workspace.
3. The results are parsed and each row is sent as a telemetry event to Application Insights.
4. Retry logic is implemented to handle transient HTTP errors.

## Technologies Used

- .NET 8 SDK
- Azure Functions
- Azure Monitor (Log Analytics)
- Application Insights
- Azure.Identity
- Newtonsoft.Json

## Getting Started

1. Clone the repository.
2. Deploy the function to Azure using your preferred method (VS Code, Azure CLI, etc.).
3. Set the required environment variables.
4. Ensure the Function App has permission to access Log Analytics via Managed Identity.

