using System;
using System.Collections.Generic;

namespace ZuneSocialTagger.Core
{
    public class MetaData
    {
        public string Genre { get; set; }
        public string DiscNumber { get; set; }
        public string AlbumName { get; set; }
        public string Year { get; set; }
        public string Title { get; set; }
        public string AlbumArtist { get; set; }
        public string TrackNumber { get; set; }
        public IEnumerable<String> ContributingArtists { get; set; }
    }
}