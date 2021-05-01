using System;

namespace SingAl.Models
{
    public record QueuedSong(Singer Singer, Song Song)
    {
        public Guid Id { get; init; } = Guid.NewGuid();
    }
}
