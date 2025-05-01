using Microsoft.Extensions.Logging;
using Serilog;
using StreamDeckMicrosoftFabric.Models;
using StreamDeckMicrosoftFabric.Services;
using StreamDeckLib;
using StreamDeckLib.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

namespace StreamDeckMicrosoftFabric
{
    public enum SupportedActions
    {
        RunNotebook = 0,
        RunDatapipeline = 1,
    }


    [ActionUuid(Uuid = "net.oksala.microsoftfabric.runner")]
    public class FabricActionRunner : BaseAction
    {
        private readonly FabricService _service;
        private readonly LoginService _login;

        public FabricActionRunner(IHttpClientFactory clientFactory, ILoggerFactory loggerFactory)
        {
            _login = new LoginService(loggerFactory.CreateLogger<LoginService>(), SettingsModel);
            _service = new FabricService(loggerFactory.CreateLogger<FabricService>(), clientFactory, SettingsModel, _login);
        }

        public override bool IsSettingsValid()
        {
            return SettingsModel.IsValid();
        }

        public async Task<FabricService.JobRunStatus> UpdateStatus(string context)
        {
            try
            {
                if (_service.LastJobItemStatus == FabricService.JobRunStatus.Running)
                {
                    // Job is still running, check status
                    var jobStatus = await _service.CheckLastJobStatusAsync();
                    await Manager.SetImageAsync(context, ConvertJobStatusToImage(jobStatus));

                    return jobStatus;
                }
            }
            catch (Exception ex)
            {
                await Manager.SetImageAsync(context, "images/Fabric-unknown.png");
                Logger.LogError(ex, "Failed to update status. Stopping checking");
                return FabricService.JobRunStatus.Failed;
            }

            return FabricService.JobRunStatus.Success;
        }

        private string ConvertJobStatusToImage(FabricService.JobRunStatus jobStatus)
        {
            return jobStatus switch
            {
                FabricService.JobRunStatus.Success => "images/Fabric-success.png",
                FabricService.JobRunStatus.Failed => "images/Fabric-failed.png",
                FabricService.JobRunStatus.Running => "images/Fabric-waiting.png",
                _ => "images/Fabric-unknown.png",
            };
        }

        public override async Task OnTap(StreamDeckEventPayload args)
        {
            var action = ResolveAction(args.action);
            await ExecuteKeyPress(args, action);
        }

        private static SupportedActions ResolveAction(string action)
        {
            return action.ToLower() switch
            {
                "net.oksala.microsoftfabric.runner" => SupportedActions.RunNotebook,
                "net.oksala.microsoftfabric.datapipelinerunner" => SupportedActions.RunDatapipeline,
                _ => SupportedActions.RunNotebook,
            };
        }

        public override async Task OnLongPress(StreamDeckEventPayload args)
        {
            var action = ResolveAction(args.action);
            await ExecuteKeyPress(args, action);
        }

        public override async Task OnError(StreamDeckEventPayload args, Exception ex)
        {
            try
            {
                SettingsModel.ErrorMessage = ex.Message;

                await Manager.ShowAlertAsync(args.context);
                await Manager.SetImageAsync(args.context, "images/Fabric-unknown.png");

                await Manager.SetSettingsAsync(args.context, SettingsModel);
            }
            catch (Exception handlingException)
            {
                Logger.LogError(handlingException, $"Failed to handle error: {ex.Message}");
            }
        }

        private async Task ExecuteKeyPress(StreamDeckEventPayload args, SupportedActions action)
        {
            try
            {
                // Clear possible error message
                SettingsModel.ErrorMessage = string.Empty;

                // Check login status
                if (!_login.IsLoginValid())
                {
                    await _login.Login();
                }

                await Manager.SetImageAsync(args.context, "images/Fabric-updating.png");

                await _service.RunJob(SettingsModel.WorkspaceId, SettingsModel.ResourceId, action);
                // Wait 3000ms to let the job start
                await Task.Delay(TimeSpan.FromSeconds(3));

                var status = await _service.CheckLastJobStatusAsync();
                await Manager.SetImageAsync(args.context, ConvertJobStatusToImage(status));

                if ((StatusUpdateFrequency)SettingsModel.UpdateStatusEverySecond != StatusUpdateFrequency.Never)
                {
                    // Start background task to check status later
                    StartBackgroundTask(args);
                }
                else
                {
                    if(status == FabricService.JobRunStatus.Running)
                    {
                        // There won't be later check so always set to success to avoid in progress image
                        // If it is failed to start we will leave that icon as is
                        await Manager.SetImageAsync(args.context, "images/Fabric-success.png");
                    }
                }

                await Manager.SetSettingsAsync(args.context, SettingsModel);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to initiate action from keypress.");
                throw;
            }
        }

        public async override Task UpdateDisplay(StreamDeckEventPayload args)
        {
            var jobStatus = await UpdateStatus(args.context);
            if (jobStatus != FabricService.JobRunStatus.Running)
            {
                StopBackgroundTask();
            }
        }
    }
}
