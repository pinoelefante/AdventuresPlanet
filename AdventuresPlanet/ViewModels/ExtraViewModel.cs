using AdventuresPlanetRuntime;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template10.Common;
using Template10.Mvvm;
using Windows.UI.Popups;
using Windows.UI.Xaml.Navigation;

namespace AdventuresPlanet.ViewModels
{
    public class ExtraViewModel : ViewModelBase
    {
        private AVPManager manager;
        public ExtraViewModel(AVPManager m)
        {
            manager = m;
        }
        public override Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            if(mode == NavigationMode.New || mode == NavigationMode.Refresh)
            {
                if (parameter != null && parameter is string && Uri.IsWellFormedUriString(parameter.ToString(), UriKind.RelativeOrAbsolute))
                {
                    var url = parameter.ToString();
                    LoadExtra(url);
                }
                else
                {

                }
            }
            
            return base.OnNavigatedToAsync(parameter, mode, state);
        }
        private async Task LoadExtra(string url)
        {
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                ListaExtra?.Clear();
                IsExtraLoading = true;
                RaisePropertyChanged(() => IsExtraLoading);
            });
            var list = await manager.LoadExtra(url);
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                if (list != null)
                {
                    foreach (var item in list)
                        ListaExtra.Add(item);
                }
                else
                {
                    new MessageDialog("Errore").ShowAsync();
                }

                IsExtraLoading = false;
                RaisePropertyChanged(() => IsExtraLoading);
            });
        }
        public bool IsExtraLoading { get; set; }
        public ObservableCollection<KeyValuePair<string, string>> ListaExtra { get; } = new ObservableCollection<KeyValuePair<string, string>>();
        private DelegateCommand<KeyValuePair<string, string>> _extraSelectedCommand;
        public DelegateCommand<KeyValuePair<string, string>> ExtraSelectedCmd =>
            _extraSelectedCommand ??
            (_extraSelectedCommand = new DelegateCommand<KeyValuePair<string, string>>((x) =>
            {
                NavigationService.Navigate(typeof(Views.VideoPlayerPage), x.Value);
            }));
    }
}
