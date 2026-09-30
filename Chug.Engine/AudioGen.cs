namespace Chug.Engine;

public class AudioGen : OpcodeEngine.Core.Engine
{
    public int BPM = 120;
    private float lastDuration;
    public Dictionary<string, SampleData> Samples = new();
    public List<AudioClip> Clips = new();

    public AudioGen(bool immediateMode = false, int randomSeed = 0) : base(immediateMode, randomSeed)
    {
    }

    private float CalculateDuration(int length, bool triplet)
    {
        float duration = (60f / BPM) * (4f / length);
        if (triplet)
        {
            duration *= (2f / 3f);
        }
        return duration;
    }

    public void AddRest(int length, bool triplet = false)
    {
        float duration = CalculateDuration(length, triplet);

        if (Clips.Count > 0)
        {
            lastDuration += duration;
        }
    }

    public void AddNote(string sampleName, int length, bool oneShot, bool append = true, bool triplet = false)
    {
        if (!Samples.TryGetValue(sampleName, out var sample))
        {
            throw new ArgumentException($"Sample key '{sampleName}' not found in sample dictionary.");
        }

        var currentSolo = Samples.Values.FirstOrDefault(p => p.Solo);

        float startTime = 0f;
        if (Clips.Count > 0)
        {
            var lastClip = Clips[^1];
            startTime = lastClip.StartTime + (append ? lastDuration : 0);
        }

        float duration = CalculateDuration(length, triplet);

        if (append)
        {
            lastDuration = duration;
        }

        var ac = new AudioClip()
        {
            FileName = sample.FilePath,
            StartTime = startTime,
            Volume = currentSolo == null || currentSolo == sample? sample.Volume : 0,
            Duration = oneShot || sample.OneShot ? null : duration + 0.1f //slight padding
        };

        Clips.Add(ac);
    }
}
