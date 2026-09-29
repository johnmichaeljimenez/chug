using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Chug.Engine;

public sealed class AudioClip
{
    public string FileName { get; set; } = "";
    public float StartTime { get; set; }
    public float? Duration { get; set; }    //null or 0 = full length
    public float Volume { get; set; } = 1f;

    public AudioClip() { }

    public AudioClip(string fileName, float startTime, float? duration = null, float volume = 1f)
    {
        FileName = fileName;
        StartTime = startTime;
        Duration = duration;
        Volume = volume;
    }
}

public static class Mixer
{
    public static TimeSpan MixToWav(
        IEnumerable<AudioClip> clips,
        string outputWavPath,
        int outputSampleRate = 44100)
    {
        if (clips == null) throw new ArgumentNullException(nameof(clips));
        if (string.IsNullOrWhiteSpace(outputWavPath))
            throw new ArgumentException("Output path is required.", nameof(outputWavPath));
        if (outputSampleRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(outputSampleRate));

        var clipList = clips.ToList();
        if (clipList.Count == 0)
            throw new ArgumentException("At least one clip is required.", nameof(clips));

        var targetFormat = WaveFormat.CreateIeeeFloatWaveFormat(outputSampleRate, 1);
        var mixer = new MixingSampleProvider(targetFormat);
        var disposables = new List<IDisposable>();
        double timelineEndSeconds = 0;

        try
        {
            foreach (var clip in clipList)
            {
                if (clip == null)
                    throw new ArgumentException("Clip list contains a null entry.", nameof(clips));
                if (string.IsNullOrWhiteSpace(clip.FileName))
                    throw new ArgumentException("Each clip needs a FileName.");
                if (!File.Exists(clip.FileName))
                    throw new FileNotFoundException("Audio file not found.", clip.FileName);
                if (clip.StartTime < 0)
                    throw new ArgumentOutOfRangeException(nameof(clip.StartTime), "StartTime cannot be negative.");

                var reader = new AudioFileReader(clip.FileName);
                disposables.Add(reader);

                double sourceSeconds = reader.TotalTime.TotalSeconds;
                double playSeconds = ResolvePlayDuration(clip.Duration, sourceSeconds);
                double startSeconds = clip.StartTime;
                timelineEndSeconds = Math.Max(timelineEndSeconds, startSeconds + playSeconds);

                ISampleProvider samples = reader;

                if (Math.Abs(clip.Volume - 1f) > 0.0001f)
                    reader.Volume = clip.Volume;

                samples = ToMono(samples);
                samples = ResampleIfNeeded(samples, outputSampleRate);

                var placed = new OffsetSampleProvider(samples)
                {
                    DelayBy = TimeSpan.FromSeconds(startSeconds)
                };

                if (clip.Duration.HasValue && clip.Duration.Value > 0)
                    placed.Take = TimeSpan.FromSeconds(playSeconds);

                mixer.AddMixerInput(placed);
            }

            var outputDir = Path.GetDirectoryName(outputWavPath);
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);

            WaveFileWriter.CreateWaveFile16(outputWavPath, mixer);
            return TimeSpan.FromSeconds(timelineEndSeconds);
        }
        finally
        {
            foreach (var d in disposables)
                d.Dispose();
        }
    }

    public static (float[] Samples, int SampleRate, TimeSpan Duration) MixToSamples(
        IEnumerable<AudioClip> clips,
        int outputSampleRate = 44100)
    {
        var temp = Path.Combine(Path.GetTempPath(), "mix-" + Guid.NewGuid().ToString("N") + ".wav");
        try
        {
            var duration = MixToWav(clips, temp, outputSampleRate);
            using var reader = new AudioFileReader(temp);
            ISampleProvider samples = reader;
            var buffer = new float[checked((int)(reader.Length / sizeof(float)))];
            int read = samples.Read(buffer.AsSpan());
            if (read != buffer.Length)
                Array.Resize(ref buffer, read);
            return (buffer, outputSampleRate, duration);
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }

    private static double ResolvePlayDuration(float? requested, double sourceSeconds)
    {
        if (!requested.HasValue || requested.Value <= 0)
            return sourceSeconds;
        return Math.Min(requested.Value, sourceSeconds);
    }

    private static ISampleProvider ToMono(ISampleProvider source)
    {
        int channels = source.WaveFormat.Channels;
        if (channels == 1)
            return source;
        if (channels == 2)
            return new StereoToMonoSampleProvider(source);
        return new AverageToMonoSampleProvider(source);
    }

    private static ISampleProvider ResampleIfNeeded(ISampleProvider source, int sampleRate)
    {
        if (source.WaveFormat.SampleRate == sampleRate)
            return source;
        return new WdlResamplingSampleProvider(source, sampleRate);
    }
}

internal class AverageToMonoSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly int _channels;
    private float[]? _sourceBuffer;

    public AverageToMonoSampleProvider(ISampleProvider source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _channels = source.WaveFormat.Channels;
        if (_channels < 1)
            throw new ArgumentException("Source has no channels.");
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, 1);
    }

    public WaveFormat WaveFormat { get; }

    public int Read(Span<float> buffer)
    {
        int sourceCount = buffer.Length * _channels;
        if (_sourceBuffer == null || _sourceBuffer.Length < sourceCount)
            _sourceBuffer = new float[sourceCount];

        int sourceRead = _source.Read(_sourceBuffer.AsSpan(0, sourceCount));
        int frames = sourceRead / _channels;
        float inv = 1f / _channels;

        for (int i = 0; i < frames; i++)
        {
            float sum = 0f;
            int baseIndex = i * _channels;
            for (int c = 0; c < _channels; c++)
                sum += _sourceBuffer[baseIndex + c];
            buffer[i] = sum * inv;
        }

        return frames;
    }
}