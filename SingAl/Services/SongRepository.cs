using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SingAl.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using Microsoft.Extensions.Hosting;
using System.Threading;

namespace SingAl.Services
{
    public class SongRepository: IHostedService
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
            //_songDbLoadTask = Task.Run(BuildSongDb);
            _env = env;
            _settings = settings.Value;
            _converter = converter;
            _lyricExtractor = lyricExtractor;
            _logger = logger;
        }

        public void StartCachingSong(Guid songId)
        {
            Task.Run(()=>_songs[songId].Audio.Value);
        }

        async Task BuildSongDb()
        {
            _logger.LogInformation("Building song database...");
            var lazyLoadTasks = (from karFile in Directory.GetFiles(_settings.SongDirectory, "*.kar")
                                 let lyrics = new Lazy<Task<(Song song, Lyric[] lyrics)>>(() => _lyricExtractor.GetLyrics(karFile))
                                 let audio = new Lazy<Task<string>>(() => _converter.ConvertKarToOgg(karFile))                                
                                 select (id: Guid.NewGuid(), audio, lyrics, filename: karFile)).ToList();

            using var db = GetSqliteDb();

            // load pre-loaded songs from DB
            var dbItems = await db.QueryAsync<(string id, string sourceFile, string title, string artist, string lyrics, string tags)>("SELECT id, sourceFile, title, artist, lyrics, tags FROM songs;");
            foreach(var dbItem in dbItems)
            {
                var id = Guid.Parse(dbItem.id);
                _songs[id] = new SongEntry(id, new Song()
                {
                    Title = dbItem.title,
                    Artist = dbItem.artist,
                    Id = id,
                    Tags = dbItem.tags.Split(',')
                },
                JsonConvert.DeserializeObject<Lyric[]>(dbItem.lyrics),
                new Lazy<Task<string>>(() => _converter.ConvertKarToOgg(dbItem.sourceFile))
                );
            }
            _logger.LogInformation("Loaded {count} songs from DB", _songs.Count);

            foreach(var entry in lazyLoadTasks)
            {
                if (await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM songs WHERE sourceFile=@filename LIMIT 1;", new { filename= entry.filename }) > 0)
                {
                    continue;
                }

                var (song, lyrics) = await entry.lyrics.Value; // preload lyrics
                if (song == null || lyrics == null || string.IsNullOrEmpty(song.Title) || string.IsNullOrEmpty(song.Artist) || lyrics.Length <= 0)
                    continue;
                song.Id = entry.id;
                await db.ExecuteAsync("INSERT INTO songs(id, sourceFile, title, artist, lyrics, tags) VALUES(@id, @sourceFile, @title, @artist, @lyrics, @tags)", new { 
                    id = song.Id.ToString(),
                    sourceFile = entry.filename,
                    title = song.Title,
                    artist = song.Artist,
                    lyrics = JsonConvert.SerializeObject(lyrics),
                    tags = ""
                });

                _songs[entry.id] = new(entry.id, song, lyrics, entry.audio);
                _logger.LogInformation("Adding {song} to db", song.Title);  
            }
            _logger.LogInformation("Final song count: {count}", _songs.Count);
        }

        private SQLiteConnection GetSqliteDb()
        {
            if (!string.IsNullOrWhiteSpace(_settings.SqliteFile) && !File.Exists(_settings.SqliteFile)) {
                SQLiteConnection.CreateFile(_settings.SqliteFile);
            }

            var db = new SQLiteConnection(_settings.SqliteConnectionString);

            db.Execute(
@"CREATE TABLE IF NOT EXISTS songs (
id string not NULL primary key, 
sourceFile string NOT NULL,
title string NOT NULL,
artist string,
lyrics string NOT NULL,
tags string
);
CREATE INDEX IF NOT EXISTS sourceFileIndex ON songs (sourceFile ASC);
CREATE INDEX IF NOT EXISTS titleIndex ON songs (title ASC);
CREATE INDEX IF NOT EXISTS artistIndex ON songs (artist ASC);
");

            return db;
        }

        public async Task<string> GetSongFilename(Guid songId)
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

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _songDbLoadTask = Task.Run(BuildSongDb);
            await _songDbLoadTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _songDbLoadTask;
        }
    }
}
