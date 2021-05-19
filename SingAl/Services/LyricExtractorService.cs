using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;
using Microsoft.Extensions.Options;
using SingAl.Models;

namespace SingAl.Services
{
    public interface ILyricExtractor
    {
        Task<(Song song, Lyric[] lyrics)> GetLyrics(string filename);
    }

    public class LyricExtractorService: ILyricExtractor
    {
        public Task<(Song song, Lyric[] lyrics)> GetLyrics(string filename) => Task.Run(() => {
            Song theSong = new();
            List<Lyric> lyrics = new();
            try
            {
                var midi = MidiFile.Read(filename);
                var tempoMap = midi.GetTempoMap();

                var textEvents = midi.GetTimedEvents().Where(t => t.Event is TextEvent).ToArray();

                bool hasTitle = false;
                bool hasArtist = false;
                string titleStr = null, artistStr = null;

                foreach (var evt in textEvents)
                {
                    var textEvent = (TextEvent)evt.Event;
                    if (!hasTitle && textEvent.Text.StartsWith("@T"))
                    {
                        titleStr = textEvent.Text.Substring(2);
                        hasTitle = true;
                        //Console.WriteLine($"TITLE {titleStr}");
                        theSong.Title = titleStr;
                        continue;
                    }
                    else if (hasTitle && !hasArtist && textEvent.Text.StartsWith("@T"))
                    {
                        hasArtist = true;
                        artistStr = textEvent.Text.Substring(2);
                        theSong.Artist = artistStr;
                        //Console.WriteLine($"ARTIST {artistStr}");
                        continue;
                    }
                    else if (textEvent.Text.StartsWith("@"))
                        continue;

                    var text = textEvent.Text.Replace('\r', ' ').Replace('\n', ' ');
                    TimeSpan realTime = evt.TimeAs<MetricTimeSpan>(tempoMap);
                    lyrics.Add(new Lyric { Seconds = realTime.TotalSeconds, Text = text });
                    //Console.WriteLine($"{realTime.TotalSeconds} {text}");

                }

                return (theSong, lyrics.ToArray());
            }
            catch(Exception)
            {
                return (null, null);
            }
        });
    }
}
