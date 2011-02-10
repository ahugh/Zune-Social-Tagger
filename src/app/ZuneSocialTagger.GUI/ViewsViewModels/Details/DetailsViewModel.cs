using System.Collections.Generic;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using ZuneSocialTagger.Core;
using ZuneSocialTagger.Core.ZuneWebsite;
using ZuneSocialTagger.GUI.Models;
using ZuneSocialTagger.GUI.Properties;
using System.Linq;
using ZuneSocialTagger.GUI.ViewsViewModels.Search;
using ZuneSocialTagger.GUI.ViewsViewModels.Shared;
using ZuneSocialTagger.Core.IO;
using System;
using GalaSoft.MvvmLight.Messaging;
using ZuneSocialTagger.GUI.ViewsViewModels.Success;
using ZuneSocialTagger.GUI.Controls;

namespace ZuneSocialTagger.GUI.ViewsViewModels.Details
{
    public class DetailsViewModel : ViewModelBase
    {
        private readonly IViewLocator _locator;
        private readonly SharedModel _sharedModel;

        public DetailsViewModel(IViewLocator locator, SharedModel sharedModel)
        {
            _locator = locator;
            _sharedModel = sharedModel;
        }

        public WebAlbum WebAlbum { get { return _sharedModel.WebAlbum; } }
        public IEnumerable<IZuneTagContainer> FileTracks { get { return _sharedModel.SongsFromFile; } }
        public ExpandedAlbumDetailsViewModel AlbumDetailsFromWebsite { get { return _sharedModel.AlbumDetailsFromWeb; } }
        public ExpandedAlbumDetailsViewModel AlbumDetailsFromFile { get { return _sharedModel.AlbumDetailsFromFile; } }

        private RelayCommand _saveCommand;
        public RelayCommand SaveCommand
        {
            get
            {
                if (_saveCommand == null)
                    _saveCommand = new RelayCommand(Save);

                return _saveCommand;
            }
        }

        private RelayCommand _moveBackCommand;
        public RelayCommand MoveBackCommand
        {
            get
            {
                if (_moveBackCommand == null)
                    _moveBackCommand = new RelayCommand(MoveBack);

                return _moveBackCommand;
            }
        }

        private List<object> _rows;
        public List<object> Rows 
        {
            get 
            {
                if (_rows == null)
                    _rows = new List<object>();

                return _rows;
            }
        }

        public bool UpdateAlbumInfo
        {
            get { return Settings.Default.UpdateAlbumInfo; }
            set
            {
                if (value != UpdateAlbumInfo)
                {
                    Settings.Default.UpdateAlbumInfo = value;
                }
            }
        }

        public void PopulateRows()
        {
            this.Rows.Clear();

            var discs = FileTracks.Select(x=> SharedMethods.DiscNumberConverter(x.MetaData.DiscNumber)).Distinct().ToList();

            //get lists of tracks by discNumer
            var tracksByDiscNumber =
                discs.Select(number => FileTracks.Where(x => SharedMethods.DiscNumberConverter(x.MetaData.DiscNumber) == number));

            //if the disc count is just one then dont add any headers
            if (tracksByDiscNumber.Count() == 1)
                tracksByDiscNumber.First().ForEach(AddTrackRow);
            else
            {
                foreach (IEnumerable<IZuneTagContainer> discTracks in tracksByDiscNumber)
                {
                    AddHeaderRow(discTracks.First()); //add header for each disc before adding the tracks
                    discTracks.ForEach(AddTrackRow);
                }
            }
        }

        private void AddHeaderRow(IZuneTagContainer track)
        {
            this.Rows.Add(new DiscHeader
            {
                DiscNumber = "Disc " + track.MetaData.DiscNumber
            });
        }

        private void AddTrackRow(IZuneTagContainer track)
        {
            var detailRow = new DetailRow();

            detailRow.SongDetails = new TrackWithTrackNum
            {
                TrackNumber = SharedMethods.TrackNumberConverter(track.MetaData.TrackNumber),
                TrackTitle = track.MetaData.Title,
                BackingData = track
            };

            foreach (WebTrack webTrack in WebAlbum.Tracks)
            {
                detailRow.AvailableZuneTracks.Add(new TrackWithTrackNum
                {
                    TrackNumber = webTrack.TrackNumber,
                    TrackTitle = webTrack.Title,
                    BackingData = webTrack
                });
            }

            detailRow.MatchTheSelectedSongToTheAvailableSongs();

            this.Rows.Add(detailRow);
        }

        private void Save()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            var uaeExceptions = new List<UnauthorizedAccessException>();

            foreach (var row in Rows.OfType<DetailRow>())
            {
                try
                {
                    if (row.SelectedSong != null)
                    {
                        var container = (IZuneTagContainer)row.SongDetails.BackingData;
                        container.RemoveZuneAttribute("WM/WMContentID");
                        container.RemoveZuneAttribute("WM/WMCollectionID");
                        container.RemoveZuneAttribute("WM/WMCollectionGroupID");
                        container.RemoveZuneAttribute("ZuneCollectionID");
                        container.RemoveZuneAttribute("WM/UniqueFileIdentifier");

                        var webTrack = (WebTrack)row.SelectedSong.BackingData;
                        container.AddZuneAttribute(new ZuneAttribute(ZuneIds.Album, webTrack.AlbumMediaId));
                        container.AddZuneAttribute(new ZuneAttribute(ZuneIds.Artist, webTrack.ArtistMediaId));
                        container.AddZuneAttribute(new ZuneAttribute(ZuneIds.Track, webTrack.MediaId));

                        if (UpdateAlbumInfo)
                        {
                            const string message = "You have 'Update album information' checked, this will modify the track's metadata " +
                                "with information from the zune marketplace. Are you sure?";

                            DetailRow row1 = row;
                            ZuneMessageBox.Show(new ErrorMessage(ErrorMode.Warning, message), () =>
                                {
                                    container.AddMetaData(CreateMetaDataFromWebDetails((WebTrack)row1.SelectedSong.BackingData));
                                });
                        }

                        container.WriteToFile();
                        //TODO: run a verifier over whats been written to ensure that the tags have actually been written to file
                    }
                }
                catch (UnauthorizedAccessException uae)
                {
                    uaeExceptions.Add(uae);
                    //TODO: better error handling
                }
            }

            if (uaeExceptions.Count > 0)
            {   //usually occurs when a file is readonly
                Messenger.Default.Send(new ErrorMessage(ErrorMode.Error,
                                    "One or more files could not be written to. Have you checked the files are not marked read-only?"));
            }
            else
            {
                _locator.SwitchToView<SuccessView, SuccessViewModel>();
            }

            Mouse.OverrideCursor = null;
        }

        private MetaData CreateMetaDataFromWebDetails(WebTrack webTrack)
        {
            var metaData = new MetaData
                   {
                       AlbumArtist = this.WebAlbum.Artist,
                       AlbumName = this.WebAlbum.Title,
                       ContributingArtists = webTrack.ContributingArtists,
                       DiscNumber = webTrack.DiscNumber,
                       Genre = webTrack.Genre,
                       Title = webTrack.Title,
                       TrackNumber = webTrack.TrackNumber,
                       Year = this.WebAlbum.ReleaseYear
                   };

            //use album artist as song artist if there are no contributing artists
            if (metaData.ContributingArtists.Count() == 0)
                metaData.ContributingArtists.Add(metaData.AlbumArtist);

            return metaData;
        }

        private void MoveBack()
        {
            _locator.SwitchToView<SearchView,SearchViewModel>();
        }
    }
}