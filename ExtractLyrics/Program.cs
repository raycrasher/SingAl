using NAudio.Midi;
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



            var midi = new MidiFile(args[0]);
            var events = midi.Events.SelectMany(e => e).Where(e => e is TextEvent).OrderBy(e=>e.AbsoluteTime).ToArray();
            //Console.WriteLine(midi.DeltaTicksPerQuarterNote);

            TimeSignatureEvent tse = null;

            decimal lastRealTime = 0m;
            decimal lastAbsoluteTime = 0m;
            decimal currentMicroSecondsPerTick = 500000m / midi.DeltaTicksPerQuarterNote;

            bool hasTitle = false;
            bool hasArtist = false;
            string titleStr = null, artistStr = null;

            List<Lyric> lyrics = new();

            foreach (var midiEvent in events)
            {
                TempoEvent tempoEvent = midiEvent as TempoEvent;

                if (!hasTitle && midiEvent.AbsoluteTime == 0 && midiEvent is TextEvent title && title.Text.StartsWith("@T"))
                {
                    titleStr = title.Text.Substring(2);
                    hasTitle = true;
                    Console.WriteLine($"TITLE {titleStr}");
                    
                    continue;
                }
                else if (hasTitle && !hasArtist && midiEvent.AbsoluteTime == 0 && midiEvent is TextEvent artist && artist.Text.StartsWith("@T"))
                {
                    hasArtist = true;
                    artistStr = artist.Text.Substring(2);
                    Console.WriteLine($"ARTIST {artistStr}");
                    continue;
                }
                else if (midiEvent.AbsoluteTime == 0)
                    continue;

                // Just append to last real time the microseconds passed
                // since the last event (DeltaTime * MicroSecondsPerTick
                if (midiEvent.AbsoluteTime > lastAbsoluteTime)
                {
                    lastRealTime += ((decimal)midiEvent.AbsoluteTime - lastAbsoluteTime) * currentMicroSecondsPerTick;
                }

                lastAbsoluteTime = midiEvent.AbsoluteTime;

                if (tempoEvent != null)
                {
                    // Console.Write($"TempoEvent MS: {tempoEvent.MicrosecondsPerQuarterNote}");
                    // Recalculate microseconds per tick
                    currentMicroSecondsPerTick = (decimal)tempoEvent.MicrosecondsPerQuarterNote / (decimal)midi.DeltaTicksPerQuarterNote;

                    // Remove the tempo event to make events and timings match - index-wise
                    // Do not add to the eventTimes
                    continue;
                }

                // Add the time to the collection.

                TimeSpan lastRealTimeTs = TimeSpan.FromMilliseconds((double)lastRealTime/1000);

                if (midiEvent is TextEvent textEvent)
                {
                    var text = textEvent.Text.Replace('\r', ' ').Replace('\n', ' ');
                    Console.WriteLine($"{lastRealTimeTs.TotalSeconds} {text}");

                    bool isClear = text.StartsWith("\\");
                    bool isNewLine = text.StartsWith("/");

                    lyrics.Add(new(lastRealTimeTs.TotalSeconds, isClear || isNewLine ? text.Substring(1) : text, isClear, isNewLine));
                }
            }
            //Song song = new(titleStr, artistStr, lyrics.ToArray());
            //Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(song));
        }
    }
}
