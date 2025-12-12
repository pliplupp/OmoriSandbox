using Godot;

namespace OmoriSandbox.Editor;

internal partial class SettingsMenuManager : Control
{
	public override void _Ready()
	{
		// TODO: save and load settings
		SetBusVolume("Master", 0.75f);
		SetBusVolume("SFX", 1f);
		SetBusVolume("BGM", 0.5f);
		
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

	[Export] private HSlider MasterSlider;
	[Export] private HSlider BGMSlider;
	[Export] private HSlider SFXSlider;
	[Export] private Button TestSFXButton;
	[Export] private CheckBox DisableDamageLimitCheckbox;
	[Export] private Button BackButton;
	[Export] private VBoxContainer MainControls;
}
