using AdventuresPlanet.Views;
using AdventuresPlanetRuntime;
using AdventuresPlanetRuntime.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Template10.Common;
using Template10.Mvvm;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace AdventuresPlanet.ViewModels
{
    public class RecensioniPageViewModel : ViewModelBase
    {
        private AVPManager manager;
        private AVPDatabase db;
        private AVPPreferiti prefs;
        public RecensioniPageViewModel(AVPManager m, AVPDatabase d, AVPPreferiti p)
        {
            db = d;
            manager = m;
            prefs = p;

            ListaRecensioni = new Dictionary<string, ObservableCollection<RecensioneItem>>();
            ListaRecensioni.Add("#", new ObservableCollection<RecensioneItem>());
            for (char c = 'A'; c <= 'Z'; c++)
                ListaRecensioni.Add(c.ToString(), new ObservableCollection<RecensioneItem>());
        }
        private DataTransferManager _dataTransferManager;
        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            IsParameterOpen = false; //reset parametro
            Task aggiornaTask = null, loadTask = null;
            NavigationService.FrameFacade.BackRequested += FrameFacade_BackRequested;
            _dataTransferManager = DataTransferManager.GetForCurrentView();
            _dataTransferManager.DataRequested += OnShareRequested;
            if (IsListaRecensioniEmpty())
                loadTask = CaricaRecensioniDaDatabase();
            if (IsToUpdateByTime())
            {
                if (loadTask != null) await loadTask;
                aggiornaTask = AggiornaRecensioni();
            }

            if (mode == NavigationMode.Back | mode == NavigationMode.Forward)
            {
                if (RecensioneSelezionata != null)
                    CaricaPosizione();
                else if (state.Any())
                    RecensioneSelezionata = FindRecensioneById(state["RecensioneId"]?.ToString());
            }
            else
            {
                if (parameter is RecensioneItem)
                    SelezionaRecensione.Execute(parameter);
                else if (parameter is string && AVPManager.IsRecensione(parameter.ToString()))
                {
                    if (loadTask != null) await loadTask;
                    if (aggiornaTask != null) await aggiornaTask;
                    var found = FindRecensioneByLink(parameter.ToString().Replace(AVPManager.URL_BASE, ""));
                    IsParameterOpen = true;
                    if (found != null)
                    {
                        RecensioneSelezionata = found;
                    }
                    else
                    {
                        IsRecensioneSelezionata = true;
                        IsRecensioneDownload = true;
                        RecensioneSelezionata = await manager.LoadRecensione(parameter.ToString());
                        IsRecensioneDownload = false;
                    }
                }
            }
        }
        private static long TIME_UPDATE = 86400; //1 giorno
        public bool IsToUpdateByTime()
        {
            if (Utils.TimeUtils.GetUnixTimestamp() - manager.UpdateTimeRecensioni > TIME_UPDATE)
                return true;
            return false;
        }
        private Task CaricaRecensioniDaDatabase()
        {
            return Task.Run(() =>
            {
                WindowWrapper.Current().Dispatcher.Dispatch(() =>
                {
                    IsCaricaRecensioni = true;
                });
                var recensioni = db.SelectAllRecensioni();
                InsertAction.Invoke(recensioni);
                WindowWrapper.Current().Dispatcher.Dispatch(() =>
                {
                    IsCaricaRecensioni = false;
                });
            });
        }
        private RecensioneItem FindRecensioneById(string id)
        {
            foreach (var item in ListaRecensioni.Values)
            {
                var found = item.Where(x => x.Id.CompareTo(id) == 0);
                if (found?.Count() > 0)
                    return found.ElementAt(0);
            }
            return null;
        }
        private bool IsParameterOpen;
        private RecensioneItem FindRecensioneByLink(string link)
        {
            foreach (var item in ListaRecensioni.Values)
            {
                var listFound = item.Where(x => x.Link.CompareTo(link) == 0);
                if (listFound?.Count() > 0)
                    return listFound.ElementAt(0);
            }
            return null;
        }
        private bool IsListaRecensioniEmpty()
        {
            foreach (var item in ListaRecensioni)
            {
                if (item.Value.Count > 0)
                    return false;
            }
            return true;
        }
        public override Task OnNavigatedFromAsync(IDictionary<string, object> pageState, bool suspending)
        {
            pageState["RecensioneId"] = RecensioneSelezionata?.Id;
            NavigationService.FrameFacade.BackRequested -= FrameFacade_BackRequested;
            _dataTransferManager.DataRequested -= OnShareRequested;
            return base.OnNavigatedFromAsync(pageState, suspending);
        }
        private void FrameFacade_BackRequested(object sender, Template10.Common.HandledEventArgs e)
        {
            e.Handled = true;
            if (IsRecensioneSelezionata)
                ChiudiRecensione();
            else if (IsCercaRecensione)
                IsCercaRecensione = false;
            else if (NavigationService.CanGoBack)
                NavigationService.GoBack();
            if (IsParameterOpen)
            {
                IsParameterOpen = false;
                if (NavigationService.CanGoBack)
                    NavigationService.GoBack();
            }
        }
        private bool _isCaricaRecensioni, _isCercaRecensione;
        public bool IsCaricaRecensioni { get { return _isCaricaRecensioni; } set { Set(ref _isCaricaRecensioni, value); } }
        public bool IsCercaRecensione { get { return _isCercaRecensione; } set { Set(ref _isCercaRecensione, value); } }
        public Dictionary<string, ObservableCollection<RecensioneItem>> ListaRecensioni { get; }
        private DelegateCommand _aggiornaRece;
        public DelegateCommand AggiornaListaRecensioni =>
            _aggiornaRece ??
            (_aggiornaRece = new DelegateCommand(() =>
            {
                if (!IsCaricaRecensioni)
                    AggiornaRecensioni();
            }));
        private Action<IEnumerable<RecensioneItem>> _insertAction;
        private Action<IEnumerable<RecensioneItem>> InsertAction
        {
            get
            {
                return _insertAction ??
                    (_insertAction = (list) =>
                    {
                        WindowWrapper.Current().Dispatcher.Dispatch(() =>
                        {
                            bool wasEmpty = IsListaRecensioniEmpty();
                            foreach (var item in list)
                            {
                                string firstChar = item.Titolo.TrimStart().Substring(0, 1).ToUpper();
                                switch (firstChar)
                                {
                                    case "0":
                                    case "1":
                                    case "2":
                                    case "3":
                                    case "4":
                                    case "5":
                                    case "6":
                                    case "7":
                                    case "8":
                                    case "9":
                                        firstChar = "#";
                                        break;
                                }
                                if (wasEmpty)
                                    ListaRecensioni[firstChar].Add(item);
                                else
                                {
                                    ObservableCollection<RecensioneItem> currList = ListaRecensioni[firstChar];
                                    int count = currList.Count;
                                    bool added = false;
                                    for (int i = 0; i < count && !added; i++)
                                    {
                                        var currItem = currList[i];
                                        if (item.Titolo.CompareTo(currItem.Titolo) <= 0)
                                        {
                                            currList.Insert(i, item);
                                            added = true;
                                        }
                                    }
                                    if (!added)
                                        currList.Add(item);
                                }
                                RaisePropertyChanged(() => ListaRecensioni);
                            }
                        });
                    });
            }
        }
        private async Task AggiornaRecensioni()
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                IsCaricaRecensioni = true;
            });
            manager.UpdateTimeRecensioni = await manager.LoadListRecensioni(InsertAction, 
                                                                           (list)=> { db.InsertAll(list); }, 
                                                                           manager.UpdateTimeRecensioni);
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                IsCaricaRecensioni = false;
            });
        }
        private bool _isReceSel;
        public bool IsRecensioneSelezionata { get { return _isReceSel; } set { Set(ref _isReceSel, value); } }
        private RecensioneItem _receSelezionata;
        public RecensioneItem RecensioneSelezionata
        {
            get { return _receSelezionata; }
            set
            {
                Set(ref _receSelezionata, value);
                if (value != null)
                {
                    IsCercaRecensione = false;
                    IsRecensioneSelezionata = true;
                    ComponiRecensione();
                    CaricaPosizione();
                    RaisePropertyChanged(() => IsPreferita);
                }
                else
                    IsRecensioneSelezionata = false;
            }
        }
        private DelegateCommand<RecensioneItem> _selezionaRecensione;
        public DelegateCommand<RecensioneItem> SelezionaRecensione =>
            _selezionaRecensione ??
            (_selezionaRecensione = new DelegateCommand<RecensioneItem>((x =>
            {
                if (x.IsVideo)
                    NavigationService.Navigate(typeof(VideoPlayerPage), x);
                else
                    RecensioneSelezionata = x;
            })));
        public ObservableCollection<FrameworkElement> ListaComponenti { get; } = new ObservableCollection<FrameworkElement>();
        private void ChiudiRecensione()
        {
            RecensioneSelezionata = null;
            ListaComponenti?.Clear();
        }
        private async void ComponiRecensione()
        {
            ListaComponenti?.Clear();
            await ScaricaRecensione();
            if (!string.IsNullOrEmpty(RecensioneSelezionata.TestoBreve))
            {
                TextBlock inBreve = new TextBlock()
                {
                    Text = "In breve",
                    FontWeight = FontWeights.Bold
                };
                StackPanel rec_breve_cont = new StackPanel()
                {
                    Margin = new Thickness(0, 8, 0, 8),
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(0, 2, 0, 2)
                };
                TextBlock rec_breve = new TextBlock()
                {
                    Margin = new Thickness(0, 8, 0, 8),
                    Text = RecensioneSelezionata.TestoBreve,
                    TextWrapping = TextWrapping.Wrap
                };
                rec_breve_cont.Children.Add(rec_breve);
                ListaComponenti.Add(inBreve);
                ListaComponenti.Add(rec_breve_cont);
            }
            List<string> componenti = RecensioneSelezionata.TestoRich;
            if (componenti == null || componenti.Count == 0)
            {
                if (!string.IsNullOrEmpty(RecensioneSelezionata.Testo?.Trim()))
                {
                    TextBlock tb = new TextBlock();
                    tb.TextWrapping = TextWrapping.Wrap;
                    tb.Text = RecensioneSelezionata.Testo;
                    ListaComponenti.Add(tb);
                    return;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(RecensioneSelezionata.AutoreText.Trim()))
                {
                    TextBlock tb = new TextBlock()
                    {
                        Text = $"recensione a cura di {RecensioneSelezionata.AutoreText}",
                        TextWrapping = TextWrapping.Wrap,
                        FontStyle = FontStyle.Italic,
                        FontSize = 12,
                        Margin = new Thickness(0, 4, 0, 8)
                    };
                    ListaComponenti.Add(tb);
                }
                foreach (string s in componenti)
                {
                    if (s.StartsWith("@TEXT"))
                    {
                        string testo = s.Replace("@TEXT", "");
                        TextBlock tb = new TextBlock()
                        {
                            TextWrapping = TextWrapping.Wrap,
                            Text = testo,
                            Margin = new Thickness(0, 4, 0, 4)
                        };
                        tb.Tapped += (send, e) =>
                        {
                            SalvaPosizione(ListaComponenti.IndexOf(tb));
                        };
                        ListaComponenti.Add(tb);
                    }
                    else if (s.StartsWith("@DIVIDER"))
                    {
                        /*
                        string testo = s.Replace("@DIVIDER", "");
                        TextBlock tb = new TextBlock();
                        tb.TextWrapping = TextWrapping.Wrap;
                        tb.Text = testo;
                        ListaComponenti.Add(tb);
                        */
                    }
                    else if (s.StartsWith("@IMG"))
                    {
                        string url = s.Replace("@IMG", "");
                        BitmapImage img_source = new BitmapImage(new Uri(url));
                        Image immagine = new Image()
                        {
                            Source = img_source,
                            MaxWidth = 640,
                            MaxHeight = 480
                        };
                        immagine.Tapped += (send, e) =>
                        {
                            SalvaPosizione(ListaComponenti.IndexOf(immagine));
                        };
                        immagine.DoubleTapped += (sender, e) =>
                        {
                            SalvaPosizione(ListaComponenti.IndexOf(immagine));
                            NavigationService.Navigate(typeof(Views.ImageViewerPage), url);
                        };
                        ListaComponenti.Add(immagine);
                    }
                }
                string textVoto = RecensioneSelezionata.VotoInt == 0 ? "N.D." : $"{RecensioneSelezionata.VotoInt}/100";
                TextBlock votoTb = new TextBlock()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Text = $"Voto : {textVoto}",
                    Margin = new Thickness(0, 8, 0, 0)
                };
                ListaComponenti.Add(votoTb);
                if (!string.IsNullOrEmpty(RecensioneSelezionata.VotoUtentiText))
                {
                    TextBlock votoUtenti = new TextBlock()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        Text = $"Utenti : {RecensioneSelezionata.VotoUtentiText}/100",
                        Margin = new Thickness(0, 8, 0, 0)
                    };
                    ListaComponenti.Add(votoUtenti);
                }
            }

        }
        private bool _isRecensioneDownload;
        public bool IsRecensioneDownload { get { return _isRecensioneDownload; } set { Set(ref _isRecensioneDownload, value); } }
        private async Task ScaricaRecensione()
        {
            if (string.IsNullOrEmpty(RecensioneSelezionata.Testo))
            {
                WindowWrapper.Current().Dispatcher.Dispatch(() =>
                {
                    IsRecensioneDownload = true;
                });
                if (!await manager.LoadRecensione(RecensioneSelezionata))
                    new MessageDialog("Si è verificato un errore durante il caricamento della recensione").ShowAsync();
                else
                    db.Update(RecensioneSelezionata);
                WindowWrapper.Current().Dispatcher.Dispatch(() =>
                {
                    IsRecensioneDownload = false;
                });
            }
        }
        private ApplicationDataContainer appData = ApplicationData.Current.RoamingSettings;
        public int RecensioneLoadPositionIndex { get; set; }
        public void SalvaPosizione(int pos)
        {
            appData.Values[$"rec_pos_{RecensioneSelezionata.Id}"] = pos;
        }
        private void CaricaPosizione()
        {
            RecensioneLoadPositionIndex = appData.Values.ContainsKey($"rec_pos_{RecensioneSelezionata.Id}")
                ? (int)appData.Values[$"rec_pos_{RecensioneSelezionata.Id}"]
                : 0;
            RaisePropertyChanged(() => RecensioneLoadPositionIndex);
        }
        private DelegateCommand _storeCmd, _shareCmd;
        public DelegateCommand OpenStore =>
            _storeCmd ??
            (_storeCmd = new DelegateCommand(() =>
            {
                if (!string.IsNullOrEmpty(RecensioneSelezionata.LinkStore))
                    Launcher.LaunchUriAsync(new Uri(RecensioneSelezionata.LinkStore));
            }));
        public DelegateCommand CondividiCommand =>
            _shareCmd ??
            (_shareCmd = new DelegateCommand(() =>
            {
                DataTransferManager.ShowShareUI();
            }));
        private void OnShareRequested(DataTransferManager sender, DataRequestedEventArgs e)
        {
            e.Request.Data.Properties.Title = $"Leggi la recensione di {RecensioneSelezionata.Titolo} su adventuresplanet.it";
            e.Request.Data.SetWebLink(new Uri($"{AVPManager.URL_BASE}{RecensioneSelezionata.Link}"));
        }
        private DelegateCommand<string> _searchCommand;
        private ObservableCollection<RecensioneItem> _searchColl;
        public ObservableCollection<RecensioneItem> ListaSearch { get { return _searchColl; } set { Set(ref _searchColl, value); } }
        public DelegateCommand<string> OnSearchText =>
            _searchCommand ??
            (_searchCommand = new DelegateCommand<string>((text) =>
            {
                text = text.Trim();
                if (string.IsNullOrEmpty(text))
                    ListaSearch?.Clear();
                else
                    ListaSearch = new ObservableCollection<RecensioneItem>(CercaRecensioni(text));
            }));
        private List<RecensioneItem> CercaRecensioni(string text)
        {
            List<RecensioneItem> founds = new List<RecensioneItem>();
            foreach(var coll in ListaRecensioni.Values)
            {
                var f = coll.Where(x => x.Titolo.ToLower().Contains(text.ToLower()));
                if (f != null && f.Any())
                    founds.AddRange(f);
            }
            return founds;
        }
        private DelegateCommand _toggleSearch, _togglePreferiti;
        public DelegateCommand ToggleSearch =>
            _toggleSearch ??
            (_toggleSearch = new DelegateCommand(() =>
            {
                IsCercaRecensione = !IsCercaRecensione;
            }));
        public DelegateCommand TogglePreferitiCommand =>
            _togglePreferiti ??
            (_togglePreferiti = new DelegateCommand(() =>
            {
                if (prefs.IsPreferita(RecensioneSelezionata.Id))
                    prefs.RimuoviPreferiti(RecensioneSelezionata.Id);
                else
                    prefs.AggiungiPreferiti(RecensioneSelezionata.Id);
                RaisePropertyChanged(() => IsPreferita);
            }));
        public bool IsPreferita
        {
            get
            {
                return prefs.IsPreferita(RecensioneSelezionata?.Id);
            }
        }
    }
}
