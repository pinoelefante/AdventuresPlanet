﻿using AdventuresPlanetRuntime;
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
            #endregion

            #region ViewModels
            SimpleIoc.Default.Register<NewsPageViewModel>();
            SimpleIoc.Default.Register<PodcastPageViewModel>();
            SimpleIoc.Default.Register<RecensioniPageViewModel>();
            SimpleIoc.Default.Register<SoluzioniPageViewModel>();
            SimpleIoc.Default.Register<GalleriaPageViewModel>();
            SimpleIoc.Default.Register<ImageViewerViewModel>();
            SimpleIoc.Default.Register<BrowserVideoPlayerViewModel>();
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
    }
}