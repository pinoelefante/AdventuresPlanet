using AdventuresPlanet.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template10.Mvvm;

namespace AdventuresPlanet.ViewModels
{
    public class DownloadPageViewModel : ViewModelBase
    {
        public DownloadService Download { get; }
        public DownloadPageViewModel(DownloadService d)
        {
            Download = d;
        }
        private DelegateCommand<DownloadItem> _downRimuovi;
        public DelegateCommand<DownloadItem> RimuoviDownload =>
            _downRimuovi ??
            (_downRimuovi = new DelegateCommand<DownloadItem>((x) =>
            {
                Download.InterrompiDownload(x);
            }));
    }
}
