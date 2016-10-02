using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template10.Mvvm;

namespace AdventuresPlanet.ViewModels
{
    public class InfoViewModel : ViewModelBase
    {
        public string VersioneApp
        {
            get
            {
                var myPackage = Windows.ApplicationModel.Package.Current;
                var version = myPackage.Id.Version;

                var appVersion = version.Major + "." +
                                 version.Minor + "." +
                                 version.Build;
                return appVersion;
            }
        }
    }
}
