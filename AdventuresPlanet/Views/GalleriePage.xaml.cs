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
    public sealed partial class GalleriePage : Page
    {
        public GalleriePage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        private void ResetZoom(object sender, TappedRoutedEventArgs e)
        {
            imageContainer.ChangeView(imageContainer.HorizontalOffset, imageContainer.VerticalOffset, 1);
        }

        private void ZoomOut(object sender, TappedRoutedEventArgs e)
        {
            imageContainer.ChangeView(imageContainer.HorizontalOffset, imageContainer.VerticalOffset, imageContainer.ZoomFactor - 0.25f);
        }

        private void ZoomIn(object sender, TappedRoutedEventArgs e)
        {
            imageContainer.ChangeView(imageContainer.HorizontalOffset, imageContainer.VerticalOffset, imageContainer.ZoomFactor + 0.25f);
        }
    }
}
