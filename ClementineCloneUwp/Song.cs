using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClementineCloneUwp
{
    public class Song : IComparer<Song>
    {
        private IList<string> genre;


        public string Title { get; set; }
        public string Artist{ get; set; }

        public string Album { get; set; }

        public double Duration { get; set; }

        public string Genre { get; set; }

        public string Path { get; set; }

        public Song(string title, string artist, string album, double duration, string genre, string path)
        {
            Title = title;
            Artist = artist;
            Album = album;
            Duration = duration;
            Genre = genre;
            Path = path;
        }

        public Song(string title, string artist, string album, double duration, IList<string> genre, string path)
        {
            Title = title;
            Artist = artist;
            Album = album;
            this.genre = genre;
            Duration = duration;
            Path = path;
        }

        public int Compare(Song x, Song y)
        {
           return  x.Title.CompareTo(y.Title);
        }
    }
}
