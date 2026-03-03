using Discord;
using Godot;
using OmoriSandbox.Actors;
using OmoriSandbox.Animation;
using OmoriSandbox.Battle;
using OmoriSandbox.Editor;
using OmoriSandbox.Modding;
using System.Collections.Generic;
using System.Linq;

namespace OmoriSandbox;

/* TODO: Update 1.0
 Features:
 - Modify text speed - done
 - More Info / State Icons - in testing
 - Edit BGM loop point - done
 - Queue restart via keybind during battle - done
 - Text effects - done
 - Boss Alt Stats - in testing
 - Minibosses (Snaley, Shady Mole, etc.) - done
 - Skip dialogue with 'X' - done
 - Modifiable keybinds - done
 - Fullscreen option - done
 - Premade vanilla presets
 - Humphrey - in testing
 - Other Sunny skills - in progress
 - Update wiki
 - Add modded animation support - done
 - Console exclusive stats + mechanics - in testing
 - Add quit button - done
 - Allow damage to be overriden at various points of the calculation - done
 - basil release energy double use bonus - done
 - Other TODOs
 - custom boss rush - in testing
 - auto-generate default mod - done
 Bugfixes:
 - Tiered stat modifiers do not increment - done
 - Enemies can stack on top of each other preventing selection - in testing
 - Capitalize 'emotions' and 'heart' in perfectheart text - done
 - 'OMORI did not succumb' text is missing - done
 - Certain hit sounds do not play, seemingly at random
 - Mr Jawsum redirect damage should ignore juice - done
 - Mr Jawsum shouldn't be crit for 2 damage - done
 - Last Resort shouldn't hurt the user if it misses - done
 - Audio pitch/volume still doesn't get reset after being modified - in testing
 - Boss Hero/Kel/Aubrey shouldn't be able to call themselves - done
 - Pluto (expanded)'s headbutt has bugged text - done
 - Backing out of selecting a target for an item deletes the item - done
 - Bossman Hero's enemy buff should remove all debuffs first - done
 - Basil followups use incorrect targeting - done
 - Using certain enemy skills on party members breaks the game
 - Given key was not present in the dictionary BattleManager.cs:756 - done
 - Poetry book has no animation - done
 - Perfectheart exploit breaks with plot armor - fixed?
 - Fix menu wrapping - done
 - Fix Skill/Snack/Toy menu back going back to menu it came from - done
 - Make skill/toy menu appear on top of battle menu - done
 - Fix followups (party and bosses) with less than 4 party members - done (needs testing for bosses)
 */

internal partial class GameManager : Node
{
	public const string Version = "OmoriSandbox v1.0.0 (dev build)";
	
	[Export] private PackedScene BattlecardUI;
	[Export] private PackedScene EnemyNode;
	[Export] private TextureRect BattlebackParent;
	[Export] private Label FPSLabel;
	[Export] private Node Party;

	[Export] private PackedScene[] Followups;

	public RandomNumberGenerator Random = new();
	internal DiscordManager DiscordManager { get; private set; }
	public static GameManager Instance { get; private set; }

	public override void _PhysicsProcess(double delta)
	{
		FPSLabel.Text = $"{(SettingsMenuManager.Instance.ShowFPS ? Engine.GetFramesPerSecond() : "")} {Version}";

		DiscordManager.Tick();
	}

	public override void _Ready()
	{
		Instance = this;
		
		GD.Print("Version: " + Version);

		DiscordManager = new();

		AnimationManager.Instance.Init();
		AudioManager.Instance.Init();
		ModManager.Instance.LoadMods();
		MainMenuManager.Instance.Init();
		EditorManager.Instance.Init();
	}

	public override void _ExitTree()
	{
		DiscordManager.Shutdown();
	}

	internal void SetBattleback(string name)
	{
		if (ResourceLoader.Exists("res://assets/battlebacks/" + name + ".png"))
			BattlebackParent.Texture = ResourceLoader.Load<Texture2D>("res://assets/battlebacks/" + name + ".png");
		else if (ModManager.Instance.Battlebacks.TryGetValue(name, out Texture2D texture))
			BattlebackParent.Texture = texture;
		else
		{
			BattlebackParent.Texture = ResourceLoader.Load<Texture2D>("res://assets/battlebacks/battleback_vf_default.png");
			GD.PushWarning($"Failed to load battleback {name}, falling back to default.");
		}
	}

	internal void LoadBattlePreset(BattlePreset preset)
	{
		List<PartyMemberComponent> party = [];
		List<EnemyComponent> enemy = [];

		string battleback = preset.Type is GameModeType.Normal ? preset.Battleback : preset.Stages[0].Battleback;
		string bgm = preset.Type is GameModeType.Normal ? preset.BGM : preset.Stages[0].BGM;
		double pitch = preset.Type is GameModeType.Normal ? preset.BGMPitch : preset.Stages[0].BGMPitch;
		double loopPoint = preset.Type is GameModeType.Normal ? preset.BGMLoopPoint : preset.Stages[0].BGMLoopPoint;
		
		SetBattleback(battleback);
		
		AudioManager.Instance.PlayBGM(bgm, 1f, (float)pitch);
		AudioManager.Instance.SetBGMLoopOffset(loopPoint);

		foreach (BattlePresetActor entry in preset.Actors)
		{
			if (party.Count >= 4)
			{
				GD.PushWarning("Party is full, skipping extra actor");
				continue;
			}

			PackedScene followup = null;
			if (!entry.FollowupsDisabled)
			{
				if (preset.BasilFollowups && entry.Position == 2)
					followup = Followups[4];
				else
					followup = Followups[entry.Position];
			}

			PartyMemberComponent actor = SpawnPartyMember(followup, entry);

			if (actor == null)
			{
				GD.PrintErr("Failed to spawn party member: " + entry.Name);
				continue;
			}

			party.Add(actor);
		}

		foreach (BattlePresetEnemy entry in preset.Enemies)
		{
			if (!entry.Position.StartsWith("Vector2"))
				entry.Position = "Vector2" + entry.Position;
			Vector2 position = GD.StrToVar(entry.Position).AsVector2();
			while (enemy.Any(x => x.Actor.CenterPoint == position))
			{
				// prevent stacking
				position += new Vector2(0.01f, 0f);
			}
			EnemyComponent en = SpawnEnemy(entry, position);
			if (en == null)
			{
				GD.PrintErr("Failed to spawn enemy: " + entry.Name);
				continue;
			}

			enemy.Add(en);
		}

		DialogueManager.Instance.DialogueDisabled = preset.DisableDialogue;
		DiscordManager.SetBattling(enemy.Count);
		BattleManager.Instance.Init(party, enemy, preset.Stages, preset);
	}

	internal void DespawnAll()
	{
		foreach (Node child in Party.GetChildren())
		{
			child.QueueFree();
		}

		// skip the first child as the first child is the FullscreenEffects
		foreach (Node child in BattlebackParent.GetChildren().Skip(1))
		{
			child.QueueFree();
		}
	}

	internal EnemyComponent SpawnEnemy(BattlePresetEnemy enemy, Vector2 position)
	{
		Enemy instance = Database.CreateEnemy(enemy.Name);
		Node2D node = EnemyNode.Instantiate<Node2D>();
		BattlebackParent.AddChild(node);
		GD.Print("Spawning enemy at: " + enemy.Position);
		node.GlobalPosition = position;
		EnemyComponent component = new();
		node.AddChild(component);
		node.ZIndex -= (int)enemy.Layer;
		component.SetEnemy(instance, enemy.Emotion, enemy.FallsOffScreen, (int)enemy.Layer);
		return component;
	}

	private PartyMemberComponent SpawnPartyMember(PackedScene followup, BattlePresetActor actor)
	{
		PartyMember instance = Database.CreatePartyMember(actor.Name);
		if (instance == null)
			return null;
		Control card = BattlecardUI.Instantiate<Control>();
		Party.AddChild(card);
		card.Position = actor.Position switch
		{
			0 => new Vector2(20, 306),
			1 => new Vector2(20, 5),
			2 => new Vector2(506, 306),
			3 => new Vector2(506, 5),
			_ => card.Position
		};
		PartyMemberComponent component = new();
		card.AddChild(component);
		component.SetPartyMember(instance, followup, actor.Position, actor.Emotion, actor.Level, actor.Weapon, actor.Charm, actor.Skills);
		return component;
	}
}
