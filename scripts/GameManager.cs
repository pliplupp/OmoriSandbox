using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;

public partial class GameManager : Node
{
	[Export] public PackedScene BattlecardUI;
	[Export] public PackedScene EnemyUI;
	[Export] public TextureRect BattlebackParent;
	[Export] public Label FPSLabel;
	[Export] public Node Party;

	[Export] public PackedScene[] Followups;

	public RandomNumberGenerator Random = new();
	public AnimationManager AnimationManager { get; private set; }

	public string CustomDataPath = "user://custom/";

	public static GameManager Instance { get; private set; }

	public override void _PhysicsProcess(double delta)
	{
#if DEBUG
		FPSLabel.Text = $"{Engine.GetFramesPerSecond()} : {OS.GetStaticMemoryUsage() / 1000000}";
#else
		FPSLabel.Text = $"{Engine.GetFramesPerSecond()}";
#endif
	}

	public override void _Ready()
	{
		Instance = this;

		AnimationManager = new();
		AddChild(AnimationManager);

		AudioManager.Instance.Init();

		// Omori, Aubrey, Hero, Kel
		// TODO: properly handle less than 4 party members
	}

	public void LoadBattlePreset(Godot.Collections.Dictionary<string, Variant> data)
	{
		List<PartyMemberComponent> party = [];
		List<EnemyComponent> enemy = [];
		Godot.Collections.Dictionary<string, int> items = data["items"].AsGodotDictionary<string, int>();
		Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> actors = data["actors"].AsGodotArray<Godot.Collections.Dictionary<string, Variant>>();
		Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> enemies = data["enemies"].AsGodotArray<Godot.Collections.Dictionary<string, Variant>>();
		int FollowupTier = data["followupTier"].AsInt32();
		bool UseBasilFollowups = data["basilFollowups"].AsBool();
		bool UseBasilReleaseEnergy = data["basilReleaseEnergy"].AsBool();

		string battleback = data["battleback"].AsString();
		if (ResourceLoader.Exists("res://assets/battlebacks/" + battleback))
			BattlebackParent.Texture = ResourceLoader.Load<Texture2D>("res://assets/battlebacks/" + battleback);
		else if (FileAccess.FileExists(CustomDataPath + "/battlebacks/" + battleback))
			BattlebackParent.Texture = ImageTexture.CreateFromImage(Image.LoadFromFile(CustomDataPath + "/battlebacks/" + battleback));
		else
			GD.PrintErr("Failed to load battleback: " + battleback);

		string bgm = StringExtensions.GetBaseName(data["bgm"].AsString());
		AudioManager.Instance.PlayBGM(bgm);

		foreach (var entry in actors)
		{
			if (party.Count >= 4)
			{
				GD.PushWarning("Party is full, skipping extra actor");
				continue;
			}

			PackedScene followup = null;
			// TODO: improve setting basil followup
			int position = entry["position"].AsInt32();
			bool followupsDisabled = entry["followupsDisabled"].AsBool();
			if (!followupsDisabled)
			{
				if (UseBasilFollowups && position == 3)
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
			EnemyComponent en = SpawnEnemy(
					entry["name"].ToString(),
					position,
					entry["emotion"].ToString(),
					entry["fallsOffScreen"].AsBool()
				);
			if (en == null)
				continue;
			enemy.Add(en);
		}

		BattleManager.Instance.Init(party, enemy, items, FollowupTier, UseBasilFollowups, UseBasilReleaseEnergy);
	}

	public void DespawnAll()
	{
		foreach (Node child in Party.GetChildren())
		{
			child.QueueFree();
		}

		foreach (Node child in BattlebackParent.GetChildren())
		{
			child.QueueFree();
		}
	}

	private EnemyComponent SpawnEnemy(string who, Vector2 position, string startingEmotion = "neutral", bool fallsOffScreen = true)
	{
		Enemy instance = Database.CreateEnemy(who);
		Node2D node = EnemyUI.Instantiate<Node2D>();
		BattlebackParent.AddChild(node);
		GD.Print("Spawning enemy at: " + position);
		node.GlobalPosition = position;
		EnemyComponent component = new();
		node.AddChild(component);
		component.SetEnemy(instance, startingEmotion, fallsOffScreen);
		return component;
	}

	private PartyMemberComponent SpawnPartyMember(string who, PackedScene followup, int position, string weapon, string charm, string[] skills, int level = 1, string startingEmotion = "neutral")
	{
		PartyMember instance = Database.CreatePartyMember(who);
		if (instance == null)
			return null;
		Control card = BattlecardUI.Instantiate<Control>();
		Party.AddChild(card);
		switch (position)
		{
			case 0:
				card.Position = new Vector2(20, 306);
				break;
			case 1:
				card.Position = new Vector2(20, 5);
				break;
			case 2:
				card.Position = new Vector2(506, 5);
				break;
			case 3:
				card.Position = new Vector2(506, 306);
				break;
		}
		PartyMemberComponent component = new();
		card.AddChild(component);
		component.SetPartyMember(instance, followup, position, startingEmotion, level, weapon, charm, skills);
		return component;
	}


}
