using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace AdventuresPlanet.Views.Utils
{
    public class PreferitiContentSelector : DataTemplateSelector
    {
        public DataTemplate AggiungiPreferito { get; set; }
        public DataTemplate RimuoviPreferito { get; set; }
        protected override DataTemplate SelectTemplateCore(object item)
        {
            bool pref = (bool)item;
            if (pref)
                return RimuoviPreferito;
            else
                return AggiungiPreferito;
        }
    }
}
