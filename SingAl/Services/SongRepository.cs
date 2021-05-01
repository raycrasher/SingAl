using Microsoft.AspNetCore.Hosting;
using SingAl.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SingAl.Services
{
    public class SongRepository
    {
        Task _songDbLoadTask;
        private IWebHostEnvironment _env;

        public SongRepository(IWebHostEnvironment env)
        {
            _songDbLoadTask = Task.Run(BuildSongDb);
            _env = env;

        }

        void BuildSongDb()
        {
            var files = from lyricsFile in Directory.GetFiles("./songs", "*.lyrics")
                        let imageFile = lyricsFile.Substring(0, lyricsFile.Length - ".lyrics".Length) + ".mp3"
                        where File.Exists(imageFile)
                        select (lyricsFile, imageFile);
            
            foreach(var songEntry in files)
            {
                var guid = Guid.NewGuid();
                var songdata = new Song();
                songdata.Id = guid;
                using var reader = new StreamReader(songEntry.lyricsFile);
                for(int i = 0; i < 3; i++)
                {
                    var line = reader.ReadLine();
                    if (line.StartsWith("TITLE "))
                        songdata.Title = line.Substring("TITLE ".Length);
                    else if(line.StartsWith("ALBUM "))
                        songdata.Album = line.Substring("ALBUM ".Length);
                    else if (line.StartsWith("ARTIST "))
                        songdata.Artist = line.Substring("ARTIST ".Length);
                }
                if (songdata.Title != null)
                {
                    _songs[guid] = new(songEntry.lyricsFile, songEntry.imageFile, songdata);
                }
            }
        }


        record SongEntry(string LyricsFile, string ImageFile, Song Song);

        Dictionary<Guid, SongEntry> _songs = new();

        public async Task<Song> GetSong(Guid songId)
        {
            await _songDbLoadTask;
            return _songs.TryGetValue(songId, out var song) ? song.Song : null;
        }

        internal async Task<IEnumerable<Song>> GetMatchingSongs(string query)
        {
            await _songDbLoadTask;
            return _songs.Values.Where(s => {
                bool titleMatch = false, albumMatch = false, artistMatch = false;
                if (!string.IsNullOrWhiteSpace(query))
                {
                    titleMatch = s.Song.Title?.Contains(query, StringComparison.CurrentCultureIgnoreCase) ?? false;
                }
                if (!string.IsNullOrWhiteSpace(query))
                {
                    artistMatch = s.Song.Artist?.Contains(query, StringComparison.CurrentCultureIgnoreCase) ?? false;
                }
                if (!string.IsNullOrWhiteSpace(query))
                {
                    albumMatch = s.Song.Album?.Contains(query, StringComparison.CurrentCultureIgnoreCase) ?? false;
                }
                return titleMatch || albumMatch || albumMatch;
            }).Select(s=>s.Song);
        }

        public async Task<IEnumerable<Lyric>> GetSongLyrics(Guid songId)
        {
            await _songDbLoadTask;
            if (!_songs.TryGetValue(songId, out var songEntry))
                return Enumerable.Empty<Lyric>();
            var lines = (await File.ReadAllLinesAsync(songEntry.LyricsFile))
                .Where(l => l.Length > 0 && char.IsDigit(l[0]));
            List<Lyric> lyrics = new();
            foreach(var line in lines)
            {
                var firstSpace = line.IndexOf(' ');
                var lyric = new Lyric()
                {
                    Seconds = double.Parse(line.Substring(0, firstSpace)),
                    Text = line.Substring(firstSpace + 1)
                };
                lyrics.Add(lyric);
            }
            return lyrics;
        }
    }
}
