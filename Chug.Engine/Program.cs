using System.Diagnostics;

namespace Chug.Engine;

public class SampleData
{
    public string Name;
    public string FilePath;
    public float Volume = 1;
    public bool OneShot;
}

public class Program
{
    public static void Main(string[] args)
    {
        // if (args.Length == 0)
        //     throw new InvalidOperationException("Usage: chug.exe <.ops file path>");

        // var filePath = args[0];
        // if (!File.Exists(filePath))
        //     throw new FileNotFoundException($"'{filePath}' not found.");

        var filePath = "Scripts/Test.ops";

        var audioGen = new AudioGen(true);
        audioGen.CompileFile(filePath);
        audioGen.Initialize();

        Mixer.MixToWav
        (
            audioGen.Clips,
            outputWavPath: @"output.wav",
            outputSampleRate: 44100
        );

        Process.Start(new ProcessStartInfo("output.wav")
        {
            UseShellExecute = true
        });
    }
}