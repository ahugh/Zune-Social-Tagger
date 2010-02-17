using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Practices.Unity;
using ZuneSocialTagger.Core;
using ZuneSocialTagger.Core.ZuneWebsite;
using ZuneSocialTagger.GUIV2.Models;
using System.Threading;
using Caliburn.PresentationFramework.Screens;
using Album = ZuneSocialTagger.Core.Album;
using AlbumDocumentReader = ZuneSocialTagger.Core.ZuneWebsite.AlbumDocumentReader;


namespace ZuneSocialTagger.GUIV2.ViewModels
{
    public class SearchResultsViewModel : Screen
    {
        private readonly IUnityContainer _container;
        private readonly IZuneWizardModel _model;
        private bool _isLoading;
        private bool _canMoveNext;
        private SearchResultsDetailViewModel _searchResultsDetailViewModel;

        public SearchResultsViewModel(IUnityContainer container, IZuneWizardModel model)
        {
            _container = container;
            _model = model;
            this.SearchResultsDetailViewModel = new SearchResultsDetailViewModel();
        }


        public ObservableCollection<Album> Albums
        {
            get { return this.SearchBarViewModel.SearchResults; }
        }

        public SearchResultsDetailViewModel SearchResultsDetailViewModel
        {
            get { return _searchResultsDetailViewModel; }
            set
            {
                    _searchResultsDetailViewModel = value;
                   NotifyOfPropertyChange(() => SearchResultsDetailViewModel);
            }
        }

        public string AlbumCount
        {
            get { return String.Format("ALBUMS ({0})", Albums.Count); }
        }

        public SearchBarViewModel SearchBarViewModel
        {
            get { return _model.SearchBarViewModel; }
        }

        public WebsiteAlbumMetaDataViewModel AlbumDetailsFromFile
        {
            get { return _model.AlbumDetailsFromFile; }
        }

        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                NotifyOfPropertyChange(() => IsLoading);
            }
        }

        public void MoveBack()
        {
            _model.CurrentPage = _container.Resolve<SearchViewModel>();
        }

        public void MoveNext()
        {
            _model.CurrentPage = _container.Resolve<DetailsViewModel>();
        }

        public void LoadAlbum(Album album)
        {
            this.IsLoading = true;

            if (album == null) return;

            string fullUrlToAlbumXmlDetails = String.Concat(Urls.Album, album.AlbumMediaID);

            ThreadPool.QueueUserWorkItem(_ =>
             {
                 try
                 {
                     var reader = new AlbumDocumentReader(fullUrlToAlbumXmlDetails);

                     IEnumerable<Track> tracks = reader.Read();

                     UpdateAlbumMetaDataViewModel(tracks);
                     AddSelectedSongs(tracks);


                     this.IsLoading = false;
                 }
                 catch (Exception)
                 {
                     this.SearchResultsDetailViewModel.SelectedAlbumTitle =
                            "Could not get album details";
                 }
             });

        }

        private void UpdateAlbumMetaDataViewModel(IEnumerable<Track> tracks)
        {
            MetaData firstTracksMetaData = tracks.First().MetaData;

            _model.AlbumDetailsFromWebsite = new WebsiteAlbumMetaDataViewModel
                                                 {
                                                     Title = firstTracksMetaData.AlbumName,
                                                     Artist = firstTracksMetaData.AlbumArtist,
                                                     ArtworkUrl = tracks.First().ArtworkUrl,
                                                     Year = firstTracksMetaData.Year,
                                                     SongCount = tracks.Count().ToString()
                                                 };
        }

        private void AddSelectedSongs(IEnumerable<Track> tracks)
        {
            this.SearchResultsDetailViewModel = new SearchResultsDetailViewModel { SelectedAlbumTitle = tracks.First().MetaData.AlbumName };

            foreach (var track in tracks)
                this.SearchResultsDetailViewModel.SelectedAlbumSongs.Add(track);

            foreach (var row in _model.Rows)
            {
                row.SongsFromWebsite = this.SearchResultsDetailViewModel.SelectedAlbumSongs;
                row.Tracks = tracks;
            }
        }
    }
}