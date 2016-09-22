using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace AdventuresPlanetRuntime
{
    public class AVPPreferiti
    {
        private ApplicationDataContainer data;
        public AVPPreferiti()
        {
            data = ApplicationData.Current.RoamingSettings;
        }
        public void AggiungiPreferiti(string id)
        {
            if (!data.Values.ContainsKey("preferiti"))
                data.Values["preferiti"] = string.Empty;
            data.Values["preferiti"] = $"{data.Values["preferiti"]}{id};";
        }
        public bool IsPreferita(string id)
        {
            if (data.Values.ContainsKey("preferiti"))
                return (data.Values["preferiti"] as string).Contains($"{id};");
            return false;
        }
        public void RimuoviPreferiti(string id)
        {
            if (data.Values.ContainsKey("preferiti"))
                data.Values["preferiti"] = (data.Values["preferiti"] as string).Replace($"{id};", "");
        }
        private static readonly char[] splitBy = new char[] {';' };
        public IEnumerable<string> ListPreferiti()
        {
            if (data.Values.ContainsKey("preferiti"))
                return (data.Values["preferiti"] as string).Split(splitBy, StringSplitOptions.RemoveEmptyEntries);
            return null;
        }
    }
}
