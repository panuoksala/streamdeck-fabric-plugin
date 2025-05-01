﻿using StreamDeckMicrosoftFabric.Models;
using StreamDeckMicrosoftFabric.Services;
using StreamDeckLib;
using StreamDeckLib.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace StreamDeckMicrosoftFabric
{
    public abstract class BaseAction : BaseStreamDeckActionWithSettingsModel<FabricSettingsModel>
    {
        private CancellationTokenSource _backgroundTaskToken;
        private DateTime _pressDownDateTime;
        private int _currentUpdateFrequency;

        protected double DoublePressDuration { get; set; } = 1;

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

                if ((StatusUpdateFrequency)SettingsModel.UpdateStatusEverySecond != StatusUpdateFrequency.Never)
                {
                    StartBackgroundTask(args);
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

        public abstract Task UpdateDisplay(StreamDeckEventPayload args);
        public abstract Task OnTap(StreamDeckEventPayload args);
        public abstract Task OnLongPress(StreamDeckEventPayload args);
        public abstract Task OnError(StreamDeckEventPayload args, Exception ex);
        public abstract bool IsSettingsValid();

        public virtual Task MissingSettings(StreamDeckEventPayload args)
        {
            return Task.CompletedTask;
        }

        protected void StartBackgroundTask(StreamDeckEventPayload args)
        {
            _backgroundTaskToken?.Cancel();
            _backgroundTaskToken = new CancellationTokenSource();

            _ = Task.Run(() => BackgroundTask(args, _backgroundTaskToken.Token));
        }

        protected void StopBackgroundTask()
        {
            if (_backgroundTaskToken != null)
            {
                _backgroundTaskToken.Cancel();
                _backgroundTaskToken = null;
            }
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
    }
}
