using Microsoft.AspNetCore.SignalR;
using SingAl.Controllers;
using SingAl.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SingAl.Services
{
    

    public class SingAlService
    {
        
        SongRepository _repo;

        public SingAlService( SongRepository repo)
        {
            _repo = repo;
            BackgroundVideos = Directory.GetFiles( "./background-movies", "*.mp4");
        }

        public string CurrentVideoFilePath { get; set; }
        public ConcurrentQueue<QueuedSong> SongQueue { get; } = new ConcurrentQueue<QueuedSong>();

        public ConcurrentBag<Singer> Singers { get; } = new ConcurrentBag<Singer>();
        public string[] BackgroundVideos { get; init; }


        public async Task<(Singer Singer, Song Song)> QueueSong(Guid singerId, Guid songId)
        {            
            var singer = Singers.FirstOrDefault(s => s.Id == singerId);
            if (singer == null)
                return (null, null);

            Song song = await _repo.GetSong(songId);
            if (song == null)
                return (null, null);            
            SongQueue.Enqueue(new (singer, song));
            return (singer, song);
        }

        internal (Guid Id, bool Ok, string Error) TryAddSinger(string nickname)
        {
            if(Singers.Any(s=>s.Nickname.ToLower() == nickname.ToLower()))
            {
                return (Guid.Empty, false, "NicknameAlreadyTaken");
            }
            var singer = new Singer
            {
                Id = Guid.NewGuid(),
                Nickname = nickname
            };
            Singers.Add(singer);
            return (singer.Id, true, "");
        }
    }
}
