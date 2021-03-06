﻿using AdventuresPlanetRuntime;
using AdventuresPlanetRuntime.Data;
using NotificationsExtensions.Toasts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI.Notifications;

namespace Tasks
{
    public sealed class PodcastNotifier : IBackgroundTask
    {
        private BackgroundTaskDeferral deferral;
        private ApplicationDataContainer data;
        private AVPManager adventuresPlanet;
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            var cost = BackgroundWorkCost.CurrentBackgroundWorkCost;
            if (cost == BackgroundWorkCostValue.High)
                return;
            data = ApplicationData.Current.LocalSettings;
            adventuresPlanet = new AVPManager();
            deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(TaskInstance_Canceled);
            DoUpdate();
        }

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            deferral.Complete();
        }
        private async void DoUpdate()
        {
            long time = data.Values.ContainsKey("bg_podcast") ? (long)data.Values["bg_podcast"] : adventuresPlanet.UpdateTimePodcast;
            if (time < adventuresPlanet.UpdateTimePodcast)
                time = adventuresPlanet.UpdateTimePodcast;
            if (time > 0)
            {
                List<PodcastItem> pods = new List<PodcastItem>(5);
                time = await adventuresPlanet.LoadListPodcast((list) =>
                {
                    pods.AddRange(list);
                }, null, time);
                data.Values["bg_podcast"] = time;
                if (pods.Any())
                    Notify(pods);
            }
            deferral.Complete();
        }
        private void Notify(List<PodcastItem> pods)
        {
            for (int i = pods.Count - 1; i >= 0; i--)
            {
                var item = pods[i];
                ToastContent toast = new ToastContent()
                {
                    ActivationType = ToastActivationType.Foreground,
                    Visual = new ToastVisual()
                    {
                        AppLogoOverride = new ToastAppLogo()
                        {
                            Crop = ToastImageCrop.None,
                            Source = new ToastImageSource(item.Immagine)
                        },
                        TitleText = new ToastText()
                        {
                            Text = "Calavera Cafè"
                        },
                        BodyTextLine1 = new ToastText()
                        {
                            Text = item.TitoloBG
                        },
                        BodyTextLine2 = new ToastText()
                        {
                            Text = item.Descrizione
                        }
                    },
                    Launch = $"action=listenPodcast&titolo={WebUtility.UrlEncode(item.Titolo)}&link={WebUtility.UrlEncode(item.Link)}&img={WebUtility.UrlEncode(item.Immagine)}&data={WebUtility.UrlEncode(item.Data)}&descrizione={WebUtility.UrlEncode(item.Descrizione)}"
                };
                var xmlToast = toast.GetXml();
                var notification = new ToastNotification(xmlToast);
                ToastNotificationManager.CreateToastNotifier().Show(notification);
            }
        }
    }
}
