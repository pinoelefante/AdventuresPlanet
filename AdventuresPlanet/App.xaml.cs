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
using Windows.ApplicationModel.Background;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Utils;
using AdventuresPlanetRuntime.Data;
using Windows.System;
using AdventuresPlanet.Views;

namespace AdventuresPlanet
{
    /// Documentation on APIs used in this page:
    /// https://github.com/Windows-XAML/Template10/wiki

    [Bindable]
    sealed partial class App : Template10.Common.BootStrapper
    {
        public readonly static string StoreId = "9NBLGGH4XZHB";
        public App()
        {
            InitializeComponent();
            this.UnhandledException += App_UnhandledException;
            //SplashFactory = (e) => new Views.Splash(e);
        }

        private async void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
#if DEBUG
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
            //await Task.Delay(5000);
            TaskRegister("NewsNotifier", "Tasks.NewsNotifier");
            TaskRegister("PodcastNotifier", "Tasks.PodcastNotifier");
#if !DEBUG
            ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
#endif
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            if(args is ToastNotificationActivatedEventArgs)
            {
                var argsToast = args as ToastNotificationActivatedEventArgs;
                ManageToastLaunch(argsToast);
            }
            else
                NavigationService.Navigate(typeof(Views.NewsPage));
            await Task.CompletedTask;
        }
        private static MessageDialog votaDialog = null;
        public static async void VotaApplicazione(Services.SettingsService settings)
        {
            if (votaDialog == null)
            {
                votaDialog = new MessageDialog("", "Vota applicazione") { CancelCommandIndex = 1, DefaultCommandIndex = 0 };
                votaDialog.Commands.Add(new UICommand("Vota", async (x) =>
                {
                    Busy.SetBusy(true, "");
                    await Launcher.LaunchUriAsync(new Uri($"ms-windows-store://review/?ProductId={App.StoreId}"));
                    settings.IsAppVoted = true;
                    Busy.SetBusy(false, "");
                }));
                votaDialog.Commands.Add(new UICommand("Non ora"));
            }
            await votaDialog.ShowAsync();
        }
        private void ManageToastLaunch(ToastNotificationActivatedEventArgs toast)
        {
            var parameters = UrlUtils.GetUrlParameters(toast.Argument);
            switch (parameters["action"])
            {
                case "viewNews":
                    {
                        News news = new News()
                        {
                            AnteprimaNews = parameters["anteprima"],
                            DataPubblicazione = parameters["data"],
                            Immagine = parameters["img"],
                            Link = parameters["link"],
                            Titolo = parameters["titolo"],
                            MeseLink = parameters["meseLink"],
                            Id = Int32.Parse(parameters["id"])
                        };
                        NavigationService.Navigate(typeof(Views.NewsPage), news);
                    }
                    break;
                case "listenPodcast":
                    {
                        PodcastItem podcast = new PodcastItem()
                        {
                            Data = parameters["data"],
                            Descrizione = parameters["descrizione"],
                            Immagine = parameters["img"],
                            Link = parameters["link"],
                            Titolo = parameters["titolo"]
                        };
                        NavigationService.Navigate(typeof(Views.PodcastPage), podcast);
                    }
                    break;
                case "viewRecensione":
                    break;
                case "viewSoluzione":
                    break;
                case "viewGalleria":
                    break;
                case "viewTrailer":
                    break;
                default:
                    NavigationService.Navigate(typeof(Views.NewsPage));
                    break;
            }
            
        }
        private async void TaskRegister(string name, string entry)
        {
            var taskRegistered = false;

            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == name)
                {
                    taskRegistered = true;
                    break;
                }
            }
            if (!taskRegistered)
            {
                BackgroundTaskBuilder builder = new BackgroundTaskBuilder()
                {
                    Name = name,
                    TaskEntryPoint = entry
                };
                BackgroundAccessStatus access;
                BackgroundTaskRegistration reg;
                switch (name)
                {
                    case "PodcastNotifier":
                        access = await BackgroundExecutionManager.RequestAccessAsync();
                        builder.IsNetworkRequested = true;
                        builder.CancelOnConditionLoss = true;
                        builder.SetTrigger(new TimeTrigger(90, false));
                        builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                        builder.AddCondition(new SystemCondition(SystemConditionType.BackgroundWorkCostNotHigh));
                        reg = builder.Register();
                        reg.Completed += (s, e) =>
                        {
                            Debug.WriteLine("Podcast Notifier background task complete");
                        };
                        break;
                    case "NewsNotifier":
                        access = await BackgroundExecutionManager.RequestAccessAsync();
                        builder.IsNetworkRequested = true;
                        builder.CancelOnConditionLoss = true;
                        builder.SetTrigger(new TimeTrigger(60, false));
                        builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                        builder.AddCondition(new SystemCondition(SystemConditionType.BackgroundWorkCostNotHigh));
                        reg = builder.Register();
                        reg.Completed += (s, e) =>
                        {
                            Debug.WriteLine("News Notifier background task complete");
                        };
                        break;
                }
            }
        }
    }
}

