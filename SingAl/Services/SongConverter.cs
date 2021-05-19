using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CliWrap;
using System.Collections.Concurrent;

namespace SingAl.Services
{
    public interface ISongConverter
    {
        Task<string> ConvertKarToOgg(string inputFile);
    }

    public class SongConverter: ISongConverter
    {
        private AppSettings _settings;
        
        public SongConverter(IOptions<AppSettings> settings)
        {
            _settings = settings.Value;
            if (!Directory.Exists(_settings.SongCacheDir))
                Directory.CreateDirectory(_settings.SongCacheDir);
        }

        public async Task<string> ConvertKarToOgg(string inputFile)
        {
            var outputFilename = Path.Combine(_settings.SongCacheDir, Path.GetFileNameWithoutExtension(inputFile)) + _settings.ConvertedSongFormatExtension;

            if (File.Exists(outputFilename))
                return outputFilename;

            var timidity = Cli.Wrap(_settings.TimidityPath)
                .WithArguments($"-Ow -W- '{inputFile}'");
            var lame = Cli.Wrap(_settings.LamePath)
                .WithArguments($"- '{outputFilename}'");

            Command cmd = timidity | lame;

            await cmd.ExecuteAsync();

            return outputFilename;
        }
    }
}
