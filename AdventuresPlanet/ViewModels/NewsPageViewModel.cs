using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Data;
using Template10.Common;
using Windows.UI.Popups;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.System;
using Template10.Mvvm;
using AdventuresPlanetRuntime;
using AdventuresPlanetRuntime.Data;
using AdventuresPlanet.Views;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Utils;

namespace AdventuresPlanet.ViewModels
{
    public class NewsPageViewModel : ViewModelBase
    {
        private AVPManager manager;
        private AVPDatabase db;
        public NewsPageViewModel(AVPManager avp, AVPDatabase d)
        {
            manager = avp;
            db = d;
            ListaNews = new NewsCollection(manager, db);
        }
        private DataTransferManager _dataTransferManager;
        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            if (mode == NavigationMode.Back | mode == NavigationMode.Forward)
            {
                if (state.Any())
                {
                    var news = ListaNews.Where(x => x.Link.CompareTo(state["NewsLink"]?.ToString()) == 0);
                    if (news?.Count() == 1)
                        NewsSelezionata = news.ElementAt(0);
                }
            }
            else if (parameter != null)
            {
                if (parameter is News)
                {
                    NewsSelezionata = parameter as News;
                }
                else if (parameter is string)
                {
                    if(Uri.IsWellFormedUriString(parameter.ToString(), UriKind.RelativeOrAbsolute))
                        NewsSelezionata = await manager.LoadNews(parameter.ToString());
                    else if (parameter.ToString().CompareTo("forzaAggiornamento") == 0)
                    {

                    }
                }
            }
            NavigationService.FrameFacade.BackRequested += FrameFacade_BackRequested;
            _dataTransferManager = DataTransferManager.GetForCurrentView();
            _dataTransferManager.DataRequested += OnShareRequested;
        }
        public override Task OnNavigatedFromAsync(IDictionary<string, object> pageState, bool suspending)
        {
            pageState["NewsLink"] = NewsSelezionata?.Link;
            NavigationService.FrameFacade.BackRequested -= FrameFacade_BackRequested;
            _dataTransferManager.DataRequested -= OnShareRequested;
            return base.OnNavigatedFromAsync(pageState, suspending);
        }
        private void OnShareRequested(DataTransferManager sender, DataRequestedEventArgs e)
        {
            e.Request.Data.Properties.Title = NewsSelezionata.Titolo;
            e.Request.Data.Properties.Description = NewsSelezionata.AnteprimaNews;
            e.Request.Data.SetWebLink(new Uri(NewsSelezionata.Link));
        }
        private MessageDialog CloseDialog;
        private async void FrameFacade_BackRequested(object sender, HandledEventArgs e)
        {
            e.Handled = true;
            if (NewsSelezionata!=null)
                NewsSelezionata = null;
            else
            {
                if (CloseDialog == null)
                {
                    CloseDialog = new MessageDialog("Vuoi uscire dall'applicazione?") { CancelCommandIndex = 1, DefaultCommandIndex = 1 };
                    CloseDialog.Commands.Add(new UICommand("Si", (c) => { App.Current.Exit(); }));
                    CloseDialog.Commands.Add(new UICommand("No"));
                }
                await CloseDialog.ShowAsync();
            }
        }
        private DelegateCommand _shareCmd, _openInBrowserCmd;
        public DelegateCommand ShareCommand =>
            _shareCmd ??
            (_shareCmd = new DelegateCommand(() =>
            {
                DataTransferManager.ShowShareUI();
            }));
        public DelegateCommand OpenWebBrowserCommand =>
            _openInBrowserCmd ??
            (_openInBrowserCmd = new DelegateCommand(async () =>
            {
                await Launcher.LaunchUriAsync(new Uri(NewsSelezionata.Link));
            }));
        public NewsCollection ListaNews { get; private set; }
        private DelegateCommand<News> _selezionaNews;
        public DelegateCommand<News> SelezionaNews =>
            _selezionaNews ??
            (_selezionaNews = new DelegateCommand<News>((x) =>
            {
                if (AVPManager.IsRecensione(x.Link))
                {
                    NewsSelezionata = null;
                    NavigationService.Navigate(typeof(RecensioniPage), x.Link);
                }
                else if (AVPManager.IsSoluzione(x.Link))
                {
                    NewsSelezionata = null;
                    NavigationService.Navigate(typeof(SoluzioniPage), x.Link);
                }
                else if (AVPManager.IsGalleriaImmagini(x.Link))
                {
                    NewsSelezionata = null;
                    NavigationService.Navigate(typeof(GalleriePage), x.Link);
                }
                else if (AVPManager.IsPodcast(x.Link))
                {
                    NewsSelezionata = null;
                    NavigationService.Navigate(typeof(PodcastPage), x.Link);
                }
                else
                    NewsSelezionata = x;
            }));
        private bool _isNewsSelezionata, _isCaricaNews;
        private News _newsSelezionata;
        public bool IsNewsSelezionata { get { return NewsSelezionata!=null; } }
        public bool IsCaricaNews { get { return _isCaricaNews; } set { Set(ref _isCaricaNews, value); } }
        public News NewsSelezionata
        {
            get { return _newsSelezionata; }
            set
            {
                Set(ref _newsSelezionata, value);
                RaisePropertyChanged(() => IsNewsSelezionata);
                if (value != null)
                {
                    componiNews();
                }
            }
        }
        private async Task CaricaNews()
        {
            if (string.IsNullOrEmpty(NewsSelezionata.CorpoNews))
            {
                WindowWrapper.Current().Dispatcher.Dispatch(() =>
                {
                    IsCaricaNews = true;
                });
                if (!await manager.LoadNews(NewsSelezionata))
                    await new MessageDialog("Si è verificato un errore").ShowAsync();
                WindowWrapper.Current().Dispatcher.Dispatch(() =>
                {
                    IsCaricaNews = false;
                });
            }
        }
        public ObservableCollection<FrameworkElement> ListComponents { get; } = new ObservableCollection<FrameworkElement>();
        private async void componiNews()
        {
            ListComponents.Clear();
            await CaricaNews();
            if (NewsSelezionata?.TestoRich != null && NewsSelezionata?.TestoRich.Count > 0)
            {
                TextBlock c_news = new TextBlock();
                c_news.TextWrapping = TextWrapping.Wrap;
                foreach (string s in NewsSelezionata.TestoRich)
                {
                    if (s.StartsWith("@TEXT"))
                    {
                        Run tb = new Run();
                        tb.Text = s.Replace("@TEXT", "") + " ";
                        c_news.Inlines.Add(tb);
                    }
                    else if (s.StartsWith("@BOLD"))
                    {
                        Run tb = new Run();
                        tb.Text = s.Replace("@BOLD", "") + " ";
                        tb.FontWeight = FontWeights.Bold;
                        c_news.Inlines.Add(tb);
                    }
                    else if (s.StartsWith("@ITALIC"))
                    {
                        Run tb = new Run();
                        tb.Text = s.Replace("@ITALIC", "") + " ";
                        tb.FontStyle = FontStyle.Italic;
                        c_news.Inlines.Add(tb);
                    }
                    else if (s.StartsWith("@DIVIDER"))
                    {
                        Run tb = new Run();
                        tb.Text = "\n";
                        c_news.Inlines.Add(tb);
                    }
                    else if (s.StartsWith("@ANCHOR;"))
                    {

                        string t = s.Replace("@ANCHOR;", "");
                        string link = t.Substring(t.IndexOf("link=") + "link=".Length, t.IndexOf(";text=") - ";text".Length);
                        string text = t.Substring(t.IndexOf(";text=") + ";text=".Length);
                        Hyperlink tb = new Hyperlink();
                        tb.Click += async (sender, e) =>
                        {
                            if (AVPManager.IsSoluzione(link))
                                NavigationService.Navigate(typeof(SoluzioniPage), link);
                            else if (AVPManager.IsRecensione(link))
                                NavigationService.Navigate(typeof(RecensioniPage), link);
                            else if (AVPManager.IsPodcast(link))
                            {
                                if(link.Equals(AVPManager.URL_BASE + "podcast.php"))
                                    NavigationService.Navigate(typeof(PodcastPage));
                                else
                                    NavigationService.Navigate(typeof(PodcastPage), link);
                            }
                            else if (AVPManager.IsGalleriaImmagini(link))
                                NavigationService.Navigate(typeof(GalleriePage), link);
                            else if (AVPManager.IsTrailer(link) || AVPManager.IsExtra(link))
                                NavigationService.Navigate(typeof(VideoPlayerPage), link);
                            else if (AVPManager.IsSaga(link))
                                NavigationService.Navigate(typeof(SagaPage), link);
                            else
                                await Launcher.LaunchUriAsync(new Uri(link));
                        };
                        Run link_text = new Run();
                        link_text.Text = text;
                        link_text.FontWeight = FontWeights.Bold;

                        if (AVPManager.IsSoluzione(link) || AVPManager.IsRecensione(link) || AVPManager.IsPodcast(link) || AVPManager.IsGalleriaImmagini(link) || AVPManager.IsExtra(link) || AVPManager.IsTrailer(link) || AVPManager.IsSaga(link))
                        {
                            link_text.Foreground = new SolidColorBrush(Colors.Orange);
                        }
                        else
                        {
                            link_text.Foreground = new SolidColorBrush(Colors.ForestGreen);
                        }
                        tb.Inlines.Add(link_text);

                        Run space = new Run();
                        space.Text = " ";

                        c_news.Inlines.Add(tb);
                        c_news.Inlines.Add(space);
                    }
                }
                ListComponents.Add(c_news);
                RaisePropertyChanged(() => ListComponents);
                RaisePropertyChanged(() => NewsSelezionata);
                RaisePropertyChanged(() => NewsSelezionata.CorpoNews);
                RaisePropertyChanged(() => NewsSelezionata.TestoRich);
            }
            else if (!string.IsNullOrEmpty(NewsSelezionata.CorpoNews))
            {
                TextBlock tCorpo = new TextBlock()
                {
                    TextWrapping = TextWrapping.Wrap,
                    Text = NewsSelezionata.CorpoNews
                };
                ListComponents.Add(tCorpo);
            }
            else
                await new MessageDialog("News vuota").ShowAsync();
        }
        public class NewsCollection : ObservableCollection<News>, ISupportIncrementalLoading
        {
            private bool _loading;
            public bool IsLoading
            {
                get { return _loading; }
                set
                {
                    _loading = value;
                    this.OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(IsLoading)));
                }
            }
            private int currAnno, currMese;
            private AVPManager manager;
            private AVPDatabase db;
            public NewsCollection(AVPManager man, AVPDatabase d)
            {
                manager = man;
                db = d;
                Reset();
            }
            public bool HasMoreItems
            {
                get
                {
                    if (currAnno <= 2004 && currMese < 2)
                        return false;
                    return true;
                }
            }
            private Action<List<News>> _saveNews;
            private Action<News> _addNews;
            private Action<List<News>> SaveNews =>
                _saveNews ??
                (_saveNews = (x) =>
                {
                    db.InsertNews(x);
                });
            private Action<News> AddNews =>
                _addNews ??
                (_addNews = (x) =>
                {
                    WindowWrapper.Current().Dispatcher.Dispatch(() =>
                    {
                        Add(x);
                    });
                });
            private ApplicationDataContainer data = ApplicationData.Current.LocalSettings;
            public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
            {
                return Task.Run<LoadMoreItemsResult>(async () =>
                {
                    WindowWrapper.Current().Dispatcher.Dispatch(() =>
                    {
                        IsLoading = true;
                    });
                    bool found = false;
                    if (data.Values.ContainsKey($"news_{currAnno.ToString("D4")}{currMese.ToString("D2")}"))
                    {
                        var news = db.SelectNews(currAnno, currMese);
                        if (news.Any())
                        {
                            foreach (var item in news)
                                AddNews.Invoke(item);
                            found = true;
                        }
                        else
                        {
                            data.Values.Remove($"news_{currAnno.ToString("D4")}{currMese.ToString("D2")}");
                        }
                    }
                    else
                    {
                        if (currAnno == Now.Year && currMese == Now.Month && !IsToUpdate()) //non è da aggiornare
                        {
                            var dbFound = db.SelectNews(currAnno, currMese);
                            if (dbFound.Any())
                            {
                                foreach (var item in dbFound)
                                    AddNews.Invoke(item);
                                found = true;
                            }
                        }
                        if(!found) //da aggiornare
                        {
                            if (await manager.LoadListNews(currAnno, currMese, AddNews, SaveNews)) //news caricate dal sito web
                            {
                                if (IsMesePersistente(currAnno, currMese))
                                    data.Values[$"news_{currAnno.ToString("D4")}{currMese.ToString("D2")}"] = true;
                                else
                                    manager.UpdateTimeNews = TimeUtils.GetUnixTimestamp();
                                found = true;
                            }
                            else //errore durante il caricamento dal sito web
                            {
                                await Task.Delay(5000);
                            }
                        }
                    }

                    if (found) //cambio periodo dopo aver caricato le news
                    {
                        if (currMese == 1)
                        {
                            currMese = 12;
                            currAnno--;
                        }
                        else
                            currMese--;
                    }
                    WindowWrapper.Current().Dispatcher.Dispatch(() =>
                    {
                        IsLoading = false;
                    });
                    return new LoadMoreItemsResult() { Count = count };

                }).AsAsyncOperation<LoadMoreItemsResult>();
            }
            private DateTime Now;
            public void Reset()
            {
                Clear();
                Now = DateTime.Now;
                currAnno = Now.Year;
                currMese = Now.Month;
            }
            private bool IsToUpdate()
            {
                return TimeUtils.GetUnixTimestamp() > manager.UpdateTimeNews + 3600;
            }
            private bool IsMesePersistente(int anno, int mese)
            {
                if(anno == Now.Year)
                {
                    if (mese == Now.Month)
                        return false;
                }
                return true;
            }
        }
    }
}
