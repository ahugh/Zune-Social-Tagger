using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System;
using ZuneSocialTagger.Core.ID3Tagger;

namespace ZuneSocialTagger.GUIV2.Models
{
    public class DetailRow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public DetailRow(SongWithNumberAndGuid fileSong)
        {
            this.FileSong = fileSong;
        }

        public MediaIdGuid AlbumArtistGuid { get; set; }
        public MediaIdGuid AlbumMediaGuid { get; set; }

        public FilePathAndContainer SongPathAndContainer { get; set; }

        private ObservableCollection<SongWithNumberAndGuid> _songsFromWebsite;
        public ObservableCollection<SongWithNumberAndGuid> SongsFromWebsite
        {
            get { return _songsFromWebsite; }
            set
            {
                _songsFromWebsite = value;
                SelectedSong = GetSongFromSongsFromFileIfItAvailable();
            }
        }


        private SongWithNumberAndGuid _fileSong;
        public SongWithNumberAndGuid FileSong
        {
            get
            {
                return _fileSong;
            }
            set
            {
                _fileSong = value;
                OnPropertyChanged("FileSong");
            }

        }

        private SongWithNumberAndGuid _selectedSong;
        public SongWithNumberAndGuid SelectedSong
        {
            get
            {
                return _selectedSong;
            }
            set
            {
                _selectedSong = value;
                OnPropertyChanged("SelectedSong");
            }

        }

        /// <summary>
        /// Updates the container with the new details selected by the user
        /// </summary>
        public void UpdateContainer()
        {
            ZuneTagContainer container = this.SongPathAndContainer.Container;

            //VERY IMPORTANT WE DO NOT WRITE BLANK GUIDS
            if (SelectedSong.Guid != Guid.Empty)
            {
                container.Add(AlbumArtistGuid);
                container.Add(AlbumMediaGuid);
                container.Add(new MediaIdGuid() { Guid = this.SelectedSong.Guid, MediaId = MediaIds.ZuneMediaID });
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler changed = PropertyChanged;
            if (changed != null) changed(this, new PropertyChangedEventArgs(propertyName));
        }

        private SongWithNumberAndGuid GetSongFromSongsFromFileIfItAvailable()
        {
            IEnumerable<SongWithNumberAndGuid> foundSongs = SongsFromWebsite.Where(song => song.Title == FileSong.Title);

            return foundSongs.Count() > 0 ? foundSongs.First() : new SongWithNumberAndGuid();
        }
    }
}