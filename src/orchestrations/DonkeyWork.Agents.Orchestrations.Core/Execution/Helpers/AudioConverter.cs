using System.Diagnostics;
using NAudio.Wave;

namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Helpers;

public static class AudioConverter
{
    public static byte[] ConvertPcm(byte[] pcmData, string format, int sampleRate = 24000, int bitsPerSample = 16, int channels = 1)
    {
        return format.ToLowerInvariant() switch
        {
            "mp3" => ConvertToMp3(pcmData, sampleRate, bitsPerSample, channels),
            "wav" => ConvertToWav(pcmData, sampleRate, bitsPerSample, channels),
            _ => pcmData,
        };
    }

    public static string GetContentType(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "mp3" => "audio/mpeg",
            "wav" => "audio/wav",
            "opus" => "audio/opus",
            "aac" => "audio/aac",
            "flac" => "audio/flac",
            _ => "audio/pcm",
        };
    }

    public static string GetFileExtension(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "mp3" => "mp3",
            "wav" => "wav",
            "opus" => "opus",
            "aac" => "aac",
            "flac" => "flac",
            _ => "pcm",
        };
    }

    private static byte[] ConvertToMp3(byte[] pcmData, int sampleRate, int bitsPerSample, int channels)
    {
        var sampleFormat = bitsPerSample switch
        {
            16 => "s16le",
            24 => "s24le",
            32 => "s32le",
            8 => "u8",
            _ => throw new NotSupportedException($"Unsupported bits per sample: {bitsPerSample}"),
        };

        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        psi.ArgumentList.Add("-hide_banner");
        psi.ArgumentList.Add("-loglevel");
        psi.ArgumentList.Add("error");
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add(sampleFormat);
        psi.ArgumentList.Add("-ar");
        psi.ArgumentList.Add(sampleRate.ToString());
        psi.ArgumentList.Add("-ac");
        psi.ArgumentList.Add(channels.ToString());
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add("pipe:0");
        psi.ArgumentList.Add("-codec:a");
        psi.ArgumentList.Add("libmp3lame");
        psi.ArgumentList.Add("-b:a");
        psi.ArgumentList.Add("192k");
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add("mp3");
        psi.ArgumentList.Add("pipe:1");

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start ffmpeg process.");

        var stdoutTask = Task.Run(() =>
        {
            using var ms = new MemoryStream();
            process.StandardOutput.BaseStream.CopyTo(ms);
            return ms.ToArray();
        });
        var stderrTask = process.StandardError.ReadToEndAsync();

        process.StandardInput.BaseStream.Write(pcmData, 0, pcmData.Length);
        process.StandardInput.BaseStream.Flush();
        process.StandardInput.Close();

        var output = stdoutTask.GetAwaiter().GetResult();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var error = stderrTask.GetAwaiter().GetResult();
            throw new InvalidOperationException($"ffmpeg failed with exit code {process.ExitCode}: {error}");
        }

        return output;
    }

    private static byte[] ConvertToWav(byte[] pcmData, int sampleRate, int bitsPerSample, int channels)
    {
        var waveFormat = new WaveFormat(sampleRate, bitsPerSample, channels);
        using var wavStream = new MemoryStream();
        using (var writer = new WaveFileWriter(wavStream, waveFormat))
        {
            writer.Write(pcmData, 0, pcmData.Length);
        }

        return wavStream.ToArray();
    }

    /// <summary>
    /// Concatenates a sequence of same-format audio clips into a single blob using
    /// ffmpeg's concat demuxer with stream-copy (no re-encode) for MP3/AAC/FLAC/WAV/Opus.
    /// Temp files are cleaned up on success and on failure.
    /// </summary>
    public static byte[] Concat(IReadOnlyList<byte[]> clipBytes, string format)
    {
        if (clipBytes.Count == 0)
        {
            throw new ArgumentException("Concat requires at least one clip.", nameof(clipBytes));
        }

        if (clipBytes.Count == 1)
        {
            return clipBytes[0];
        }

        var extension = GetFileExtension(format);
        var workDir = Path.Combine(Path.GetTempPath(), $"tts-concat-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workDir);

        try
        {
            var clipPaths = new List<string>(clipBytes.Count);
            for (var i = 0; i < clipBytes.Count; i++)
            {
                var path = Path.Combine(workDir, $"clip-{i:D5}.{extension}");
                File.WriteAllBytes(path, clipBytes[i]);
                clipPaths.Add(path);
            }

            var listPath = Path.Combine(workDir, "list.txt");
            File.WriteAllLines(listPath, clipPaths.Select(p => $"file '{p.Replace("'", "'\\''")}'"));

            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                RedirectStandardInput = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            psi.ArgumentList.Add("-hide_banner");
            psi.ArgumentList.Add("-loglevel");
            psi.ArgumentList.Add("error");
            psi.ArgumentList.Add("-f");
            psi.ArgumentList.Add("concat");
            psi.ArgumentList.Add("-safe");
            psi.ArgumentList.Add("0");
            psi.ArgumentList.Add("-i");
            psi.ArgumentList.Add(listPath);
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add("copy");
            psi.ArgumentList.Add("-f");
            psi.ArgumentList.Add(MapFormatForFfmpegMuxer(format));
            psi.ArgumentList.Add("pipe:1");

            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start ffmpeg process.");

            var stdoutTask = Task.Run(() =>
            {
                using var ms = new MemoryStream();
                process.StandardOutput.BaseStream.CopyTo(ms);
                return ms.ToArray();
            });
            var stderrTask = process.StandardError.ReadToEndAsync();

            var output = stdoutTask.GetAwaiter().GetResult();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var error = stderrTask.GetAwaiter().GetResult();
                throw new InvalidOperationException($"ffmpeg concat failed with exit code {process.ExitCode}: {error}");
            }

            return output;
        }
        finally
        {
            try
            {
                Directory.Delete(workDir, recursive: true);
            }
            catch
            {
                // best-effort cleanup; don't mask the original exception
            }
        }
    }

    private static string MapFormatForFfmpegMuxer(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "mp3" => "mp3",
            "wav" => "wav",
            "aac" => "adts",
            "flac" => "flac",
            "opus" => "opus",
            _ => format.ToLowerInvariant(),
        };
    }
}
