using AdventuresPlanet.Services;
using NotificationsExtensions.Toasts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Template10.Mvvm;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.UI.Xaml.Navigation;

namespace AdventuresPlanet.ViewModels
{
    public class ImageViewerViewModel : ViewModelBase
    {
        private DownloadService downloader;
        public ImageViewerViewModel(DownloadService d)
        {
            downloader = d;
        }
        public override Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            if (parameter != null)
                UrlImage = parameter.ToString();
            return base.OnNavigatedToAsync(parameter, mode, state);
        }
        private string _urlImage;
        public string UrlImage
        {
            get { return _urlImage; }
            set
            {
                Set(ref _urlImage, value);
                IsDownloadable = value.StartsWith("http");
            }
        }
        private bool _isDownloadable;
        public bool IsDownloadable { get { return _isDownloadable; } set { Set(ref _isDownloadable, value); } }
        private DelegateCommand _ScaricaImmagineCommand;
        private HttpClient http = new HttpClient();
        public DelegateCommand ScaricaImmagineCommand =>
            _ScaricaImmagineCommand ??
            (_ScaricaImmagineCommand = new DelegateCommand(() =>
            {
                var filename = UrlImage.Substring(UrlImage.LastIndexOf('/')+1);
                downloader.DownloadImmagine(UrlImage, filename);
            }));
        private string correctStringDirectory(string s)
        {
            return s.Replace(":", " ")
                    .Replace("\"", " ")
                    .Replace("<", " ")
                    .Replace(">", " ")
                    .Replace("|", " ");
        }
    }
}
