using AdventuresPlanet.Views;
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
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace AdventuresPlanet.ViewModels
{
    public class SoluzioniPageViewModel : ViewModelBase
    {
        private AVPManager manager;
        private AVPDatabase db;
        private AVPPreferiti prefs;
        public SoluzioniPageViewModel(AVPManager m, AVPDatabase d, AVPPreferiti p)
        {
            manager = m;
            db = d;
            prefs = p;
            ListaSoluzioni = new Dictionary<string, ObservableCollection<SoluzioneItem>>();
            ListaSoluzioni.Add("#", new ObservableCollection<SoluzioneItem>());
            for (char c = 'A'; c <= 'Z'; c++)
                ListaSoluzioni.Add(c.ToString(), new ObservableCollection<SoluzioneItem>());
        }
        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            Task aggiornaSolTask = null, loadSolTask = null;
            if (IsListaSoluzioniEmpty())
                loadSolTask = CaricaSoluzioniDaDatabase();
            if (IsToUpdateByTime())
            {
                if (loadSolTask != null) await loadSolTask;
                aggiornaSolTask = AggiornaSoluzioni();
            }
            if (mode == NavigationMode.Back | mode == NavigationMode.Forward)
            {
                if (SoluzioneSelezionata != null)
                    CaricaPosizione();
                else if (state.Any())
                {
                    var sol = FindSoluzioneById(state["SoluzioneId"]?.ToString());
                    SoluzioneSelezionata = sol;
                }
            }
            else
            {
                if (parameter is string && AVPManager.IsSoluzione(parameter.ToString()))
                {
                    IsParameterOpen = true;
                    var linkSol = parameter.ToString().Replace(AVPManager.URL_BASE, "");
                    if (loadSolTask != null) await loadSolTask;
                    if (aggiornaSolTask != null) await aggiornaSolTask;
                    var found = FindSoluzioneByLink(linkSol);
                    if (found != null)
                        SoluzioneSelezionata = found;
                    else
                    {
                        IsSoluzioneDownload = true;
                        IsSoluzioneSelezionata = true;
                        SoluzioneSelezionata = await manager.LoadSoluzione(linkSol);
                        IsSoluzioneDownload = false;
                    }
                }
                else if (parameter is SoluzioneItem)
                    SoluzioneSelezionata = parameter as SoluzioneItem;
            }
            
            NavigationService.FrameFacade.BackRequested += FrameFacade_BackRequested;
            _dataTransferManager = DataTransferManager.GetForCurrentView();
            _dataTransferManager.DataRequested += OnShareRequested;
        }
        private DataTransferManager _dataTransferManager;
        private readonly static long TIME_UPDATE = 86400; //1 giorno
        private bool IsToUpdateByTime()
        {
            if (TimeUtils.GetUnixTimestamp() - manager.UpdateTimeSoluzioni > TIME_UPDATE)
                return true;
            return false;
        }

        private Task CaricaSoluzioniDaDatabase()
        {
            return Task.Run(() =>
            {
                WindowWrapper.Current().Dispatcher.Dispatch(() =>
                {
                    IsCaricaSoluzioni = true;
                });
                var soluzioni = db.SelectAllSoluzioni();
                WindowWrapper.Current().Dispatcher.Dispatch(() =>
                {
                    InsertAction.Invoke(soluzioni);
                    IsCaricaSoluzioni = true;
                });
            });
        }

        private bool IsParameterOpen;
        private SoluzioneItem FindSoluzioneById(string id)
        {
            foreach (var item in ListaSoluzioni.Values)
            {
                var found = item.Where(x => x.Id.CompareTo(id) == 0);
                if (found?.Count() > 0)
                    return found.ElementAt(0);
            }
            return null;
        }
        private SoluzioneItem FindSoluzioneByLink(string link)
        {
            foreach (var item in ListaSoluzioni.Values)
            {
                var found = item.Where(x => x.Link.CompareTo(link) == 0);
                if (found?.Count() > 0)
                    return found.ElementAt(0);
            }
            return null;
        }
        public override Task OnNavigatedFromAsync(IDictionary<string, object> pageState, bool suspending)
        {
            pageState["SoluzioneId"] = SoluzioneSelezionata?.Id;
            NavigationService.FrameFacade.BackRequested -= FrameFacade_BackRequested;
            _dataTransferManager.DataRequested -= OnShareRequested;
            return base.OnNavigatedFromAsync(pageState, suspending);
        }
        private bool IsListaSoluzioniEmpty()
        {
            foreach (var item in ListaSoluzioni)
            {
                if (item.Value.Count > 0)
                    return false;
            }
            return true;
        }
        private void FrameFacade_BackRequested(object sender, Template10.Common.HandledEventArgs e)
        {
            e.Handled = true;
            if (IsSoluzioneSelezionata)
                ChiudiSoluzione();
            else if (IsCercaSoluzione)
                IsCercaSoluzione = false;
            else if (NavigationService.CanGoBack)
                NavigationService.GoBack();

            if (IsParameterOpen)
            {
                IsParameterOpen = false;
                if (NavigationService.CanGoBack)
                    NavigationService.GoBack();
            }
        }

        public Dictionary<string, ObservableCollection<SoluzioneItem>> ListaSoluzioni { get; }
        private bool _isCaricaSol;
        private string _indexSelected;
        public string CurrentPageIndex { get { return _indexSelected; } set { Set(ref _indexSelected, value); } }
        public bool IsCaricaSoluzioni { get { return _isCaricaSol; } set { Set(ref _isCaricaSol, value); } }
        private DelegateCommand _solAggiorna;
        private DelegateCommand<string> _goToIndex;
        public DelegateCommand AggiornaListaSoluzioni =>
            _solAggiorna ??
            (_solAggiorna = new DelegateCommand(() =>
            {
                AggiornaSoluzioni();
            }));
        public DelegateCommand<string> GoToIndex =>
            _goToIndex ??
            (_goToIndex = new DelegateCommand<string>((tagString) =>
            {
                CurrentPageIndex = tagString;
            }));
        private Action<IEnumerable<SoluzioneItem>> _actionInsert;
        public Action<IEnumerable<SoluzioneItem>> InsertAction
        {
            get
            {
                return _actionInsert ??
                    (_actionInsert = (list) =>
                    {
                        bool wasEmpty = IsListaSoluzioniEmpty();
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
                            {
                                ListaSoluzioni[firstChar].Add(item);
                            }
                            else
                            {
                                ObservableCollection<SoluzioneItem> currList = ListaSoluzioni[firstChar];
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
                            RaisePropertyChanged(() => ListaSoluzioni);
                        }
                    });
            }
        }
        private async Task AggiornaSoluzioni()
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                IsCaricaSoluzioni = true;
            });
            manager.UpdateTimeSoluzioni = await manager.LoadListSoluzioni(InsertAction,
                                                                          (list) => { db.InsertAll(list); },
                                                                          manager.UpdateTimeSoluzioni);
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                IsCaricaSoluzioni = false;
            });
        }
        private DelegateCommand<SoluzioneItem> _selezionaSol;
        public DelegateCommand<SoluzioneItem> SelezionaSoluzione =>
            _selezionaSol ??
            (_selezionaSol = new DelegateCommand<SoluzioneItem>((x) =>
            {
                if (x.IsVideo)
                    NavigationService.Navigate(typeof(VideoPlayerPage), x);
                else
                    SoluzioneSelezionata = x;
            }));
        private SoluzioneItem _solSelezionata;
        public SoluzioneItem SoluzioneSelezionata
        {
            get { return _solSelezionata; }
            set
            {
                Set(ref _solSelezionata, value);
                if(value != null)
                {
                    IsCercaSoluzione = false;
                    IsSoluzioneSelezionata = true;
                    ComponiSoluzione();
                    CaricaPosizione();
                    RaisePropertyChanged(() => IsPreferita);
                }
                else
                {
                    IsSoluzioneSelezionata = false;
                }
            }
        }
        private bool _isSolSel;
        public bool IsSoluzioneSelezionata { get { return _isSolSel; } private set { Set(ref _isSolSel, value); } }
        private void ChiudiSoluzione()
        {
            SoluzioneSelezionata = null;
            ListaComponenti?.Clear();
            Indice?.Clear();
        }
        public int SoluzioneLoadPositionIndex { get; set; }
        public ObservableCollection<FrameworkElement> ListaComponenti { get; } = new ObservableCollection<FrameworkElement>();
        public ObservableCollection<KeyValuePair<string, string>> Indice { get; } = new ObservableCollection<KeyValuePair<string, string>>();
        private async void ComponiSoluzione()
        {
            ListaComponenti?.Clear();
            Indice?.Clear();
            await ScaricaSoluzione();
            
            {
                List<string> componenti = SoluzioneSelezionata.TestoRich;
                Indice?.Clear();
                if (componenti == null || componenti.Count == 0)
                {
                    if (!string.IsNullOrEmpty(SoluzioneSelezionata.Testo?.Trim()))
                    {
                        TextBlock tb = new TextBlock();
                        tb.TextWrapping = TextWrapping.Wrap;
                        tb.Text = SoluzioneSelezionata.Testo;
                        ListaComponenti.Add(tb);
                        return;
                    }
                    else
                    {
                        Launcher.LaunchUriAsync(new Uri(SoluzioneSelezionata.Link));
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(SoluzioneSelezionata.AutoreText.Trim()))
                    {
                        TextBlock tb = new TextBlock()
                        {
                            Text = $"soluzione a cura di {SoluzioneSelezionata.AutoreText}",
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
                        else if (s.StartsWith("@POSINDEX"))
                        {
                            string[] split = s.Split(new char[] { ';' });
                            string index = split[1].Substring(5);
                            string titolo = "\n" + split[2].Substring(6);
                            TextBlock tb = new TextBlock()
                            {
                                Text = titolo,
                                FontWeight = FontWeights.Bold,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                TextWrapping = TextWrapping.Wrap,
                                Tag = index
                            };
                            tb.Tapped += (send, e) =>
                            {
                                SalvaPosizione(ListaComponenti.IndexOf(tb));
                            };
                            ListaComponenti.Add(tb);
                        }
                        else if (s.StartsWith("@INDEX"))
                        {
                            string[] split = s.Split(new char[] { ';' });
                            string index = split[1].Substring(5);
                            string titolo = split[2].Substring(6);
                            Indice.Add(new KeyValuePair<string, string>(titolo, index));
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
                }
            }
        }
        private bool _isSolDownload;
        public bool IsSoluzioneDownload { get { return _isSolDownload; } set { Set(ref _isSolDownload, value); } }
        private async Task ScaricaSoluzione()
        {
            if (string.IsNullOrEmpty(SoluzioneSelezionata.Testo))
            {
                WindowWrapper.Current().Dispatcher.Dispatch(() =>
                {
                    IsSoluzioneDownload = true;
                });
                if (!await manager.LoadSoluzione(SoluzioneSelezionata))
                    new MessageDialog("Si è verificato un errore durante il caricamento della recensione").ShowAsync();
                else
                    db.Update(SoluzioneSelezionata);
                WindowWrapper.Current().Dispatcher.Dispatch(() =>
                {
                    IsSoluzioneDownload = false;
                });
            }
        }
        private ApplicationDataContainer appData = ApplicationData.Current.RoamingSettings;
        public void SalvaPosizione(int pos)
        {
            appData.Values[$"sol_pos_{SoluzioneSelezionata.Id}"] = pos;
        }
        private void CaricaPosizione()
        {
            SoluzioneLoadPositionIndex = appData.Values.ContainsKey($"sol_pos_{SoluzioneSelezionata.Id}") 
                ? (int)appData.Values[$"sol_pos_{SoluzioneSelezionata.Id}"]
                : 0;
            RaisePropertyChanged(() => SoluzioneLoadPositionIndex);
        }
        private DelegateCommand _storeCmd, _shareCmd;
        public DelegateCommand OpenStore =>
            _storeCmd ??
            (_storeCmd = new DelegateCommand(() =>
            {
                if (!string.IsNullOrEmpty(SoluzioneSelezionata.LinkStore))
                    Launcher.LaunchUriAsync(new Uri(SoluzioneSelezionata.LinkStore));
            }));
        public DelegateCommand CondividiCommand =>
            _shareCmd ??
            (_shareCmd = new DelegateCommand(() =>
            {
                DataTransferManager.ShowShareUI();
            }));
        private void OnShareRequested(DataTransferManager sender, DataRequestedEventArgs e)
        {
            e.Request.Data.Properties.Title = $"Leggi la soluzione di {SoluzioneSelezionata.Titolo} su adventuresplanet.it";
            e.Request.Data.SetWebLink(new Uri($"{AVPManager.URL_BASE}{SoluzioneSelezionata.Link}"));
        }
        private DelegateCommand<string> _searchCommand;
        private ObservableCollection<SoluzioneItem> _searchColl;
        public ObservableCollection<SoluzioneItem> ListaSearch { get { return _searchColl; } set { Set(ref _searchColl, value); } }
        public DelegateCommand<string> OnSearchText =>
            _searchCommand ??
            (_searchCommand = new DelegateCommand<string>((text) =>
            {
                text = text.Trim();
                if (string.IsNullOrEmpty(text))
                    ListaSearch?.Clear();
                else
                    ListaSearch = new ObservableCollection<SoluzioneItem>(CercaSoluzioni(text));
            }));
        private List<SoluzioneItem> CercaSoluzioni(string text)
        {
            List<SoluzioneItem> founds = new List<SoluzioneItem>();
            foreach (var coll in ListaSoluzioni.Values)
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
                IsCercaSoluzione = !IsCercaSoluzione;
            }));
        private bool _isCercaSol;
        public bool IsCercaSoluzione { get { return _isCercaSol; } set { Set(ref _isCercaSol, value); } }
        public DelegateCommand TogglePreferitiCommand =>
            _togglePreferiti ??
            (_togglePreferiti = new DelegateCommand(() =>
            {
                if (prefs.IsPreferita(SoluzioneSelezionata.Id))
                    prefs.RimuoviPreferiti(SoluzioneSelezionata.Id);
                else
                    prefs.AggiungiPreferiti(SoluzioneSelezionata.Id);
                RaisePropertyChanged(() => IsPreferita);
            }));
        public bool IsPreferita
        {
            get
            {
                return prefs.IsPreferita(SoluzioneSelezionata?.Id);
            }
        }
    }
    
}
