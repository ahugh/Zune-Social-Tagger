using System;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using ZuneSocialTagger.Core.ZuneWebsite;
using ZuneSocialTagger.GUI.Models;

namespace ZuneSocialTagger.GUI.ViewModels
{
    public class SearchViewModel : ViewModelBase
    {
        private readonly IZuneWizardModel _model;
        private readonly Dispatcher _dispatcher;
        private string _searchText;
        private bool _isSearching;
        private bool _canMoveNext;
        private SearchResultsViewModel _searchResultsViewModel;
        private bool _canShowResults;

        public SearchViewModel(IZuneWizardModel model,Dispatcher dispatcher)
        {
            _model = model;
            _dispatcher = dispatcher;

            this.MoveBackCommand = new RelayCommand(MoveBack);
            this.MoveNextCommand = new RelayCommand(MoveNext);

            this.AlbumDetails = model.SelectedAlbum.ZuneAlbumMetaData;
            this.SearchText = model.SearchText;

            Search();
        }

        public RelayCommand MoveBackCommand { get; private set; }
        public RelayCommand MoveNextCommand { get; private set; }
        public ExpandedAlbumDetailsViewModel AlbumDetails { get; set; }

        public SearchResultsViewModel SearchResultsViewModel
        {
            get { return _searchResultsViewModel; }
            set
            {
                _searchResultsViewModel = value;
                RaisePropertyChanged("SearchResultsViewModel");
            }
        }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value;
                RaisePropertyChanged("SearchText");
            }
        }

        public bool IsSearching
        {
            get { return _isSearching; }
            set
            {
                _isSearching = value;
                RaisePropertyChanged("IsSearching");
            }
        }

        public bool CanMoveNext
        {
            get { return _canMoveNext; }
            set
            {
                _canMoveNext = value;
                RaisePropertyChanged("CanMoveNext");
            }
        }

        public bool CanShowResults
        {
            get { return _canShowResults; }
            set
            {
                _canShowResults = value;
                RaisePropertyChanged("CanShowResults");
            }
        }

        public void SearchClicked()
        {
            this.IsSearching = true;
        }

        public void Search()
        {
            this.CanShowResults = false;
            this.CanMoveNext = false;
            this.IsSearching = true;
            this.SearchResultsViewModel = null;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                var albums = AlbumSearch.SearchFor(this.SearchText).ToList();
                this.CanMoveNext = albums.Count() > 0;
                this.CanShowResults = true;

                _dispatcher.Invoke(new Action(delegate {
                    this.SearchResultsViewModel = new SearchResultsViewModel(_model, _dispatcher, albums);
                }));

                SearchForArtists();

                this.IsSearching = false;
            });
        }

        private void SearchForArtists()
        {
            //ThreadPool.QueueUserWorkItem(_ =>
            //{
                var artists = ArtistSearch.SearchFor(this.SearchText).ToList();

                _dispatcher.Invoke(new Action(() => {
                    this.SearchResultsViewModel.LoadArtists(artists);
                }));

                this.IsSearching = false;
            //});
        }

        public void MoveBack()
        {
            Messenger.Default.Send(typeof(IFirstPage));
        }

        public void MoveNext()
        {
            Messenger.Default.Send(typeof(DetailsViewModel));
        }
    }
}