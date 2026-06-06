using System.Media;
using System.Runtime.InteropServices;

namespace CleanYourSetup;

internal static class SoundAlertPlayer
{
    [DllImport("user32.dll", SetLastError = false)]
    private static extern bool MessageBeep(uint uType);

    [DllImport("kernel32.dll", SetLastError = false)]
    private static extern bool Beep(uint dwFreq, uint dwDuration);

    public static void PlayDefaultBeep()
    {
        _ = Task.Run(() =>
        {
            if (TryPlayGeneratedTone())
            {
                return;
            }

            try
            {
                SystemSounds.Exclamation.Play();
                MessageBeep(0x00000030); // MB_ICONEXCLAMATION
                Beep(950, 120);
            }
            catch
            {
                // Sound is optional. Never break cleaning mode because audio failed.
            }
        });
    }

    private static bool TryPlayGeneratedTone()
    {
        try
        {
            using MemoryStream stream = CreateToneWav(frequencyHz: 950, durationMs: 120, volume: 0.35);
            using SoundPlayer player = new(stream);
            player.PlaySync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static MemoryStream CreateToneWav(int frequencyHz, int durationMs, double volume)
    {
        const int sampleRate = 44100;
        const short channels = 1;
        const short bitsPerSample = 16;
        int sampleCount = sampleRate * durationMs / 1000;
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        short blockAlign = channels * bitsPerSample / 8;
        int dataSize = sampleCount * blockAlign;

        MemoryStream stream = new();
        using BinaryWriter writer = new(stream, System.Text.Encoding.ASCII, leaveOpen: true);

        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataSize);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1); // PCM
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);

        for (int i = 0; i < sampleCount; i++)
        {
            double t = (double)i / sampleRate;
            double envelope = Math.Min(1.0, Math.Min(i / 600.0, (sampleCount - i) / 600.0));
            short sample = (short)(Math.Sin(2 * Math.PI * frequencyHz * t) * short.MaxValue * volume * envelope);
            writer.Write(sample);
        }

        writer.Flush();
        stream.Position = 0;
        return stream;
    }
}
