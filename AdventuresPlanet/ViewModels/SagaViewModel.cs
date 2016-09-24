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
using Windows.UI.Xaml.Navigation;

namespace AdventuresPlanet.ViewModels
{
    public class SagaViewModel : ViewModelBase
    {
        private AVPManager manager;
        public SagaViewModel(AVPManager m)
        {
            manager = m;
        }
        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            if (mode == NavigationMode.New || mode == NavigationMode.Refresh)
            {
                var url = parameter.ToString();
                ListaGiochi?.Clear();
                await CaricaGiochi(url);
            }
        }
        private async Task CaricaGiochi(string url)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                IsLoading = true;
            });
            var games = await manager.LoadSaga(url);
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                ListaGiochi = new ObservableCollection<PaginaContenuti>(games);
                RaisePropertyChanged(() => ListaGiochi);
                IsLoading = false;
            });
        }
        public ObservableCollection<PaginaContenuti> ListaGiochi { get; private set; }
        private bool _isLoading;
        public bool IsLoading { get { return _isLoading; } set { Set(ref _isLoading, value); } }
        private DelegateCommand<PaginaContenuti> _itemSelected;
        public DelegateCommand<PaginaContenuti> ItemSelectedCommand =>
            _itemSelected ??
            (_itemSelected = new DelegateCommand<PaginaContenuti>((x) =>
            {
                if (x is RecensioneItem)
                    NavigationService.Navigate(typeof(RecensioniPage), x.Link);
                if(x is SoluzioneItem)
                    NavigationService.Navigate(typeof(SoluzioniPage), x.Link);
            }));
    }
}
