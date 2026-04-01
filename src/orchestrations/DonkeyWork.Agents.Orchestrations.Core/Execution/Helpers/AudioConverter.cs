using NAudio.Lame;
using NAudio.Wave;

namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Helpers;

internal static class AudioConverter
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
            _ => "audio/pcm",
        };
    }

    public static string GetFileExtension(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "mp3" => "mp3",
            "wav" => "wav",
            _ => "pcm",
        };
    }

    private static byte[] ConvertToMp3(byte[] pcmData, int sampleRate, int bitsPerSample, int channels)
    {
        var waveFormat = new WaveFormat(sampleRate, bitsPerSample, channels);
        using var pcmStream = new RawSourceWaveStream(pcmData, 0, pcmData.Length, waveFormat);
        using var mp3Stream = new MemoryStream();
        using (var writer = new LameMP3FileWriter(mp3Stream, waveFormat, LAMEPreset.STANDARD))
        {
            pcmStream.CopyTo(writer);
        }

        return mp3Stream.ToArray();
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
}
