using System;
using System.Windows.Controls;
using Ninject;
using ZuneSocialTagger.GUI.ViewsViewModels.WebAlbumList;
using ZuneSocialTagger.GUI.ViewsViewModels.SelectAudioFiles;

namespace ZuneSocialTagger.GUI.ViewsViewModels.Shared
{
    /// <summary>
    /// Handles switching between views
    /// </summary>
    public class ViewLocator : IViewLocator
    {
        private readonly IKernel _container;
        private UserControl _firstView { get; set; }

        public event Action<UserControl> SwitchToViewRequested = delegate { };

        public ViewLocator(IKernel container)
        {
            _container = container;
        }

        public void SwitchToFirstView()
        {
            SwitchToViewRequested.Invoke(_firstView);
        }

        public TViewModel SwitchToView<TView, TViewModel>() where TView : UserControl
        {
            TView viewToSwitchTo = Resolve<TView>();
            TViewModel viewModel = Resolve<TViewModel>();

            if (viewToSwitchTo is WebAlbumListView || viewToSwitchTo is SelectAudioFilesView)
                _firstView = viewToSwitchTo;

            viewToSwitchTo.DataContext = viewModel;

            SwitchToViewRequested.Invoke(viewToSwitchTo);

            return viewModel;
        }

        public T Resolve<T>()
        {
            return _container.Get<T>();
        }
    }
}