using Godot;
using OmoriSandbox.Animation;

namespace OmoriSandbox.Editor;

internal partial class SettingsMenuManager : Control
{
	// TODO: extra settings: Swap T3 speed buff pc/console values, vertigo uses attack in JP/KR, CJK toys affected by emotion damage
	
	public override void _Ready()
	{
		ConfigFile config = new();
		if (config.Load("user://settings.cfg") != Error.Ok)
		{
			GD.PushWarning("Generating new settings file...");
			GenerateDefaultConfig(ref config);
		}
		
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
			AudioManager.Instance.PlaySFX("BA_basic_attack_omori", volume: 0.9f);
		};

		BackButton.Pressed += () =>
		{
			MainControls.Visible = true;
			Visible = false;
		};
		
		// calling these after subscribing to the above events
		MasterSlider.Value = (float)config.GetValue("Settings", "MasterVolume", 0.75f);
		SFXSlider.Value = (float)config.GetValue("Settings", "SFXVolume", 1f);
		BGMSlider.Value = (float)config.GetValue("Settings", "BGMVolume", 0.5f);
		DisableDamageLimitCheckbox.ButtonPressed = (bool)config.GetValue("Settings", "DisableDamageLimit", false);

		Instance = this;
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
		config.SetValue("Settings", "MasterVolume", AudioServer.GetBusVolumeLinear(AudioServer.GetBusIndex("Master")));
		config.SetValue("Settings", "BGMVolume", AudioServer.GetBusVolumeLinear(AudioServer.GetBusIndex("BGM")));
		config.SetValue("Settings", "SFXVolume", AudioServer.GetBusVolumeLinear(AudioServer.GetBusIndex("SFX")));
		config.SetValue("Settings",  "DisableDamageLimit", DisableDamageLimitCheckbox.ButtonPressed);
		config.Save("user://settings.cfg");
	}

	private void GenerateDefaultConfig(ref ConfigFile config)
	{
		config.SetValue("Settings", "MasterVolume", 0.75f);
		config.SetValue("Settings", "BGMVolume", 1f);
		config.SetValue("Settings", "SFXVolume", 0.5f);
		config.SetValue("Settings", "DisableDamageLimit", false);
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

	[Export] private HSlider MasterSlider;
	[Export] private HSlider BGMSlider;
	[Export] private HSlider SFXSlider;
	[Export] private Button TestSFXButton;
	[Export] private CheckBox DisableDamageLimitCheckbox;
	[Export] private Button BackButton;
	[Export] private VBoxContainer MainControls;
}
