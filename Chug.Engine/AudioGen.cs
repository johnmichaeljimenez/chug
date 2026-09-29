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

    public void AddNote(string sampleName, int length, bool oneShot, bool append = true, bool triplet = false)
    {
        if (!Samples.TryGetValue(sampleName, out var sample))
        {
            throw new ArgumentException($"Sample key '{sampleName}' not found in sample dictionary.");
        }

        float startTime = 0f;
        if (Clips.Count > 0)
        {
            var lastClip = Clips[^1];
            startTime = lastClip.StartTime + (append ? lastDuration : 0);
        }

        float duration = (60f / BPM) * (4f / length);
        if (triplet)
            duration *= (2f / 3f);

        if (append)
            lastDuration = duration;

        var ac = new AudioClip()
        {
            FileName = sample.FilePath,
            StartTime = startTime,
            Volume = sample.Volume,
            Duration = oneShot || sample.OneShot ? null : duration + 0.1f //slight padding
        };

        Clips.Add(ac);
    }
}
