using Godot;
using Newtonsoft.Json;
using OmoriSandbox.Actors;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OmoriSandbox.Animation;

/// <summary>
/// Handles all animation related functionality, including playing animations and screen shake.
/// </summary>
public partial class AnimationManager : Node
{
	/// <summary>
	/// Fired whenever all animations finish playing.
	/// </summary>
	[Signal]
	public delegate void AnimationFinishedEventHandler();

	[Export] private TextureRect Battleback;
	[Export] private AnimatedSprite2D ReleaseEnergy;
	[Export] private AnimatedSprite2D ReleaseEnergyBasil;
	[Export] private AnimatedSprite2D RedHands;
	[Export] private AnimatedSprite2D FlowerCrown;
	[Export] private ColorRect Photograph;
	[Export] private PackedScene PerfectheartOverlaySprite;
	[Export] private Node2D PerfectheartOverlayParent;
	[Export] private Node2D FullScreenEffectNode;

	private Dictionary<int, RPGMAnimatedSprite> Animations = [];

	private const float FPS = 15f;
	private float FrameDuration = 1f / FPS;
	private float FrameTimer = 0f;
	private List<PlayingAnimation> PlayingAnimations = [];

	private float Shake = 0f;
	private float ShakePwr = 0f;
	private float ShakeSpd = 0f;
	private int ShakeDuration = 0;
	private float ShakeDirection = -1f;

	public static AnimationManager Instance { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;
	}

	internal void Init()
	{
		string data = FileAccess.GetFileAsString("res://animations/animations.json");
		List<AnimationInfo> animationData = JsonConvert.DeserializeObject<List<AnimationInfo>>(data);
		foreach (AnimationInfo info in animationData)
		{
			bool missingTexture = string.IsNullOrWhiteSpace(info.Texture);
			bool missingAltTexture = string.IsNullOrWhiteSpace(info.AltTexture);

			if (missingTexture && missingAltTexture)
				continue;

			RPGMAnimatedSprite animation = new(info.Id, info.Layer,
				missingTexture ? null : ResourceLoader.Load<Texture2D>($"res://assets/animations/{info.Texture}.png"),
				missingAltTexture ? null : ResourceLoader.Load<Texture2D>($"res://assets/animations/{info.AltTexture}.png"));

			foreach (float[][] frame in info.Frames)
			{
				List<Frame> frames = [];
				foreach (float[] f in frame)
				{
					frames.Add(new Frame((int)f[0], f[1], f[2], f[3], f[4], f[5] == 1, f[6]));
				}
				animation.CreateFrame(frames);
			}
			foreach (SFXInfo sfx in info.SFX)
			{
				animation.SetFrameSFX(sfx.Frame, new SFX(sfx.Name, sfx.Pitch, sfx.Volume));
			}
			foreach (ShakeInfo shake in info.Shake)
			{
				animation.SetFrameShake(shake.Frame, shake.Power, shake.Speed, shake.Duration);
			}
			if (!Animations.TryAdd(info.Id, animation))
			{
				GD.PrintErr("Unable to add animation ID " + info.Id + ", is there a duplicate?");
			}
		}
		GD.Print($"Loaded {Animations.Count} animations");
	}

	public override void _Process(double delta)
	{
		if (PlayingAnimations.Count == 0)
			return;

		FrameTimer += (float)delta;

		if (FrameTimer >= FrameDuration)
		{
			FrameTimer -= FrameDuration;
			NextFrame();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		// physics process runs at 60 fps, just like screen shake
		if (ShakeDuration > 0 || Shake != 0f)
		{
			UpdateShake();
		}

		float x = 0f;
		x += (float)Math.Round(Shake) - 640f;
		Battleback.Position = new Vector2(x, 0);
	}

	private void NextFrame()
	{
		for (int i = PlayingAnimations.Count - 1; i >= 0; i--)
		{
			// returns true if we're out of frames
			if (PlayingAnimations[i].AdvanceFrame())
			{
				PlayingAnimations[i].QueueFree();
				PlayingAnimations.RemoveAt(i);
				if (PlayingAnimations.Count == 0)
				{
					FrameTimer = 0f;
					EmitSignal(SignalName.AnimationFinished);
					return;
				}
				continue;
			}
			if (PlayingAnimations[i].Animation.TryGetFrameSFX(PlayingAnimations[i].CurrentFrame, out List<SFX> sfx))
			{
				sfx.ForEach(AudioManager.Instance.PlaySFX);
			}
			if (PlayingAnimations[i].Animation.TryGetFrameShake(PlayingAnimations[i].CurrentFrame, out Shake shake))
			{
				InitShake(shake);
			}
		}
	}

	private void UpdateShake()
	{
		float delta = (ShakePwr * (2f * ShakeSpd) * ShakeDirection) / 5f;
		if (ShakeDuration <= 1 && Shake * (Shake + delta) < 0)
			Shake = 0;
		else
			Shake += delta;
		if (Shake > ShakePwr * 2f)
			ShakeDirection = -1;
		if (Shake < -ShakePwr * 2f)
			ShakeDirection = 1;
		ShakePwr *= 0.9f;
		ShakeDuration--;
	}

	/// <summary>
	/// Initializes a new screenshake that will begin on the next valid frame.
	/// Calling this method while a shake is already happening will stop the currently playing one.
	/// </summary>
	public void InitShake(Shake shake)
	{
		Battleback.Position = new Vector2(-640, 0);
		Shake = 0f;
		ShakePwr = shake.Power;
		ShakeSpd = shake.Speed;
		ShakeDuration = shake.Duration;
	}

	private void ResetShake()
	{
		Battleback.Position = new Vector2(-640, 0);
		Shake = 0f;
		ShakePwr = 0f;
		ShakeSpd = 0f;
		ShakeDuration = 0;
	}

	/// <summary>
	/// Plays an animation with the given <paramref name="id"/> centered on the given <paramref name="target"/>.<br/>
	/// Use <see cref="WaitForAnimation(int, Actor, bool)"/> if you want to wait for the animation to finish.
	/// </summary>
	/// <param name="id">The animation ID to play. Uses the same ID numbers as OMORI for all vanilla animations.</param>
	/// <param name="target">The <see cref="Actor"/> that this animation will play centered on.</param>
	/// <param name="b"></param>
	/// <param name="targetsEnemy">Whether or not this animation targets an enemy.<br/>
	/// Mainly used for animation layering, such as skill animations that target enemies and need to display underneath the UI.</param>
	public void PlayAnimation(int id, Actor target)
	{
		StartAnimation(id, target.CenterPoint, target is Enemy);
	}

	/// <summary>
	/// Plays an animation with the given <paramref name="id"/> centered on the screen.<br/>
	/// Use <see cref="WaitForScreenAnimation(int, bool)"/> if you want to wait for the animation to finish.
	/// </summary>
	/// <param name="id">The animation ID to play. Uses the same ID numbers as OMORI for all vanilla animations.</param>
	/// <param name="targetsEnemy">Whether or not this animation targets an enemy.<br/>
	/// Mainly used for animation layering, such as skill animations that target enemies and need to display underneath the UI.</param>
	public void PlayScreenAnimation(int id, bool targetsEnemy)
	{
		StartAnimation(id, new Vector2(320, 240), targetsEnemy);
	}

	/// <summary>
	/// Plays an animation with the given <paramref name="id"/> centered on the given <paramref name="target"/>, and waits for it to finish.<br/>
	/// Use <see cref="PlayAnimation(int, Actor, bool)"/> if you want the animation to play without waiting.
	/// </summary>
	/// <param name="id">The animation ID to play. Uses the same ID numbers as OMORI for all vanilla animations.</param>
	/// <param name="target">The <see cref="Actor"/> that this animation will play centered on.</param>
	/// <param name="targetsEnemy">Whether or not this animation targets an enemy.<br/>
	/// Mainly used for animation layering, such as skill animations that target enemies and need to display underneath the UI.</param>
	/// <returns>An awaitable <see cref="Task"/> that will complete whenever the animation finishes playing.</returns>
	public Task WaitForAnimation(int id, Actor target)
	{
		TaskCompletionSource tcs = new();

		void Handle()
		{
			AnimationFinished -= Handle;
			tcs.SetResult();
		}	

		PlayAnimation(id, target);
		AnimationFinished += Handle;
		return tcs.Task;
	}

	/// <summary>
	/// Plays an animation with the given <paramref name="id"/> centered on the screen, and waits for it to finish.<br/>
	/// Use <see cref="PlayScreenAnimation(int, bool)"/> if you want the animation to play without waiting.
	/// </summary>
	/// <param name="id">The animation ID to play. Uses the same ID numbers as OMORI for all vanilla animations.</param>
	/// <param name="targetsEnemy">Whether or not this animation targets an enemy.<br/>
	/// Mainly used for animation layering, such as skill animations that target enemies and need to display underneath the UI.</param>
	/// <returns>An awaitable <see cref="Task"/> that will complete whenever the animation finishes playing.</returns>
	public Task WaitForScreenAnimation(int id, bool targetsEnemy)
	{
		TaskCompletionSource tcs = new();

		void Handle()
		{
			AnimationFinished -= Handle;
			tcs.SetResult();
		}

		PlayScreenAnimation(id, targetsEnemy);
		AnimationFinished += Handle;
		return tcs.Task;
	}

	/// <summary>
	/// Plays the Omori version of the Release Energy animation, and waits for it to finish.
	/// </summary>
	/// <returns>An awaitable <see cref="Task"/> that will complete whenever the animation finishes playing.</returns>
	public Task WaitForReleaseEnergy()
	{
		TaskCompletionSource tcs = new();
		void Handle()
		{
			ReleaseEnergy.AnimationFinished -= Handle;
			ReleaseEnergy.Visible = false;
			tcs.SetResult();
		}

		ReleaseEnergy.Visible = true;
		AudioManager.Instance.PlaySFX("BA_release_energy", 1, 0.9f);
		ReleaseEnergy.Play();
		ReleaseEnergy.AnimationFinished += Handle;
		return tcs.Task;
	}

	/// <summary>
	/// Plays the Basil version of the Release Energy animation, and waits for it to finish.
	/// </summary>
	/// <returns>An awaitable <see cref="Task"/> that will complete whenever the animation finishes playing.</returns>
	public Task WaitForReleaseEnergyBasil()
	{
		TaskCompletionSource tcs = new();
		void Handle()
		{
			ReleaseEnergyBasil.Visible = false;
			tcs.SetResult();
		}

		ReleaseEnergyBasil.Visible = true;
		ReleaseEnergyBasil.Modulate = Colors.Transparent;
		AudioManager.Instance.PlaySFX("BA_release_energy", 1, 0.9f);
		ReleaseEnergyBasil.Play();
		Tween tween = GetTree().CreateTween();
		tween.TweenProperty(ReleaseEnergyBasil, "modulate:a", 1f, 0.5f);
		tween.TweenInterval(2.25f);
		tween.TweenProperty(ReleaseEnergyBasil, "modulate:a", 0f, 0.5f);
		tween.TweenCallback(Callable.From(Handle));

		return tcs.Task;
	}

	/// <summary>
	/// Plays the Red Hands skill animation, and waits for it to finish.
	/// </summary>
	/// <returns>An awaitable <see cref="Task"/> that will complete whenever the animation finishes playing.</returns>
	public Task WaitForRedHands()
	{
		TaskCompletionSource tcs = new();
		void Handle()
		{
			RedHands.AnimationFinished -= Handle;
			RedHands.Visible = false;
			tcs.SetResult();
		}

		RedHands.Visible = true;
		AudioManager.Instance.PlaySFX("SE_red_hands", 0.8f, 0.9f);
		RedHands.Play();
		RedHands.AnimationFinished += Handle;
		return tcs.Task;
	}

	/// <summary>
	/// Plays the Flower Crown skill animation, and waits for it to finish.
	/// </summary>
	/// <returns>An awaitable <see cref="Task"/> that will complete whenever the animation finishes playing.</returns>
	public Task WaitForFlowerCrown()
	{
		TaskCompletionSource tcs = new();
		void Handle()
		{
			FlowerCrown.AnimationFinished -= Handle;
			FlowerCrown.Visible = false;
			tcs.SetResult();
		}

		FlowerCrown.Visible = true;
		AudioManager.Instance.PlaySFX("SE_red_hands", 0.8f, 0.9f);
		FlowerCrown.Play();
		FlowerCrown.AnimationFinished += Handle;
		return tcs.Task;
	}


	internal Task WaitForOmoriSpecialAnimation(string overlay, string effect)
	{
		TaskCompletionSource tcs = new();

		Sprite2D effectTex = new()
		{
			Texture = ResourceLoader.Load<Texture2D>(effect),
			Position = new Vector2(320f, 150f),
			Scale = new Vector2(2f, 2f),
			Modulate = Colors.Transparent
		};
		FullScreenEffectNode.AddChild(effectTex);

		Sprite2D overlayTex = new()
		{
			Texture = ResourceLoader.Load<Texture2D>(overlay),
			Position = Vector2.Zero,
			Centered = false,
			Modulate = Colors.Transparent
		};
		FullScreenEffectNode.AddChild(overlayTex);

		void Finished()
		{
			overlayTex.QueueFree();
			effectTex.QueueFree();
			tcs.SetResult();
		}

		Tween overlayTween = GetTree().CreateTween();
		overlayTween.TweenProperty(overlayTex, "modulate:a", 0.60f, 1f);
		overlayTween.TweenInterval(0.66f);
		overlayTween.TweenProperty(overlayTex, "modulate:a", 0f, 0.66f);

		Tween effectTween = GetTree().CreateTween();
		effectTween.TweenProperty(effectTex, "modulate:a", 1f, 1f);
		effectTween.Parallel().TweenProperty(effectTex, "position:y", 180f, 1f);
		effectTween.Parallel().TweenProperty(effectTex, "scale", new Vector2(0.65f, 0.65f), 1f);
		effectTween.TweenInterval(0.66f);
		effectTween.TweenProperty(effectTex, "modulate:a", 0f, 0.66f);
		effectTween.Parallel().TweenProperty(effectTex, "position:y", 150f, 0.66f);
		effectTween.Parallel().TweenProperty(effectTex, "scale", new Vector2(2f, 2f), 0.66f);
		effectTween.TweenInterval(0.33f);
		// only one tween needs to call Finished
		effectTween.TweenCallback(Callable.From(Finished));

		return tcs.Task;
	}

	internal Task WaitForBasilSpecialAnimation(string effect, int animationId)
	{
		TaskCompletionSource tcs = new();

		Sprite2D effectTex = new()
		{
			Texture = ResourceLoader.Load<Texture2D>(effect),
			Position = new Vector2(320f, 150f),
			Scale = new Vector2(2f, 2f),
			Modulate = Colors.Transparent
		};
		FullScreenEffectNode.AddChild(effectTex);

		void Finished()
		{
			effectTex.QueueFree();
			tcs.SetResult();
		}

		Tween effectTween = GetTree().CreateTween();
		effectTween.TweenProperty(effectTex, "modulate:a", 1f, 1f);
		effectTween.Parallel().TweenProperty(effectTex, "position:y", 240f, 1f);
		effectTween.Parallel().TweenProperty(effectTex, "scale", new Vector2(1f, 1f), 1f);
		effectTween.TweenCallback(Callable.From(() => PlayScreenAnimation(animationId, true)));
		effectTween.TweenInterval(1.5f);
		effectTween.TweenProperty(effectTex, "modulate:a", 0f, 0.66f);
		effectTween.Parallel().TweenProperty(effectTex, "position:y", 150f, 0.66f);
		effectTween.Parallel().TweenProperty(effectTex, "scale", new Vector2(2f, 2f), 0.66f);
		effectTween.TweenInterval(0.33f);
		// only one tween needs to call Finished
		effectTween.TweenCallback(Callable.From(Finished));

		return tcs.Task;
	}

	/// <summary>
	/// Plays the Photograph animation. Mainly used by Basil skills.
	/// </summary>
	public void PlayPhotograph()
	{
		Photograph.Visible = true;
		Tween tween = GetTree().CreateTween();
		tween.TweenProperty(Photograph, "modulate:a", 0f, 1f);
		tween.TweenCallback(Callable.From(() =>
		{
			Photograph.Modulate = Colors.White;
			Photograph.Visible = false;
		}));
	}

	internal Sprite2D SpawnPerfectheartOverlay(Vector2 position)
	{
		Sprite2D sprite = PerfectheartOverlaySprite.Instantiate<Sprite2D>();
		PerfectheartOverlayParent.AddChild(sprite);
		sprite.Modulate = Colors.Transparent;
		sprite.Position = position;
		Tween tween = sprite.CreateTween();
		tween.TweenProperty(sprite, "modulate:a", 1f, 1f);
		return sprite;
	}

	internal void DespawnAll()
	{
		foreach (Node child in PerfectheartOverlayParent.GetChildren())
			child.QueueFree();
	}

	private void StartAnimation(int id, Vector2 position, bool targetsEnemy)
	{
		if (!Animations.TryGetValue(id, out RPGMAnimatedSprite animation))
		{
			GD.PrintErr("Unknown animation: " + id);
			return;
		}

		Vector2 drawPosition = position - new Vector2(96f, 96f);
		// hack fix for the headbutt curtain animation
		if (id == 30)
			drawPosition += new Vector2(6f, 0f);

		int index = 0;
		switch (animation.Layer)
		{
			case 0:
				index = 10;
				break;
			case 2:
				index = -1;
				break;
			case 3:
				index = targetsEnemy ? -4 : 0;
				break;
		}

		if (animation.TryGetFrameSFX(0, out List<SFX> sfx))
		{
			sfx.ForEach(AudioManager.Instance.PlaySFX);
		}

		PlayingAnimation playing = new(animation, drawPosition, index);
		AddChild(playing);
		PlayingAnimations.Add(playing);
	}

	internal IEnumerable<RPGMAnimatedSprite> GetAllAnimations()
	{
		return Animations.Values;
	}
}

#pragma warning disable CS0649
internal class AnimationInfo
{
	public int Id;
	public int Layer;
	public string Texture;
	public string AltTexture;
	public float[][][] Frames;
	public SFXInfo[] SFX;
	public ShakeInfo[] Shake;
}

internal class SFXInfo
{
	public int Frame;
	public string Name;
	public float Pitch;
	public float Volume;
}

internal class ShakeInfo
{
	public int Frame;
	public int Power;
	public int Speed;
	public int Duration;
}
#pragma warning restore CS0649
