using System;
using System.IO;
using System.Media;

namespace TellMeWhenYouSeeThisChange
{
    /// <summary>A steady, gap-free alarm tone built in memory and looped by SoundPlayer.</summary>
    internal sealed class ToneGenerator : IDisposable
    {
        private const int SampleRate = 44100;
        private readonly SoundPlayer _player;
        private readonly MemoryStream _wav;
        private bool _playing;

        public ToneGenerator(double frequency)
        {
            _wav = BuildWav(frequency);
            _player = new SoundPlayer(_wav);
            _player.Load();
        }

        public bool IsPlaying { get { return _playing; } }

        public void Start()
        {
            if (_playing) return;
            _player.PlayLooping();
            _playing = true;
        }

        public void Stop()
        {
            if (!_playing) return;
            _player.Stop();
            _playing = false;
        }

        public void Dispose()
        {
            Stop();
            _player.Dispose();
            _wav.Dispose();
        }

        private static MemoryStream BuildWav(double frequency)
        {
            // Snap the frequency so the buffer holds a whole number of cycles: the loop joins seamlessly.
            int cycles = (int)Math.Round(frequency);
            int samples = SampleRate; // exactly 1 second
            short[] data = new short[samples];
            const double amplitude = 0.55 * short.MaxValue;
            for (int i = 0; i < samples; i++)
            {
                double t = (double)i / SampleRate;
                // Fundamental plus a little 3rd harmonic for a slightly "brassy" buzzer timbre.
                double v = Math.Sin(2 * Math.PI * cycles * t) + 0.18 * Math.Sin(2 * Math.PI * cycles * 3 * t);
                data[i] = (short)(amplitude * v / 1.18);
            }

            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);
            int dataBytes = samples * 2;
            w.Write(new[] { 'R', 'I', 'F', 'F' });
            w.Write(36 + dataBytes);
            w.Write(new[] { 'W', 'A', 'V', 'E' });
            w.Write(new[] { 'f', 'm', 't', ' ' });
            w.Write(16);              // PCM chunk size
            w.Write((short)1);        // PCM
            w.Write((short)1);        // mono
            w.Write(SampleRate);
            w.Write(SampleRate * 2);  // byte rate
            w.Write((short)2);        // block align
            w.Write((short)16);       // bits per sample
            w.Write(new[] { 'd', 'a', 't', 'a' });
            w.Write(dataBytes);
            foreach (short s in data) w.Write(s);
            w.Flush();
            ms.Position = 0;
            return ms;
        }
    }
}
