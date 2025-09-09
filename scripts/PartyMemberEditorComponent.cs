using Godot;
using System.Linq;

public partial class PartyMemberEditorComponent : Control
{
	[Export]
	public OptionButton ActorDropdown { get; private set; }

	[Export]
	public OptionButton WeaponDropdown { get; private set; }

	[Export]
	public OptionButton CharmDropdown { get; private set; }

	[Export]
	public OptionButton EmotionDropdown { get; private set; }

	[Export]
	public HSlider LevelSlider { get; private set; }

	[Export]
	private Label LevelSliderValue;

	[Export]
	public CheckBox DisableFollowups { get; private set; }

	[Export]
	public LineEdit AttackSkill { get; private set; }

	[Export]
	public LineEdit[] Skills;

	[Export]
	private Button RemoveButton;

	private Control BattleCard;
	private AnimatedSprite2D Face;
	private StateAnimator Animator;

	public int ActorPosition { get; private set; }

	private readonly string[] States = ["neutral", "happy", "sad", "angry", "ecstatic", "depressed", "furious", "manic", "miserable", "furious", "manic", "afraid", "stressed"];

	public override void _Ready()
	{
		LevelSlider.ValueChanged += (value) => LevelSliderValue.Text = value.ToString();
		foreach (string member in Database.GetAllPartyMemberNames())
			ActorDropdown.AddItem(member);

		ActorDropdown.ItemSelected += (idx) => Populate(ActorDropdown.GetItemText((int)idx));
		EmotionDropdown.ItemSelected += (idx) => UpdateState(EmotionDropdown.GetItemText((int)idx));

		foreach (string weapon in Database.GetAllWeaponNames())
			WeaponDropdown.AddItem(weapon);

		CharmDropdown.AddItem("None");
		foreach (string charm in Database.GetAllCharmNames())
			CharmDropdown.AddItem(charm);
	}

	public void Init(Control battleCard, int position)
	{
		BattleCard = battleCard;
		Animator = BattleCard.GetNode<StateAnimator>("Battlecard/StateAnimatorComponent");
		Face = BattleCard.GetNode<AnimatedSprite2D>("Battlecard/Face");

		ActorPosition = position;

		RemoveButton.Pressed += () =>
		{
			BattleCard.QueueFree();
			QueueFree();
		};

		// default to Omori
		ActorDropdown.Selected = 0;
		WeaponDropdown.Selected = 0;
		// charms are optional so we can leave it unselected
		Populate("Omori");
	}
	public void Init(Control battleCard, string name, string weapon, string charm, int level, bool followupsDisabled, string emotion, string[] skills, int position)
	{
		BattleCard = battleCard;
		Animator = BattleCard.GetNode<StateAnimator>("Battlecard/StateAnimatorComponent");
		Face = BattleCard.GetNode<AnimatedSprite2D>("Battlecard/Face");

		ActorPosition = position;

		RemoveButton.Pressed += () =>
		{
			BattleCard.QueueFree();
			QueueFree();
		};

		Name = name;
		ActorDropdown.Selected = ActorDropdown.GetItemIndex(name);
		Populate(name);
		WeaponDropdown.Selected = WeaponDropdown.GetItemIndex(weapon);
		CharmDropdown.Selected = CharmDropdown.GetItemIndex(charm);
		EmotionDropdown.Selected = EmotionDropdown.GetItemIndex(emotion);
		DisableFollowups.ButtonPressed = followupsDisabled;
		LevelSlider.Value = level;
		UpdateState(emotion);
		if (skills.Length > 0)
		{
			// first index should always be the attack skill
			AttackSkill.Text = skills[0];
			for (int i = 0; i < Skills.Length; i++)
			{
				Skills[i].Text = skills[i + 1];
			}
		}
	}

	public void Populate(string who)
	{
		Name = who;
		PartyMember member = Database.CreatePartyMember(who);

		string attackSkill;
		if (member.IsRealWorld)
			attackSkill = member.Name[0] + "RWAttack";
		else
			attackSkill = member.Name[0] + "Attack";

		if (Database.TryGetSkill(attackSkill, out _))
			AttackSkill.Text = attackSkill;

		SpriteFrames animation = ResourceLoader.Load<SpriteFrames>(member.AnimationPath);
		if (animation == null)
		{
			GD.PrintErr("Failed to load Face animations for PartyMember: " + Name);
			return;
		}

		Face.SpriteFrames = animation;
		Face.Animation = "neutral";
		Face.Play();
		Animator.SetState("neutral");

		LevelSlider.Value = 1;
		LevelSlider.MinValue = 1;
		LevelSlider.MaxValue = member.HPTree.Length;

		EmotionDropdown.Clear();
		foreach (string state in States.Except(member.InvalidStates))
			EmotionDropdown.AddItem(state);
		EmotionDropdown.Selected = 0;
	}

	public void UpdateState(string state)
	{
		Face.Animation = state;
		Animator.SetState(state);
	}
}
