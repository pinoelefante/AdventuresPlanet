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
                DownloadImage(UrlImage, filename);
                //downloader.DownloadImmagine(UrlImage, filename);
            }));
        
        private async Task DownloadImage(string url, string filename, string titoloAvv = null)
        {
            bool error = false;
            try
            {
                //var correctFolder = correctStringDirectory(titoloAvv);
                StorageFolder folder = KnownFolders.PicturesLibrary;
                StorageFolder folderAdv = await folder.CreateFolderAsync("AdventuresPlanet", CreationCollisionOption.OpenIfExists);
                //StorageFolder folderImgs = await folderAdv.CreateFolderAsync(correctFolder, CreationCollisionOption.OpenIfExists);
                //StorageFile s_file = await folderImgs.CreateFileAsync(filename, CreationCollisionOption.GenerateUniqueName);
                StorageFile s_file = await folderAdv.CreateFileAsync(filename, CreationCollisionOption.GenerateUniqueName);
                using (Stream imgBytes = await http.GetStreamAsync(new Uri(url)))
                using (Stream sw = await s_file.OpenStreamForWriteAsync())
                {
                    imgBytes.CopyTo(sw);
                }
                Debug.WriteLine("Download completato");
                //Shell.Instance.ShowMessagePopup($"Download {filename} completato!");
            }
            catch (Exception e)
            {
                error = true;
                Debug.WriteLine("Download errore");
                //Shell.Instance.ShowMessagePopup($"Download {filename} fallito!", true);
            }
            finally
            {
                ToastContent toast = new ToastContent()
                {
                    ActivationType = ToastActivationType.Background,
                    Visual = new ToastVisual()
                    {
                        AppLogoOverride = new ToastAppLogo()
                        {
                            Source = new ToastImageSource("ms-appx:///Assets/Store.png")
                        },
                        TitleText = new ToastText()
                        {
                            Text = !error ? "Download completato" : "Errore durante il download"
                        },
                        BodyTextLine1 = new ToastText()
                        {
                            Text = filename
                        }
                    }
                };
                var xmlToast = toast.GetXml();
                var notification = new ToastNotification(xmlToast);
                ToastNotificationManager.CreateToastNotifier().Show(notification);
            }
        }
        
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
