using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace ClementineCloneUwp
{
    public static class AudioFileRetriever
    {
        public static double FormatTrackDuration(double length)
        {
            double minutes = Math.Floor(length);

            length -= minutes;
            length = Math.Floor(length * 100.0);
            if (length > 59)
            {
                minutes++;
                length -= 60;
            }

            double seconds = Math.Floor(length);

            length = ((double)minutes) + ((double)seconds) / 100.0;

            return length;
        }


        public static async Task RetrieveSongMetadata(List<StorageFile> listsongStorage, List<Song> listSong)
        {
            foreach (var item in listsongStorage)
            {
                MusicProperties metaData = await item.Properties.GetMusicPropertiesAsync();
                listSong.Add(new Song(metaData.Title, metaData.Artist, metaData.Album, FormatTrackDuration(metaData.Duration.TotalMinutes), metaData.Genre, item.Path));
            }
        }

        public static async Task RetreiveFilesInFolders(List<StorageFile> list, StorageFolder parent)
        {
            if(parent != null)
            {
                foreach (var item in await parent.GetFilesAsync())
                {
                    if (item.FileType == ".mp3")
                    {
                        list.Add(item);
                    }
                }

                foreach (var item in await parent.GetFoldersAsync())
                {
                    await RetreiveFilesInFolders(list, item);
                }
            }
       
        }

    }
}
