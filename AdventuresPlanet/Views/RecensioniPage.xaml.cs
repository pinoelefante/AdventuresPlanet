using AdventuresPlanet.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.ComponentModel;
using AdventuresPlanet.Views.Utils;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace AdventuresPlanet.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RecensioniPage : Page
    {
        public RecensioniPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
        }
        private RecensioniPageViewModel ViewModel => this.DataContext as RecensioniPageViewModel;
        private ScrollViewer scroll;
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            scroll = VisualTreeHelperExtensions.GetFirstDescendantOfType<ScrollViewer>(recensione);
            if (scroll != null)
                scroll.ViewChanged += Scroll_ViewChanged;
        }
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            if (scroll != null)
                scroll.ViewChanged -= Scroll_ViewChanged;
        }
        private long lastUpdate;
        private void Scroll_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if ((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - lastUpdate > 500)
            {
                if (ViewModel.IsRecensioneSelezionata && scroll != null)
                {
                    double vOffset = scroll.VerticalOffset;
                    int index = 0;
                    for (double cOff = 0; index < recensione.Items.Count; index++)
                    {
                        FrameworkElement elem = recensione.Items[index] as FrameworkElement;
                        cOff += elem.ActualHeight;
                        if (cOff >= vOffset)
                            break;
                    }
                    ViewModel.SalvaPosizione(index);
                    System.Diagnostics.Debug.WriteLine("Index = " + index);
                    lastUpdate = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                }
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ViewModel.ListaComponenti):
                    //resetta lo scroll
                    if (recensione.Items.Count > 0)
                        recensione.ScrollIntoView(recensione.Items.ElementAt(0));
                    break;
                case nameof(ViewModel.RecensioneLoadPositionIndex):
                    if (ViewModel.RecensioneLoadPositionIndex < recensione.Items.Count)
                    {
                        var pos = recensione.Items[ViewModel.RecensioneLoadPositionIndex];
                        recensione.ScrollIntoView(pos);
                    }
                    break;
            }
        }
    }
}
