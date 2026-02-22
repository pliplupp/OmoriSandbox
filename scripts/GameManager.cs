using Discord;
using Godot;
using OmoriSandbox.Actors;
using OmoriSandbox.Animation;
using OmoriSandbox.Battle;
using OmoriSandbox.Editor;
using OmoriSandbox.Modding;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OmoriSandbox;

/* TODO: Update 1.0
 Features:
 - Modify text speed - done
 - More Info / State Icons - in testing
 - Edit BGM loop point - done
 - Queue restart via keybind during battle - done
 - Text effects - done
 - Boss Alt Stats - in progress
 - Minibosses (Snaley, Shady Mole, etc.) - done
 - Skip dialogue with 'X' - done
 - Modifiable keybinds - done
 - Fullscreen option - done
 - Premade vanilla presets
 - Humphrey - in testing
 - Other Sunny skills - in progress
 - Update wiki
 - Add modded animation support
 - Console exclusive stats + mechanics - in testing
 - Add quit button - done
 - Allow damage to be overriden at various points of the calculation - done
 - basil release energy double use bonus - done
 - Other TODOs
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
#if DEBUG
		FPSLabel.Text = $"{Engine.GetFramesPerSecond()} : {OS.GetStaticMemoryUsage() / 1000000}";
#else
		FPSLabel.Text = $"{Engine.GetFramesPerSecond()}";
#endif

		DiscordManager.Tick();
	}

	public override void _Ready()
	{
		Instance = this;

		DiscordManager = new();

		AnimationManager.Instance.Init();
		AudioManager.Instance.Init();
		ModManager.Instance.LoadMods();
		MainMenuManager.Instance.Init();
	}

	public override void _ExitTree()
	{
		DiscordManager.Shutdown();
	}

	internal void LoadBattlePreset(Godot.Collections.Dictionary<string, Variant> data)
	{
		List<PartyMemberComponent> party = [];
		List<EnemyComponent> enemy = [];
		Godot.Collections.Dictionary<string, int> items = data["items"].AsGodotDictionary<string, int>();
		Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> actors = data["actors"].AsGodotArray<Godot.Collections.Dictionary<string, Variant>>();
		Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> enemies = data["enemies"].AsGodotArray<Godot.Collections.Dictionary<string, Variant>>();
		int FollowupTier = data["followupTier"].AsInt32();
		bool UseBasilFollowups = data["basilFollowups"].AsBool();
		bool UseBasilReleaseEnergy = data["basilReleaseEnergy"].AsBool();
		bool DisableDialogue = false;
		if (data.TryGetValue("disableDialogue", out Variant value))
			DisableDialogue = value.AsBool();
		bool DisableDamageNumbers = false;
		if (data.TryGetValue("disableDamageNumbers", out value))
			DisableDamageNumbers = value.AsBool();

		string battleback = data["battleback"].AsString();
		if (ResourceLoader.Exists("res://assets/battlebacks/" + battleback + ".png"))
			BattlebackParent.Texture = ResourceLoader.Load<Texture2D>("res://assets/battlebacks/" + battleback + ".png");
		else if (ModManager.Instance.Battlebacks.TryGetValue(battleback, out Texture2D texture))
			BattlebackParent.Texture = texture;
		else
			GD.PrintErr("Failed to load battleback: " + battleback);

		string bgm = StringExtensions.GetBaseName(data["bgm"].AsString());
		double bgmPitch = 1.0d;
		if (data.TryGetValue("bgmPitch", out value))
			bgmPitch = value.AsDouble();
		double bgmLoopPoint = 0d;
		if (data.TryGetValue("bgmLoopPoint", out value))
			bgmLoopPoint = value.AsDouble();
		AudioManager.Instance.PlayBGM(bgm, 1f, (float)bgmPitch);
		AudioManager.Instance.SetBGMLoopOffset(bgmLoopPoint);

		foreach (var entry in actors)
		{
			if (party.Count >= 4)
			{
				GD.PushWarning("Party is full, skipping extra actor");
				continue;
			}

			PackedScene followup = null;
			int position = entry["position"].AsInt32();
			bool followupsDisabled = entry["followupsDisabled"].AsBool();
			if (!followupsDisabled)
			{
				if (UseBasilFollowups && position == 2)
					followup = Followups[4];
				else
					followup = Followups[position];
			}

			PartyMemberComponent actor = SpawnPartyMember(
				entry["name"].ToString(),
				followup,
				position,
				entry["weapon"].ToString(),
				entry["charm"].ToString(),
				entry["skills"].AsStringArray(),
				entry["level"].AsInt32(),
				entry["emotion"].ToString()
				);

			if (actor == null)
				continue;

			party.Add(actor);
		}

		foreach (var entry in enemies)
		{
			// dumb hack to read the Vector2 since AsVector2() doesn't seem to work here
			string positionStr = entry["position"].ToString();
			string[] positionArr = positionStr.Substring(1, positionStr.Length - 2).Split(',');
			Vector2 position = new(float.Parse(positionArr[0], CultureInfo.InvariantCulture), float.Parse(positionArr[1], CultureInfo.InvariantCulture));
			if (!entry.TryGetValue("layer", out Variant layer))
				layer = 0;
			while (enemy.Any(x => x.Actor.CenterPoint == position))
			{
				// prevent stacking
				position += new Vector2(0.01f, 0f);
			}
			EnemyComponent en = SpawnEnemy(
					entry["name"].ToString(),
					position,
					entry["emotion"].ToString(),
					entry["fallsOffScreen"].AsBool(),
					layer.AsInt32()
				);
			if (en == null)
				continue;
			enemy.Add(en);
		}

		DialogueManager.Instance.DialogueDisabled = DisableDialogue;
		DiscordManager.SetBattling(enemies.Count);
		BattleManager.Instance.Init(party, enemy, items, FollowupTier, UseBasilFollowups, UseBasilReleaseEnergy, DisableDamageNumbers);
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

	internal EnemyComponent SpawnEnemy(string who, Vector2 position, string startingEmotion = "neutral", bool fallsOffScreen = true, int layer = 0)
	{
		Enemy instance = Database.CreateEnemy(who);
		Node2D node = EnemyNode.Instantiate<Node2D>();
		BattlebackParent.AddChild(node);
		GD.Print("Spawning enemy at: " + position);
		node.GlobalPosition = position;
		EnemyComponent component = new();
		node.AddChild(component);
		node.ZIndex -= layer;
		component.SetEnemy(instance, startingEmotion, fallsOffScreen, layer);
		return component;
	}

	internal PartyMemberComponent SpawnPartyMember(string who, PackedScene followup, int position, string weapon, string charm, string[] skills, int level = 1, string startingEmotion = "neutral")
	{
		PartyMember instance = Database.CreatePartyMember(who);
		if (instance == null)
			return null;
		Control card = BattlecardUI.Instantiate<Control>();
		Party.AddChild(card);
		card.Position = position switch
		{
			0 => new Vector2(20, 306),
			1 => new Vector2(20, 5),
			2 => new Vector2(506, 306),
			3 => new Vector2(506, 5),
			_ => card.Position
		};
		PartyMemberComponent component = new();
		card.AddChild(component);
		component.SetPartyMember(instance, followup, position, startingEmotion, level, weapon, charm, skills);
		return component;
	}
}
