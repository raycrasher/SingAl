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


        public async Task<QueuedSong> QueueSong(Guid singerId, Guid songId)
        {            
            var singer = Singers.FirstOrDefault(s => s.Id == singerId);
            if (singer == null)
                return new (null, null);

            Song song = await _repo.GetSong(songId);
            if (song == null)
                return new (null, null);
            var queued = new QueuedSong(singer, song);
            SongQueue.Enqueue(queued);
            _repo.StartCachingSong(songId);
            return queued;
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
