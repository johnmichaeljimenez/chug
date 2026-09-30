using OpcodeEngine.Commands;
using OpcodeEngine.Core;

namespace Chug.Engine;

public abstract class BaseCommand : Command
{
	protected AudioGen AudioGen => (AudioGen)Engine;
}

public class BPM : BaseCommand
{
	[CommandParameter]
	private int value;

	public override void OnEnter()
	{
		AudioGen.BPM = value;
	}
}

public class Solo : BaseCommand
{
	[CommandParameter]
	private string sampleID;

	public override void OnEnter()
	{
		base.OnEnter();

		AudioGen.Samples[sampleID].Solo = true;
	}
}

public class Sample : BaseCommand
{
	[CommandParameter]
	private string key;
	[CommandParameter]
	private string fileName;
	[CommandParameter]
	private float Volume = 1;
	[CommandParameter]
	private bool OneShot = false;

	public override void OnEnter()
	{
		AudioGen.Samples[key] = new()
		{
			FilePath = fileName,
			Volume = Volume,
			OneShot = OneShot,
			Name = key
		};
	}
}

public class P : BaseCommand
{
	[CommandParameter]
	private string sampleName;

	[CommandParameter]
	private string length = "1"; //1 = whole note, 2 = half note, 4, 8, 16, 32, T suffix means triplet

	[CommandParameter]
	private bool oneShot = false; //false = gate, true = play fully

	public override void OnEnter()
	{
		var sn = sampleName;
		var append = true;
		if (Utils.HasPrefix("!", ref sn))
			append = false;

		var triplet = false;
		var rawLength = length;
		if (rawLength.EndsWith("T", StringComparison.InvariantCultureIgnoreCase))
		{
			triplet = true;
			rawLength = rawLength.Remove(rawLength.Length - 1);
		}

		int.TryParse(rawLength, out var lengthValue);
		AudioGen.AddNote(sn, lengthValue, oneShot, append, triplet);
	}
}

public class R : BaseCommand
{
	[CommandParameter]
	private string length = "1";

	public override void OnEnter()
	{
		var triplet = false;
		var rawLength = length;

		if (rawLength.EndsWith("T", StringComparison.InvariantCultureIgnoreCase))
		{
			triplet = true;
			rawLength = rawLength.Remove(rawLength.Length - 1);
		}

		int.TryParse(rawLength, out var lengthValue);
		AudioGen.AddRest(lengthValue, triplet);
	}
}