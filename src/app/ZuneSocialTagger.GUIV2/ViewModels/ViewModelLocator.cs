using System;
using GalaSoft.MvvmLight;
using leetreveil.AutoUpdate.Framework;
using ZuneSocialTagger.Core.ZuneDatabase;
using ZuneSocialTagger.GUIV2.Models;
using Ninject;
using ZuneSocialTagger.ZunePlugin;

namespace ZuneSocialTagger.GUIV2.ViewModels
{
    /// <summary>
    /// Handles the creation and lifetime of view models, views should databind to properties set here
    /// </summary>
    public class ViewModelLocator
    {
        static readonly StandardKernel Container = new StandardKernel();

        static ViewModelLocator()
        {
            if (ViewModelBase.IsInDesignModeStatic)
            {
                //bind to design database
            }
            else
            {
                Container.Bind<IZuneWizardModel>().To<ZuneWizardModel>().InSingletonScope();
                Container.Bind<IZuneDatabaseReader>().To<ZuneDatabaseReader>().InSingletonScope();
                Container.Bind<IZuneDbAdapter>().To<CachedZuneDatabaseReader>().InSingletonScope();
            }

            Container.Bind<ApplicationModel>().ToSelf().InSingletonScope();
            Container.Bind<WebAlbumListViewModel>().ToSelf().InSingletonScope();
            Container.Bind<SearchHeaderViewModel>().ToSelf().InSingletonScope();
        }

        public ApplicationModel Application
        {
            get
            {
                try
                {
                    return Container.Get<ApplicationModel>();
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }

        public SelectAudioFilesViewModel SelectAudioFiles
        {
            get
            {
                return Container.Get<SelectAudioFilesViewModel>();
            }
        }

        public WebAlbumListViewModel WebAlbumListView
        {
            get
            {
                return Container.Get<WebAlbumListViewModel>();
            }
        }

        public SearchViewModel SearchView
        {
            get
            {
                return Container.Get<SearchViewModel>();
            }
        }

        public UpdateViewModel Update
        {
            get
            {
                return new UpdateViewModel(UpdateManager.Instance.NewUpdate.Version);
            }
        }

        public AboutViewModel About
        {
            get
            {
                return new AboutViewModel();
            }
        }
        public SuccessViewModel Success
        {
            get
            {
                return null;
            }
        }
    }
}