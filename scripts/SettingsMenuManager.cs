using System;
using Godot;

namespace OmoriSandbox.Editor;

internal partial class SettingsMenuManager : Control
{
	public override void _Ready()
	{
		ConfigFile config = new();
		if (config.Load("user://settings.cfg") != Error.Ok)
		{
			GD.PushWarning("Generating new settings file...");
			GenerateDefaultConfig(ref config);
		}

		FullscreenCheckbox.Toggled += value =>
		{
			DisplayServer.WindowSetMode(value ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
		};
		
		MasterSlider.ValueChanged += value =>
		{
			SetBusVolume("Master", (float)value);
		};
		
		BGMSlider.ValueChanged += value =>
		{
			SetBusVolume("BGM", (float)value);
		};

		SFXSlider.ValueChanged += value =>
		{
			SetBusVolume("SFX",  (float)value);
		};

		TestSFXButton.Pressed += () =>
		{
			AudioManager.Instance.PlaySFX("BA_basic_attack_omori");
		};

		ResetKeybindsButton.Pressed += () =>
		{
			foreach (Node node in KeybindGrid.GetChildren())
			{
				if (node is KeybindButton keybind)
				{
					keybind.Reset();
					config.SetValue("Keybinds", keybind.AssociatedAction, OS.GetKeycodeString(keybind.DefaultKey));
				}
			}
		};

		BackButton.Pressed += () =>
		{
			MainControls.Visible = true;
			Visible = false;
		};
		
		// calling these after subscribing to the above events
		FullscreenCheckbox.ButtonPressed = (bool)config.GetValue("Settings", "Fullscreen", false);
		MasterSlider.Value = (float)config.GetValue("Settings", "MasterVolume", 0.75f);
		SFXSlider.Value = (float)config.GetValue("Settings", "SFXVolume", 1f);
		BGMSlider.Value = (float)config.GetValue("Settings", "BGMVolume", 0.5f);
		DisableDamageLimitCheckbox.ButtonPressed = (bool)config.GetValue("Settings", "DisableDamageLimit", false);
		ShowMoreInfoCheckbox.ButtonPressed = (bool)config.GetValue("Settings", "ShowMoreInfo", false);
		ShowStateIconsCheckbox.ButtonPressed = (bool)config.GetValue("Settings", "ShowStateIcons", false);
		UseConsoleSpdCheckbox.ButtonPressed = (bool)config.GetValue("Settings", "UseConsoleSpd", false);
		UseConsoleDefCheckbox.ButtonPressed = (bool)config.GetValue("Settings", "UseConsoleDef", false);
		InfiniteBuffsDebuffsCheckbox.ButtonPressed = (bool)config.GetValue("Settings", "InfiniteBuffsDebuffs", false);
		VertigoUsesAtkCheckbox.ButtonPressed =  (bool)config.GetValue("Settings", "VertigoUsesAtk", false);
		ToysUseEmotionDamageCheckbox.ButtonPressed = (bool)config.GetValue("Settings", "ToysUseEmotionDamage", false);
		SpaceExHusbandReleaseEnergyCheckbox.ButtonPressed = (bool)config.GetValue("Settings", "SpaceExHusbandReleaseEnergy", false);
		BattlelogSpeedSlider.Value = (int)config.GetValue("Settings", "BattlelogSpeed", 3);
		ActionDelaySlider.Value = (int)config.GetValue("Settings", "ActionDelay", 3);
		
		foreach (Node node in KeybindGrid.GetChildren())
		{
			if (node is KeybindButton keybind)
			{
				Key key = OS.FindKeycodeFromString((string)config.GetValue("Keybinds", keybind.AssociatedAction,
					"unknown"));
				if (key is Key.Unknown)
					keybind.Reset();
				else
					keybind.SetKey(key);
			}
		}

		Instance = this;
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("ToggleFullscreen"))
		{
			FullscreenCheckbox.ButtonPressed = !FullscreenCheckbox.ButtonPressed;
		}
	}

	public override void _ExitTree()
	{
		ConfigFile config = new();
		if (config.Load("user://settings.cfg") != Error.Ok)
		{
			GD.PrintErr("Generating new settings file...");
			GenerateDefaultConfig(ref config);
			return;
		}
		config.SetValue("Settings", "Fullscreen", FullscreenCheckbox.ButtonPressed);
		config.SetValue("Settings", "MasterVolume", AudioServer.GetBusVolumeLinear(AudioServer.GetBusIndex("Master")));
		config.SetValue("Settings", "BGMVolume", AudioServer.GetBusVolumeLinear(AudioServer.GetBusIndex("BGM")));
		config.SetValue("Settings", "SFXVolume", AudioServer.GetBusVolumeLinear(AudioServer.GetBusIndex("SFX")));
		config.SetValue("Settings", "BattlelogSpeed", (int)BattlelogSpeedSlider.Value);
		config.SetValue("Settings", "ActionDelay", (int)ActionDelaySlider.Value);
		config.SetValue("Settings",  "DisableDamageLimit", DisableDamageLimitCheckbox.ButtonPressed);
		config.SetValue("Settings", "ShowMoreInfo", ShowMoreInfoCheckbox.ButtonPressed);
		config.SetValue("Settings", "ShowStateIcons", ShowStateIconsCheckbox.ButtonPressed);
		config.SetValue("Settings","UseConsoleSpd", UseConsoleSpdCheckbox.ButtonPressed);
		config.SetValue("Settings","UseConsoleDef", UseConsoleDefCheckbox.ButtonPressed);
		config.SetValue("Settings", "InfiniteBuffsDebuffs", InfiniteBuffsDebuffsCheckbox.ButtonPressed);
		config.SetValue("Settings", "VertigoUsesAtk", VertigoUsesAtkCheckbox.ButtonPressed);
		config.SetValue("Settings", "ToysUseEmotionDamage", ToysUseEmotionDamageCheckbox.ButtonPressed);
		config.SetValue("Settings", "SpaceExHusbandReleaseEnergy", SpaceExHusbandReleaseEnergyCheckbox.ButtonPressed);

		foreach (Node node in KeybindGrid.GetChildren())
		{
			if (node is KeybindButton keybind)
				config.SetValue("Keybinds", keybind.AssociatedAction, OS.GetKeycodeString(keybind.CurrentKey));
		}
		
		config.Save("user://settings.cfg");
	}

	public Key GetKeybindForAction(string action)
	{
		foreach (Node node in KeybindGrid.GetChildren())
		{
			if (node is KeybindButton keybind && keybind.AssociatedAction == action)
				return keybind.CurrentKey;
		}
		return Key.Unknown;
	}

	private void GenerateDefaultConfig(ref ConfigFile config)
	{
		config.SetValue("Settings", "Fullscreen", false);
		config.SetValue("Settings", "MasterVolume", 0.75f);
		config.SetValue("Settings", "BGMVolume", 1f);
		config.SetValue("Settings", "SFXVolume", 0.5f);
		config.SetValue("Settings", "BattlelogSpeed", 3);
		config.SetValue("Settings", "ActionDelay", 3);
		config.SetValue("Settings", "DisableDamageLimit", false);
		config.SetValue("Settings", "ShowMoreInfo", false);
		config.SetValue("Settings", "ShowStateIcons", false);
		config.SetValue("Settings","UseConsoleSpd", false);
		config.SetValue("Settings","UseConsoleDef", false);
		config.SetValue("Settings", "InfiniteBuffsDebuffs", false);
		config.SetValue("Settings", "VertigoUsesAtk", false);
		config.SetValue("Settings", "ToysUseEmotionDamage", false);
		config.SetValue("Settings", "SpaceExHusbandReleaseEnergy", false);
		foreach (Node node in KeybindGrid.GetChildren())
		{
			if (node is KeybindButton keybind)
				config.SetValue("Keybinds", keybind.AssociatedAction, OS.GetKeycodeString(keybind.DefaultKey));
		}
		config.Save("user://settings.cfg");
	}

	private void SetBusVolume(string bus, float volume)
	{
		int index = AudioServer.GetBusIndex(bus);
		if (index == -1)
		{
			GD.PrintErr("Unknown audio bus: " + bus);
			return;
		}
		AudioServer.SetBusVolumeLinear(index, volume);
	}

	public static SettingsMenuManager Instance;
	public bool DisableDamageLimit => DisableDamageLimitCheckbox.ButtonPressed;
	public bool ShowMoreInfo => ShowMoreInfoCheckbox.ButtonPressed;
	public bool ShowStateIcons => ShowStateIconsCheckbox.ButtonPressed;
	public bool UseConsoleSpeed => UseConsoleSpdCheckbox.ButtonPressed;
	public bool UseConsoleDefense => UseConsoleDefCheckbox.ButtonPressed;
	public bool InfiniteBuffsDebuffs => InfiniteBuffsDebuffsCheckbox.ButtonPressed;
	public bool VertigoUsesAtk => VertigoUsesAtkCheckbox.ButtonPressed;
	public bool ToysUseEmotionDamage =>  ToysUseEmotionDamageCheckbox.ButtonPressed;
	public bool SpaceExHusbandReleaseEnergy =>  SpaceExHusbandReleaseEnergyCheckbox.ButtonPressed;
	public int BattlelogSpeed => (int)BattlelogSpeedSlider.Value;
	public int ActionDelay => (int)ActionDelaySlider.Value;

	[Export] private HSlider MasterSlider;
	[Export] private HSlider BGMSlider;
	[Export] private HSlider SFXSlider;
	[Export] private Button TestSFXButton;
	[Export] private CheckBox FullscreenCheckbox;
	[Export] private HSlider BattlelogSpeedSlider;
	[Export] private HSlider ActionDelaySlider;
	[Export] private CheckBox DisableDamageLimitCheckbox;
	[Export] private CheckBox ShowMoreInfoCheckbox;
	[Export] private CheckBox ShowStateIconsCheckbox;
	[Export] private CheckBox UseConsoleSpdCheckbox;
	[Export] private CheckBox UseConsoleDefCheckbox;
	[Export] private CheckBox InfiniteBuffsDebuffsCheckbox;
	[Export] private CheckBox VertigoUsesAtkCheckbox;
	[Export] private CheckBox ToysUseEmotionDamageCheckbox;
	[Export] private CheckBox SpaceExHusbandReleaseEnergyCheckbox;
	[Export] private GridContainer KeybindGrid;
	[Export] private Button ResetKeybindsButton;
	[Export] private Button BackButton;
	[Export] private VBoxContainer MainControls;
}
