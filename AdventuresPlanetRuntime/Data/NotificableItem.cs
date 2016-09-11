using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AdventuresPlanetRuntime.Data
{
    [DataContract]
    public class NotificableItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void Set<T>(ref T o, T val, [CallerMemberName] string name = "")
        {
            o = val;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
