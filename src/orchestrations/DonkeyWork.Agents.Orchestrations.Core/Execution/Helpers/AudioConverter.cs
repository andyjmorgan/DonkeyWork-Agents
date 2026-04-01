using NAudio.Lame;
using NAudio.Wave;

namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Helpers;

internal static class AudioConverter
{
    public static byte[] ConvertPcmToMp3(byte[] pcmData, int sampleRate = 24000, int bitsPerSample = 16, int channels = 1)
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
}
