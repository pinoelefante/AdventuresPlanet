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
using Windows.UI.Popups;
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
            if (NavigationService.CanGoBack)
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
                if(item.IntValue == 1)
                {
                    if(item.Recensione!=null) NavigationService.Navigate(typeof(RecensioniPage), item.Recensione);
                    else if(item.Soluzione!=null) NavigationService.Navigate(typeof(SoluzioniPage), item.Soluzione);
                    else if(item.Galleria!=null) NavigationService.Navigate(typeof(GalleriePage), item.Galleria);
                }
            }));
        private DelegateCommand<GameWrapper> _openReceCmd, _openSolCmd, _openGallCmd;
        public DelegateCommand<GameWrapper> ApriRecensioneCommand =>
            _openReceCmd ??
            (_openReceCmd = new DelegateCommand<GameWrapper>((item) =>
            {
                NavigationService.Navigate(typeof(RecensioniPage), item.Recensione);
            }));
        public DelegateCommand<GameWrapper> ApriSoluzioneCommand =>
            _openSolCmd ??
            (_openSolCmd = new DelegateCommand<GameWrapper>((item) =>
            {
                NavigationService.Navigate(typeof(SoluzioniPage), item.Soluzione);
            }));
        public DelegateCommand<GameWrapper> ApriGalleriaCommand =>
            _openGallCmd ??
            (_openGallCmd = new DelegateCommand<GameWrapper>((item) =>
            {
                NavigationService.Navigate(typeof(GalleriePage), item.Galleria);
            }));
    }
}
