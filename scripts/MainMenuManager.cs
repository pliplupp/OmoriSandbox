using Godot;
using System;
using System.Collections.Generic;

public partial class MainMenuManager : Node
{
	public override void _Ready()
	{
		AudioManager.Instance.PlayBGM("ow_cattail_fields");

		if (!DirAccess.DirExistsAbsolute("user://presets"))
		{
			using DirAccess access = DirAccess.Open("user://");
			access.MakeDir("presets");
			GD.Print("Created user://presets directory");
		}

		if (!DirAccess.DirExistsAbsolute("user://custom"))
		{
			using DirAccess access = DirAccess.Open("user://");
			access.MakeDir("custom");
			access.MakeDir("custom/bgm");
			access.MakeDir("custom/battlebacks");
			GD.Print("Created user://custom directory");
		}

		PlayButton.Pressed += () =>
		{
			if (TitlePresetDropdown.Selected == -1)
				return;

			string presetName = TitlePresetDropdown.GetItemText(TitlePresetDropdown.Selected);
			string path = "user://presets/" + presetName + ".json";
			if (!FileAccess.FileExists(path))
			{
				GD.PrintErr("Preset file not found at: " + path);
				return;
			}

			using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
			Variant json = Json.ParseString(file.GetAsText());

			if (json.VariantType == Variant.Type.Nil)
			{
				GD.PrintErr("Failed to parse preset " + presetName);
				return;
			}

			GameManager.Instance.LoadBattlePreset(json.AsGodotDictionary<string, Variant>());
			MainMenu.Visible = false;
		};

		ConfigureButton.Pressed += () =>
		{
			AudioManager.Instance.StopBGM();
			MainMenu.Visible = false;
			Editor.Visible = true;
		};

		DataFolderButton.Pressed += () =>
		{
			Error error = OS.ShellOpen(ProjectSettings.GlobalizePath("user://presets"));
			if (error != Error.Ok)
			{
				GD.PrintErr("Failed to open presets folder");
			}
		};

		CustomFolderButton.Pressed += () =>
		{
			Error error = OS.ShellOpen(ProjectSettings.GlobalizePath("user://custom"));
			if (error != Error.Ok)
			{
				GD.PrintErr("Failed to open custom folder");
			}
		};

		GithubButton.Pressed += () =>
		{
			Error error = OS.ShellOpen("https://github.com/EBro912/OmoriSandbox");
			if (error != Error.Ok)
			{
				GD.PrintErr("Failed to open Github link");
			}
		};

		ReturnButton.Pressed += () =>
		{
			AudioManager.Instance.PlayBGM("ow_cattail_fields");
			MainMenu.Visible = true;
			Editor.Visible = false;
		};

		foreach (Control control in AddActorControls)
		{
			control.GetChild<Button>(0).Pressed += () =>
			{
				Control card = BattleCard.Instantiate<Control>();
				control.AddChild(card);
				card.Position = Vector2.Zero;
				PartyMemberEditorComponent editor = PartyMemberEditor.Instantiate<PartyMemberEditorComponent>();
				editor.Name = "Omori";
				ActorTabs.AddChild(editor);
				editor.Init(card);
			};
		}

		AddEnemyControl.GetChild<Button>(0).Pressed += () =>
		{
			AnimatedSprite2D enemySprite = new();
			AddEnemyControl.AddChild(enemySprite);
			EnemyEditorComponent editor = EnemyEditor.Instantiate<EnemyEditorComponent>();
			editor.Name = "LostSproutMole";
			EnemyTabs.AddChild(editor);
			editor.Init(enemySprite);
		};

		// load custom stuff first
		foreach (string battleback in DirAccess.GetFilesAt("user://custom/battlebacks"))
		{
			// maybe not enforce this in the future
			if (battleback.EndsWith(".png"))
				BattlebackDropdown.AddItem(battleback);
		}

		foreach (string bgm in DirAccess.GetFilesAt("user://custom/bgm"))
		{
			// maybe not enforce this in the future
			if (bgm.EndsWith(".ogg"))
				BGMDropdown.AddItem(bgm);
		}
		
		foreach (string battleback in ResourceLoader.ListDirectory("res://assets/battlebacks"))
		{
			BattlebackDropdown.AddItem(battleback);
		}

		foreach (string bgm in ResourceLoader.ListDirectory("res://audio/bgm"))
			BGMDropdown.AddItem(bgm);

		BattlebackDropdown.Selected = BattlebackDropdown.GetItemIndex("battleback_vf_default.png");
        BattlebackDropdown.ItemSelected += (idx) =>
		{
			string battleback = BattlebackDropdown.GetItemText((int)idx);
			if (ResourceLoader.Exists("res://assets/battlebacks/" + battleback))
				BattlebackPreview.Texture = ResourceLoader.Load<Texture2D>("res://assets/battlebacks/" + battleback);
			else if (FileAccess.FileExists("user://custom/battlebacks/" + battleback))
				BattlebackPreview.Texture = ImageTexture.CreateFromImage(Image.LoadFromFile("user://custom/battlebacks/" + battleback));
			else
				GD.PrintErr("Failed to load battleback: " + battleback);
		};

		PreviewButton.Pressed += () =>
		{
			if (PreviewButton.Text == "Preview")
			{
				string bgm = BGMDropdown.GetItemText(BGMDropdown.Selected);
				AudioManager.Instance.PlayBGM(StringExtensions.GetBaseName(bgm));
				PreviewButton.Text = "Stop";
			}
			else
			{
				AudioManager.Instance.StopBGM();
				PreviewButton.Text = "Preview";
			}
		};

		AddItemButton.Pressed += () =>
		{
			HBoxContainer container = new();
			OptionButton dropdown = new();
			foreach (string item in Database.GetAllItemNames())
				dropdown.AddItem(item);
			dropdown.FitToLongestItem = false;
			container.AddChild(dropdown);
			SpinBox quantity = new()
			{
				MinValue = 1,
				MaxValue = 999,
				Value = 1,
				Rounded = true
			};
			container.AddChild(quantity);
			Button remove = new();
			remove.Text = "X";
			remove.Pressed += () =>
			{
				container.QueueFree();
			};
			container.AddChild(remove);
			ItemContainer.AddChild(container);
		};

		SearchButton.Pressed += () =>
		{
			Results.Text = "Loading...";
			List<string> results = [];
			foreach (string skill in Database.GetAllSkillNames())
			{
				if (skill.Contains(SearchInput.Text, System.StringComparison.CurrentCultureIgnoreCase))
				{
					results.Add(skill);
				}
			}
			Results.Text = string.Join(", ", results);
		};

		FollowupTierSlider.ValueChanged += (value) => FollowupTierValue.Text = value.ToString();

		SavePresetButton.Pressed += PreSave;
		LoadPresetButton.Pressed += Load;
		DeletePresetButton.Pressed += PreDelete;

		ResetButton.Pressed += PreResetToDefault;

		ResetPresetDropdown();

		Instance = this;
	}

	public void ReturnToTitle()
	{
		MainMenu.Visible = true;
        AudioManager.Instance.PlayBGM("ow_cattail_fields");
    }

	private void PreSave()
	{
		if (string.IsNullOrWhiteSpace(PresetInput.Text))
			return;

		if (FileAccess.FileExists("user://presets/" + PresetInput.Text + ".json"))
		{
			ConfirmationDialog dialog = new()
			{
				Title = "Confirmation",
				DialogText = "A preset with this name already exists.\nDo you want to overwrite it?",
				Unresizable = true
			};
			dialog.Confirmed += () =>
			{
				Save();
				dialog.QueueFree();
			};
			dialog.Canceled += dialog.QueueFree;
			AddChild(dialog);
			dialog.PopupCentered();
			dialog.Show();
		}
		else
		{
			Save();
		}
	}

	private void Save()
	{
		Godot.Collections.Dictionary<string, Variant> json = new()
		{
			{ "name", PresetInput.Text },
			{ "battleback", BattlebackDropdown.GetItemText(BattlebackDropdown.Selected) },
			{ "bgm", BGMDropdown.GetItemText(BGMDropdown.Selected) },
			{ "followupTier", (int)FollowupTierSlider.Value },
			{ "basilFollowups", BasilFollowupsCheckbox.ButtonPressed },
			{ "basilReleaseEnergy", BasilReleaseEnergyCheckbox.ButtonPressed },
		};

		Godot.Collections.Dictionary<string, int> items = [];
		// diabolical looking code
		Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> actors = [];
		Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> enemies = [];

		foreach (Node child in ItemContainer.GetChildren())
		{
			if (child is HBoxContainer container)
			{
				OptionButton dropdown = container.GetChild<OptionButton>(0);
				SpinBox quantity = container.GetChild<SpinBox>(1);
				items.Add(dropdown.GetItemText(dropdown.Selected), (int)quantity.Value);
			}
		}

		json.Add("items", items);

		List<int> positions = [];
		
		for (int i = 0; i < AddActorControls.Length; i++)
		{
			if (AddActorControls[i].GetChildCount() > 1)
				positions.Add(i);
		}

		int posIndex = 0;
		foreach (Node child in ActorTabs.GetChildren())
		{
			if (child is PartyMemberEditorComponent editor)
			{
				Godot.Collections.Array<string> skills = [];
				skills.Add(editor.AttackSkill.Text);
				foreach (LineEdit skill in editor.Skills)
					skills.Add(skill.Text);
				Godot.Collections.Dictionary<string, Variant> actor = new()
				{
					{ "name", editor.ActorDropdown.GetItemText(editor.ActorDropdown.Selected) },
					{ "level", (int)editor.LevelSlider.Value },
					{ "weapon", editor.WeaponDropdown.GetItemText(editor.WeaponDropdown.Selected) },
					{ "charm", editor.CharmDropdown.GetItemText(editor.CharmDropdown.Selected) },
					{ "emotion", editor.EmotionDropdown.GetItemText(editor.EmotionDropdown.Selected) },
					{ "followupsDisabled", editor.DisableFollowups.ButtonPressed },
					{ "skills", skills },
					{ "position", positions[posIndex] }
				};
				actors.Add(actor);
				posIndex++;
			}
		}

		json.Add("actors", actors);

		foreach (Node child in EnemyTabs.GetChildren())
		{
			if (child is EnemyEditorComponent editor)
			{
				Godot.Collections.Dictionary<string, Variant> enemy = new()
				{
					{ "name", editor.EnemyDropdown.GetItemText(editor.EnemyDropdown.Selected) },
					{ "position", new Vector2((float)editor.XPosBox.Value, (float)editor.YPosBox.Value) },
					{ "emotion", editor.EmotionDropdown.GetItemText(editor.EmotionDropdown.Selected) },
					{ "fallsOffScreen", editor.FallsOffScreenCheckbox.ButtonPressed },
				};
				enemies.Add(enemy);
			}
		}

		json.Add("enemies", enemies);

		string result = Json.Stringify(json, "\t", false);
		if (!DirAccess.DirExistsAbsolute("user://presets")) {
			using DirAccess access = DirAccess.Open("user://");
			access.MakeDir("presets");
			GD.Print("Created user://presets directory");
		}

		using FileAccess file = FileAccess.Open("user://presets/" + PresetInput.Text + ".json", FileAccess.ModeFlags.Write);
		file.StoreString(result);

		ShowWindow("Success", "Saved preset to user://presets/" + PresetInput.Text + ".json");

		PresetInput.Text = "";
		ResetPresetDropdown();
	}

	private void Load()
	{
		if (PresetDropdown.Selected == -1)
			return;

		ResetToDefault();
		string presetName = PresetDropdown.GetItemText(PresetDropdown.Selected);
		string path = "user://presets/" + presetName + ".json";
		if (!FileAccess.FileExists(path))
		{
			ShowWindow("Error", "Preset file not found at: " + path);
			return;
		}
		
		using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		Variant json = Json.ParseString(file.GetAsText());

		if (json.VariantType == Variant.Type.Nil)
		{
			ShowWindow("Error", "Failed to parse preset " + presetName);
			return;
		}

		Godot.Collections.Dictionary<string, Variant> dict = json.AsGodotDictionary<string, Variant>();

		try
		{
			string name = dict["name"].ToString();
			string battleback = dict["battleback"].ToString();
			string bgm = dict["bgm"].ToString();
			int followupTier = (int)dict["followupTier"];
			bool basilFollowups = (bool)dict["basilFollowups"];
			bool basilReleaseEnergy = (bool)dict["basilReleaseEnergy"];
			Godot.Collections.Dictionary<string, int> items = dict["items"].AsGodotDictionary<string, int>();
			Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> actors = dict["actors"].AsGodotArray<Godot.Collections.Dictionary<string, Variant>>();
			Godot.Collections.Array<Godot.Collections.Dictionary<string, Variant>> enemies = dict["enemies"].AsGodotArray<Godot.Collections.Dictionary<string, Variant>>();

			// if nothing above throws a KeyNotFoundException, begin applying the preset
			BattlebackDropdown.Selected = BattlebackDropdown.GetItemIndex(battleback);
			BattlebackPreview.Texture = ResourceLoader.Load<Texture2D>("res://assets/battlebacks/" + battleback);

			BGMDropdown.Selected = BGMDropdown.GetItemIndex(bgm);

			FollowupTierSlider.Value = followupTier;
			BasilFollowupsCheckbox.ButtonPressed = basilFollowups;
			BasilReleaseEnergyCheckbox.ButtonPressed = basilReleaseEnergy;

			foreach (var entry in items)
			{
				HBoxContainer container = new();
				OptionButton dropdown = new();
				foreach (string item in Database.GetAllItemNames())
					dropdown.AddItem(item);
				dropdown.FitToLongestItem = false;
				container.AddChild(dropdown);
				SpinBox quantity = new()
				{
					MinValue = 1,
					MaxValue = 999,
					Value = entry.Value,
					Rounded = true
				};
				container.AddChild(quantity);
				Button remove = new();
				remove.Text = "X";
				remove.Pressed += () =>
				{
					container.QueueFree();
				};
				container.AddChild(remove);
				ItemContainer.AddChild(container);
				for (int i = 0; i < dropdown.ItemCount; i++)
				{
					if (dropdown.GetItemText(i) == entry.Key)
					{
						dropdown.Selected = i;
						break;
					}
				}
			}
			foreach (var entry in actors)
			{        
				try
				{
					string actorName = entry["name"].ToString();
					string weapon = entry["weapon"].ToString();
					string charm = entry["charm"].ToString();
					int level = entry["level"].AsInt32();
					bool followupsDisabled = entry["followupsDisabled"].AsBool();
					string emotion = entry["emotion"].ToString();
					string[] skills = entry["skills"].AsStringArray();
					int position = entry["position"].AsInt32();

					Control card = BattleCard.Instantiate<Control>();
					AddActorControls[position].AddChild(card);
					card.Position = Vector2.Zero;
					PartyMemberEditorComponent editor = PartyMemberEditor.Instantiate<PartyMemberEditorComponent>();
					ActorTabs.AddChild(editor);
					editor.Init(card, actorName, weapon, charm, level, followupsDisabled, emotion, skills);
				}
				catch (KeyNotFoundException ex)
				{
					ShowWindow("Error", "Failed to load! See the console/logs for more information.");
					GD.PrintErr("Failed to load actor entry due to missing field:\n" + ex);
				}
				catch (Exception e)
				{
					ShowWindow("Error", "Failed to load! See the console/logs for more information.");
					GD.PrintErr("Failed to load actor entry due to an unknown error:\n" + e);
				}
			}

			foreach (var entry in enemies)
			{
				try
				{
					string enemyName = entry["name"].ToString();
					// dumb hack to read the Vector2 since AsVector2() doesn't seem to work here
					string positionStr = entry["position"].ToString();
					string[] positionArr = positionStr.Substring(1, positionStr.Length - 2).Split(',');
					Vector2 position = new(float.Parse(positionArr[0]), float.Parse(positionArr[1]));
					string emotion = entry["emotion"].ToString();
					bool fallsOffScreen = entry["fallsOffScreen"].AsBool();
					AnimatedSprite2D enemySprite = new();
					AddEnemyControl.AddChild(enemySprite);
					EnemyEditorComponent editor = EnemyEditor.Instantiate<EnemyEditorComponent>();
					EnemyTabs.AddChild(editor);
					editor.Init(enemySprite, enemyName, position, emotion, fallsOffScreen);
				}
				catch (KeyNotFoundException ex)
				{
					ShowWindow("Error", "Failed to load! See the console/logs for more information.");
					GD.PrintErr("Failed to load enemy entry due to missing field:\n" + ex);
				}
				catch (Exception e)
				{
					ShowWindow("Error", "Failed to load! See the console/logs for more information.");
					GD.PrintErr("Failed to load enemy entry due to an unknown error:\n" + e);
				}
			}

		}
		catch (KeyNotFoundException ex)
		{
			ShowWindow("Error", "Failed to load! See the console/logs for more information.");
			GD.PrintErr("Preset load failed due to missing field:\n" + ex);
		}
		catch (Exception e)
		{
			ShowWindow("Error", "Failed to load! See the console/logs for more information.");
			GD.PrintErr("Preset laod failed due to an unknown error:\n" + e);
		}
	}

	private void ResetPresetDropdown()
	{
		PresetDropdown.Clear();
		TitlePresetDropdown.Clear();
		if (!DirAccess.DirExistsAbsolute("user://presets"))
		{
			using DirAccess access = DirAccess.Open("user://");
			access.MakeDir("presets");
			GD.Print("Created user://presets directory");
		}
		string[] presets = DirAccess.GetFilesAt("user://presets");
		foreach (string preset in presets)
		{
			if (preset.EndsWith(".json"))
			{
				string name = preset.Replace(".json", "");
				PresetDropdown.AddItem(name);
				TitlePresetDropdown.AddItem(name);
			}
		}
	}

	private void PreDelete()
	{
		if (PresetDropdown.Selected == -1)
			return;

		string preset = PresetDropdown.GetItemText(PresetDropdown.Selected);
		ConfirmationDialog dialog = new()
		{
			Title = "Confirmation",
			DialogText = "Are you sure you want to delete the " + preset + " preset?",
			Unresizable = true
		};
		dialog.Confirmed += () =>
		{
			Delete();
			dialog.QueueFree();
		};
		dialog.Canceled += dialog.QueueFree;
		AddChild(dialog);
		dialog.PopupCentered();
		dialog.Show();
	}

	private void Delete()
	{
		string preset = PresetDropdown.GetItemText(PresetDropdown.Selected);
		string path = "user://presets/" + preset + ".json";
		if (FileAccess.FileExists(path))
		{
			DirAccess access = DirAccess.Open("user://presets");
			Error err = access.Remove(preset + ".json");
			if (err == Error.Ok)
			{
				ShowWindow("Success", "Deleted preset " + preset);
				ResetPresetDropdown();
			}
			else
			{
				ShowWindow("Error", "Failed to delete preset " + preset + ". See console/logs for more information.");
				GD.PrintErr("Failed to delete preset " + preset + " due to error: " + err);
			}
		}
	}

	private void PreResetToDefault()
	{
		ConfirmationDialog dialog = new()
		{
			Title = "Confirmation",
			DialogText = "Are you sure you want to reset everything to default?\nIf you have not created a preset, this action cannot be undone.",
			Unresizable = true
		};
		dialog.Confirmed += () =>
		{
			ResetToDefault();
			dialog.QueueFree();
		};
		dialog.Canceled += dialog.QueueFree;
		AddChild(dialog);
		dialog.PopupCentered();
		dialog.Show();
	}

	private void ResetToDefault()
	{
		// remove battlecard previews
		foreach (Control control in AddActorControls)
		{
			if (control.GetChildCount() > 1)
				control.GetChild<Control>(1).QueueFree();
		}

		// remove party member tabs
		foreach (Node child in ActorTabs.GetChildren())
		{
			if (child is PartyMemberEditorComponent editor)
			{
				editor.QueueFree();
			}
		}

		// remove enemy tabs
		foreach (Node child in EnemyTabs.GetChildren())
		{
			if (child is EnemyEditorComponent editor)
			{
				editor.QueueFree();
			}
		}

		// remove enemy sprites
		foreach (Node child in AddEnemyControl.GetChildren())
		{
			if (child is AnimatedSprite2D sprite)
			{
				sprite.QueueFree();
			}
		}

		// remove items
		foreach (Node child in ItemContainer.GetChildren())
		{
			if (child is HBoxContainer container)
			{
				container.QueueFree();
			}
		}

		BattlebackDropdown.Selected = BattlebackDropdown.GetItemIndex("battleback_vf_default.png");
		BattlebackDropdown.EmitSignal("item_selected", BattlebackDropdown.Selected);
		BGMDropdown.Selected = 0;
		FollowupTierSlider.Value = 1;
		BasilFollowupsCheckbox.ButtonPressed = false;
		BasilReleaseEnergyCheckbox.ButtonPressed = false;

		if (PreviewButton.Text == "Stop")
		{
			AudioManager.Instance.StopBGM();
			PreviewButton.Text = "Preview";
		}
	}

	private void ShowWindow(string title, string message)
	{
		AcceptDialog dialog = new()
		{
			Title = title,
			DialogText = message,
			Unresizable = true
		};
		AddChild(dialog);
		dialog.Confirmed += dialog.QueueFree;
		dialog.Canceled += dialog.QueueFree;
		dialog.PopupCentered();
		dialog.Show();
	}

	public static MainMenuManager Instance;

	[Export]
	private Control[] AddActorControls;

	[Export]
	private Control AddEnemyControl;

	[Export]
	private PackedScene PartyMemberEditor;

	[Export]
	private PackedScene EnemyEditor;

	[Export]
	private PackedScene BattleCard;

	[Export]
	private TabContainer ActorTabs;

	[Export]
	private TabContainer EnemyTabs;

	[Export]
	private CanvasLayer MainMenu;

	[Export]
	private CanvasLayer Editor;

	[Export]
	private Button PlayButton;

	[Export]
	private Button ConfigureButton;

	[Export]
	private Button DataFolderButton;

	[Export]
	private Button CustomFolderButton;

	[Export]
	private Button GithubButton;

	[Export]
	private OptionButton TitlePresetDropdown;

	[Export]
	private TextureRect BattlebackPreview;

	[Export]
	private OptionButton BattlebackDropdown;

	[Export]
	private OptionButton BGMDropdown;

	[Export]
	private Button PreviewButton;

	[Export]
	private HSlider FollowupTierSlider;

	[Export]
	private Label FollowupTierValue;

	[Export]
	private CheckBox BasilFollowupsCheckbox;

	[Export]
	private CheckBox BasilReleaseEnergyCheckbox;

	[Export]
	private Button AddItemButton;

	[Export]
	private GridContainer ItemContainer;

	[Export]
	private LineEdit SearchInput;

	[Export]
	private Button SearchButton;

	[Export]
	private TextEdit Results;

	[Export]
	private Button SavePresetButton;

	[Export]
	private LineEdit PresetInput;

	[Export]
	private Button LoadPresetButton;

	[Export]
	private Button DeletePresetButton;

	[Export]
	private OptionButton PresetDropdown;

	[Export]
	private Button ResetButton;

	[Export]
	private Button ReturnButton;
}
