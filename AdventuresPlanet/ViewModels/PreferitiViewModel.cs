using AdventuresPlanet.Views;
using AdventuresPlanetRuntime;
using AdventuresPlanetRuntime.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template10.Mvvm;
using Windows.UI.Xaml.Navigation;

namespace AdventuresPlanet.ViewModels
{
    public class PreferitiViewModel : ViewModelBase
    {
        private AVPDatabase db;
        private AVPPreferiti pref;
        public PreferitiViewModel(AVPDatabase d, AVPPreferiti prefs)
        {
            pref = prefs;
            db = d;
        }
        public override Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            NavigationService.FrameFacade.BackRequested += FrameFacade_BackRequested;
            if (mode == NavigationMode.New || mode == NavigationMode.Refresh)
                CaricaPreferiti();
            else
            {
                //DO NOTHING
            }
            return base.OnNavigatedToAsync(parameter, mode, state);
        }
        public override Task OnNavigatedFromAsync(IDictionary<string, object> pageState, bool suspending)
        {
            return base.OnNavigatedFromAsync(pageState, suspending);
        }
        private void FrameFacade_BackRequested(object sender, Template10.Common.HandledEventArgs e)
        {
            e.Handled = true;
            if (ItemSelected != null)
                ItemSelected = null;
            else if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }
        public ObservableCollection<GameWrapper> ListaPreferiti { get; set; }
        private void CaricaPreferiti()
        {
            ListaPreferiti?.Clear();
            var ids = pref.ListPreferiti();
            var list = new List<GameWrapper>(ids.Count());
            foreach (var id in ids)
            {
                var game = db.GetGameById(id);
                list.Add(game);
            }
            ListaPreferiti = new ObservableCollection<GameWrapper>(list.OrderBy(x => x.Titolo).ToList());
            RaisePropertyChanged(() => ListaPreferiti);
        }
        private DelegateCommand<GameWrapper> _itemSelCmd;
        public DelegateCommand<GameWrapper> ItemSelectedCommand =>
            _itemSelCmd ??
            (_itemSelCmd = new DelegateCommand<GameWrapper>((item) =>
            {
                ItemSelected = item;
            }));
        private GameWrapper _selectedItem;
        public GameWrapper ItemSelected { get { return _selectedItem; } set { Set(ref _selectedItem, value); } }
        private DelegateCommand _chiudiSceltaCommand, _openReceCmd, _openSolCmd, _openGallCmd;
        public DelegateCommand ChiudiSceltaCommand =>
            _chiudiSceltaCommand ??
            (_chiudiSceltaCommand = new DelegateCommand(() =>
            {
                ItemSelected = null;
            }));
        public DelegateCommand ApriRecensioneCommand =>
            _openReceCmd ??
            (_openReceCmd = new DelegateCommand(() =>
            {
                NavigationService.Navigate(typeof(RecensioniPage), ItemSelected.Recensione);
            }));
        public DelegateCommand ApriSoluzioneCommand =>
            _openReceCmd ??
            (_openReceCmd = new DelegateCommand(() =>
            {
                NavigationService.Navigate(typeof(SoluzioniPage), ItemSelected.Soluzione);
            }));
        public DelegateCommand ApriGalleriaCommand =>
            _openReceCmd ??
            (_openReceCmd = new DelegateCommand(() =>
            {
                NavigationService.Navigate(typeof(GalleriePage), ItemSelected.Galleria);
            }));
    }
}
