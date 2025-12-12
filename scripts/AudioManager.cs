using Godot;
using OmoriSandbox.Animation;
using System.Collections.Generic;

namespace OmoriSandbox;

/// <summary>
/// Handles all audio playback, including SFX and BGM.
/// </summary>
public partial class AudioManager : Node
{
	[Export] private AudioStreamPlayer BGM;

	private readonly List<AudioStreamPlayer> AudioPlayers = [];

	private readonly Dictionary<string, AudioStreamOggVorbis> SFXDictionary = [];
	private readonly Dictionary<string, AudioStreamOggVorbis> BGMDictionary = [];

	public static AudioManager Instance { get; private set; }

	// only one instance of a sound can play at once
	private Dictionary<string, AudioStreamPlayer> PlayingSounds = [];

	public override void _EnterTree()
	{
		Instance = this;
	}

	internal void Init()
	{
		if (SFXDictionary.Count > 0)
		{
			GD.PrintErr("Attempting to re-init AudioManager, ignoring!");
			return;
		}

		foreach (Node node in GetChildren())
		{
			AudioStreamPlayer player = node as AudioStreamPlayer;
			AudioPlayers.Add(player);
			player.Finished += () => OnSFXFinish(player);
		}
		// preload animation sfx
		int failedPreloads = 0;
		foreach (RPGMAnimatedSprite animation in AnimationManager.Instance.GetAllAnimations())
		{
			foreach (List<SFX> sfxList in animation.AllSFX)
			{
				foreach (SFX sfx in sfxList)
				{
					// we're technically checking if the file exists twice here,
					// but doing so avoids console spam
					if (ResourceLoader.Exists("res://audio/sfx/" + sfx.Name + ".ogg"))
					{
						SFXDictionary.TryAdd(sfx.Name, ResourceLoader.Load<AudioStreamOggVorbis>("res://audio/sfx/" + sfx.Name + ".ogg"));
					}
					else
					{
						failedPreloads++;
					}
				}
			}
		}
		GD.Print($"Preloaded {SFXDictionary.Count} SFX. ({failedPreloads} failures)");
		// BGM.Finished += OnBGMFinish;
	}

	internal void PlaySFX(SFX sfx)
	{
		PlaySFX(sfx.Name, sfx.Pitch / 100f, sfx.Volume / 100f);
	}

	/// <summary>
	/// Plays SFX with the given <paramref name="name"/>. 
	/// </summary>
	/// <remarks>
	/// If an SFX of the same <paramref name="name"/> is already playing, it will be restarted.
	/// </remarks>
	/// <param name="name">The name of the SFX to play.</param>
	/// <param name="pitch">The pitch to play the SFX at.</param>
	/// <param name="volume">The volume to play the SFX at.</param>
	public void PlaySFX(string name, float pitch = 1f, float volume = 1f)
	{
		if (!SFXDictionary.TryGetValue(name, out AudioStreamOggVorbis stream)) {
			stream = ResourceLoader.Load<AudioStreamOggVorbis>("res://audio/sfx/" + name + ".ogg");
			if (stream == null)
			{
				GD.PrintErr("Unknown SFX: " + name);
				return;
			}
			SFXDictionary.Add(name, stream);
		}

		if (PlayingSounds.TryGetValue(name, out AudioStreamPlayer existing))
		{
			existing.Stream = stream;
			existing.Play();
			return;
		}

		foreach (AudioStreamPlayer player in AudioPlayers)
		{
			if (player.Playing)
				continue;
			player.Stream = stream;
			player.PitchScale = pitch;
			player.VolumeLinear = volume;
			player.Play();
			PlayingSounds.Add(name, player);
			return;
		}

		GD.PushWarning("Overloaded! We ran out of AudioStreams!");
	}

	/// <summary>
	/// Plays BGM with the given <paramref name="name"/>.
	/// </summary>
	/// <param name="name">The name of the BGM to play.</param>
	public void PlayBGM(string name)
	{
		if (!BGMDictionary.TryGetValue(name, out AudioStreamOggVorbis stream))
		{
			if (ResourceLoader.Exists("res://audio/bgm/" + name + ".ogg"))
				stream = ResourceLoader.Load<AudioStreamOggVorbis>("res://audio/bgm/" + name + ".ogg");
			else
			{
				GD.PrintErr("Unknown BGM: " + name);
				return;
			}
			stream.Loop = true;
			BGMDictionary.Add(name, stream);
		}

		BGM.Stream = stream;
		BGM.PitchScale = 1f;
		BGM.Play();
	}

	public void PlayBGM(string name, float volume, float pitch)
	{
		if (!BGMDictionary.TryGetValue(name, out AudioStreamOggVorbis stream))
		{
			if (ResourceLoader.Exists("res://audio/bgm/" + name + ".ogg"))
				stream = ResourceLoader.Load<AudioStreamOggVorbis>("res://audio/bgm/" + name + ".ogg");
			else
			{
				GD.PrintErr("Unknown BGM: " + name);
				return;
			}
			stream.Loop = true;
			BGMDictionary.Add(name, stream);
		}
		BGM.Stream = stream;
		BGM.PitchScale = pitch;
		BGM.VolumeDb = Mathf.LinearToDb(volume);
		BGM.Play();
	}

	/// <summary>
	/// Stops the currently playing BGM.
	/// </summary>
	public void StopBGM()
	{
		BGM.Stop();
	}

	internal bool LoadCustomBGM(string path)
	{
		AudioStreamOggVorbis stream = AudioStreamOggVorbis.LoadFromFile(path);
		stream.Loop = true;
		return BGMDictionary.TryAdd(path.GetFile().GetBaseName(), stream);
	}

	internal bool LoadCustomSFX(string path)
	{
		AudioStreamOggVorbis stream = AudioStreamOggVorbis.LoadFromFile(path);
		return SFXDictionary.TryAdd(path.GetFile().GetBaseName(), stream);
	}

	/// <summary>
	/// Fades the BGM to the given <paramref name="volume"/> over the given number of <paramref name="seconds"/>.
	/// </summary>
	/// <param name="volume">The volume to fade the BGM to.</param>
	/// <param name="seconds">How long it should take for the BGM to fade, in seconds.</param>
	public void FadeBGMTo(float volume, float seconds = 1f)
	{
		float current = AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("BGM"));
		float target = current + Mathf.LinearToDb(volume / 100f);
		Tween tween = CreateTween();
		tween.TweenProperty(BGM, "volume_db", target, seconds);
	}

	private void OnSFXFinish(AudioStreamPlayer player)
	{
		foreach (var pair in PlayingSounds)
		{
			if (pair.Value == player)
			{
				PlayingSounds.Remove(pair.Key);
				break;
			}
		}
	}

	internal IEnumerable<string> GetAllBGM()
	{
		return BGMDictionary.Keys;
	}

	private void OnBGMFinish()
	{
		BGM.Play();
	}
}
