using AdventuresPlanet.ViewModels;
using AdventuresPlanet.Views.Utils;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace AdventuresPlanet.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SoluzioniPage : Page
    {
        public SoluzioniPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
        }
        private SoluzioniPageViewModel ViewModel => this.DataContext as SoluzioniPageViewModel;
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            scroll = VisualTreeHelperExtensions.GetFirstDescendantOfType<ScrollViewer>(soluzione);
            if (scroll != null)
                scroll.ViewChanged += Scroll_ViewChanged;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            if (scroll != null)
                scroll.ViewChanged -= Scroll_ViewChanged;
        }
        private ScrollViewer scroll;
        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ViewModel.ListaComponenti):
                    //resetta lo scroll
                    if (soluzione.Items.Count > 0)
                        soluzione.ScrollIntoView(soluzione.Items.ElementAt(0));
                    break;
                case nameof(ViewModel.CurrentPageIndex):
                    //scroll a tag con valore CurrentPageIndex
                    var res = soluzione.Items.Where(x=>(x as FrameworkElement)?.Tag?.ToString().CompareTo(ViewModel.CurrentPageIndex)==0);
                    if (res?.Count() == 1)
                        soluzione.ScrollIntoView(res.ElementAt(0), ScrollIntoViewAlignment.Leading);
                    break;
                case nameof(ViewModel.SoluzioneLoadPositionIndex):
                    if (ViewModel.SoluzioneLoadPositionIndex < soluzione.Items.Count)
                    {
                        var pos = soluzione.Items[ViewModel.SoluzioneLoadPositionIndex];
                        soluzione.ScrollIntoView(pos);
                    }
                    break;
            }
        }
        private long lastUpdate = 0;
        private void Scroll_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if ((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - lastUpdate > 500)
            {
                if (ViewModel.IsSoluzioneSelezionata && scroll != null)
                {
                    double vOffset = scroll.VerticalOffset;
                    int index = 0;
                    for (double cOff = 0; index < soluzione.Items.Count; index++)
                    {
                        FrameworkElement elem = soluzione.Items[index] as FrameworkElement;
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
    }
}
