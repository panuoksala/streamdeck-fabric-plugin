using Microsoft.Extensions.Logging;
using StreamDeckLib;
using StreamDeckLib.Messages;
using StreamDeckMicrosoftFabric.Models;
using StreamDeckMicrosoftFabric.Services;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace StreamDeckMicrosoftFabric
{
    public abstract class BaseAction : BaseStreamDeckActionWithSettingsModel<FabricSettingsModel>
    {
        private CancellationTokenSource _backgroundTaskToken;
        private DateTime _pressDownDateTime;

        protected double DoublePressDuration { get; set; } = 1;

        private readonly FabricService _service;
        private readonly LoginService _login;

        public BaseAction(IHttpClientFactory clientFactory, ILoggerFactory loggerFactory)
        {
            _login = new LoginService(loggerFactory.CreateLogger<LoginService>(), SettingsModel);
            _service = new FabricService(loggerFactory.CreateLogger<FabricService>(), clientFactory, SettingsModel, _login);
        }

        public override async Task OnDidReceiveSettings(StreamDeckEventPayload args)
        {
            await base.OnDidReceiveSettings(args);
        }

        public override Task OnKeyDown(StreamDeckEventPayload args)
        {
            _pressDownDateTime = DateTime.Now;

            return base.OnKeyDown(args);
        }

        public override async Task OnKeyUp(StreamDeckEventPayload args)
        {
            try
            {
                DateTime now = DateTime.Now;
                TimeSpan delta = now.Subtract(_pressDownDateTime);

                if (delta.TotalSeconds > DoublePressDuration)
                {
                    await OnLongPress(args);
                }
                else
                {
                    await OnTap(args);
                }

                // Settings seems to be updated frequently when actions are performed.
                await Manager.SetSettingsAsync(args.context, SettingsModel);
            }
            catch (Exception ex)
            {
                await OnError(args, ex);
            }
        }

        public override async Task OnWillAppear(StreamDeckEventPayload args)
        {
            await base.OnWillAppear(args);

            try
            {
                if (IsSettingsValid())
                {
                    await UpdateDisplay(args);
                }
            }
            catch (Exception ex)
            {
                await OnError(args, ex);
            }
        }

        public override async Task OnWillDisappear(StreamDeckEventPayload args)
        {
            try
            {
                if (_backgroundTaskToken != null)
                {
                    _backgroundTaskToken.Cancel();
                    _backgroundTaskToken = null;
                }
            }
            catch (Exception ex)
            {
                await OnError(args, ex);
            }
        }

        public virtual Task MissingSettings(StreamDeckEventPayload args)
        {
            return Task.CompletedTask;
        }

        protected bool IsSettingsValid()
        {
            return SettingsModel.IsValid();
        }

        protected void StartBackgroundTask(StreamDeckEventPayload args)
        {
            _backgroundTaskToken?.Cancel();
            _backgroundTaskToken = new CancellationTokenSource();

            _ = Task.Run(() => BackgroundTask(args, _backgroundTaskToken.Token));
        }

        protected void StopBackgroundTask()
        {
            _backgroundTaskToken?.Cancel();
            _backgroundTaskToken = null;
        }

        private async Task BackgroundTask(StreamDeckEventPayload args, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (SettingsModel.UpdateStatusEverySecond == 0)
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(SettingsModel.GetUpdateStatusInSeconds()), ct);
                try
                {
                    await UpdateDisplay(args);
                }
                catch (Exception ex)
                {
                    await OnError(args, ex);
                }
            }
        }

        public async Task<FabricService.JobRunStatus> UpdateStatus(string context)
        {
            try
            {
                if (_service.LastJobItemStatus == FabricService.JobRunStatus.Running)
                {
                    // Job is still running, check status
                    await _service.CheckLastJobStatusAsync();
                    await Manager.SetImageAsync(context, ConvertJobStatusToImage(_service.LastJobItemStatus));

                    return _service.LastJobItemStatus;
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

        protected abstract SupportedActions ResolveAction();

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

        public async Task OnTap(StreamDeckEventPayload args)
        {
            var action = ResolveAction();
            await ExecuteKeyPress(args, action);
        }

        public async Task OnLongPress(StreamDeckEventPayload args)
        {
            var action = ResolveAction();
            await ExecuteKeyPress(args, action);
        }

        public async Task OnError(StreamDeckEventPayload args, Exception ex)
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

                if (action == SupportedActions.GetCapacityUsage)
                {
                    await Manager.SetImageAsync(args.context, "images/Fabric-updating.png");
                    var usage = await _service.GetCapacityUsageAsync(SettingsModel.ResourceId);
                    if (usage == null)
                    {
                        await Manager.SetImageAsync(args.context, "images/Fabric-failed.png");
                        await Manager.SetTitleAsync(args.context, "N/A");
                        return;
                    }

                    var (used, total) = usage.Value;
                    string display = $"{used:0.#}/{total:0.#}"; // Could add percentage if desired
                    await Manager.SetTitleAsync(args.context, display);
                    await Manager.SetImageAsync(args.context, "images/Fabric-success.png");
                    return; // No background tracking
                }

                await Manager.SetImageAsync(args.context, "images/Fabric-updating.png");

                var runJobSucceeded = await _service.RunJob(SettingsModel.WorkspaceId, SettingsModel.ResourceId, action);
                if (runJobSucceeded)
                {
                    // Wait 3000ms to let the job start
                    await Task.Delay(TimeSpan.FromSeconds(3));
                    await _service.CheckLastJobStatusAsync();
                    await Manager.SetImageAsync(args.context, ConvertJobStatusToImage(_service.LastJobItemStatus));

                    if ((StatusUpdateFrequency)SettingsModel.UpdateStatusEverySecond != StatusUpdateFrequency.Never)
                    {
                        // Start background task to check status later
                        StartBackgroundTask(args);
                    }
                    else
                    {
                        if (_service.LastJobItemStatus == FabricService.JobRunStatus.Running)
                        {
                            // There won't be later check so always set to success to avoid in progress image
                            // If it is failed to start we will leave that icon as is
                            await Manager.SetImageAsync(args.context, "images/Fabric-success.png");
                        }
                    }
                }
                else
                {
                    await Manager.SetImageAsync(args.context, "images/Fabric-failed.png");
                }

                await Manager.SetSettingsAsync(args.context, SettingsModel);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to initiate action from keypress.");
                throw;
            }
        }

        public async Task UpdateDisplay(StreamDeckEventPayload args)
        {
            var jobStatus = await UpdateStatus(args.context);
            if (jobStatus != FabricService.JobRunStatus.Running)
            {
                StopBackgroundTask();
            }
        }
    }
}
