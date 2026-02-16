using Godot;
using OmoriSandbox.Actors;
using System;
using OmoriSandbox.Battle.Modifier;

namespace OmoriSandbox;

/// <summary>
/// The component attached to a party member <see cref="Node"/> in the scene.
/// </summary>
public partial class PartyMemberComponent : Node
{
	private PartyMember PartyMember;
	private StateAnimator StateAnimator;
	private TextureRect SelectedBox;
	private HFlowContainer StateIcons;
	private TextureProgressBar HPBar;
	private TextureProgressBar JuiceBar;
	private Label HPLabel;
	private Label JuiceLabel;
	
	private float DisplayedHP;
	private float DisplayedJuice;
	private float TargetHP;
	private float TargetJuice;

    /// <summary>
    /// The <see cref="Actors.PartyMember"/> actor this component is attached to.
    /// </summary>
    public PartyMember Actor => PartyMember;
	private FollowupBubbles FollowupBubbles;
    /// <summary>
    /// The position of the <see cref="Actors.PartyMember"/> in the party.<br/>
	/// See <see cref="BattleManager.GetPartyMember(int)"/> for valid positions.
    /// </summary>
    public int Position { get; private set; }
    /// <summary>
    /// Whether or not the <see cref="Actors.PartyMember"/> has a followup.
    /// </summary>
    public bool HasFollowup => FollowupBubbles != null;

    private Timer HurtTimer = new()
    {
	    Autostart = false,
	    OneShot = true
    };

    internal void SetPartyMember(PartyMember partyMember, PackedScene followup, int position, string initialState, int level, string weapon, string charm, string[] skills)
	{
		PartyMember = partyMember;
		AnimatedSprite2D face = GetNode<AnimatedSprite2D>("../Battlecard/Face");
		StateAnimator = GetNode<StateAnimator>("../Battlecard/StateAnimatorComponent");
		if (initialState == "hurt" || initialState == "victory")
			initialState = "neutral";
		PartyMember.Init(face, initialState, level, weapon, charm, skills);
		HPLabel = GetNode<Label>("../Battlecard/HealthLabel/");
		HPBar = GetNode<TextureProgressBar>("../Battlecard/Health");
		JuiceLabel = GetNode<Label>("../Battlecard/JuiceLabel");
		JuiceBar = GetNode<TextureProgressBar>("../Battlecard/Juice");
		SelectedBox = GetNode<TextureRect>("../SelectedCard");
		StateIcons = GetNode<HFlowContainer>("../StateIcons");
		if (position % 2 == 0)
		{
			StateIcons.Position = new Vector2(0, -65);
			StateIcons.ReverseFill = true;
		}

		HPBar.MaxValue = PartyMember.CurrentHP;
		HPBar.Value = PartyMember.CurrentHP;
		JuiceBar.MaxValue = PartyMember.CurrentJuice;
		JuiceBar.Value = PartyMember.CurrentJuice;
		DisplayedHP = PartyMember.CurrentHP;
		TargetHP = PartyMember.CurrentHP;
		DisplayedJuice = PartyMember.CurrentJuice;
		TargetJuice = PartyMember.CurrentJuice;

		if (followup != null)
		{
            FollowupBubbles bubbles = followup.Instantiate<FollowupBubbles>();
			GetParent().AddChild(bubbles);
			FollowupBubbles = bubbles;
		}

		Position = position;

		PartyMember.CenterPoint = GetParent<Control>().GlobalPosition + new Vector2(57, 79);
		PartyMember.OnStateChanged += StateChanged;
		PartyMember.OnHPChanged += HPChanged;
		PartyMember.OnJuiceChanged += JuiceChanged;
		PartyMember.OnDamaged += Damaged;
		HurtTimer.Timeout += () => PartyMember.SetHurt(false);
		AddChild(HurtTimer);
		
		PartyMember.Sprite.Animation = initialState;
		PartyMember.CurrentState = initialState;
		// delay this call to let everything initialize
		StateAnimator.CallDeferred(StateAnimator.MethodName.SetState, initialState);
	}

	private void StateChanged(object sender, EventArgs e)
	{
		StateAnimator.SetState(PartyMember.CurrentState);
	}

	private void HPChanged(object sender, EventArgs e)
	{
		TargetHP = PartyMember.CurrentHP;
	}

	private void JuiceChanged(object sender, EventArgs e)
	{
		TargetJuice = PartyMember.CurrentJuice;
	}

	private void Damaged(object sender, EventArgs e)
	{
		PartyMember.SetHurt(true);
		HurtTimer.Start(1d);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		
		DisplayedHP = Mathf.MoveToward(DisplayedHP, PartyMember.CurrentHP, dt * ((float)HPBar.MaxValue / 0.5f));
		DisplayedJuice = Mathf.MoveToward(DisplayedJuice, PartyMember.CurrentJuice, dt * ((float)JuiceBar.MaxValue / 0.5f));

		HPBar.Value = DisplayedHP;
		JuiceBar.Value = DisplayedJuice;

		HPLabel.Text = $"{Mathf.RoundToInt(DisplayedHP)}/{HPBar.MaxValue}";
		JuiceLabel.Text = $"{Mathf.RoundToInt(DisplayedJuice)}/{JuiceBar.MaxValue}";
	}

	internal void UpdateStateIcons()
	{
		// this may need to be optimized, not the best practice to fully replace nodes
		foreach (Node child in StateIcons.GetChildren())
			child.Free();
		
		foreach (StatModifier modifier in PartyMember.StatModifiers.Values)
		{
			StateIcon[] icons = modifier.GetStateIcons();
			foreach (StateIcon icon in icons)
			{
				TextureRect rect = new()
				{
					Texture = ResourceLoader.Load<Texture2D>($"res://assets/stateicons/{icon.AssetName}.png"),
					TooltipText = icon.Description
				};
				StateIcons.AddChild(rect);
			}
		}
	}

	internal bool SelectionBoxVisible
	{
		get { return SelectedBox.Visible; }
		set { SelectedBox.Visible = value; }
	}

    internal void FadeInFollowups()
	{
		FollowupBubbles.ShowBubbles();
	}

    internal void FadeOutFollowups()
	{
		FollowupBubbles.HideBubbles();
	}

}
