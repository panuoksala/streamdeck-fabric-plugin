using Azure;
using Microsoft.Extensions.Logging;
using StreamDeckMicrosoftFabric.Models;
using System;
using System.Data;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace StreamDeckMicrosoftFabric.Services
{
    public class FabricService(ILogger logger,
                         IHttpClientFactory httpClientFactory,
                         FabricSettingsModel fabricSettingsModel,
                         LoginService loginService)
    {
        public enum JobRunStatus
        {
            NotInitialized = -1,
            Running = 0,
            Success = 1,
            Failed = 2,
        }

        private readonly ILogger _logger = logger;
        private readonly IHttpClientFactory _clientFactory = httpClientFactory;
        private readonly LoginService _login = loginService;
        private Uri _lastJobLocation = null;
        private readonly SemaphoreSlim _statusCheckSemaphore = new SemaphoreSlim(1, 1);

        public JobRunStatus LastJobItemStatus { get; set; } = JobRunStatus.NotInitialized;

        public async Task<string> RunJob(string workspaceId, string jobId, SupportedActions actionType)
        {
            if (actionType == SupportedActions.GetCapacityUsage)
            {
                // Not a runnable job type; caller should invoke GetCapacityUsageAsync instead.
                return "images/Fabric-updating.png";
            }

            _logger.LogInformation($"Running job {jobId}.");

            var client = _clientFactory.CreateClient();
            string url = $"https://api.fabric.microsoft.com/v1/workspaces/{workspaceId}/items/{jobId}/jobs/instances?jobType={ResolveApiJobType(actionType)}";
            //if (actionType == SupportedActions.RunDataFlow)
            //{
            //    // Dataflows have a different API endpoint
            //    url = $"https://api.fabric.microsoft.com/v1/workspaces/{workspaceId}/dataflows/{jobId}/jobs/instances?jobType={ResolveApiJobType(actionType)}";
            //}

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_login.AccessToken}");

            var response = await client.PostAsync(url, null);

            if (response.IsSuccessStatusCode)
            {
                _lastJobLocation = response.Headers.Location;
                _logger.LogInformation($"Job started. Job location: {_lastJobLocation}");
                return await Task.FromResult("images/Fabric-updating.png");
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to run job {jobId}. Error {responseContent}");
                return await Task.FromResult("images/Fabric-failed.png");
            }
        }

        public async Task<JobRunStatus> CheckLastJobStatusAsync()
        {
            // Try to enter the semaphore, but don't wait if it's already taken
            if (!await _statusCheckSemaphore.WaitAsync(0))
            {
                _logger.LogInformation("Status check already in progress, skipping duplicate call");
                return LastJobItemStatus; // Return the current status instead of making a duplicate call
            }

            try
            {
                var client = _clientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_login.AccessToken}");

                var response = await client.GetAsync(_lastJobLocation);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Job status checked. Job location: {_lastJobLocation}");
                    var responseJson = await JsonNode.ParseAsync(await response.Content.ReadAsStreamAsync());

                    if (responseJson["status"]?.ToString() == "Completed")
                    {
                        LastJobItemStatus = JobRunStatus.Success;
                        _logger.LogInformation($"Job completed as success. Job location: {_lastJobLocation}");
                        return await Task.FromResult(JobRunStatus.Success);
                    }
                    else if (responseJson["status"]?.ToString() == "Failed")
                    {
                        LastJobItemStatus = JobRunStatus.Failed;
                        _logger.LogInformation($"Job completed as a failure. Job location: {_lastJobLocation}");
                        return await Task.FromResult(JobRunStatus.Failed);
                    }
                    else
                    {
                        LastJobItemStatus = JobRunStatus.Running;
                        _logger.LogInformation($"Job still running. Job location: {_lastJobLocation}");
                        return await Task.FromResult(JobRunStatus.Running);
                    }
                }
                else
                {
                    // Tell caller that job failed
                    LastJobItemStatus = JobRunStatus.Failed;
                    _logger.LogError($"Failed to check job status. Job location: {_lastJobLocation}");
                    return await Task.FromResult(JobRunStatus.Failed);
                }
            }
            finally
            {
                // Always release the semaphore when done
                _statusCheckSemaphore.Release();
            }
        }

        public async Task<(double used, double total)?> GetCapacityUsageAsync(string capacityName)
        {
            if (string.IsNullOrWhiteSpace(capacityName))
            {
                _logger.LogWarning("Capacity name (ResourceId) is empty.");
                return null;
            }

            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_login.AccessToken}");

            // Need subscription id and location to get capacity usage.
            //https://management.azure.com/subscriptions/548B7FB7-3B2A-4F46-BB02-66473F1FC22C/providers/Microsoft.Fabric/locations/centraluseuap/usages?api-version=2025-01-15-preview
            var url = "https://api.fabric.microsoft.com/v1/capacities/usages?api-version=2025-01-15-preview";

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to fetch capacity usages. Status {Status}. Body: {Body}", response.StatusCode, err);
                return null;
            }

            var root = await JsonNode.ParseAsync(await response.Content.ReadAsStreamAsync());
            var arr = root?["value"] as JsonArray;
            if (arr == null)
            {
                _logger.LogWarning("No 'value' array in usages response.");
                return null;
            }

            foreach (var node in arr)
            {
                var name = node?["capacityName"]?.ToString();
                if (name != null && name.Equals(capacityName, StringComparison.OrdinalIgnoreCase))
                {
                    if (double.TryParse(node?["usedCapacityUnits"]?.ToString(), out var used) &&
                        double.TryParse(node?["totalCapacityUnits"]?.ToString(), out var total))
                    {
                        _logger.LogInformation("Found capacity {Name}: {Used}/{Total} CU used.", name, used, total);
                        return (used, total);
                    }
                }
            }

            _logger.LogWarning("Capacity with name '{CapacityName}' not found in usage list.", capacityName);
            return null;
        }


        private static string ResolveApiJobType(SupportedActions action)
        {
            return action switch
            {
                SupportedActions.RunNotebook => "RunNotebook",
                SupportedActions.RunDatapipeline => "Pipeline",
                SupportedActions.RunDataFlow => "Publish",
                _ => "RunNotebook",
            };
        }
    }
}
