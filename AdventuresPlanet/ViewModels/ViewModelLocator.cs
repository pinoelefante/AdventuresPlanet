﻿using AdventuresPlanet.Services;
using AdventuresPlanetRuntime;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdventuresPlanet.ViewModels
{
    public class ViewModelLocator
    {
        static ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            #region Service
            SimpleIoc.Default.Register<AVPManager>();
            SimpleIoc.Default.Register<AVPDatabase>();
            SimpleIoc.Default.Register<AVPPreferiti>();
            SimpleIoc.Default.Register<DownloadService>();
            SimpleIoc.Default.Register<SettingsService>();
            #endregion

            #region ViewModels
            SimpleIoc.Default.Register<NewsPageViewModel>();
            SimpleIoc.Default.Register<PodcastPageViewModel>();
            SimpleIoc.Default.Register<RecensioniPageViewModel>();
            SimpleIoc.Default.Register<SoluzioniPageViewModel>();
            SimpleIoc.Default.Register<GalleriaPageViewModel>();
            SimpleIoc.Default.Register<ImageViewerViewModel>();
            SimpleIoc.Default.Register<BrowserVideoPlayerViewModel>();
            SimpleIoc.Default.Register<PreferitiViewModel>();
            SimpleIoc.Default.Register<DownloadPageViewModel>();
            SimpleIoc.Default.Register<SagaViewModel>();
            SimpleIoc.Default.Register<InfoViewModel>();
            SimpleIoc.Default.Register<OpzioniViewModel>();
            SimpleIoc.Default.Register<ExtraViewModel>();
            #endregion
        }
        public NewsPageViewModel NewsPageVM
        {
            get { return SimpleIoc.Default.GetInstance<NewsPageViewModel>(); }
        }
        public PodcastPageViewModel PodcastPageVM
        {
            get { return SimpleIoc.Default.GetInstance<PodcastPageViewModel>(); }
        }
        public RecensioniPageViewModel RecensioniPageVM
        {
            get { return SimpleIoc.Default.GetInstance<RecensioniPageViewModel>(); }
        }
        public SoluzioniPageViewModel SoluzioniPageVM
        {
            get { return SimpleIoc.Default.GetInstance<SoluzioniPageViewModel>(); }
        }
        public GalleriaPageViewModel GalleriePageVM
        {
            get { return SimpleIoc.Default.GetInstance<GalleriaPageViewModel>(); }
        }
        public ImageViewerViewModel ImageViewerPageVM
        {
            get { return SimpleIoc.Default.GetInstance<ImageViewerViewModel>(); }
        }
        public BrowserVideoPlayerViewModel BrowserPlayerPageVM
        {
            get { return SimpleIoc.Default.GetInstance<BrowserVideoPlayerViewModel>(); }
        }
        public PreferitiViewModel PreferitiVM
        {
            get { return SimpleIoc.Default.GetInstance<PreferitiViewModel>(); }
        }
        public DownloadPageViewModel DownloadVM
        {
            get { return SimpleIoc.Default.GetInstance<DownloadPageViewModel>(); }
        }
        public SagaViewModel SagaVM
        {
            get { return SimpleIoc.Default.GetInstance<SagaViewModel>(); }
        }
        public InfoViewModel InfoVM
        {
            get { return SimpleIoc.Default.GetInstance<InfoViewModel>(); }
        }
        public OpzioniViewModel OpzioniVM
        {
            get { return SimpleIoc.Default.GetInstance<OpzioniViewModel>(); }
        }
        public ExtraViewModel ExtraVM
        {
            get { return SimpleIoc.Default.GetInstance<ExtraViewModel>(); }
        }
    }
}
