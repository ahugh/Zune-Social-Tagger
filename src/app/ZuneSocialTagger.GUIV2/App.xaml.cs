﻿using Caliburn.PresentationFramework.ApplicationModel;
using Microsoft.Practices.Unity;
using ZuneSocialTagger.Core.ZuneDatabase;
using ZuneSocialTagger.GUIV2.Models;
using ZuneSocialTagger.GUIV2.ViewModels;

namespace ZuneSocialTagger.GUIV2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : CaliburnApplication
    {
        private readonly UnityContainer _container;

        public App()
        {
            _container = new UnityContainer();
            _container.RegisterType<IZuneWizardModel, ZuneWizardModel>(new ContainerControlledLifetimeManager());


            //setting the SelectAutoFilesViewModel to be a singleton, the database wont be loaded each time the viewmodel is constructed now
            _container.RegisterType<SelectAudioFilesViewModel, SelectAudioFilesViewModel>(
                new ContainerControlledLifetimeManager());

            _container.RegisterType<IZuneDatabaseReader, ZuneDatabaseReader>();
            _container.RegisterInstance(_container);
        }

        protected override object CreateRootModel()
        {
            return _container.Resolve<ApplicationModel>();
        }
    }
}
