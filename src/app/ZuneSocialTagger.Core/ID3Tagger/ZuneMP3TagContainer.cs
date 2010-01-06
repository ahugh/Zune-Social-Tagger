using System;
using System.Collections.Generic;
using ID3Tag;
using ID3Tag.HighLevel;
using ID3Tag.HighLevel.ID3Frame;
using System.Linq;
using System.Text;

namespace ZuneSocialTagger.Core.ID3Tagger
{
    /// <summary>
    /// Updates a pre-existing TagContainer with new zune PRIV tags
    /// </summary>
    public class ZuneMP3TagContainer : IZuneTagContainer
    {
        private readonly TagContainer _container;

        public ZuneMP3TagContainer(TagContainer container)
        {
            _container = container;        
        }

        public IEnumerable<MediaIdGuid> ReadMediaIds()
        {
            //OfType instead of cast because the container could contain other types other than private frames 
            //and we only want private frames
            return from frame in _container.OfType<PrivateFrame>()
                   where MediaIds.Ids.Contains(frame.Owner)
                   select new MediaIdGuid(frame.Owner, new Guid(frame.Data));

        }

        public void AddZuneMediaId(MediaIdGuid mediaIDGuid)
        {
            PrivateFrame newFrame = new PrivateFrame(mediaIDGuid.MediaId, mediaIDGuid.Guid.ToByteArray());


            //frame owner is a unique id identifying a private field so we can
            //be sure that there's only one
            PrivateFrame existingFrame = (from frame in _container.OfType<PrivateFrame>()
                                          where frame.Owner == newFrame.Owner
                                          where frame.Data != newFrame.Data
                                          select frame).FirstOrDefault();

            //if the frame already exists and the data inside is different then remove it
            if (existingFrame != null)
                _container.Remove(existingFrame);

            _container.Add(newFrame);
        }

        public MetaData ReadMetaData()
        {
            IEnumerable<TextFrame> allTextFrames = from frame in _container.OfType<TextFrame>()
                                                   select frame;

            return  new MetaData()
                       {
                           AlbumArtist = GetValue(allTextFrames, "TPE2"),
                           ContributingArtists = GetValue(allTextFrames, "TPE1").Split('/'),
                           AlbumName = GetValue(allTextFrames, "TALB"),
                           Title = GetValue(allTextFrames, "TIT2"),
                           Year = GetValue(allTextFrames, "TYER"),
                           DiscNumber = GetValue(allTextFrames,"TPOS"),
                           Genre = GetValue(allTextFrames,"TCON"),
                           TrackNumber = GetValue(allTextFrames,"TRCK")
                       };

        }


        public void AddMetaData(MetaData metaData)
        {
            foreach (var textFrame in CreateTextFramesFromMetaData(metaData))
            {
                TextFrame tempTextFrame = textFrame;

                TextFrame existingFrame = (from frame in _container.OfType<TextFrame>()
                                           where frame.Descriptor.ID == tempTextFrame.Descriptor.ID
                                           select frame).FirstOrDefault();


                if (existingFrame != null)
                    _container.Remove(existingFrame);

                _container.Add(textFrame);
            }
        }

        public void WriteToFile(string filePath)
        {
            Id3TagManager.WriteV2Tag(filePath,_container);
        }

        private static IEnumerable<TextFrame> CreateTextFramesFromMetaData(MetaData metaData)
        {
            var contribArtists = string.Join("/", metaData.ContributingArtists.ToArray());

            yield return new TextFrame("TPE2", metaData.AlbumArtist, Encoding.Default);
            yield return new TextFrame("TPE1", contribArtists, Encoding.Default);
            yield return new TextFrame("TALB", metaData.AlbumName, Encoding.Default);
            yield return new TextFrame("TPOS", metaData.DiscNumber, Encoding.Default);
            yield return new TextFrame("TCON", metaData.Genre, Encoding.Default);
            yield return new TextFrame("TIT2", metaData.Title, Encoding.Default);
            yield return new TextFrame("TRCK", metaData.TrackNumber, Encoding.Default);
            yield return new TextFrame("TYER", metaData.Year, Encoding.Default);
        }

        public TagContainer GetContainer()
        {
            return this._container;
        }

        //private Image ReadImage()
        //{
        //    var pictureFrame = _container.OfType<PictureFrame>().Select(frame => frame).FirstOrDefault();

        //    if (pictureFrame != null)
        //        return pictureFrame.Type == FrameType.Picture
        //                   ? Image.FromStream(new MemoryStream(pictureFrame.PictureData))
        //                   : null;

        //    return null;
        //}

        private static string GetValue(IEnumerable<TextFrame> textFrames, string key)
        {
            TextFrame result = textFrames.Where(x => x.Descriptor.ID == key).SingleOrDefault();

            return result != null ? result.Content : string.Empty;
        }
    }
}