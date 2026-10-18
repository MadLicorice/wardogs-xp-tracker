using System.IO;
using System.Media;
using System.Text;

namespace WardogsXpTracker.App;

internal static class CompletionAudio
{
    private static readonly byte[] Wave = CreateWave();
    private static readonly Lazy<SoundPlayer> Player = new(() => new SoundPlayer(new MemoryStream(Wave, writable: false)));
    public static byte[] CopyWave() => (byte[])Wave.Clone();

    public static bool TryPlay(out string? error)
    {
        try { Player.Value.Play(); error = null; return true; }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or System.ComponentModel.Win32Exception or TimeoutException)
        { error = exception.Message; return false; }
    }

    private static byte[] CreateWave()
    {
        const int rate = 22050;
        const double duration = 0.45;
        var samples = (int)(rate * duration);
        using var stream = new MemoryStream(44 + samples * 2);
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples * 2);
        writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
        writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2);
        writer.Write((short)2); writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(samples * 2);
        for (var i = 0; i < samples; i++)
        {
            var t = i / (double)rate;
            var envelope = Math.Min(1, t / 0.012) * Math.Min(1, (duration - t) / 0.09);
            var tone = t < 0.15 ? Math.Sin(2 * Math.PI * 880 * t)
                : (Math.Sin(2 * Math.PI * 1174.66 * t) + Math.Sin(2 * Math.PI * 1396.91 * t)) * 0.5;
            writer.Write((short)(tone * envelope * 0.24 * short.MaxValue));
        }
        writer.Flush();
        return stream.ToArray();
    }
}
