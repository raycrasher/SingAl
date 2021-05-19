using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private Task _songDbLoadTask;
        private IWebHostEnvironment _env;
        private AppSettings _settings;
        private ISongConverter _converter;
        private ILyricExtractor _lyricExtractor;
        private ILogger<SongRepository> _logger;

        private record SongEntry(Guid Id, Song Song, Lyric[] Lyrics, Lazy<Task<string>> Audio);

        Dictionary<Guid, SongEntry> _songs = new();


        public SongRepository(
            IWebHostEnvironment env, 
            IOptions<AppSettings> settings,
            ISongConverter converter,
            ILyricExtractor lyricExtractor,
            ILogger<SongRepository> logger
            )
        {
            _songDbLoadTask = Task.Run(BuildSongDb);
            _env = env;
            _settings = settings.Value;
            _converter = converter;
            _lyricExtractor = lyricExtractor;
            _logger = logger;
        }

        async Task BuildSongDb()
        {
            _logger.LogInformation("Building song database...");
            var lazyLoadTasks = (from karFile in Directory.GetFiles(_settings.SongDirectory, "*.kar")
                                 let lyrics = new Lazy<Task<(Song song, Lyric[] lyrics)>>(() => _lyricExtractor.GetLyrics(karFile))
                                 let audio = new Lazy<Task<string>>(() => _converter.ConvertKarToOgg(karFile))                                
                                 select (id: Guid.NewGuid(), audio, lyrics)).ToArray();

            foreach(var entry in lazyLoadTasks)
            {
                var (song, lyrics) = await entry.lyrics.Value; // preload lyrics
                if (song == null || lyrics == null)
                    continue;
                song.Id = entry.id;
                _songs[entry.id] = new(entry.id, song, lyrics, entry.audio);
                _logger.LogInformation("Adding {song} to db", song.Title);
            }
        }

        internal async Task<string> GetSongFilename(Guid songId)
        {
            await _songDbLoadTask;
            if (_songs.TryGetValue(songId, out var song))
            {
                return await song.Audio.Value;
            }
            else return null;
        }

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

            return songEntry.Lyrics;

            //var lines = (await File.ReadAllLinesAsync(songEntry.LyricsFile))
            //    .Where(l => l.Length > 0 && char.IsDigit(l[0]));
            //List<Lyric> lyrics = new();
            //foreach(var line in lines)
            //{
            //    var firstSpace = line.IndexOf(' ');
            //    var lyric = new Lyric()
            //    {
            //        Seconds = double.Parse(line.Substring(0, firstSpace)),
            //        Text = line.Substring(firstSpace + 1)
            //    };
            //    lyrics.Add(lyric);
            //}
            //return lyrics;
        }
    }
}
