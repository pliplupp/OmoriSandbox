using Godot;
using OmoriSandbox.Actors;
using OmoriSandbox.Battle;
using OmoriSandbox.Extensions;

namespace OmoriSandbox.Editor;

internal partial class EnemyEditorComponent : Control
{
	[Export]
	public OptionButton EnemyDropdown { get; private set; }

	[Export]
	public OptionButton EmotionDropdown { get; private set; }

	[Export]
	public SpinBox XPosBox { get; private set; }

	[Export]
	public SpinBox YPosBox { get; private set; }

	[Export]
	public SpinBox LayerBox { get; private set; }

	[Export]
	public CheckBox FallsOffScreenCheckbox { get; private set; }

	[Export]
	private CheckBox VisibleCheckbox;

	[Export]
	private Button RemoveButton;

	private AnimatedSprite2D Animator;

	private readonly string[] States = ["neutral", "happy", "sad", "angry", "ecstatic", "depressed", "furious", "manic", "miserable", "furious", "manic", "afraid", "stressed"];

	public override void _Ready()
	{
		foreach (string member in Database.GetAllEnemyNames())
			EnemyDropdown.AddItem(member);

		EnemyDropdown.Selected = EnemyDropdown.GetItemIndex("LostSproutMole");
		EnemyDropdown.ItemSelected += (idx) => Populate(EnemyDropdown.GetItemText((int)idx));
		EmotionDropdown.ItemSelected += (idx) => UpdateState(EmotionDropdown.GetItemText((int)idx));

		VisibleCheckbox.Toggled += (pressed) => Animator.Visible = pressed;

		XPosBox.ValueChanged += (value) => Animator.GlobalPosition = new Vector2((float)value, Animator.GlobalPosition.Y);
		YPosBox.ValueChanged += (value) => Animator.GlobalPosition = new Vector2(Animator.GlobalPosition.X, (float)value);

		LayerBox.ValueChanged += (value) => Animator.ZIndex = -5 - (int)LayerBox.Value;
	}

	public void Init(AnimatedSprite2D animator)
	{
		Animator = animator;
		Animator.Centered = true;
		Animator.ZIndex = -5;
		Animator.GlobalPosition = new Vector2((float)XPosBox.Value, (float)YPosBox.Value);

		RemoveButton.Pressed += () =>
		{
			Animator.QueueFree();
			QueueFree();
		};

		Populate("LostSproutMole");
	}

	public void Init(AnimatedSprite2D animator, string name, Vector2 position, string emotion, int layer, bool fallsOffScreen)
	{
		Animator = animator;
		Animator.Centered = true;
		Animator.ZIndex = -5 - layer;

		RemoveButton.Pressed += () =>
		{
			Animator.QueueFree();
			QueueFree();
		};

		Populate(name);
		EnemyDropdown.Selected = EnemyDropdown.GetItemIndex(name);
		EmotionDropdown.Selected = EmotionDropdown.GetItemIndex(emotion);
		LayerBox.Value = layer;
		XPosBox.SetValueNoSignal(position.X);
		YPosBox.SetValueNoSignal(position.Y);
		Animator.GlobalPosition = position;
		UpdateState(emotion);
		FallsOffScreenCheckbox.ButtonPressed = fallsOffScreen;
	}

	public void Populate(string who)
	{
		Name = who;
		Enemy enemy = Database.CreateEnemy(who);

		SpriteFrames animation = enemy.Animation;
		if (animation == null)
		{
			GD.PrintErr("Failed to load animations for Enemy: " + Name);
			return;
		}

		Animator.SpriteFrames = animation;
		Animator.Animation = "neutral";
		Animator.Play();

		FallsOffScreenCheckbox.ButtonPressed = enemy.FallsOffScreen;

		EmotionDropdown.Clear();
		foreach (string state in States)
		{
			if (enemy.IsStateValid(state))
			{
				EmotionDropdown.AddItem(state);
			}
		}
		EmotionDropdown.Selected = 0;
	}

	public void UpdateState(string state)
	{
		Animator.Animation = state;
	}
}
