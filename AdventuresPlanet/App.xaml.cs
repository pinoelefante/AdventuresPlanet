using Windows.UI.Xaml;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Template10.Controls;
using Template10.Common;
using System;
using System.Linq;
using Windows.UI.Xaml.Data;
using Windows.UI.Core;
using System.Diagnostics;
using Windows.UI.Popups;

namespace AdventuresPlanet
{
    /// Documentation on APIs used in this page:
    /// https://github.com/Windows-XAML/Template10/wiki

    [Bindable]
    sealed partial class App : Template10.Common.BootStrapper
    {
        public App()
        {
            InitializeComponent();
            this.UnhandledException += App_UnhandledException;
            //SplashFactory = (e) => new Views.Splash(e);
        }

        private async void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
#if DEBUG
            e.Handled = true;
            await new MessageDialog(e.Message).ShowAsync();
#else
            await new MessageDialog("Si è verificato un errore inaspettato").ShowAsync();
#endif

        }

        public override async Task OnInitializeAsync(IActivatedEventArgs args)
        {
            if (Window.Current.Content as ModalDialog == null)
            {
                // create a new frame 
                var nav = NavigationServiceFactory(BackButton.Attach, ExistingContent.Include);

                // create modal root
                Window.Current.Content = new ModalDialog
                {
                    DisableBackButtonWhenModal = true,
                    CanBackButtonDismiss = false,
                    Content = new Views.Shell(nav),
                    ModalContent = new Views.Busy(),
                };
            }
            this.ForceShowShellBackButton = true;
            await Task.CompletedTask;
        }

        public override async Task OnStartAsync(StartKind startKind, IActivatedEventArgs args)
        {
            // long-running startup tasks go here
            await Task.Delay(5000);
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            NavigationService.Navigate(typeof(Views.NewsPage));
            await Task.CompletedTask;
        }
    }
}

