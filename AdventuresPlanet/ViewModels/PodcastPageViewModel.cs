using AdventuresPlanetRuntime;
using AdventuresPlanetRuntime.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template10.Common;
using Template10.Mvvm;
using Utils;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml.Navigation;

namespace AdventuresPlanet.ViewModels
{
    public class PodcastPageViewModel : ViewModelBase
    {
        private AVPManager manager;
        private AVPDatabase db;
        public PodcastPageViewModel(AVPManager m, AVPDatabase d)
        {
            manager = m;
            db = d;
            ListaPodcast = new ObservableCollection<PodcastItem>();
        }
        public ObservableCollection<PodcastItem> ListaPodcast { get; private set; }
        private PodcastItem _podSelezionato;
        public PodcastItem PodcastSelezionato { get { return _podSelezionato; } set { Set(ref _podSelezionato, value); } }
        private bool _caricaPodcast, _isPodcastPlaying;
        public bool IsCaricaPodcast { get { return _caricaPodcast; } set { Set(ref _caricaPodcast, value); } }
        public bool PlayerPlaying { get { return _isPodcastPlaying; } set { Set(ref _isPodcastPlaying, value); } }

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            NavigationService.FrameFacade.BackRequested += FrameFacade_BackRequested;
            BackgroundMediaPlayer.MessageReceivedFromBackground += BackgroundMediaPlayer_MessageReceivedFromBackground;
            BackgroundMediaPlayer.Current.CurrentStateChanged += Current_CurrentStateChanged;
            InitPlayer();

            Task taskPodcast = null;
            if (ListaPodcast.Count == 0)
                CaricaPodcastDaDatabase();
            if(IsToUpdateByTime())
                taskPodcast = AggiornaPodcast();

            if (parameter != null)
            {
                if (parameter is PodcastItem)
                    PlayPodcast.Execute(parameter);
                else if (parameter is string && Uri.IsWellFormedUriString(parameter.ToString(), UriKind.RelativeOrAbsolute))
                {
                    if (taskPodcast != null)
                        await taskPodcast;
                    var fileName = UrlUtils.GetUrlParameterValue(parameter.ToString(), "podcast");
                    var res = ListaPodcast.Where(x => x.Filename.CompareTo(fileName) == 0);
                    if (res.Count() == 1)
                    {
                        PodcastItem item = res.ElementAt(0);
                        PlayPodcast.Execute(item);
                    }
                    else
                    {
                        var mp3Link = $"http://www.adventuresplanet.it/contenuti/podcast/media/{fileName}";
                        PodcastItem podCast = new PodcastItem()
                        {
                            Titolo = fileName,
                            Link = mp3Link,
                            Immagine = "ms-appx:///Assets/calavera_podcast.jpg"
                        };
                        PlayPodcast.Execute(podCast);
                    }
                }
            }
        }
        private static readonly long TIME_UPDATE = 14400;//4 ore
        private bool IsToUpdateByTime()
        {
            if (TimeUtils.GetUnixTimestamp() - manager.UpdateTimePodcast > TIME_UPDATE)
                return true;
            return false;
        }

        private void CaricaPodcastDaDatabase()
        {
            var podcast = db.SelectAllPodcast();
            InsertAction.Invoke(podcast);
        }

        private void Current_CurrentStateChanged(MediaPlayer sender, object args)
        {
            switch (sender.CurrentState)
            {
                case MediaPlayerState.Stopped:
                case MediaPlayerState.Closed:
                    PodcastSelezionato = null;
                    PlayerPlaying = false;
                    break;
                case MediaPlayerState.Paused:
                    PlayerPlaying = false;
                    if (sender.Position == sender.NaturalDuration)
                        PodcastSelezionato = null;
                    break;
                case MediaPlayerState.Playing:
                default:
                    PlayerPlaying = true;
                    break;
            }
        }
        private void BackgroundMediaPlayer_MessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            
        }
        public override Task OnNavigatedFromAsync(IDictionary<string, object> state, bool suspending)
        {
            NavigationService.FrameFacade.BackRequested -= FrameFacade_BackRequested;
            return base.OnNavigatedFromAsync(state, suspending);
        }
        private void FrameFacade_BackRequested(object sender, Template10.Common.HandledEventArgs e)
        {
            e.Handled = true;
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }
        private Action<List<PodcastItem>> _actionAdd;
        public Action<List<PodcastItem>> InsertAction
        {
            get
            {
                return _actionAdd ??
                    (_actionAdd = (x) => 
                    {
                        WindowWrapper.Current().Dispatcher.Dispatch(() =>
                        {
                            if (ListaPodcast.Count > 0)
                            {
                                for (int i = x.Count - 1; i >= 0; i--)
                                    ListaPodcast.Insert(0, x[i]);
                            }
                            else
                            {
                                foreach (var item in x)
                                    ListaPodcast.Add(item);
                            }
                        });
                    });
            }
        }
        private async Task AggiornaPodcast()
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                IsCaricaPodcast = true;
            });
            manager.UpdateTimePodcast = await manager.LoadListPodcast(InsertAction, (x)=> { db.InsertAll(x); }, manager.UpdateTimePodcast);
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                IsCaricaPodcast = false;
            });
        }
        private MessageDialog resumeDialog;
        private DelegateCommand<PodcastItem> _playPodcast;
        public DelegateCommand<PodcastItem> PlayPodcast =>
            _playPodcast ??
            (_playPodcast = new DelegateCommand<PodcastItem>(async (x) =>
            {
                var position = 0;
                var roaming = ApplicationData.Current.RoamingSettings;
                if (roaming.Values.ContainsKey($"pod_position_{x.Filename}") && (int)roaming.Values[$"pod_position_{x.Filename}"] > 10)
                {
                    if (resumeDialog == null)
                    {
                        resumeDialog = new MessageDialog("Vuoi riprendere dall'ultima posizione?")
                        {
                            CancelCommandIndex = 1,
                            DefaultCommandIndex = 0
                        };
                        resumeDialog.Commands.Add(new UICommand("Si", (c) => { position = (int)roaming.Values[$"pod_position_{x.Filename}"]; }));
                        resumeDialog.Commands.Add(new UICommand("No"));
                    }
                    await resumeDialog.ShowAsync();
                }
                PodcastSelezionato = x;
                BackgroundMediaPlayer.SendMessageToBackground(new ValueSet()
                {
                    {"Command", "PlayOnline" },
                    {"Url", x.Link },
                    {"Thumb", x.Immagine },
                    {"Title", x.TitoloBG },
                    {"Artist", "Calavera Cafe" },
                    {"Position", position }
                });
            }));
        private void InitPlayer()
        {
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet()
            {
                {"Command","Init" }
            });
        }
        private DelegateCommand _playerPlay, _playerStop, _playerPause;
        public DelegateCommand PlayerPlay =>
            _playerPlay ??
            (_playerPlay = new DelegateCommand(() =>
            {
                BackgroundMediaPlayer.SendMessageToBackground(new ValueSet()
                {
                    {"Command", "Play" }
                });
            }));
        public DelegateCommand PlayerPause =>
            _playerPause ??
            (_playerPause = new DelegateCommand(() =>
            {
                BackgroundMediaPlayer.SendMessageToBackground(new ValueSet()
                {
                    {"Command", "Pause" }
                });
            }));
        public DelegateCommand PlayerStop =>
            _playerStop ??
            (_playerStop = new DelegateCommand(() =>
            {
                BackgroundMediaPlayer.SendMessageToBackground(new ValueSet()
                {
                    {"Command", "Stop" }
                });
            }));
    }
}
