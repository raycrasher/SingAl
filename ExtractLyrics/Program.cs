
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExtractLyrics
{
    record Lyric (double t, string s, bool clear, bool newline);
    record Song (string title, string artist, Lyric[] lyrics);


    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1) {
                Console.WriteLine("Usage: extractlyrics.exe <midi_or_kar_file>");
                return;
            }

            var midi = MidiFile.Read(args[0]);
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
                    Console.WriteLine($"TITLE {titleStr}");
                    continue;
                }
                else if (hasTitle && !hasArtist && textEvent.Text.StartsWith("@T"))
                {
                    hasArtist = true;
                    artistStr = textEvent.Text.Substring(2);
                    Console.WriteLine($"ARTIST {artistStr}");
                    continue;
                }
                else if (textEvent.Text.StartsWith("@"))
                    continue;

                var text = textEvent.Text.Replace('\r', ' ').Replace('\n', ' ');
                TimeSpan realTime = evt.TimeAs<MetricTimeSpan>(tempoMap);
                Console.WriteLine($"{realTime.TotalSeconds} {text}");

            }
        }
    }
}
