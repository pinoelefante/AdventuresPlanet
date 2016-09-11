using AdventuresPlanetRuntime;
using AdventuresPlanetRuntime.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template10.Common;
using Template10.Mvvm;
using Utils;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Navigation;

namespace AdventuresPlanet.ViewModels
{
    public class GalleriaPageViewModel : ViewModelBase
    {
        private AVPManager manager;
        private AVPDatabase db;
        public GalleriaPageViewModel(AVPManager m, AVPDatabase d)
        {
            manager = m;
            db = d;
            ListaGallerie = new Dictionary<string, ObservableCollection<GalleriaItem>>();
            ListaGallerie.Add("#", new ObservableCollection<GalleriaItem>());
            for (char c = 'A'; c <= 'Z'; c++)
                ListaGallerie.Add(c.ToString(), new ObservableCollection<GalleriaItem>());
            Immagini = new ImagesCollection(m);
        }
        private DataTransferManager _dataTransferManager;
        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            Task taskAggiorna = null;
            if (IsListaGallerieEmpty())
                CaricaGallerieDaDatabase();
            if(IsUpdateByTime())
                taskAggiorna = AggiornaGallerie();
            if(mode == NavigationMode.Back | mode == NavigationMode.Forward)
            {
                if(GalleriaSelezionata == null)
                {
                    if (state.Any() && state.ContainsKey("GalleriaId"))
                        GalleriaSelezionata = FindGalleriaById(state["GalleriaId"].ToString());
                }
            }
            if(parameter != null)
            {
                if (parameter is GalleriaItem)
                    GalleriaSelezionata = parameter as GalleriaItem;
                else if(parameter is string && AVPManager.IsGalleriaImmagini(parameter.ToString()))
                {
                    if (taskAggiorna != null)
                        await taskAggiorna;
                    var link_gall = UrlUtils.GetUrlParameterValue(parameter.ToString(), "game");
                    var found = FindGalleriaById(link_gall);
                    if(found != null)
                    {
                        GalleriaSelezionata = found;
                    }
                    else
                    {
                        IsGalleriaSelezionata = true;
                        GalleriaSelezionata = new GalleriaItem()
                        {
                            IdGalleria = link_gall
                        };
                    }
                    IsParameterLoad = true;
                }
            }
            NavigationService.FrameFacade.BackRequested += FrameFacade_BackRequested;
            _dataTransferManager = DataTransferManager.GetForCurrentView();
            _dataTransferManager.DataRequested += OnShareRequested;
        }

        private readonly static long TIME_UPDATE = 86400;//1 giorno
        private bool IsUpdateByTime()
        {
            if (TimeUtils.GetUnixTimestamp() - manager.UpdateTimeGallerie > TIME_UPDATE)
                return true;
            return false;
        }

        private void CaricaGallerieDaDatabase()
        {
            var gallerie = db.SelectAllGallerie();
            InsertAction?.Invoke(gallerie);
        }

        private bool IsParameterLoad;
        private GalleriaItem FindGalleriaById(string id)
        {
            foreach (var item in ListaGallerie.Values)
            {
                var found = item.Where(x => x.IdGalleria.CompareTo(id) == 0);
                if (found?.Count() > 0)
                    return found.ElementAt(0);
            }
            return null;
        }
        private void FrameFacade_BackRequested(object sender, HandledEventArgs e)
        {
            e.Handled = true;
            if (IsGalleriaSelezionata)
                IsGalleriaSelezionata = false;
            else if (NavigationService.CanGoBack)
                NavigationService.GoBack();
            if (IsParameterLoad)
            {
                IsParameterLoad = false;
                if (NavigationService.CanGoBack)
                    NavigationService.GoBack();
            }
        }

        public override Task OnNavigatedFromAsync(IDictionary<string, object> pageState, bool suspending)
        {
            pageState["GalleriaId"] = GalleriaSelezionata?.IdGalleria;
            NavigationService.FrameFacade.BackRequested -= FrameFacade_BackRequested;
            _dataTransferManager.DataRequested += OnShareRequested;
            return base.OnNavigatedFromAsync(pageState, suspending);
        }
        private bool IsListaGallerieEmpty()
        {
            foreach (var item in ListaGallerie)
            {
                if (item.Value.Count > 0)
                    return false;
            }
            return true;
        }
        private bool _isCaricaGall;
        public bool IsCaricaGallerie { get { return _isCaricaGall; } set { Set(ref _isCaricaGall, value); } }
        public Dictionary<string, ObservableCollection<GalleriaItem>> ListaGallerie { get; }
        private Action<IEnumerable<GalleriaItem>> _actionAdd;
        public Action<IEnumerable<GalleriaItem>> InsertAction
        {
            get
            {
                return _actionAdd ??
                    (_actionAdd = (list) =>
                    {
                        bool wasEmpty = IsListaGallerieEmpty();
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
                                ListaGallerie[firstChar].Add(item);
                            }
                            else
                            {
                                ObservableCollection<GalleriaItem> currList = ListaGallerie[firstChar];
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
                            RaisePropertyChanged(() => ListaGallerie);
                        }
                    });
            }
        }
        private async Task AggiornaGallerie()
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                IsCaricaGallerie = true;
            });
            manager.UpdateTimeGallerie = await manager.LoadListGallerie(InsertAction, (list) => { db.InsertAll(list); } ,manager.UpdateTimeGallerie);
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                IsCaricaGallerie = false;
            });
        }
        private bool _isGalleriaSel;
        public bool IsGalleriaSelezionata { get { return _isGalleriaSel; } set { Set(ref _isGalleriaSel, value); } }
        private ImagesCollection _imgsColl;
        public ImagesCollection Immagini { get { return _imgsColl; } set { Set(ref _imgsColl, value); } }
        private GalleriaItem _selectedGall;
        public GalleriaItem GalleriaSelezionata
        {
            get { return _selectedGall; }
            set
            {
                Set(ref _selectedGall, value);
                IsGalleriaSelezionata = value != null;
                Immagini.Galleria = value;
            }
        }
        private DelegateCommand<GalleriaItem> _selezionaGallCmd;
        public DelegateCommand<GalleriaItem> SelezionaGalleriaCommand =>
            _selezionaGallCmd ??
            (_selezionaGallCmd = new DelegateCommand<GalleriaItem>((x) =>
            {
                GalleriaSelezionata = x;
            }));
        private bool _isImageSelected;
        public bool IsImmagineSelezionata { get { return _isImageSelected; } set { Set(ref _isImageSelected, value); } }
        private AdvImage _imgSelected;
        public AdvImage ImmagineSelezionata { get { return _imgSelected; } set { Set(ref _imgSelected, value); } }
        private DelegateCommand<AdvImage> _imgSelCmd;
        public DelegateCommand<AdvImage> SelezionaImmagineCommand =>
            _imgSelCmd ??
            (_imgSelCmd = new DelegateCommand<AdvImage>((x) =>
            {
                NavigationService.Navigate(typeof(Views.ImageViewerPage), x.ImageLink);
            }));
        private DelegateCommand _shareCmd;
        public DelegateCommand CondividiCommand =>
            _shareCmd ??
            (_shareCmd = new DelegateCommand(() =>
            {
                DataTransferManager.ShowShareUI();
            }));
        private void OnShareRequested(DataTransferManager sender, DataRequestedEventArgs e)
        {
            e.Request.Data.Properties.Title = $"Guarda le immagini di {GalleriaSelezionata.Titolo} su adventuresplanet.it";
            e.Request.Data.SetWebLink(new Uri($"{AVPManager.URL_BASE}scheda_immagini.php?game={GalleriaSelezionata.IdGalleria}"));
        }
        public class ImagesCollection : ObservableCollection<AdvImage>, ISupportIncrementalLoading
        {
            private AVPManager manager;
            private GalleriaItem galleriaImg;
            public ImagesCollection(AVPManager m)
            {
                manager = m;
            }
            private bool _isLoading;
            public bool IsLoading
            {
                get { return _isLoading; }
                set
                {
                    _isLoading = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsLoading)));
                }
            }

            public bool HasMoreItems
            {
                get
                {
                    if (galleriaImg == null)
                        return false;
                    return galleriaImg.HasNext;
                }
            }
            public GalleriaItem Galleria
            {
                get { return galleriaImg; }
                set
                {
                    galleriaImg = value;
                    if (value != null)
                    {
                        Clear();
                        value.Reset();
                    }
                }
            }

            public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
            {
                return Task.Run<LoadMoreItemsResult>(async () =>
                {
                    WindowWrapper.Current().Dispatcher.Dispatch(() =>
                    {
                        IsLoading = true;
                    });
                    Action<string, AdvImage> AddAction = (group, image) =>
                    {
                        WindowWrapper.Current().Dispatcher.Dispatch(() =>
                        {
                            Add(image);
                        });
                    };
                    await manager.LoadGalleria(galleriaImg, AddAction);
                    
                    WindowWrapper.Current().Dispatcher.Dispatch(() =>
                    {
                        IsLoading = false;
                    });
                    return new LoadMoreItemsResult() { Count = count };

                }).AsAsyncOperation<LoadMoreItemsResult>();
            }
        }
    }
}
