using leetreveil.AutoUpdate.Framework;
using Ninject;
using ZuneSocialTagger.Core.ZuneDatabase;
using ZuneSocialTagger.GUIV2.Models;
using ZuneSocialTagger.GUIV2.ViewModels;

namespace ZuneSocialTagger.GUIV2
{
    /// <summary>
    /// Handles the creation and lifetime of view models, views should databind to properties set here
    /// </summary>
    public class ViewModelLocator
    {
        private static readonly StandardKernel Container = new StandardKernel();

        static ViewModelLocator()
        {
            Container.Bind<IZuneWizardModel>().To<ZuneWizardModel>().InSingletonScope();
            Container.Bind<IZuneDatabaseReader>().To<TestZuneDatabaseReader>().InSingletonScope();
            Container.Bind<ApplicationViewModel>().ToSelf().InSingletonScope();
            Container.Bind<InlineZuneMessageViewModel>().ToSelf().InSingletonScope();
            Container.Bind<CachedZuneDatabaseReader>().ToSelf().InSingletonScope();
        }

        public ApplicationViewModel Application
        {
            get { return Container.Get<ApplicationViewModel>(); }
        }

        public UpdateViewModel Update
        {
            get { return new UpdateViewModel(UpdateManager.Instance.NewUpdate.Version); }
        }

        public AboutViewModel About
        {
            get { return Container.Get<AboutViewModel>(); }
        }

        public SuccessViewModel Success
        {
            get { return Container.Get<SuccessViewModel>(); }
        }

        public InlineZuneMessageViewModel InlineZuneMessageView
        {
            get { return Container.Get<InlineZuneMessageViewModel>(); }
        }
    }
}