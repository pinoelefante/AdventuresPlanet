using AdventuresPlanet.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template10.Mvvm;

namespace AdventuresPlanet.ViewModels
{
    public class OpzioniViewModel : ViewModelBase
    {
        public OpzioniViewModel(SettingsService s)
        {
            Settings = s;
        }
        public SettingsService Settings { get; }
    }
}
