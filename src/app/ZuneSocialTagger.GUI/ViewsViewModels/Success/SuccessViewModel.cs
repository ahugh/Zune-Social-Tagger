using GalaSoft.MvvmLight.Command;
using ZuneSocialTagger.GUI.ViewsViewModels.Shared;
using ZuneSocialTagger.GUI.ViewsViewModels.WebAlbumList;
using System.Windows.Input;

namespace ZuneSocialTagger.GUI.ViewsViewModels.Success
{
    public class SuccessViewModel : ViewModelBase
    {
        private ViewLocator _locator;
        private SharedModel _sharedModel;

        public SuccessViewModel(ViewLocator locator, SharedModel sharedModel)
        {
            _locator = locator;
            _sharedModel = sharedModel;
        }

        private ICommand _okCommand;
        public ICommand OKCommand
        {
            get
            {
                if (_okCommand == null)
                    _okCommand = new RelayCommand(RefreshAlbum);

                return _okCommand;
            }
        }

        private ExpandedAlbumDetailsViewModel _albumDetailsFromFile;
        public ExpandedAlbumDetailsViewModel AlbumDetailsFromFile
        {
            get
            {
                if (_albumDetailsFromFile == null)
                    _albumDetailsFromFile = _sharedModel.AlbumDetailsFromFile;

                return _albumDetailsFromFile;
            }
        }

        private ExpandedAlbumDetailsViewModel _albumDetailsFromWebsite;
        public ExpandedAlbumDetailsViewModel AlbumDetailsFromWebsite
        {
            get
            {
                if (_albumDetailsFromWebsite == null)
                    _albumDetailsFromWebsite = _sharedModel.AlbumDetailsFromWeb;

                return _albumDetailsFromWebsite;
            }
        }

        private void RefreshAlbum()
        {
            var viewModel = _locator.SwitchToFirstView();

            if (viewModel is WebAlbumListViewModel)
            {
                var webAlbumListViewModel = (WebAlbumListViewModel) viewModel;
                if (webAlbumListViewModel.SelectedAlbum != null)
                    webAlbumListViewModel.SelectedAlbum.RefreshAlbum();
            }
        }
    }
}