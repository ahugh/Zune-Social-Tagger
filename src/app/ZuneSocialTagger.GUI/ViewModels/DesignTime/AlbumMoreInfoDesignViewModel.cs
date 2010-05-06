using System;
using System.Collections.Generic;
using ZuneSocialTagger.Core;
using ZuneSocialTagger.GUI.Models;

namespace ZuneSocialTagger.GUI.ViewModels.DesignTime
{
    public class AlbumMoreInfoDesignViewModel
    {
        public List<Song> Tracks { get; set; }
        public ExpandedAlbumDetailsViewModel AlbumDetailsFromFile { get; set; }
        public ExpandedAlbumDetailsViewModel AlbumDetailsFromWebsite { get; set; }

        public AlbumMoreInfoDesignViewModel()
        {
            this.AlbumDetailsFromFile = new ExpandedAlbumDetailsViewModel()
            {
                Artist = "Pendulum",
                ArtworkUrl = "http://images.play.com/covers/12691916x.jpg",
                SongCount = "10",
                Title = "Immersion",
                Year = "2010"
            };

            this.AlbumDetailsFromWebsite = new ExpandedAlbumDetailsViewModel
            {
                Artist = "Pendulum",
                ArtworkUrl = "http://images.play.com/covers/12691916x.jpg",
                SongCount = "10",
                Title = "Immersion",
                Year = "2010"
            };

            this.Tracks = new List<Song>();

            var metaData1 = new MetaData() { TrackNumber = "1", Title = "Prelude" };
            var metaData2 = new MetaData() { TrackNumber = "2", Title = "Slam" };
            var metaData3 = new MetaData() { TrackNumber = "3", Title = "Plasticworld" };
            var metaData4 = new MetaData() { TrackNumber = "4", Title = "Fasten Your Seatbelt" };
            var metaData5 = new MetaData() { TrackNumber = "5", Title = "Through The Loop" };
            var metaData6 = new MetaData() { TrackNumber = "6", Title = "Sounds Of Life" };
            var metaData7 = new MetaData() { TrackNumber = "7", Title = "Girl In The Fire" };
            var metaData8 = new MetaData() { TrackNumber = "8", Title = "Tarantula" };
            var metaData9 = new MetaData() { TrackNumber = "9", Title = "Out Here" };

            this.Tracks.Add(new Song()
            {
                MetaData = metaData1
            });
            this.Tracks.Add(new Song()
            {
                MetaData = metaData2
            });
            this.Tracks.Add(new Song()
            {
                MetaData = metaData3
            });
            this.Tracks.Add(new Song()
            {
                MetaData = metaData4
            });
            this.Tracks.Add(new Song()
            {
                MetaData = metaData5
            });
            this.Tracks.Add(new Song()
            {
                MetaData = metaData6
            });
            this.Tracks.Add(new Song()
            {
                MetaData = metaData7
            });
            this.Tracks.Add(new Song()
            {
                MetaData = metaData8
            });
            this.Tracks.Add(new Song()
            {
                MetaData = metaData9
            });
        }
    }
}