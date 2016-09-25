using AdventuresPlanetRuntime;
using AdventuresPlanetRuntime.Data;
using NotificationsExtensions.Tiles;
using NotificationsExtensions.Toasts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI.Notifications;

namespace Tasks
{
    public sealed class NewsNotifier : IBackgroundTask
    {
        private BackgroundTaskDeferral deferral;
        private ApplicationDataContainer data;
        private AVPManager adventuresPlanet;
        private AVPDatabase database;
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();
            if (!IsTimeOk() || BackgroundWorkCost.CurrentBackgroundWorkCost == BackgroundWorkCostValue.High)
            {
                deferral.Complete();
                return;
            }
            data = ApplicationData.Current.LocalSettings;
            adventuresPlanet = new AVPManager();
            database = new AVPDatabase();
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(TaskInstance_Canceled);
            DoUpdate();
        }
        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            deferral.Complete();
        }
        
        private async void DoUpdate()
        {
            long time = data.Values.ContainsKey("bg_news") ? (long)data.Values["bg_news"] : adventuresPlanet.UpdateTimeNews;
            if (time < adventuresPlanet.UpdateTimePodcast)
                time = adventuresPlanet.UpdateTimePodcast;
            if (time > 0)
            {
                int anno = DateTime.Now.Year, mese = DateTime.Now.Month;
                List<News> listNews = null;
                bool ok = await adventuresPlanet.LoadListNews(anno, mese, null, (list)=>
                {
                    listNews = database.InsertNews(list);
                });

                if (ok)
                {
                    data.Values["bg_news"] = TimeUtils.GetUnixTimestamp();
                    if (listNews != null && listNews.Any())
                    {
                        LiveTileNotification(listNews);
                        ToastNotification(listNews);
                    }
                }
            }
            deferral.Complete();
        }
        private bool IsTimeOk()
        {
            return true;
        }
        private void ToastNotification(List<News> news)
        {
            for (int i = 0; i < news.Count; i++)
            {
                var item = news[i];
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
                            Text = item.Titolo
                        },
                        BodyTextLine1 = new ToastText()
                        {
                            Text = item.AnteprimaNews
                        }
                    }
                };
                var xmlToast = toast.GetXml();
                var notification = new ToastNotification(xmlToast);
                ToastNotificationManager.CreateToastNotifier().Show(notification);
            }
        }
        private void LiveTileNotification(List<News> listNews)
        {
            var item = listNews[0];
            
            TileContent content = new TileContent()
            {
                Visual = new TileVisual()
                {
                    TileMedium = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            BackgroundImage = new TileBackgroundImage()
                            {
                                Source = new TileImageSource(item.Immagine)
                            }
                        }
                    },

                    TileWide = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            Children =
                            {
                                new TileImage() {Align = TileImageAlign.Left, Source = new TileImageSource(item.Immagine), RemoveMargin = true },
                                new TileText() { Text = item.Titolo, Style = TileTextStyle.Subtitle, Wrap = true, MaxLines = 2, Align = TileTextAlign.Auto }
                            }
                        }
                    },

                    TileSmall = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            Children =
                            {
                                new TileImage() { Source = new TileImageSource(item.Immagine) }
                            }
                        }
                    }
                }
            };
            var notification = new TileNotification(content.GetXml());
            TileUpdateManager.CreateTileUpdaterForApplication("App").Update(notification);
        }
    }
}
