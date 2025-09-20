using Godot;
using System.Collections.Generic;

public partial class MenuManager : Node
{
	[Export] public PartyMenu PartyMenu;
	[Export] public BattleMenu BattleMenu;
	[Export] public SkillMenu SkillMenu;
	[Export] public ItemMenu SnackMenu;
	[Export] public ItemMenu ToyMenu;
	[Export] public Sprite2D EnergyBar;
	[Export] public Label EnergyText;
	private Tween EnergyBarTween;

	public static MenuManager Instance { get; private set; }

	private const float FightRunOffsetRW = 458f;
	private const float FightRunOffset = 376f;
	private const float BattleOffsetRW = 212f;
	private const float BattleOffset = 130f;

	private MenuState CurrentState = MenuState.None;
	private Menu CurrentMenu;
	private Dictionary<MenuState, Menu> Menus;

	public override void _EnterTree()
	{
		Menus = new Dictionary<MenuState, Menu>
		{
			{ MenuState.Party, PartyMenu },
			{ MenuState.Battle, BattleMenu },
			{ MenuState.Skill, SkillMenu },
			{ MenuState.Snack, SnackMenu },
			{ MenuState.Toy, ToyMenu }
		};

		Instance = this;
	}

	public void ShowButtons(bool realWorld)
	{
		if (realWorld)
		{
			PartyMenu.RegionRect = new Rect2(653f, FightRunOffsetRW, 362f, 82f);
			BattleMenu.RegionRect = new Rect2(653f, BattleOffsetRW, 362f, 82f);
		}
		else
		{
			PartyMenu.RegionRect = new Rect2(653f, FightRunOffset, 362f, 82f);
			BattleMenu.RegionRect = new Rect2(653f, BattleOffset, 362f, 82f);
		}
	}

	public void ShowMenu(MenuState state, bool reset = true, bool immediate = false)
	{
		if (CurrentState != MenuState.None)
		{
			CurrentMenu.OnClose();
			CurrentMenu.MoveDown(immediate);
		}

		CurrentState = state;
		if (CurrentState == MenuState.None)
		{
			CurrentMenu = null;
			MoveEnergyBarDown(immediate);
			return;
		}

		CurrentMenu = Menus[CurrentState];

		if (CurrentMenu is SkillMenu skill)
		{
			skill.Populate(BattleManager.Instance.GetCurrentPartyMember());
		}
		else if (CurrentMenu is ItemMenu item)
		{
			item.Populate(CurrentState == MenuState.Toy);
		}

		CurrentMenu.OnOpen(reset);
		CurrentMenu.MoveUp(immediate);
		MoveEnergyBarUp(immediate);
	}

	public override void _Process(double delta)
	{
		if (CurrentState != MenuState.None)
		{
			if (Input.IsActionJustPressed("MenuUp"))
				CurrentMenu.OnInput(Vector2I.Up);
			else if (Input.IsActionJustPressed("MenuDown"))
				CurrentMenu.OnInput(Vector2I.Down);
			else if (Input.IsActionJustPressed("MenuLeft"))
				CurrentMenu.OnInput(Vector2I.Left);
			else if (Input.IsActionJustPressed("MenuRight"))
				CurrentMenu.OnInput(Vector2I.Right);
		}
	}

	public void Select()
	{
		if (CurrentState != MenuState.None)
		{
			CurrentMenu.OnInput(Vector2I.Zero);
		}
	}

	private void MoveEnergyBarDown(bool immediate)
	{
		if (immediate)
		{
			EnergyBar.Position = new Vector2(320f, 450f);
		}
		else
		{
			EnergyBarTween?.Kill();
            EnergyBarTween = CreateTween();
            EnergyBarTween.TweenProperty(EnergyBar, "position", new Vector2(320f, 450f), 0.2f).SetTrans(Tween.TransitionType.Sine);
		}
	}

	private void MoveEnergyBarUp(bool immediate)
	{
        if (immediate)
        {
            EnergyBar.Position = new Vector2(320f, 360f);
        }
        else
        {
            EnergyBarTween?.Kill();
            EnergyBarTween = CreateTween();
            EnergyBarTween.TweenProperty(EnergyBar, "position", new Vector2(320f, 360f), 0.2f).SetTrans(Tween.TransitionType.Sine);
        }
    }
}
