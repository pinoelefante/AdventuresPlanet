using NotificationsExtensions.Tiles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI.Notifications;

namespace PodcastPlayer
{
    public sealed class Player : IBackgroundTask
    {
        private BackgroundTaskDeferral deferral;
        private SystemMediaTransportControls systemmediatransportcontrol;
        private MediaPlayer mediaPlayer;
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Initialize SMTC object to talk with UniversalVolumeControl (UVC)
            // Note that, this is intended to run after app is paused and hence all the logic must be written to run in background process
            systemmediatransportcontrol = BackgroundMediaPlayer.Current.SystemMediaTransportControls;
            systemmediatransportcontrol.ButtonPressed += SystemControlsButtonPressed;
            systemmediatransportcontrol.IsEnabled = true;
            systemmediatransportcontrol.IsPauseEnabled = true;
            systemmediatransportcontrol.IsStopEnabled = true;
            systemmediatransportcontrol.IsPlayEnabled = true;

            mediaPlayer = BackgroundMediaPlayer.Current;
            BackgroundMediaPlayer.MessageReceivedFromForeground -= BackgroundMediaPlayerOnMessageReceivedFromForeground;
            BackgroundMediaPlayer.MessageReceivedFromForeground += BackgroundMediaPlayerOnMessageReceivedFromForeground;
            mediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

            deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += TaskInstance_Canceled;
        }

        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            switch(sender.PlaybackState)
            {
                case MediaPlaybackState.Playing:
                    if (IsRecoverPosition)
                    {
                        mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(PositionRecover);
                        IsRecoverPosition = false;
                        AvviaTimer(TimerFile);
                    }
                    break;
            }
        }

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            DefaultLiveTile();
            deferral.Complete();
        }
        private int PositionRecover;
        private string PlayingTitle;
        private ThreadPoolTimer Timer;
        private string TimerFile;
        private async void BackgroundMediaPlayerOnMessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            switch (e.Data["Command"]?.ToString())
            {
                case "Init":
                    break;
                case "Stop":
                    Stop();
                    break;
                case "GetCurrentSource":
                    BackgroundMediaPlayer.SendMessageToForeground(new Windows.Foundation.Collections.ValueSet()
                    {
                        {"Command", "GetCurrentSource" },
                        {"Source",  mediaPlayer.Source?.ToString()  }
                    });
                    break;
                case "PlayOffline":
                    {
                        if(mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                            mediaPlayer.Pause();
                        string path = e.Data["Path"].ToString();
                        string imageOF = e.Data["Thumb"].ToString();
                        int positionSec = (int)e.Data["Position"];
                        StorageFile fileOff = await StorageFile.GetFileFromPathAsync(path);
                        
                        systemmediatransportcontrol.DisplayUpdater.Type = MediaPlaybackType.Music;
                        PlayingTitle = e.Data["Title"].ToString();
                        systemmediatransportcontrol.DisplayUpdater.MusicProperties.Title = e.Data["Title"].ToString();
                        systemmediatransportcontrol.DisplayUpdater.MusicProperties.Artist = e.Data["Artist"].ToString();
                        systemmediatransportcontrol.DisplayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(imageOF));
                        systemmediatransportcontrol.DisplayUpdater.Update();
                        mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(positionSec);
                        UpdateTile(PlayingTitle, imageOF);

                        if (positionSec > 0)
                        {
                            PositionRecover = positionSec;
                            IsRecoverPosition = true;
                            TimerFile = fileOff.Name;
                        }
                        else
                        {
                            AvviaTimer(fileOff.Name);
                        }
                        mediaPlayer.Source = MediaSource.CreateFromStorageFile(fileOff);
                        mediaPlayer.Play();
                    }
                    break;
                case "PlayOnline":
                    {
                        if (mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                            mediaPlayer.Pause();
                        string Url = e.Data["Url"].ToString();
                        string imageO = e.Data["Thumb"].ToString();
                        int positionSec = (int)e.Data["Position"];
                        systemmediatransportcontrol.DisplayUpdater.Type = MediaPlaybackType.Music;
                        PlayingTitle = e.Data["Title"].ToString();
                        systemmediatransportcontrol.DisplayUpdater.MusicProperties.Title = e.Data["Title"].ToString();
                        systemmediatransportcontrol.DisplayUpdater.MusicProperties.Artist = e.Data["Artist"].ToString();
                        systemmediatransportcontrol.DisplayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(imageO));
                        systemmediatransportcontrol.DisplayUpdater.Update();
                        if (positionSec > 0)
                        {
                            TimerFile = Url;
                            PositionRecover = positionSec;
                            IsRecoverPosition = true;
                        }
                        else
                        {
                            AvviaTimer(Url);
                        }
                        UpdateTile(PlayingTitle, imageO);
                        mediaPlayer.Source = MediaSource.CreateFromUri(new Uri(Url));
                        mediaPlayer.Play();
                    }
                    break;
                case "Play":
                    mediaPlayer.Play();
                    break;
                case "Pause":
                    mediaPlayer.Pause();
                    break;
            }
        }
        private string Filename;
        private ApplicationDataContainer dataContainer = ApplicationData.Current.RoamingSettings;
        private void AvviaTimer(string url)
        {
            if (url.Contains('/'))
                Filename = url.Substring(url.LastIndexOf('/') + 1);
            else
                Filename = url;

            if (Timer != null)
                Timer.Cancel();

            Timer = ThreadPoolTimer.CreatePeriodicTimer((t) =>
            {
                if (mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                {
                    dataContainer.Values[$"pod_position_{Filename}"] = (int)mediaPlayer.PlaybackSession.Position.TotalSeconds;
                    Debug.WriteLine("Time = " + mediaPlayer.PlaybackSession.Position.TotalSeconds);
                }
                if (mediaPlayer.PlaybackSession.Position == mediaPlayer.PlaybackSession.NaturalDuration)
                {
                    dataContainer.Values[$"pod_position_{Filename}"] = 0;
                    Timer.Cancel();
                }
            }, TimeSpan.FromSeconds(5));
        }
        private void SystemControlsButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Stop:
                    Stop();
                    break;
                case SystemMediaTransportControlsButton.Play:
                    mediaPlayer.Play();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    mediaPlayer.Pause();
                    break;
                case SystemMediaTransportControlsButton.Next:
                    //TODO request next
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    //TODO request prev
                    break;
            }
        }
        private void UpdateTile(string title, string imageUri)
        {
            TileContent content = new TileContent()
            {
                Visual = new TileVisual()
                {
                    TileMedium = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            Children =
                            {
                                new TileText() { Text = title, Style = TileTextStyle.Subtitle, Wrap = true }
                            }
                        }
                    },

                    TileWide = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            Children =
                            {
                                new TileText() { Text = title, Style = TileTextStyle.Subtitle, Wrap = true }
                            }
                        }
                    },

                    TileSmall = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                    }
                }
            };
            (content.Visual.TileSmall.Content as TileBindingContentAdaptive).BackgroundImage = new TileBackgroundImage() { Source = new TileImageSource(imageUri) };
            (content.Visual.TileMedium.Content as TileBindingContentAdaptive).BackgroundImage = new TileBackgroundImage() { Source = new TileImageSource(imageUri) };
            (content.Visual.TileWide.Content as TileBindingContentAdaptive).BackgroundImage = new TileBackgroundImage() { Source = new TileImageSource(imageUri) };
            var notification = new TileNotification(content.GetXml());
            TileUpdateManager.CreateTileUpdaterForApplication("App").Update(notification);
        }
        private void DefaultLiveTile()
        {
            TileUpdateManager.CreateTileUpdaterForApplication("App").Clear(); //set default live tile
        }
        private MediaSource SourceStop;
        private bool IsRecoverPosition;

        private async void Stop()
        {
            Timer?.Cancel();
            dataContainer.Values[$"pod_position_{Filename}"] = (int)mediaPlayer.PlaybackSession.Position.TotalSeconds;//salva la posizione del podcast

            mediaPlayer.SystemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Stopped;
            mediaPlayer.PlaybackSession.Position = mediaPlayer.PlaybackSession.NaturalDuration;
            if (SourceStop == null)
            {
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///stop.mp3"));
                SourceStop = MediaSource.CreateFromStorageFile(file);
            }
            mediaPlayer.Source = SourceStop;
            PlayingTitle = string.Empty;
            systemmediatransportcontrol.DisplayUpdater.ClearAll();
            systemmediatransportcontrol.DisplayUpdater.Update();
            DefaultLiveTile();
        }
    }
}
