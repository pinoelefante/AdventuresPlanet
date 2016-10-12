using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace AdventuresPlanet.Services
{
    public class SettingsService : INotifyPropertyChanged
    {
        private ApplicationDataContainer local;
        private ApplicationDataContainer roaming;

        public event PropertyChangedEventHandler PropertyChanged;

        public SettingsService()
        {
            local = ApplicationData.Current.LocalSettings;
            roaming = ApplicationData.Current.RoamingSettings;
        }
        public int NumeroAvvii
        {
            get { return GetLocal<int>("numero_avvii"); }
            set { SetLocal("numero_avvii", value); }
        }
        public bool IsAppVoted
        {
            get { return GetRoaming<bool>("is_voted"); }
            set { SetRoaming("is_voted", value); }
        }
        public bool ChiediChiusuraApp
        {
            get { return GetRoaming<bool>("is_ask_close", true); }
            set { SetRoaming("is_ask_close", value); }
        }
        public bool VideoTubecast
        {
            get { return GetRoaming<bool>("video_tubecast"); }
            set { SetRoaming("video_tubecast", value); }
        }
        private void SetLocal<T>(string key, T value, [CallerMemberName]string caller = "")
        {
            local.Values[key] = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
        }
        private void SetRoaming<T>(string key, T value, [CallerMemberName]string caller = "")
        {
            roaming.Values[key] = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
        }
        private T GetLocal<T>(string key, T def = default(T))
        {
            if (local.Values.ContainsKey(key))
                return (T)local.Values[key];
            return def;
        }
        private T GetRoaming<T>(string key, T def = default(T))
        {
            if (roaming.Values.ContainsKey(key))
                return (T)roaming.Values[key];
            return def;
        }
    }
}
