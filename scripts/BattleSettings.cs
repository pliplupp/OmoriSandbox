using Godot;
using OmoriSandbox;
using OmoriSandbox.Battle;
using System.Collections.Generic;

namespace OmoriSandbox;

internal partial class BattleSettings : Control
{
	public override void _Ready()
	{
		Battleback = GetNode<TextureRect>("/root/Main/BattleCanvas/UI/Battleback");

		int index = 0;
		string[] battlebacks = ResourceLoader.ListDirectory("res://assets/battlebacks");
		for (int i = 0; i < battlebacks.Length; i++)
		{
			if (battlebacks[i] == "battleback_vf_default.png")
				index = i;
			BattlebackDropdown.AddItem(battlebacks[i]);
		}

		foreach (string bgm in ResourceLoader.ListDirectory("res://audio/bgm"))
			BGMDropdown.AddItem(bgm);

		BattlebackDropdown.Selected = index;
		BattlebackDropdown.ItemSelected += (idx) =>
		{
			string battleback = BattlebackDropdown.GetItemText((int)idx);
			Battleback.Texture = ResourceLoader.Load<Texture2D>("res://assets/battlebacks/" + battleback);
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
	}

	private void Save()
	{
		if (string.IsNullOrWhiteSpace(PresetInput.Text))
			return;

		string battleback = BattlebackDropdown.GetItemText(BattlebackDropdown.Selected);
		string bgm = BGMDropdown.GetItemText(BGMDropdown.Selected);
		int followupTier = (int)FollowupTierSlider.Value;
		bool basilFollowups = BasilFollowupsCheckbox.ButtonPressed;
		bool basilReleaseEnergy = BasilReleaseEnergyCheckbox.ButtonPressed;
		List<(string item, int quantity)> items = [];
		List<BattlePresetActor> actors = [];
		List<BattlePresetEnemy> enemies = [];

		foreach (Node child in ItemContainer.GetChildren())
		{
			if (child is HBoxContainer container)
			{
				OptionButton dropdown = container.GetChild<OptionButton>(0);
				SpinBox quantity = container.GetChild<SpinBox>(1);
				items.Add((dropdown.GetItemText(dropdown.Selected), (int)quantity.Value));
			}
		}


	}

	[Export]
	public OptionButton BattlebackDropdown { get; private set; }

	[Export]
	public OptionButton BGMDropdown { get; private set; }

	[Export]
	public Button PreviewButton { get; private set; }

	[Export]
	public HSlider FollowupTierSlider { get; private set; }

	[Export]
	public Label FollowupTierValue { get; private set; }

	[Export]
	public CheckBox BasilFollowupsCheckbox { get; private set; }

	[Export]
	public CheckBox BasilReleaseEnergyCheckbox { get; private set; }

	[Export]
	private Button AddItemButton;

	[Export]
	public GridContainer ItemContainer { get; private set; }

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
	private OptionButton PresetDropdown;

	private TextureRect Battleback;
}
