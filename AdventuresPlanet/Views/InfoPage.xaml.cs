﻿using System;
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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace AdventuresPlanet.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class InfoPage : Page
    {
        public InfoPage()
        {
            this.InitializeComponent();
        }
        private int splashCountTap = 0;
        private void imageSplashTapped(object sender, TappedRoutedEventArgs e)
        {
            splashCountTap++;
            if(splashCountTap % 10 == 0)
                imageSplash.Source = new BitmapImage(new Uri("ms-appx:///Assets/SplashScreen.png"));
            else if (splashCountTap % 5 == 0)
                imageSplash.Source = new BitmapImage(new Uri("ms-appx:///Assets/SplashScreenGrimFandango.png"));
        }
    }
}
