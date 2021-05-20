using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CliWrap;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace SingAl.Services
{
    public interface ISongConverter
    {
        Task<string> ConvertKarToOgg(string inputFile);
    }

    public class SongConverter: ISongConverter
    {
        private AppSettings _settings;
        private ILogger<SongConverter> _logger;

        public SongConverter(IOptions<AppSettings> settings, ILogger<SongConverter> logger)
        {
            _settings = settings.Value;
            if (!Directory.Exists(_settings.SongCacheDir))
                Directory.CreateDirectory(_settings.SongCacheDir);
            _logger = logger;
        }

        public async Task<string> ConvertKarToOgg(string inputFile)
        {
            var outputFilename = Path.Combine(_settings.SongCacheDir, Path.GetFileNameWithoutExtension(inputFile)) + _settings.ConvertedSongFormatExtension;

            if (File.Exists(outputFilename))
                return outputFilename;

            using var memStream = new MemoryStream(1024000);
            var guidStr = Guid.NewGuid().ToString("D");

            var timidity = Cli.Wrap(_settings.TimidityPath)
                .WithArguments($"-Ow -W- --preserve-silence \"{inputFile}\" -o \"{guidStr}.wav\"");
            
            var lame = Cli.Wrap(_settings.LamePath)
                .WithArguments($"{guidStr}.wav \"{outputFilename}\"")
                .WithStandardErrorPipe(PipeTarget.ToDelegate(Log));

            await timidity.ExecuteAsync();
            await lame.ExecuteAsync();

            File.Delete($"{guidStr}.wav");

            return outputFilename;
        }

        private void Log(string msg)
        {
            _logger.LogDebug(msg);
        }
    }
}
