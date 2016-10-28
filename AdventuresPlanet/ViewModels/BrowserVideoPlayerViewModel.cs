using AdventuresPlanet.Services;
using AdventuresPlanetRuntime;
using AdventuresPlanetRuntime.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Template10.Mvvm;
using Utils;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml.Navigation;

namespace AdventuresPlanet.ViewModels
{
    public class BrowserVideoPlayerViewModel : ViewModelBase
    {
        private AVPManager manager;
        private SettingsService settings;
        public BrowserVideoPlayerViewModel(AVPManager m, SettingsService s)
        {
            manager = m;
            settings = s;
        }
        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            IsLoading = true;
            if ((mode == NavigationMode.New || mode == NavigationMode.Refresh) && parameter != null)
            {
                ElencoVideo.Clear();
                TubecastLaunched = false;
                if (parameter is SoluzioneItem)
                {
                    var sol = parameter as SoluzioneItem;
                    if (await manager.LoadSoluzione(sol))
                    {
                        foreach (var item in sol.TestoRich)
                        {
                            if (item.StartsWith("@VIDEO"))
                            {
                                string video = item.Replace("@VIDEO;", "");
                                string[] values = video.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                                string videosrc = values[0];
                                ElencoVideo.Add(videosrc);
                            }
                        }
                    }
                }
                else if (parameter is RecensioneItem)
                {
                    var rec = parameter as RecensioneItem;
                    if (await manager.LoadRecensione(rec))
                    {
                        foreach (var item in rec.TestoRich)
                        {
                            if (item.StartsWith("@VIDEO"))
                            {
                                string video = item.Replace("@VIDEO;", "");
                                string[] values = video.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                                string videosrc = values[0];
                                ElencoVideo.Add(videosrc);
                            }
                        }
                        
                    }
                }
                else if(parameter is string)
                {
                    var link = parameter.ToString();
                    List<string> componenti = null;
                    if (UrlUtils.IsDomain(link, "youtube.com"))
                    {
                        var videoId = UrlUtils.GetUrlParameterValue(link, "v");
                        if (!string.IsNullOrEmpty(videoId))
                            ElencoVideo.Add($"https://www.youtube.com/embed/{videoId}");
                        else
                            await new MessageDialog("Si è verificato un errore").ShowAsync();
                    }
                    else
                    {
                        if (AVPManager.IsExtra(link))
                            componenti = await manager.GetComponentsFrom(link, "scheda_completa");
                        else if (AVPManager.IsTrailer(link))
                            componenti = await manager.GetComponentsFrom(link, "scheda_video");
                        if (componenti?.Count > 0)
                        {
                            foreach (var item in componenti)
                            {
                                if (item.StartsWith("@VIDEO"))
                                {
                                    string video = item.Replace("@VIDEO;", "");
                                    string[] values = video.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                                    string videosrc = values[0];
                                    ElencoVideo.Add(videosrc);
                                }
                            }
                        }
                        else
                        {
                            await new MessageDialog("Si è verificato un errore").ShowAsync();
                        }
                    }
                }
                if (ElencoVideo.Any())
                {
                    if (settings.VideoTubecast)
                    {
                        var uri = new Uri($"tubecast:link={WebUtility.UrlEncode(ElencoVideo[0])}");
                        if (await Launcher.QueryUriSupportAsync(uri, LaunchQuerySupportType.Uri) == LaunchQuerySupportStatus.Available)
                        {
                            TubecastLaunched = true;
                            OpenTubecast(ElencoVideo[0]);
                        }
                    }
                    if(!TubecastLaunched)
                        CurrentVideo = 0;
                }
            }
            IsLoading = false;
        }
        public override Task OnNavigatedFromAsync(IDictionary<string, object> pageState, bool suspending)
        {
            ElencoVideo?.Clear();
            WebSource = null;
            return base.OnNavigatedFromAsync(pageState, suspending);
        }
        private bool _isLoading, _tubecastLaunched, _hasPrev, _hasNext;
        public bool TubecastLaunched { get { return _tubecastLaunched; } set { Set(ref _tubecastLaunched, value); } }
        public bool IsLoading { get { return _isLoading; } set { Set(ref _isLoading, value); } }
        public bool ShowCommands { get { return ElencoVideo.Count > 1; } }
        private string _webSource;
        public string WebSource { get { return _webSource; } set { Set(ref _webSource, value); } }
        public ObservableCollection<string> ElencoVideo { get; } = new ObservableCollection<string>();
        public bool HasPrev { get { return _hasPrev; } set { Set(ref _hasPrev, value); } }
        public bool HasNext { get { return _hasNext; } set { Set(ref _hasNext, value); } }
        private int _currVideo;
        private int CurrentVideo
        {
            get { return _currVideo; }
            set
            {
                Set(ref _currVideo, value);
                HasPrev = value > 0;
                HasNext = value < ElencoVideo.Count && value + 1 != ElencoVideo.Count;
                if(ElencoVideo.Any())
                    WebSource = ElencoVideo[value];
            }
        }
        private DelegateCommand _nextCmd, _prevCmd, _openHereCmd;
        public DelegateCommand NextCommand =>
            _nextCmd ??
            (_nextCmd = new DelegateCommand(() =>
            {
                if (HasNext)
                    CurrentVideo++;
            }));
        public DelegateCommand PrevCommand =>
            _prevCmd ??
            (_prevCmd = new DelegateCommand(() =>
            {
                if (HasPrev)
                    CurrentVideo--;
            }));
        public DelegateCommand OpenVideoHereCmd =>
            _openHereCmd ??
            (_openHereCmd = new DelegateCommand(() =>
            {
                if (ElencoVideo.Any())
                {
                    TubecastLaunched = false;
                    CurrentVideo = 0;
                }
            }));
        private void OpenTubecast(string link)
        {
            if (UrlUtils.UrlHasParameter(link, "list"))
                OpenPlaylistTubecast(UrlUtils.GetUrlParameterValue(link, "list"));
            else if (UrlUtils.UrlHasParameter(link, "v"))
                OpenVideoTubecast(UrlUtils.GetUrlParameterValue(link, "v"));
            else
                OpenLinkTubecast(link);
        }
        private async void OpenPlaylistTubecast(string playlistId)
        {
            var url = $"tubecast:PlaylistID={playlistId}";
            await Launcher.LaunchUriAsync(new Uri(url));
        }
        private async void OpenVideoTubecast(string videoId)
        {
            var url = $"tubecast:VideoID={videoId}";
            await Launcher.LaunchUriAsync(new Uri(url));
        }
        private async void OpenLinkTubecast(string link)
        {
            var url = $"tubecast:link={WebUtility.UrlEncode(link)}";
            await Launcher.LaunchUriAsync(new Uri(url));
        }
    }
}
