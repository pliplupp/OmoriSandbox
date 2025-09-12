using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class BattleManager : Node
{
	[Export] public Label EnergyText;
	[Export] private Sprite2D EnergyBar;
	[Export] private EnergyDots EnergyDots;
	[Export] private HBoxContainer MenuButtonContainer;

	private List<PartyMemberComponent> CurrentParty = [];
	private List<EnemyComponent> Enemies = [];

	private BattlePhase Phase = BattlePhase.FightRun;
	private int CurrentPartyMember = -1;
	private int CurrentEnemyTarget = -1;
	private int CurrentPartyMemberTarget = -1;
	private List<BattleCommand> Commands = [];
	private int CommandIndex = -1;
	public BattleCommand LastSelectedCommand { get; private set; } = null;
	private Timer Delay;
	private List<Node2D> DyingEnemies = [];
	private Godot.Collections.Dictionary<string, int> Items = [];
	private BattleAction SelectedAction;
	private HashSet<Vector2> DamageNumbers = [];
	public int Energy { get; private set; } = 0;
	private bool FollowupActive = false;
	private bool FollowupSelected = false;
	private bool ForceHideFollowup = false;
	private int FollowupTier = 1;
	private bool UseBasilReleaseEnergy = false;
	private bool UseBasilFollowups = false;

	private bool IsBattling = false;

	public static BattleManager Instance { get; private set; }

	// table used for handling selecting a party member target
	// has a preferred and fallback target if the preferred party member does not exist
	private Dictionary<(int Position, InputDirection Direction), (int Preferred, int Fallback)> DirectionTable = new()
	{
		{ (0, InputDirection.Up),    (1, 3) },
		{ (0, InputDirection.Right), (2, 3) },
		{ (1, InputDirection.Down),  (0, 2) },
		{ (1, InputDirection.Right), (3, 2) },
		{ (2, InputDirection.Left),  (0, 1) },
		{ (2, InputDirection.Up),    (3, 1) },
		{ (3, InputDirection.Left),  (1, 0) },
		{ (3, InputDirection.Down),  (2, 0) },
	};

	// table used for handling followup behavior
	// has a followup target and followup skill to use
	private Dictionary<(int Position, InputDirection Direction), (int Target, string SkillName)> FollowupTable = new()
	{
		// the skill names here will get their tier number added later
		// Omori
		{ (0, InputDirection.Up), (0, "AttackAgain") },
		{ (0, InputDirection.Right), (0, "Trip") },
		{ (0, InputDirection.Down), (0, "ReleaseEnergy") },

		// Aubrey
		{ (1, InputDirection.Up), (3, "LookAtHero") },
		{ (1, InputDirection.Right), (2, "LookAtKel") },
		{ (1, InputDirection.Down), (0, "LookAtOmori") },

		// Hero
		{ (3, InputDirection.Up), (1, "CallAubrey") },
		{ (3, InputDirection.Left), (0, "CallOmori") },
		{ (3, InputDirection.Down), (2, "CallKel") },

		// Kel and Basil are added dynamically in Init()
	};

	public override void _EnterTree()
	{
		MenuButtonContainer.GetChild<Button>(0).Pressed += () =>
		{
			Reset();
			string presetName = MainMenuManager.Instance.LastLoadedPreset;
			string path = "user://presets/" + MainMenuManager.Instance.LastLoadedPreset + ".json";
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
		};

		MenuButtonContainer.GetChild<Button>(1).Pressed += () =>
		{
			Reset();
			MainMenuManager.Instance.ReturnToTitle();
		};
		Instance = this;
	}

	public void Init(List<PartyMemberComponent> party, List<EnemyComponent> enemies, Godot.Collections.Dictionary<string, int> items, int followupTier, bool useBasilFollowups, bool useBasilReleaseEnergy)
	{
		CurrentParty = party.OrderBy(x => x.Position).ToList();
		Enemies = enemies;
		Items = items;
		Energy = 3;
		FollowupTier = followupTier;
		UseBasilFollowups = useBasilFollowups;
		UseBasilReleaseEnergy = useBasilReleaseEnergy;
		MenuButtonContainer.Visible = false;

		EnergyBar.Visible = CurrentParty.Any(x => x.HasFollowup);

		BattleLogManager.Instance.Visible = true;

		Delay = new Timer
		{
			OneShot = true,
			Autostart = false,
		};
		AddChild(Delay);
		Delay.Timeout += OnDelayTimeout;
		BattleLogManager.Instance.FinishedLogging += OnBattleLogFinished;

		// update the FollowupTable depending on if Basil followups are enabled
		if (UseBasilFollowups)
		{
			FollowupTable[(2, InputDirection.Down)] = (0, "Comfort");
			FollowupTable[(2, InputDirection.Left)] = (1, "Mull");
			FollowupTable[(2, InputDirection.Up)] = (3, "Vent");
		}
		else
		{
			FollowupTable[(2, InputDirection.Down)] = (0, "PassToOmori");
			FollowupTable[(2, InputDirection.Left)] = (1, "PassToAubrey");
			FollowupTable[(2, InputDirection.Up)] = (3, "PassToHero");
		}

		DamageNumber.CacheTexture(ResourceLoader.Load<Texture2D>("res://assets/system/Damage.png"));

		CallDeferred(MethodName.PreBattle);

		SetPhase(BattlePhase.FightRun);

		IsBattling = true;
	}

	private void PreBattle()
	{
		if (CurrentParty.Any(x => x.Actor.Weapon.Name == "Hero's Trophy"))
		{
			foreach (PartyMemberComponent party in CurrentParty)
			{
				if (party.Actor is Hero)
				{
					party.Actor.SetState("sad", true);
				}
			}
		}

		foreach (PartyMemberComponent party in CurrentParty)
		{
			if (party.Actor.Weapon.Name == "LOL Sword")
			{
				party.Actor.SetState("happy", true);
			}
			party.Actor.Charm?.StartOfBattle(party.Actor);
		}

		// TODO: add more events for different parts of the battle
		for (int i = 0; i < Enemies.Count; i++)
		{
			if (Enemies[i].Actor is MrJawsum jawsum)
			{
				// jawsum always starts with 2 gator guys
				// this may be configurable in the future
				jawsum.AddStatModifier("MrJawsumBarrier");
				jawsum.GatorGuys.Add(SummonEnemy("GatorGuyJawsum", new Vector2(jawsum.CenterPoint.X - 145, jawsum.CenterPoint.Y + 65)));
				jawsum.GatorGuys.Add(SummonEnemy("GatorGuyJawsum", new Vector2(jawsum.CenterPoint.X + 145, jawsum.CenterPoint.Y + 65)));
			}
		}
	}

	public override void _Process(double delta)
	{
		if (!IsBattling)
			return;

		EnergyDots.Tick(delta);

		CurrentParty.ForEach(x =>
		{
			if (Phase == BattlePhase.TargetSelection)
				x.SelectionBoxVisible = x.Position == CurrentPartyMemberTarget;
			else if (Phase == BattlePhase.PlayerCommand)
				x.SelectionBoxVisible = x.Position == CurrentPartyMember;
			else
				x.SelectionBoxVisible = false;
		});

		for (int i = 0; i < Enemies.Count; i++)
		{
			Enemies[i].ShowInfoBox(i == CurrentEnemyTarget);
		}

		MenuManager.Instance.EnergyText.Text = $"{Energy:00}";
		MenuManager.Instance.EnergyBar.RegionRect = new Rect2(0, (float)Math.Ceiling(Energy / 3f) * 45f, 362f, 49f);

		if (Input.IsActionJustPressed("Accept"))
		{
			if (Phase == BattlePhase.TargetSelection)
			{
				SelectTarget(); 
			}
			else
			{
				MenuManager.Instance.Select();
			}
		}

		// handle Back here instead of MenuManager to have more control and easier variable access
		if (Input.IsActionJustPressed("Back"))
		{
			switch (Phase)
			{
				case BattlePhase.PlayerCommand:
					if (CurrentPartyMember == 0)
					{
						AudioManager.Instance.PlaySFX("sys_cancel");
						MenuManager.Instance.ShowMenu(MenuState.Party);
						SetPhase(BattlePhase.FightRun);
					}
					else
					{
						BattleAction action = Commands[^1].Action;
						if (action is Item item)
						{
							// Capitalize the item name for dictionary lookup
							string name = item.Name.Capitalize();
							if (!Items.ContainsKey(name))
								Items.Add(name, 1);
							else
								Items[name]++;
						}
						SkillTarget target = action.Target;
						bool rememberCursor = Commands[^1].Target != null || target == SkillTarget.AllAllies || target == SkillTarget.AllEnemies || target == SkillTarget.AllDeadAllies;
						if (rememberCursor)
						{
							LastSelectedCommand = Commands[^1];
						}
						Commands.RemoveAt(Commands.Count - 1);
						CurrentPartyMember--;
						AudioManager.Instance.PlaySFX("sys_cancel");
						SetPhase(BattlePhase.PlayerCommand);
					}
					break;
				case BattlePhase.TargetSelection:
					AudioManager.Instance.PlaySFX("sys_cancel");
					CurrentEnemyTarget = -1;
					CurrentPartyMemberTarget = -1;
					SetPhase(BattlePhase.PlayerCommand);
					break;
				case BattlePhase.SkillSelection:
					AudioManager.Instance.PlaySFX("sys_cancel");
					SetPhase(BattlePhase.PlayerCommand);
					break;
			}
		}

		if (Input.IsActionJustPressed("MenuLeft"))
		{
			HandleInputDirection(InputDirection.Left);
		}

		if (Input.IsActionJustPressed("MenuRight"))
		{
			HandleInputDirection(InputDirection.Right);
		}

		if (Input.IsActionJustPressed("MenuUp"))
		{
			HandleInputDirection(InputDirection.Up);
		}

		if (Input.IsActionJustPressed("MenuDown"))
		{
			HandleInputDirection(InputDirection.Down);
		}

		if (Input.IsActionJustPressed("SwitchSides"))
		{
			if (Phase == BattlePhase.TargetSelection && SelectedAction.Target == SkillTarget.AllyOrEnemy)
			{
				if (CurrentPartyMemberTarget > -1)
				{
					CurrentPartyMemberTarget = -1;
					CurrentEnemyTarget = 0;
				}
				else
				{
					CurrentPartyMemberTarget = CurrentParty.First(x => x != null).Position;
					CurrentEnemyTarget = -1;
				}
			}
		}
	}

	private void HandleInputDirection(InputDirection direction)
	{
		if (Phase == BattlePhase.CommandExecute || Phase == BattlePhase.WaitForBattleLog)
		{
			if (HandleFollowup(direction))
			{
				ProcessFollowupSuccess();
			}
		}
		if (Phase == BattlePhase.TargetSelection)
		{
			if (SelectedAction.Target == SkillTarget.Enemy || (SelectedAction.Target == SkillTarget.AllyOrEnemy && CurrentEnemyTarget > -1) && Enemies.Count > 1)
			{
				if (direction == InputDirection.Right)
				{
					CurrentEnemyTarget++;
					if (CurrentEnemyTarget >= Enemies.Count)
						CurrentEnemyTarget = 0;
				}
				else if (direction == InputDirection.Left)
				{
					CurrentEnemyTarget--;
					if (CurrentEnemyTarget < 0)
						CurrentEnemyTarget = Enemies.Count - 1;
				}
				return;
			}
			if (SelectedAction.Target == SkillTarget.Ally || SelectedAction.Target == SkillTarget.AllyNotSelf || SelectedAction.Target == SkillTarget.DeadAlly || (SelectedAction.Target == SkillTarget.AllyOrEnemy && CurrentPartyMemberTarget > -1))
			{
				int target = SelectPartyMember(CurrentPartyMemberTarget, direction);
				GD.Print(target);
				if (target > -1)
				{
					AudioManager.Instance.PlaySFX("SYS_move");
					CurrentPartyMemberTarget = target;
				}
			}
		}
	}

	private int SelectPartyMember(int current, InputDirection direction)
	{
		if (!DirectionTable.TryGetValue((current, direction), out var pair))
			return -1;
		if (CurrentParty.Any(x => x.Position == pair.Preferred))
			return pair.Preferred;
		if (CurrentParty.Any(x => x.Position == pair.Fallback))
			return pair.Fallback;
		return -1;
	}

	public void OnFightSelected()
	{
		CurrentPartyMember++;
		SetPhase(BattlePhase.PlayerCommand);
	}

	public void Reset()
	{
		GameManager.Instance.DespawnAll();
		CurrentParty.Clear();
		Enemies.Clear();
		Items.Clear();
		MenuManager.Instance.ShowMenu(MenuState.None);
		EnergyBar.Visible = false;
		BattleLogManager.Instance.ClearBattleLog();
		BattleLogManager.Instance.Visible = false;
		Delay.Timeout -= OnDelayTimeout;
		Delay.QueueFree();
		BattleLogManager.Instance.FinishedLogging -= OnBattleLogFinished;
		Phase = BattlePhase.FightRun;
		IsBattling = false;
	}

	public void OnSelectAttack()
	{
		SelectedAction = CurrentParty[CurrentPartyMember].Actor.Skills.Values.First();
		SetPhase(BattlePhase.TargetSelection);
	}

	// idfk
	public void OnSelectNotAttack()
	{
		SetPhase(BattlePhase.SkillSelection);
	}

	public void OnSelectSkill(Skill skill)
	{
		SelectedAction = skill;
		// TODO: handle emotion locked skills better
		if (CurrentParty[CurrentPartyMember].Actor.CurrentState == "afraid" && !(skill.Name == "GUARD" || skill.Name == "CALM DOWN"))
		{
			AudioManager.Instance.PlaySFX("sys_buzzer");
			return;
		}
		if (CurrentParty[CurrentPartyMember].Actor.CurrentState == "stressed" && skill.Name != "GUARD")
		{
			AudioManager.Instance.PlaySFX("sys_buzzer");
			return;
		}
		if (CurrentParty[CurrentPartyMember].Actor.CurrentJuice - skill.Cost < 0)
		{
			AudioManager.Instance.PlaySFX("sys_buzzer");
			return;
		}
		if ((SelectedAction.Target == SkillTarget.DeadAlly || SelectedAction.Target == SkillTarget.AllDeadAllies) && !CurrentParty.Any(x => x.Actor.CurrentState == "toast"))
		{
			AudioManager.Instance.PlaySFX("sys_buzzer");
			return;
		}
		AudioManager.Instance.PlaySFX("SYS_select");
		SetPhase(BattlePhase.TargetSelection);
	}

	public void OnSelectItem(Item item)
	{
		SelectedAction = item;
		if ((SelectedAction.Target == SkillTarget.DeadAlly || SelectedAction.Target == SkillTarget.AllDeadAllies) && !CurrentParty.Any(x => x.Actor.CurrentState == "toast"))
		{
			AudioManager.Instance.PlaySFX("sys_buzzer");
			return;
		}

		Item i = SelectedAction as Item;
		// convert item name to CamelCase for dictionary lookup
		string name = i.Name.Capitalize();
		// Godot's Captialize treats - as a regular character and puts a space after it
		// manually fix that for sno-cone
		if (i.Name == "SNO-CONE")
			name = "Sno-Cone";
		Items[name]--;
		if (Items[name] == 0)
			Items.Remove(name);

		AudioManager.Instance.PlaySFX("SYS_select");
		SetPhase(BattlePhase.TargetSelection);
	}

	private void SetPhase(BattlePhase phase)
	{
		GD.Print("Entering Phase: " + phase);
		Phase = phase;

		switch (Phase)
		{
			case BattlePhase.FightRun:
				HandleFightRun();
				break;
			case BattlePhase.PlayerCommand:
				HandlePlayerCommand();
				break;
			case BattlePhase.TargetSelection:
				HandleTargetSelection();
				break;
			case BattlePhase.PreCommand:
				CheckBattleOver();
				Delay.Start(0.75d);
				break;
			case BattlePhase.CommandExecute:
				HandleCommandExecute(); 
				break;
			case BattlePhase.PostCommand:
				Delay.Start(0.75d);
				break;
			case BattlePhase.EnemyDying:
				HandleEnemyDying();
				break;

		}
	}

	private void OnDelayTimeout()
	{
		switch (Phase)
		{
			case BattlePhase.PreCommand:
				GD.Print("Command Index: " + CommandIndex);
				if (CommandIndex >= Commands.Count)
				{
					EndOfTurn();
					SetPhase(BattlePhase.FightRun);
				}
				else
				{
					SetPhase(BattlePhase.CommandExecute);
				}
				break;
			case BattlePhase.PostCommand:
				{
					CurrentParty.ForEach(x =>
					{
						x.Actor.SetHurt(false);
						if (x.Actor.CurrentHP == 0 && x.Actor.CurrentState != "toast")
						{
							x.Actor.SetState("toast", true);
							AudioManager.Instance.PlaySFX("SYS_you died_2", 1.2f);
						}
					});
					if (Commands[CommandIndex].Actor is PartyMember && Commands[CommandIndex].Action.Name.EndsWith("Attack"))
					{
						PartyMemberComponent component = CurrentParty.First(x => x.Actor == Commands[CommandIndex].Actor);
						if (component.HasFollowup)
						{
							component.FadeOutFollowups();
						}
					}
					FollowupSelected = false;
					FollowupActive = false;

					foreach (EnemyComponent enemy in Enemies.ToList())
					{
						enemy.Actor.SetHurt(false);
						if (enemy.Actor.CurrentHP == 0)
						{
							enemy.Actor.SetState("toast", true);
							if (enemy.Actor.FallsOffScreen)
								DyingEnemies.Add(enemy.GetParent<Node2D>());
							Enemies.Remove(enemy);
						}
						enemy.Actor.ProcessBattleConditions();
					}
					CommandIndex++;
					if (DyingEnemies.Count > 0)
						SetPhase(BattlePhase.EnemyDying);
					else
						SetPhase(BattlePhase.PreCommand);
					break;
				}
		}
	}

	private void PrepareCommandExecution()
	{
		MenuManager.Instance.ShowMenu(MenuState.None);
		foreach (EnemyComponent enemy in Enemies)
		{
			// Add an empty action for each enemy so the sorting below still works
			Commands.Add(new BattleCommand(enemy.Actor, null, null));
		}

		Commands = Commands.OrderByDescending(x => x.Action is Skill s && s.GoesFirst)
			.ThenByDescending(x => x.Actor.CurrentStats.SPD)
			.ThenBy(x =>
			{
				PartyMemberComponent c = CurrentParty.FirstOrDefault(y => y.Actor == x.Actor);
				if (c == null)
					return int.MaxValue;
				else
					return c.Position;
			})
			.ToList();

		CommandIndex = 0;
		GD.Print("Preparing to process " + Commands.Count + " commands...");
	}

	private void HandleFightRun()
	{
		CurrentPartyMember = -1;
		CurrentEnemyTarget = -1;
		CurrentPartyMemberTarget = -1;
		CommandIndex = -1;
		Commands.Clear();
		if (CurrentParty.Count > 1)
			BattleLogManager.Instance.ClearAndShowMessage("What will " + CurrentParty[0].Actor.Name.ToUpper() + " and friends do?");
		else
			BattleLogManager.Instance.ClearAndShowMessage("What will " + CurrentParty[0].Actor.Name.ToUpper() + " do?");
		MenuManager.Instance.ShowButtons(CurrentParty[0].Actor.IsRealWorld);
		MenuManager.Instance.ShowMenu(MenuState.Party);
	}

	private void HandlePlayerCommand()
	{
		while (CurrentParty[CurrentPartyMember].Actor.CurrentState == "toast")
		{
			CurrentPartyMember++;
			if (CurrentPartyMember >= CurrentParty.Count)
			{
				BattleLogManager.Instance.ClearBattleLog();
				PrepareCommandExecution();
				SetPhase(BattlePhase.PreCommand);
				return;
			}
		}
		BattleLogManager.Instance.ClearAndShowMessage("What will " + CurrentParty[CurrentPartyMember].Actor.Name.ToUpper() + " do?");
		MenuManager.Instance.ShowButtons(CurrentParty[CurrentPartyMember].Actor.IsRealWorld);
		MenuManager.Instance.ShowMenu(MenuState.Battle, LastSelectedCommand);
	}

	private void HandleTargetSelection()
	{
		switch (SelectedAction.Target)
		{
			case SkillTarget.Ally:
			case SkillTarget.AllyNotSelf:
			case SkillTarget.DeadAlly:
				// keep selection box on current ally for ally targeting
				CurrentPartyMemberTarget = CurrentParty[CurrentPartyMember].Position;
				BattleLogManager.Instance.ClearAndShowMessage("Use on whom?");
				MenuManager.Instance.ShowMenu(MenuState.None);
				return;
			case SkillTarget.Enemy:
				CurrentEnemyTarget++;
				BattleLogManager.Instance.ClearAndShowMessage("Use on whom?");
				MenuManager.Instance.ShowMenu(MenuState.None);
				return;
			case SkillTarget.AllyOrEnemy:
				CurrentPartyMemberTarget = CurrentParty[CurrentPartyMember].Position;
				BattleLogManager.Instance.ClearAndShowMessage("Use on whom?\nPress SHIFT to switch sides.");
				MenuManager.Instance.ShowMenu(MenuState.None);
				return;
		}
		SelectTarget();
	}

	private void SelectTarget()
	{
		if (Enemies.Count == 0)
		{
			AudioManager.Instance.PlaySFX("sys_buzzer");
			return;
		}

		if ((SelectedAction.Target == SkillTarget.Ally || 
			(SelectedAction.Target == SkillTarget.AllyOrEnemy && CurrentPartyMemberTarget > -1)) 
			&& CurrentParty.First(x => x.Position == CurrentPartyMemberTarget).Actor.CurrentState == "toast")
		{
			AudioManager.Instance.PlaySFX("sys_buzzer");
			return;
		}

		if ((SelectedAction.Target == SkillTarget.DeadAlly || 
			SelectedAction.Target == SkillTarget.AllDeadAllies && CurrentPartyMemberTarget > -1)
			&& CurrentParty.First(x => x.Position == CurrentPartyMemberTarget).Actor.CurrentState != "toast")
		{
			AudioManager.Instance.PlaySFX("sys_buzzer");
			return;
		}

		if (SelectedAction.Target == SkillTarget.AllyNotSelf && CurrentPartyMemberTarget == CurrentParty[CurrentPartyMember].Position)
		{
			AudioManager.Instance.PlaySFX("sys_buzzer");
			return;
		}

		AudioManager.Instance.PlaySFX("SYS_select");
		switch (SelectedAction.Target)
		{
			case SkillTarget.Self:
				Commands.Add(new BattleCommand(CurrentParty[CurrentPartyMember].Actor, CurrentParty[CurrentPartyMember].Actor, SelectedAction));
				break;
			case SkillTarget.Ally:
			case SkillTarget.AllyNotSelf:
			case SkillTarget.DeadAlly:
				Commands.Add(new BattleCommand(CurrentParty[CurrentPartyMember].Actor, CurrentParty.First(x => x.Position == CurrentPartyMemberTarget).Actor, SelectedAction));
				break;
			case SkillTarget.Enemy:
				Commands.Add(new BattleCommand(CurrentParty[CurrentPartyMember].Actor, Enemies[CurrentEnemyTarget].Actor, SelectedAction));
				break;
			case SkillTarget.AllyOrEnemy:
				if (CurrentEnemyTarget > -1)
					Commands.Add(new BattleCommand(CurrentParty[CurrentPartyMember].Actor, Enemies[CurrentEnemyTarget].Actor, SelectedAction));
				else
					Commands.Add(new BattleCommand(CurrentParty[CurrentPartyMember].Actor, CurrentParty.First(x => x.Position == CurrentPartyMemberTarget).Actor, SelectedAction));
				break;
			default:
				// group skills have no "target"
				// targets are selected in the skill itself
				// ... which is probably bad behavior but oh well
				Commands.Add(new BattleCommand(CurrentParty[CurrentPartyMember].Actor, null, SelectedAction));
				break;
		}

		CurrentEnemyTarget = -1;
		CurrentPartyMemberTarget = -1;
		CurrentPartyMember++;
		LastSelectedCommand = null;
		SelectedAction = null;
		if (CurrentPartyMember >= CurrentParty.Count)
		{
			BattleLogManager.Instance.ClearBattleLog();
			PrepareCommandExecution();
			SetPhase(BattlePhase.PreCommand);
		}
		else
			SetPhase(BattlePhase.PlayerCommand);
	}

	private async void HandleCommandExecute()
	{
		while (Commands[CommandIndex].Actor.CurrentState == "toast")
		{
			if (Commands[CommandIndex].Action is Item item)
			{
				// refund items if the character died before using it
				string name = item.Name.Capitalize();
				Items[name]++;
			}
			CommandIndex++;
			if (CommandIndex >= Commands.Count)
			{
				EndOfTurn();
				SetPhase(BattlePhase.FightRun);
				return;
			}
		}

		BattleCommand currentAction = Commands[CommandIndex];

		if (currentAction.Actor is Enemy enemy && currentAction.Action == null)
		{
			// overwrite the empty enemy skill with an actual command
			currentAction = enemy.ProcessAI();
		}

		BattleLogManager.Instance.ClearBattleLog();
		Actor target = currentAction.Target;
		// if the enemy we're trying to target is null for whatever reason, pick a new one
		if (target == null && (currentAction.Action.Target == SkillTarget.Enemy || currentAction.Action.Target == SkillTarget.AllyOrEnemy))
		{
			target = GetRandomAliveEnemy();
			if (target == null)
			{
				GD.PrintErr("Unable to find enemy target!");
				return;
			}
		}
		if (target != null)
		{
			if (target.CurrentHP == 0 && currentAction.Action.Target != SkillTarget.DeadAlly && currentAction.Action.Target != SkillTarget.AllDeadAllies)
			{
				if (target is Enemy)
					target = GetRandomAliveEnemy();
				else
					target = GetRandomAlivePartyMember();
				if (target == null)
				{
					GD.PrintErr("Running Command when all targets are dead!");
					return;
				}
				currentAction.Target = target;
			}
		}
		if (currentAction.Action is Skill skill)
		{
			if (skill.Cost > 0)
			{
				// if the current actor does not have enough juice to use their skill, replace it with a basic attack
				if (currentAction.Actor.CurrentJuice < skill.Cost)
				{
					currentAction.Action = currentAction.Actor.Skills.First().Value;
				}
				else
				{
					currentAction.Actor.CurrentJuice -= skill.Cost;
				}
			}
			if (currentAction.Actor is PartyMember && currentAction.Action.Name.EndsWith("Attack"))
			{
				if (ForceHideFollowup)
				{
					ForceHideFollowup = false;
				}
				else
				{
					PartyMemberComponent component = CurrentParty.First(x => x.Actor == currentAction.Actor);
					if (component.HasFollowup)
					{
						component.FadeInFollowups(Energy);
						FollowupActive = true;
					}
				}
			}
		}

		await currentAction.Action.Effect(currentAction.Actor, currentAction.Target);

		if (BattleLogManager.Instance.IsProcessingMessage)
			SetPhase(BattlePhase.WaitForBattleLog);
		else
			SetPhase(BattlePhase.PostCommand);
	}

	private bool HandleFollowup(InputDirection direction)
	{
		if (Energy < 3 || !FollowupActive || ForceHideFollowup || !EnergyBar.Visible || FollowupSelected)
			return false;

		PartyMemberComponent current = CurrentParty.First(x => x.Actor == Commands[CommandIndex].Actor);
		if (!FollowupTable.TryGetValue((current.Position, direction), out var pair))
			return false;

		PartyMemberComponent target = CurrentParty.First(x => x.Position == pair.Target);
		if (target == null || target.Actor.CurrentState == "toast")
			return false;

		string name = pair.SkillName;
		bool basil = false;
		if (name.StartsWith("ReleaseEnergy"))
		{
			if (Energy != 10 || CurrentParty.Any(x => x.Actor.CurrentState == "toast"))
				return false;

			if (UseBasilReleaseEnergy)
				name += "Basil";
			else
				name += FollowupTier;
		}
		else
		{
			if (current.Position == 2 && UseBasilFollowups)
				basil = true;

			if (!basil)
				name += FollowupTier;
		}

		if (!Database.TryGetSkill(name, out Skill skill))
			return false;

		ForceCommand(current.Actor, Commands[CommandIndex].Target, skill);
		return true;
	}

	public void ForceCommand(Actor self, Actor target, Skill skill)
	{
		if (skill.Name.EndsWith("Attack"))
			// if the forced skill is an attack, hide the followup bubbles
			ForceHideFollowup = true;
		Commands.Insert(CommandIndex + 1, new BattleCommand(self, target, skill));
	}

	private void ProcessFollowupSuccess()
	{
		FollowupSelected = true;
		AudioManager.Instance.PlaySFX("Skill2", 1f, 0.8f);
		CurrentParty.First(x => x.Actor == Commands[CommandIndex].Actor).FadeOutFollowups();
		if (Commands[CommandIndex + 1].Action.Name.Contains("Release Energy"))
			Energy = 0;
		else
			Energy -= 3;
	}

	private void EndOfTurn()
	{
		LastSelectedCommand = null;
		CheckBattleOver();
		// tick down stat turn timers
		CurrentParty.ForEach(x =>
		{
			x.Actor.DecreaseStatTurnCounter();
			if (x.Actor.HasStatModifier("ReleaseEnergyBasil"))
			{
				int heal = (int)Math.Round(x.Actor.CurrentStats.MaxHP * 0.1f, MidpointRounding.AwayFromZero);
				int juice = (int)Math.Round(x.Actor.CurrentStats.MaxJuice * 0.05f, MidpointRounding.AwayFromZero);
				x.Actor.Heal(heal);
				x.Actor.HealJuice(juice);
				SpawnDamageNumber(heal, x.Actor.CenterPoint, DamageType.Heal);
				SpawnDamageNumber(juice, x.Actor.CenterPoint, DamageType.JuiceGain);
			}
		});
		Enemies.ForEach(x =>
		{
			x.Actor.ProcessEndOfTurn();
			x.Actor.DecreaseStatTurnCounter();
			x.Actor.ProcessStartOfTurn();
		});
	}

	public void OnBattleLogFinished()
	{
		if (Phase == BattlePhase.WaitForBattleLog)
			SetPhase(BattlePhase.PostCommand);
	}

	private void HandleEnemyDying()
	{
		Tween tween = CreateTween();
		foreach (Node2D enemy in DyingEnemies)
		{
			tween.TweenInterval(0.5f);
			tween.TweenProperty(enemy, "position", enemy.Position + new Vector2(0, 400f), 0.50f);
		}
		tween.TweenCallback(Callable.From(EnemiesDoneDying));
	}

	private void EnemiesDoneDying()
	{
		DyingEnemies.ForEach(x => x.QueueFree());
		DyingEnemies.Clear();
		SetPhase(BattlePhase.PreCommand);
	}

	private void CheckBattleOver()
	{
		if (Enemies.Count == 0)
		{
			SetPhase(BattlePhase.BattleOver);
			CurrentParty.ForEach(x =>
			{
				if (x.Actor.CurrentState != "toast")
					x.Actor.SetState("victory", true);
			});
			AudioManager.Instance.PlayBGM("xx_victory");
			BattleLogManager.Instance.ClearAndShowMessage(CurrentParty[0].Actor.Name.ToUpper() + "'s party was victorious!");
			MenuButtonContainer.Visible = true;
			return;
		}
		if (CurrentParty.All(x => x.Actor.CurrentHP == 0))
		{
			SetPhase(BattlePhase.BattleOver);
			BattleLogManager.Instance.ClearAndShowMessage(CurrentParty[0].Actor.Name.ToUpper() + "'s party was defeated...");
			MenuButtonContainer.Visible = true;
			return;
		}
		PartyMemberComponent omori = CurrentParty.FirstOrDefault(x => x.Actor is Omori omori && omori.CurrentState == "toast");
		// if any omori is toast, the battle is over
		// this may change in the future
		if (omori != null)
		{
			SetPhase(BattlePhase.BattleOver);
			BattleLogManager.Instance.ClearAndShowMessage(CurrentParty[0].Actor.Name.ToUpper() + "'s party was defeated...");
			MenuButtonContainer.Visible = true;
		}
	}

	public bool Damage(Actor self, Actor target, Func<float> damageFunc, bool neverMiss = true, float variance = 0.2f, bool guaranteeCrit = false, bool neverCrit = false)
	{
		if (!neverMiss)
		{
			bool miss = self.CurrentStats.HIT < GameManager.Instance.Random.RandiRange(0, 100);
			if (miss)
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor]'s attack missed...");
				AudioManager.Instance.PlaySFX("BA_miss");
				// Miss text spawns a little further down
				SpawnDamageNumber(-1, target.CenterPoint + new Vector2(0, 30), DamageType.Miss);
				return false;
			}
		}
		float baseDamage = damageFunc();
		float damageVariance = GameManager.Instance.Random.RandfRange(1f - variance, 1f + variance);
		bool critical = self.CurrentStats.LCK * .01f >= GameManager.Instance.Random.Randf() || guaranteeCrit;
		float finalDamage = baseDamage * damageVariance;
		string selfState = self.CurrentState;
		string targetState = target.CurrentState;

		if (self.HasLockedEmotion())
		{
			selfState = (self.StateStatModifier as EmotionLockStatModifier).OverrideEmotion();
		}

		if (target.HasLockedEmotion())
		{
			targetState = (target.StateStatModifier as EmotionLockStatModifier).OverrideEmotion();
		}

		finalDamage = CalculateEmotionModifiers(selfState, targetState, finalDamage, out int effectiveness);
		if ((critical || target.HasStatModifier("Tickle")) && !neverCrit)
		{
			finalDamage = (finalDamage * 1.5f) + 2;
			BattleLogManager.Instance.QueueMessage("IT HIT RIGHT IN THE HEART!");
			AudioManager.Instance.PlaySFX("BA_CRITICAL_HIT", volume: 2f);
		}

		int juiceLost = 0;
		switch (target.CurrentState)
		{
			case "miserable":
				juiceLost = Math.Min((int)Math.Floor(finalDamage), 0);
				finalDamage -= juiceLost;
				break;
			case "depressed":
				juiceLost = Math.Min((int)Math.Floor(finalDamage * 0.5f), target.CurrentJuice);
				finalDamage -= juiceLost;
				break;
			case "sad":
				juiceLost = Math.Min((int)Math.Floor(finalDamage * 0.3f), target.CurrentJuice);
				finalDamage -= juiceLost;
				break;
		}
		target.CurrentJuice -= juiceLost;

		foreach (StatModifier mod in self.StatModifiers.Values)
		{
			mod.OverrideDamage(ref finalDamage, self, target, true);
		}

		// this could (should) be moved out of here
		if (self.HasStatModifier("Flex"))
		{
			self.RemoveStatModifier("Flex");
		}

		foreach (StatModifier mod in target.StatModifiers.Values)
		{
			mod.OverrideDamage(ref finalDamage, self, target, false);
		}

		int rounded = (int)Math.Round(finalDamage, MidpointRounding.AwayFromZero);
		if (rounded < 0)
			rounded = 0;
		if (rounded > 9999)
			rounded = 9999;
		target.Damage(rounded);
		if (target is PartyMember)
		{
			Energy++;
			if (Energy > 10)
				Energy = 10;
		}
		SpawnDamageNumber(rounded, target.CenterPoint, critical: (critical && !neverCrit));
		// we don't need to play a hitsound if the attack is a critical
		if (!critical)
		{
			GD.Print("Effectiveness: " + effectiveness);
			if (effectiveness > 0)
			{
				BattleLogManager.Instance.QueueMessage("...It was a moving attack!");
				AudioManager.Instance.PlaySFX("se_impact_double", 1f, 0.9f);
			}
			else if (effectiveness < 0)
			{
				BattleLogManager.Instance.QueueMessage("...It was a dull attack.");
				AudioManager.Instance.PlaySFX("se_impact_soft", 1f, 0.9f);
			}
			else
				AudioManager.Instance.PlaySFX("SE_dig", 0.7f, 0.9f);
		}
		BattleLogManager.Instance.QueueMessage(self, target, "[target] takes " + rounded + " damage!");
		if (juiceLost > 0)
		{
			BattleLogManager.Instance.QueueMessage(self, target, "[target] lost " + juiceLost + " juice...");
			SpawnDamageNumber(juiceLost, target.CenterPoint, DamageType.JuiceLoss);
		}

		return true;
	}

	// some healing and juice skills are affected by emotion

	public void Heal(Actor self, Actor target, Func<float> healFunc, float variance = 0.2f)
	{
		float baseHealing = healFunc();
		float healingVariance = GameManager.Instance.Random.RandfRange(1f - variance, 1f + variance);
		float finalHealing = baseHealing * healingVariance;
		finalHealing = CalculateEmotionModifiers(self.CurrentState, target.CurrentState, finalHealing, out _);
		int rounded = (int)Math.Round(finalHealing, MidpointRounding.AwayFromZero);
		target.Heal(rounded);
		SpawnDamageNumber(rounded, target.CenterPoint, DamageType.Heal);
		BattleLogManager.Instance.QueueMessage(self, target, $"[target] recovered {rounded} HEART!");
	}

	public void HealJuice(Actor self, Actor target, Func<float> healFunc)
	{
		float baseJuice = healFunc();
		float finalJuice = CalculateEmotionModifiers(self.CurrentState, target.CurrentState, baseJuice, out _);
		int rounded = (int)Math.Round(finalJuice, MidpointRounding.AwayFromZero);
		target.HealJuice(rounded);
		SpawnDamageNumber(rounded, target.CenterPoint, DamageType.JuiceGain);
		BattleLogManager.Instance.QueueMessage(self, target, $"[target] recovered {rounded} JUICE!");
	}

	private readonly int[,] EffectivenessMatrix = new int[3, 3]
	{
	//			angry sad happy
	/* angry */	{0, -1, 1},
	/* sad   */	{1, 0, -1},
	/* happy */	{-1, 1, 0}
	};
	private readonly float[] weakness = [1.5f, 2f, 2.5f];
	private readonly float[] resistance = [0.8f, 0.65f, 0.5f];

	private float CalculateEmotionModifiers(string self, string target, float damage, out int effect)
	{
		if (self != "neutral" && target == "afraid")
		{
			// afraid takes 50% more damage from all emotions
			effect = 0;
			return damage * 1.5f;
		}

		int selfIndex = GetEffectivenessIndex(self);
		int targetIndex = GetEffectivenessIndex(target);
		effect = 0;
		if (selfIndex == -1 || targetIndex == -1)
		{
			return damage;
		}
		int targetTier = GetEmotionTier(target);
		int effectiveness = EffectivenessMatrix[targetIndex, selfIndex];
		float multiplier = 1.0f;

		if (effectiveness > 0)
		{
			multiplier = weakness[targetTier];
		}
		else if (effectiveness < 0)
		{
			multiplier = resistance[targetTier];
		}

		effect = effectiveness;
		return damage * multiplier;
	}

	private int GetEffectivenessIndex(string emotion)
	{
		return emotion switch
		{
			"angry" or "enraged" or "furious" => 0,
			"sad" or "depressed" or "miserable" => 1,
			"happy" or "ecstatic" or "manic" => 2,
			_ => -1
		};;
	}

	private int GetEmotionTier(string emotion)
	{
		return emotion switch
		{
			"miserable" or "manic" or "furious" => 2,
			"depressed" or "ecstatic" or "enraged" => 1,
			"sad" or "happy" or "angry" => 0,
			_ => -1,
		};
	}

	public void RandomEmotion(Actor who)
	{
		int roll = GameManager.Instance.Random.RandiRange(0, 2);
		string state = "";
		switch (roll)
		{
			case 0:
				state = "sad";
				switch (who.CurrentState)
				{
					case "miserable":
						return;
					case "depressed":
						state = "miserable";
						break;
					case "sad":
						state = "depressed";
						break;
				}
				break;
			case 1:
				state = "angry";
				switch (who.CurrentState)
				{
					case "furious":
						return;
					case "enraged":
						state = "furious";
						break;
					case "angry":
						state = "enraged";
						break;
				}
				break;
			case 2:
				state = "happy";
				switch (who.CurrentState)
				{
					case "manic":
						return;
					case "ecstatic":
						state = "manic";
						break;
					case "happy":
						state = "ecstatic";
						break;
				}
				break;
		}
		if (who.IsStateValid(state))
			who.SetState(state);
	}

	public void SpawnDamageNumber(int damage, Vector2 position, DamageType type = DamageType.Damage, bool critical = false)
	{
		DamageNumber dmg = new(damage, type, critical)
		{
			ZAsRelative = false,
			ZIndex = 5
		};
		while (DamageNumbers.Contains(position))
		{
			position.Y += 40;
		}
		dmg.Position = position;
		AddChild(dmg);
		DamageNumbers.Add(position);

		Task.Delay(TimeSpan.FromSeconds(1.5f)).ContinueWith(_ =>
		{
			dmg.CallDeferred(DamageNumber.MethodName.Despawn);
			DamageNumbers.Remove(position);
		});
	}

	public EnemyComponent SummonEnemy(string who, Vector2 position, string startingEmotion = "neutral", bool fallsOffScreen = true, int layer = 0)
	{
		EnemyComponent enemy = GameManager.Instance.SpawnEnemy(who, position, startingEmotion, fallsOffScreen, layer);
		Enemies.Add(enemy);
		return enemy;
	}

	public void AddEnergy(int amount)
	{
		Energy = Math.Min(Energy + amount, 10);
	}

	public PartyMember GetRandomAlivePartyMember()
	{
		IEnumerable<PartyMemberComponent> alive = CurrentParty.Where(x => x.Actor.CurrentHP > 0);
		return alive.ElementAt(GameManager.Instance.Random.RandiRange(0, alive.Count() - 1)).Actor;
	}

	public PartyMember GetRandomDeadPartyMember()
	{
		PartyMemberComponent result = CurrentParty.FirstOrDefault(x => x.Actor.CurrentHP <= 0);
		return result?.Actor;
	}

	public Enemy GetRandomAliveEnemy()
	{
		IEnumerable<EnemyComponent> alive = Enemies.Where(x => x.Actor.CurrentHP > 0);
		return alive.ElementAt(GameManager.Instance.Random.RandiRange(0, alive.Count() - 1)).Actor;
	}

	public List<Enemy> GetAllEnemies()
	{
		return Enemies.Select(x => x.Actor).ToList();
	}

	/// <summary>
	/// Gets all party members who are not toast.
	/// </summary>
	public List<PartyMemberComponent> GetAlivePartyMembers()
	{
		return CurrentParty.Where(x => x.Actor.CurrentHP > 0).ToList();
	}

	/// <summary>
	/// Gets all party members who are toast.
	/// </summary>
	public List<PartyMemberComponent> GetDeadPartyMembers()
	{
		return CurrentParty.Where(x => x.Actor.CurrentState == "toast").ToList();
	}

	/// <summary>
	/// Gets all party members, including ones who are toast.
	/// </summary>
	/// <remarks>
	/// Should NOT be used for skill logic in most situations, use <see cref="GetAlivePartyMembers"/> instead.
	/// </remarks>
	public List<PartyMemberComponent> GetAllPartyMembers()
	{
		return CurrentParty;
	}

	public PartyMember GetPartyMember(int index)
	{
		// eh who needs bounds checks these days
		return CurrentParty[index].Actor;
	}

	public PartyMember GetCurrentPartyMember()
	{
		return CurrentParty[CurrentPartyMember].Actor;
	}

	public List<(Item, int)> GetSnacks()
	{
		List<(Item, int)> result = [];
		foreach (var entry in Items)
		{
			if (Database.TryGetItem(entry.Key, out Item item))
			{
				if (!item.IsToy)
					result.Add((item, entry.Value));
			}
		}

		return result;
	}

	public List<(Item, int)> GetToys()
	{
		List<(Item, int)> result = [];
		foreach (var entry in Items)
		{
			if (Database.TryGetItem(entry.Key, out Item item))
			{
				if (item.IsToy)
					result.Add((item, entry.Value));
			}
		}

		return result;
	}
}

public enum BattlePhase
{
	FightRun,
	PlayerCommand,
	TargetSelection,
	SkillSelection,
	PreCommand,
	CommandExecute,
	WaitForBattleLog,
	PostCommand,
	EnemyDying,
	BattleOver
}
