using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using System.Xml.Serialization;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using ZuneSocialTagger.Core;
using ZuneSocialTagger.Core.ZuneDatabase;
using ZuneSocialTagger.Core.ZuneWebsite;
using ZuneSocialTagger.GUI.Models;
using Album = ZuneSocialTagger.Core.ZuneDatabase.Album;
using Track = ZuneSocialTagger.Core.ZuneDatabase.Track;

namespace ZuneSocialTagger.GUI.ViewModels
{
    public class AlbumDetailsViewModel : ViewModelBaseExtended
    {
        private readonly IZuneDatabaseReader _dbReader;
        private readonly ZuneWizardModel _model;
        private Album _zuneAlbumMetaData;
        private Album _webAlbumMetaData;
        private LinkStatus _linkStatus;

        public AlbumDetailsViewModel(IZuneDatabaseReader dbReader, ZuneWizardModel model)
        {
            _dbReader = dbReader;
            _model = model;

            this.DelinkCommand = new RelayCommand(DelinkAlbum);
            this.LinkCommand = new RelayCommand(LinkAlbum);
            this.RefreshCommand = new RelayCommand(RefreshAlbum);
        }

        public AlbumDetailsViewModel()
        {
            //used for serialization purposes
        }

        [XmlIgnore]
        public RelayCommand RefreshCommand { get; private set; }
        [XmlIgnore]
        public RelayCommand LinkCommand { get; private set; }
        [XmlIgnore]
        public RelayCommand DelinkCommand { get; private set; }

        public Album ZuneAlbumMetaData
        {
            get { return _zuneAlbumMetaData; }
            set
            {
                _zuneAlbumMetaData = value;
                RaisePropertyChanged(() => this.ZuneAlbumMetaData);
            }
        }

        public Album WebAlbumMetaData
        {
            get { return _webAlbumMetaData; }
            set
            {
                _webAlbumMetaData = value;
                RaisePropertyChanged(() => this.WebAlbumMetaData);
            }
        }

        public LinkStatus LinkStatus
        {
            get { return _linkStatus; }
            set
            {
                _linkStatus = value;
                RaisePropertyChanged(() => this.LinkStatus);
                RaisePropertyChanged(() => this.CanDelink);
                RaisePropertyChanged(() => this.CanLink);
            }
        }

        [XmlIgnore]
        public bool CanDelink
        {
            get 
            {
                return _linkStatus != LinkStatus.Unlinked && _linkStatus != LinkStatus.Unknown;
            }
        }

        [XmlIgnore]
        public bool CanLink
        {
            get {
                return _linkStatus != LinkStatus.Unknown;
            }
        }

        [XmlIgnore]
        /// <summary>
        /// Set this boolean value when you want the view to refresh the details from the zune database
        /// </summary>
        public bool NeedsRefreshing { get; set; }

        public void LinkAlbum()
        {
            var albumDetails = this.ZuneAlbumMetaData;

            if (!DoesAlbumExistInDbAndDisplayError(albumDetails)) return;

            IEnumerable<Track> tracksForAlbum = _dbReader.GetTracksForAlbum(albumDetails.MediaId);

            var containers = GetFileNamesAndContainers(tracksForAlbum);

            var selectedAlbum = new SelectedAlbum();

            foreach (var item in containers)
                selectedAlbum.Tracks.Add(new Song(item.Key, item.Value));

            if (containers.Count > 0)
            {
                selectedAlbum.ZuneAlbumMetaData = new ExpandedAlbumDetailsViewModel
                                                      {
                                                          Artist = albumDetails.AlbumArtist,
                                                          Title = albumDetails.AlbumTitle,
                                                          ArtworkUrl = albumDetails.ArtworkUrl,
                                                          SongCount = albumDetails.TrackCount.ToString(),
                                                          Year = albumDetails.ReleaseYear.ToString()
                                                      };

                selectedAlbum.AlbumDetails = this;

                _model.SelectedAlbum = selectedAlbum;
                _model.SearchText = albumDetails.AlbumTitle + " " + albumDetails.AlbumArtist;

                //tell the application to switch to the search view
                Messenger.Default.Send(typeof(SearchViewModel));
            }
        }

        public void DelinkAlbum()
        {
            if (!DoesAlbumExistInDbAndDisplayError(this.ZuneAlbumMetaData)) return;

            Mouse.OverrideCursor = Cursors.Wait;

            //TODO: fix bug where application crashes when removing an album that is currently playing

            var tracksForAlbum = _dbReader.GetTracksForAlbum(this.ZuneAlbumMetaData.MediaId).ToList();

            //_dbReader.RemoveAlbumFromDatabase(this.ZuneAlbumMetaData.MediaId);

            //make sure we can actually read all the files before doing anything to them
            var containers = GetFileNamesAndContainers(tracksForAlbum);

            foreach (var item in containers)
            {
                item.Value.RemoveZuneAttribute("WM/WMContentID");
                item.Value.RemoveZuneAttribute("WM/WMCollectionID");
                item.Value.RemoveZuneAttribute("WM/WMCollectionGroupID");
                item.Value.RemoveZuneAttribute("ZuneCollectionID");
                item.Value.RemoveZuneAttribute("WM/UniqueFileIdentifier");
                item.Value.RemoveZuneAttribute("ZuneCollectionID");
                item.Value.RemoveZuneAttribute("ZuneUserEditedFields");
                item.Value.RemoveZuneAttribute(ZuneIds.Album);
                item.Value.RemoveZuneAttribute(ZuneIds.Artist);
                item.Value.RemoveZuneAttribute(ZuneIds.Track);

                item.Value.WriteToFile(item.Key);
            }

            //foreach (var track in tracksForAlbum)
            //    _dbReader.AddTrackToDatabase(track.FilePath);

            Mouse.OverrideCursor = null;

            if (containers.Count > 0)
                Messenger.Default.Send(new ErrorMessage(ErrorMode.Warning,
                                                        "Album should now be de-linked. You may need to " +
                                                        "remove then re-add the album for the changes to take effect."));

            //force a refresh on the album to see if the de-link worked
            //this probably wont work because the zunedatabase does not correctly change the albums
            //details when delinking, but does when linking
            RefreshAlbum();
        }

        public void RefreshAlbum()
        {
            if (!DoesAlbumExistInDbAndDisplayError(this.ZuneAlbumMetaData)) return;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                Album albumMetaData = _dbReader.GetAlbum(this.ZuneAlbumMetaData.MediaId);

                this.LinkStatus = LinkStatus.Unknown;

                if (albumMetaData.AlbumMediaId != Guid.Empty)
                {
                    var downloader = new AlbumDetailsDownloader(String.Concat(Urls.Album, albumMetaData.AlbumMediaId));

                    downloader.DownloadCompleted += (alb, state) =>
                    {
                        if (alb != null)
                        {
                            this.LinkStatus = SharedMethods.GetAlbumLinkStatus(alb.AlbumTitle,
                                                        alb.AlbumArtist,
                                                        albumMetaData.AlbumTitle,
                                                        albumMetaData.AlbumArtist);

                            this.WebAlbumMetaData = new Album
                            {
                                AlbumArtist = alb.AlbumArtist,
                                AlbumTitle = alb.AlbumTitle,
                                ArtworkUrl = alb.ArtworkUrl
                            };
                        }
                        else
                        {
                            this.LinkStatus = LinkStatus.Unavailable;
                        }
                    };

                    downloader.DownloadAsync();
                }
                else
                {
                    this.LinkStatus = LinkStatus.Unlinked;
                }
            });
        }

        private bool DoesAlbumExistInDbAndDisplayError(Album selectedAlbum)
        {
            bool doesAlbumExist = _dbReader.DoesAlbumExist(selectedAlbum.MediaId);

            if (!doesAlbumExist)
            {
                Messenger.Default.Send(new ErrorMessage(ErrorMode.Error,
                                                        "Could not find album, you may need to refresh the database."));
                return false;
            }

            return true;
        }

        private static Dictionary<string, IZuneTagContainer> GetFileNamesAndContainers(IEnumerable<Track> tracks)
        {
            var albumContainers = new Dictionary<string, IZuneTagContainer>();

            foreach (var track in tracks)
            {
                try
                {
                    IZuneTagContainer container = ZuneTagContainerFactory.GetContainer(track.FilePath);
                    albumContainers.Add(track.FilePath, container);
                }
                catch (Exception ex)
                {
                    Messenger.Default.Send(new ErrorMessage(ErrorMode.Error, ex.Message));
                    break;
                }
            }

            return albumContainers;
        }
    }
}