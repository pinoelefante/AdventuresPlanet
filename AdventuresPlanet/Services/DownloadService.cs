using AdventuresPlanetRuntime.Data;
using NotificationsExtensions.Toasts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Template10.Common;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI.Notifications;

namespace AdventuresPlanet.Services
{
    public class DownloadService
    {
        private BackgroundDownloader downloader;
        private CancellationTokenSource cts;
        public ObservableCollection<DownloadItem> ListaDownload { get; }
        public DownloadService()
        {
            downloader = new BackgroundDownloader();
            cts = new CancellationTokenSource();
            ListaDownload = new ObservableCollection<DownloadItem>();
            Init();
        }
        public async void Init()
        {
            LoadDownload();
            IReadOnlyList<DownloadOperation> downloads = null;
            try
            {
                downloads = await BackgroundDownloader.GetCurrentDownloadsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Discovery error", ex);
                return;
            }
            if (downloads.Count > 0)
            {
                foreach (DownloadOperation download in downloads)
                {
                    //Log(String.Format(CultureInfo.CurrentCulture, "Discovered background download: {0}, Status: {1}", download.Guid, download.Progress.Status));

                    // Attach progress and completion handlers.
                    var found = ListaDownload.Where(x => x.Link.CompareTo(download.RequestedUri.AbsoluteUri) == 0);
                    if(found!=null && found.Count() == 1)
                    {
                        DownloadItem downItem = found.ElementAt(0);
                        downItem.DownloadOp = download;
                        await download.AttachAsync();
                        var task = HandleDownloadAsync(downItem, true);
                        downItem.DownloadTask = task;
                    }
                    else
                    {
                        DownloadItem downItem = new DownloadItem()
                        {
                            DownloadPath = download.ResultFile.Path,
                            FriendlyName = download.RequestedUri.AbsoluteUri.Substring(download.RequestedUri.AbsoluteUri.LastIndexOf('/') + 1),
                            Link = download.RequestedUri.AbsoluteUri,
                            DownloadOp = download
                        };
                        await download.AttachAsync();
                        AggiungiDownload(downItem);
                        var task = HandleDownloadAsync(downItem, true);
                        downItem.DownloadTask = task;
                    }
                    
                }
            }
        }
        private void DownloadProgress(DownloadOperation download)
        {
            var found = ListaDownload.Where(x => x.Link.CompareTo(download.RequestedUri.AbsoluteUri) == 0);
            DownloadItem downItem = null;
            if (found.Count() == 1)
                downItem = found.ElementAt(0);
            else
            {

            }
            int progress = (int)(100 * ((double)download.Progress.BytesReceived / (double)download.Progress.TotalBytesToReceive));
            string textProgress = null;
            switch (download.Progress.Status)
            {
                case BackgroundTransferStatus.Running:
                    textProgress = "Scaricando";
                    break;
                case BackgroundTransferStatus.PausedByApplication:
                    textProgress = "In pausa";
                    break;
                case BackgroundTransferStatus.PausedCostedNetwork:
                    textProgress = "Connessione a consumo";
                    break;
                case BackgroundTransferStatus.PausedNoNetwork:
                    textProgress = "Connessione assente";
                    break;
                case BackgroundTransferStatus.Error:
                    textProgress = "Errore";
                    break;
            }
            if (progress >= 100)
            {
                textProgress = "Completato";
            }

            if (downItem != null)
            {
                downItem.Progress = progress;
                downItem.ProgressText = textProgress;
            }
        }
        private async Task HandleDownloadAsync(DownloadItem download, bool isAttached = false)
        {
            if (download.DownloadOp.Progress.TotalBytesToReceive!=0 && download.DownloadOp.Progress.TotalBytesToReceive == download.DownloadOp.Progress.BytesReceived)
            {
                RimuoviDownload(download);
                return;
            }
            bool fail = false;
            try
            {
                Progress<DownloadOperation> progressCallback = new Progress<DownloadOperation>(DownloadProgress);
                if(!isAttached)
                    await download.DownloadOp.StartAsync().AsTask(cts.Token, progressCallback);
                ResponseInformation response = download.DownloadOp.GetResponseInformation();
            }
            catch (Exception ex)
            {
                fail = true;
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                await download.DownloadOp.AttachAsync();
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
                            Text = !fail ? "Download completato": "Errore durante il download"
                        },
                        BodyTextLine1 = new ToastText()
                        {
                            Text = download.FriendlyName
                        }
                    }
                };
                var xmlToast = toast.GetXml();
                var notification = new ToastNotification(xmlToast);
                ToastNotificationManager.CreateToastNotifier().Show(notification);
                RimuoviDownload(download);
            }
        }
        public void InterrompiDownload(DownloadItem down)
        {
            down.DownloadOp?.AttachAsync()?.Cancel();
            RimuoviDownload(down, true);
        }
        public async void DownloadImmagine(string url, string filename, string titoloAvv = null)
        {
            StorageFolder folder = KnownFolders.PicturesLibrary;
            StorageFolder folderAdv = await folder.CreateFolderAsync("AdventuresPlanet", CreationCollisionOption.OpenIfExists);
            if (!string.IsNullOrEmpty(titoloAvv))
                folderAdv = await folderAdv.CreateFolderAsync(correctStringDirectory(titoloAvv), CreationCollisionOption.OpenIfExists);
            StorageFile destFile = await folderAdv.CreateFileAsync(filename, CreationCollisionOption.GenerateUniqueName);

            DownloadOperation download = downloader.CreateDownload(new Uri(url), destFile);
            download.Priority = BackgroundTransferPriority.Default;

            DownloadItem downItem = new DownloadItem()
            {
                DownloadOp = download,
                DownloadPath = destFile.Path,
                FriendlyName = $"{(titoloAvv!=null?$"{titoloAvv} - ":string.Empty)}{filename}",
                Link = url
            };
            AggiungiDownload(downItem);
            var task = HandleDownloadAsync(downItem);
            downItem.DownloadTask = task;
        }
        private string correctStringDirectory(string s)
        {
            return s.Replace(":", " ")
                    .Replace("\"", " ")
                    .Replace("<", " ")
                    .Replace(">", " ")
                    .Replace("|", " ");
        }
        public async void DownloadPodcast(PodcastItem podcast)
        {
            if(ListaDownload.Where(x=>x.Link.CompareTo(podcast.Link) == 0).Any())
            {
                Debug.WriteLine("Download già in corso");
                return;
            }
            StorageFolder music = KnownFolders.MusicLibrary;
            StorageFolder podcastDir = await music.CreateFolderAsync("CalaveraCafe", CreationCollisionOption.OpenIfExists);
            StorageFile destFile = await podcastDir.CreateFileAsync(podcast.Filename, CreationCollisionOption.ReplaceExisting);
            
            DownloadOperation download = downloader.CreateDownload(new Uri(podcast.Link), destFile);
            download.Priority = BackgroundTransferPriority.Default;

            DownloadItem downItem = new DownloadItem()
            {
                DownloadOp = download,
                DownloadPath = destFile.Path,
                FriendlyName = podcast.TitoloBG,
                Link = podcast.Link,
            };
            AggiungiDownload(downItem);

            var task = HandleDownloadAsync(downItem);
            downItem.DownloadTask = task;
        }
        private ApplicationDataContainer data = ApplicationData.Current.LocalSettings;
        private void AggiungiDownload(DownloadItem down)
        {
            data.Values[$"download_{down.Link}"] = $"{down.Link};{down.FriendlyName};{down.DownloadPath}";
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                ListaDownload.Insert(0, down);
            });
        }
        private async void RimuoviDownload(DownloadItem down, bool delete = false)
        {
            data.Values.Remove($"download_{down.Link}");
            ListaDownload.Remove(down);
            if (delete)
            {
                try
                {
                    var file = await StorageFile.GetFileFromPathAsync(down.DownloadPath);
                    file.DeleteAsync();
                }
                catch { }
            }
        }
        private void LoadDownload()
        {
            var found = data.Values.Where(x => x.Key.StartsWith("download_"));
            if (found!=null)
            {
                foreach (var item in found)
                {
                    var down = item.Value.ToString().Split(new char[] { ';' }, StringSplitOptions.None);
                    var downItem = new DownloadItem()
                    {
                        Link = down[0],
                        FriendlyName = down[1],
                        DownloadPath = down[2]
                    };
                    ListaDownload.Add(downItem);
                }
            }
        }
    }
    public class DownloadItem : INotifyPropertyChanged
    {
        private string link, friendly, downpath, progressText;
        private int progressNo;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Link { get { return link; } set { Set(ref link, value); } }
        public string FriendlyName { get { return friendly; } set { Set(ref friendly, value); } }
        public int Progress { get { return progressNo; } set { Set(ref progressNo, value); } }
        public string DownloadPath { get { return downpath; } set { Set(ref downpath, value); } }
        public string ProgressText { get { return progressText; } set { Set(ref progressText, value); } }
        public DownloadOperation DownloadOp { get; set; }
        public Task DownloadTask { get; set; }
        private void Set<T>(ref T r, T val, [CallerMemberName] string member = "")
        {
            r = val;
            WindowWrapper.Current().Dispatcher.Dispatch(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(member));
            });
        }
    }
}
