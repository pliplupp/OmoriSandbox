using Godot;
using OmoriSandbox.Actors;
using OmoriSandbox.Animation;
using OmoriSandbox.Battle;
using OmoriSandbox.Battle.Modifier;
using OmoriSandbox.Editor;
using OmoriSandbox.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmoriSandbox;

/// <summary>
/// Handles the bulk flow of battles.
/// </summary>
public partial class BattleManager : Node
{
	[Export] private Label EnergyText;
	[Export] private Sprite2D EnergyBar;
	[Export] private EnergyDots EnergyDots;
	[Export] private HBoxContainer MenuButtonContainer;
	[Export] private Label RestartLabel;

	private List<PartyMemberComponent> CurrentParty = [];
	private List<EnemyComponent> Enemies = [];

	private BattlePhase Phase = BattlePhase.PreBattle;
	private int CurrentPartyMember = -1;
	private int CurrentEnemyTarget = -1;
	private int CurrentPartyMemberTarget = -1;
	private List<BattleCommand> Commands = [];
	private int CommandIndex = -1;
	private Timer Delay;
	private List<Node2D> DyingEnemies = [];
	private Godot.Collections.Dictionary<string, int> Items = [];
	private BattleAction SelectedAction;
	private HashSet<Vector2> DamageNumbers = [];
	/// <summary>
	/// The amount of Energy the party currently has.
	/// </summary>
	public int Energy { get; private set; } = 0;
	private bool FollowupActive = false;
	private bool FollowupSelected = false;
	private bool ForceHideFollowup = false;
	private int FollowupTier = 1;
	private bool UseBasilReleaseEnergy = false;
	private bool UseBasilFollowups = false;
	private bool DamageNumbersDisabled = false;

	/// <summary>
	/// Whether a battle is currently ongoing.
	/// </summary>
	public bool IsBattling { get; private set; } = false;

	// TODO: This is a poor way to do this, should probably be improved
	private bool ProcessedStartOfTurn = false;
	private bool ProcessedEndOfTurn = false;

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
			ReloadPreset();
		};

		MenuButtonContainer.GetChild<Button>(1).Pressed += () =>
		{
			Reset();
			MainMenuManager.Instance.ReturnToTitle();
		};
		Instance = this;
	}

	internal void Init(List<PartyMemberComponent> party, List<EnemyComponent> enemies, Godot.Collections.Dictionary<string, int> items, int followupTier, bool useBasilFollowups, bool useBasilReleaseEnergy, bool damageNumbersDisabled)
	{
		CurrentParty = party.OrderBy(x => x.Position).ToList();
		Enemies = enemies;
		Items = items;
		Energy = 3;
		FollowupTier = followupTier;
		UseBasilFollowups = useBasilFollowups;
		UseBasilReleaseEnergy = useBasilReleaseEnergy;
		DamageNumbersDisabled = damageNumbersDisabled;
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

		MenuManager.Instance.ShowMenu(MenuState.None, true);

		IsBattling = true;
	}

	private async void PreBattle()
	{
		foreach (PartyMemberComponent p in CurrentParty)
			await p.Actor.OnStartOfBattle();
		for (int i = 0; i < Enemies.Count; i++)
			await Enemies[i].Actor.OnStartOfBattle();
		SetPhase(BattlePhase.FightRun);
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
				x.SelectionBoxVisible = CurrentPartyMember > -1 && x.Actor == CurrentParty[CurrentPartyMember].Actor;
			else
				x.SelectionBoxVisible = false;
		});

		for (int i = 0; i < Enemies.Count; i++)
		{
			Enemies[i].ShowInfoBox(i == CurrentEnemyTarget);
		}

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
					do
					{
						CurrentPartyMember--;
						if (CurrentPartyMember < 0)
						{
							AudioManager.Instance.PlaySFX("sys_cancel");
							MenuManager.Instance.ShowMenu(MenuState.Party, true);
							SetPhase(BattlePhase.FightRun);
							return;
						}
					} while (CurrentParty[CurrentPartyMember].Actor.CurrentState == "toast");
					
					if (Commands[^1].Action is Item item)
					{
						string name = CaptializeItemName(item);
						if (!Items.TryAdd(name, 1))
							Items[name]++;
					}
					Commands.RemoveAt(Commands.Count - 1);
					AudioManager.Instance.PlaySFX("sys_cancel");
					MenuManager.Instance.ShowMenu(MenuState.Battle);
					SetPhase(BattlePhase.PlayerCommand);
					break;
				case BattlePhase.TargetSelection:
					MenuState targetMenu;
					if (SelectedAction is Item i)
					{
						targetMenu = i.IsToy ? MenuState.Toy : MenuState.Snack;
						string name = CaptializeItemName(i);
						if (!Items.TryAdd(name, 1))
							Items[name]++;
					}
					else
						targetMenu = SelectedAction.Name.EndsWith("Attack") ? MenuState.Battle : MenuState.Skill;
					AudioManager.Instance.PlaySFX("sys_cancel");
					CurrentEnemyTarget = -1;
					CurrentPartyMemberTarget = -1;
					MenuManager.Instance.ShowMenu(targetMenu);
					SetPhase(targetMenu is MenuState.Battle ? BattlePhase.PlayerCommand : BattlePhase.SkillSelection);
					break;
				case BattlePhase.SkillSelection:
					AudioManager.Instance.PlaySFX("sys_cancel");
					MenuManager.Instance.ShowMenu(MenuState.Battle, ignoreMemory: true);
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

		if (Input.IsActionJustPressed("Restart"))
		{
			// don't allow restarts during the setup phase
			// prevents stuff from breaking
			if (Phase is BattlePhase.PreBattle)
				return;
			
			// if the restart is requested during a turn, queue it to prevent anything breaking
			if (Phase is BattlePhase.PreCommand or
			    BattlePhase.CommandExecute or
			    BattlePhase.WaitForBattleLog or
			    BattlePhase.PostCommand or
				BattlePhase.EnemyDying)
				RestartLabel.Visible = true;
			else
			{
				// otherwise, just reset immediately
				Reset();
				ReloadPreset();
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
			if (SelectedAction.Target == SkillTarget.Enemy || 
				(SelectedAction.Target == SkillTarget.AllyOrEnemy && CurrentEnemyTarget > -1)
				&& Enemies.Count > 1
				&& (direction == InputDirection.Left || direction == InputDirection.Right))
			{
				CurrentEnemyTarget = SelectEnemy(CurrentEnemyTarget, direction);
				AudioManager.Instance.PlaySFX("SYS_move");
				return;
			}
			if (SelectedAction.Target == SkillTarget.Ally || SelectedAction.Target == SkillTarget.AllyNotSelf || SelectedAction.Target == SkillTarget.DeadAlly || (SelectedAction.Target == SkillTarget.AllyOrEnemy && CurrentPartyMemberTarget > -1))
			{
				int target = SelectPartyMember(CurrentPartyMemberTarget, direction);
				if (target > -1)
				{
					AudioManager.Instance.PlaySFX("SYS_move");
					CurrentPartyMemberTarget = target;
				}
			}
		}
	}

	private int SelectEnemy(int current, InputDirection direction)
	{
		if (Enemies.Count < 2)
			return current;

		var sortedEnemies = Enemies
			.Select((enemy, index) => new { Enemy = enemy, Index = index })
			.OrderBy(e => e.Enemy.Actor.CenterPoint.X)
			.ThenBy(e => e.Index)
			.ToList();
		
		int sortedPosition = sortedEnemies.FindIndex(e => e.Index == current);

		int nextPosition;
		if (direction == InputDirection.Right)
			nextPosition = (sortedPosition + 1) % sortedEnemies.Count;
		else
			nextPosition = (sortedPosition - 1 + sortedEnemies.Count) % sortedEnemies.Count;
		return sortedEnemies[nextPosition].Index;
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

	internal void OnFightSelected()
	{
		CurrentPartyMember = 0;
		MenuManager.Instance.ShowMenu(MenuState.Battle, true);
		SetPhase(BattlePhase.PlayerCommand);
	}

	internal void Reset()
	{
		GameManager.Instance.DespawnAll();
		AnimationManager.Instance.DespawnAll();
		CurrentParty.Clear();
		Enemies.Clear();
		Items.Clear();
		Commands.Clear();
		MenuManager.Instance.ShowMenu(MenuState.None, true);
		MenuManager.Instance.ClearLastSelected();
		EnergyBar.Visible = false;
		BattleLogManager.Instance.ClearBattleLog();
		BattleLogManager.Instance.Visible = false;
		Delay.Timeout -= OnDelayTimeout;
		Delay.QueueFree();
		BattleLogManager.Instance.FinishedLogging -= OnBattleLogFinished;
		DialogueManager.Instance.Reset();
		AudioManager.Instance.Reset();
		Phase = BattlePhase.PreBattle;
		ProcessedStartOfTurn = false;
		ProcessedEndOfTurn = false;
		RestartLabel.Visible = false;
		IsBattling = false;
	}

	private void ReloadPreset()
	{
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
	}

	internal void OnSelectAttack()
	{
		SelectedAction = CurrentParty[CurrentPartyMember].Actor.Skills.Values.First();
		MenuManager.Instance.SaveLastSelected(CurrentParty[CurrentPartyMember].Actor);
		SetPhase(BattlePhase.TargetSelection);
	}

	// idfk
	internal void OnSelectNotAttack(MenuState what)
	{
		MenuManager.Instance.SaveLastSelected(CurrentParty[CurrentPartyMember].Actor);
		MenuManager.Instance.ShowMenu(what);
		SetPhase(BattlePhase.SkillSelection);
	}

	internal void OnSelectSkill(Skill skill)
	{
		SelectedAction = skill;
		if (!skill.MeetsRequirements(CurrentParty[CurrentPartyMember].Actor))
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
		MenuManager.Instance.SaveLastSelected(CurrentParty[CurrentPartyMember].Actor);
		SetPhase(BattlePhase.TargetSelection);
	}

	internal void OnSelectItem(Item item)
	{
		SelectedAction = item;
		if ((SelectedAction.Target == SkillTarget.DeadAlly || SelectedAction.Target == SkillTarget.AllDeadAllies) && !CurrentParty.Any(x => x.Actor.CurrentState == "toast"))
		{
			AudioManager.Instance.PlaySFX("sys_buzzer");
			return;
		}

		Item i = SelectedAction as Item;
		string name = CaptializeItemName(i);
		Items[name]--;
		if (Items[name] == 0)
			Items.Remove(name);

		AudioManager.Instance.PlaySFX("SYS_select");
		MenuManager.Instance.SaveLastSelected(CurrentParty[CurrentPartyMember].Actor);
		SetPhase(BattlePhase.TargetSelection);
	}

	private void SetPhase(BattlePhase phase)
	{
		GD.Print("Entering Phase: " + phase);
		Phase = phase;

		double delayTime = SettingsMenuManager.Instance.ActionDelay switch
		{
			1 => 1.25d,
			2 => 1d,
			4 => 0.5d,
			5 => 0.25d,
			_ => 1d
		};
		
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
				Delay.Start(delayTime);
				break;
			case BattlePhase.CommandExecute:
				HandleCommandExecute(); 
				break;
			case BattlePhase.PostCommand:
				Delay.Start(delayTime);
				break;
			case BattlePhase.EnemyDying:
				HandleEnemyDying();
				break;

		}
	}

	private async void OnDelayTimeout()
	{
		switch (Phase)
		{
			case BattlePhase.PreCommand:
				if (RestartLabel.Visible)
				{
					Reset();
					ReloadPreset();
					return;
				}
				
				GD.Print("Command Index: " + CommandIndex);
				if (CommandIndex >= Commands.Count)
				{
					EndOfTurn();
				}
				else
				{
					SetPhase(BattlePhase.CommandExecute);
				}
				break;
			case BattlePhase.PostCommand:
				{
					foreach (PartyMemberComponent member in CurrentParty)
					{
						member.Actor.SetHurt(false);
						if (member.Actor.CurrentHP == 0 && member.Actor.CurrentState != "toast")
						{
							member.Actor.SetState("toast", true);
							member.Actor.RemoveAllStatModifiers();
							AudioManager.Instance.PlaySFX("SYS_you died_2", 1.2f);
						}

						if (SettingsMenuManager.Instance.ShowStateIcons)
						{
							member.UpdateStateIcons();
						}

						// this needs to be bundled in with the rest of the plotarmor changes
						// because this is hard to look at
						if (member.Actor is Omori omori && omori.CurrentState == "plotarmor" && !omori.HasAnnouncedPlotArmor)
						{
							DialogueManager.Instance.QueueMessage("OMORI did not succumb.");
							await DialogueManager.Instance.WaitForDialogue();
							omori.HasAnnouncedPlotArmor = true;
						}
					}
					
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
						await enemy.Actor.ProcessBattleConditions();
						if (enemy.Actor.CurrentHP == 0)
						{
							enemy.Actor.SetState("toast", true);
							if (enemy.Actor.FallsOffScreen)
								DyingEnemies.Add(enemy.GetParent<Node2D>());
							Enemies.Remove(enemy);
						}
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
			Commands.Add(new BattleCommand(enemy.Actor, [], null));
		}

		Commands = Commands.OrderByDescending(x => x.Action is Skill s && s.GoesFirst)
			.ThenByDescending(x => x.Actor.CurrentStats.SPD)
			.ThenBy(x =>
			{
				PartyMemberComponent c = CurrentParty.FirstOrDefault(y => y.Actor == x.Actor);
				if (c == null)
					return int.MaxValue;
				return c.Position;
			})
			.ToList();

		CommandIndex = 0;
		GD.Print("Preparing to process " + Commands.Count + " commands...");
	}

	private async void HandleFightRun()
	{
		CurrentPartyMember = -1;
		CurrentEnemyTarget = -1;
		CurrentPartyMemberTarget = -1;
		CommandIndex = -1;
		Commands.Clear();
		ProcessedEndOfTurn = false;
		if (!ProcessedStartOfTurn)
		{
			foreach (PartyMemberComponent member in CurrentParty.Where(x => x.Actor.CurrentState != "toast"))
			{
				if (member.Actor is Omori omori && omori.CurrentState == "plotarmor")
				{
					omori.RemovePlotArmor();
					omori.RemoveStatModifier("SecondChance");
				}
				if (!member.Actor.HasStatModifier("ReleaseEnergyBasil")) 
					continue;
				int heal = (int)Math.Round(member.Actor.CurrentStats.MaxHP * 0.1f, MidpointRounding.AwayFromZero);
				int juice = (int)Math.Round(member.Actor.CurrentStats.MaxJuice * 0.05f, MidpointRounding.AwayFromZero);
				member.Actor.Heal(heal);
				member.Actor.HealJuice(juice);
				SpawnDamageNumber(heal, member.Actor.CenterPoint, DamageType.Heal);
				SpawnDamageNumber(juice, member.Actor.CenterPoint, DamageType.JuiceGain);
			}
			
			for (int i = 0; i < Enemies.Count; i++)
				await Enemies[i].Actor.ProcessStartOfTurn();
			ProcessedStartOfTurn = true;
		}
		GameManager.Instance.DiscordManager.SetBattling(Enemies.Count);
		switch (CurrentParty.Count)
		{
			case 1:
				BattleLogManager.Instance.ClearAndShowMessage("What will " + CurrentParty[0].Actor.Name.ToUpper() + " do?");
				break;
			case 2:
				BattleLogManager.Instance.ClearAndShowMessage("What will " + CurrentParty[0].Actor.Name.ToUpper() + " and " + CurrentParty[1].Actor.Name.ToUpper() + " do?");
				break;
			default:
				BattleLogManager.Instance.ClearAndShowMessage("What will " + CurrentParty[0].Actor.Name.ToUpper() + " and friends do?");
				break;
		}
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

		if (SettingsMenuManager.Instance.ShowMoreInfo)
		{
			Stats stats = CurrentParty[CurrentPartyMember].Actor.CurrentStats;
			BattleLogManager.Instance.ClearAndShowMessage($"What will {CurrentParty[CurrentPartyMember].Actor.Name.ToUpper()} do?" + 
			                                              $"\n(ATK: {stats.ATK}, DEF: {stats.DEF}, SPD: {stats.SPD}, LCK: {stats.LCK})");
		}
		else
			BattleLogManager.Instance.ClearAndShowMessage("What will " + CurrentParty[CurrentPartyMember].Actor.Name.ToUpper() + " do?");
		MenuManager.Instance.ShowButtons(CurrentParty[CurrentPartyMember].Actor.IsRealWorld);
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
				string key = OS.GetKeycodeString(SettingsMenuManager.Instance.GetKeybindForAction("SwitchSides")).ToUpper();	
				BattleLogManager.Instance.ClearAndShowMessage($"Use on whom?\nPress {key} to switch sides.");
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
			// select all party members for now, these will be resolved later
			case SkillTarget.AllAllies:
				Commands.Add(new BattleCommand(CurrentParty[CurrentPartyMember].Actor, GetAllPartyMembers().Select(x => x.Actor).ToList(), SelectedAction));
				break;
			case SkillTarget.AllDeadAllies:
				Commands.Add(new BattleCommand(CurrentParty[CurrentPartyMember].Actor, GetAllPartyMembers().Select(x => x.Actor).ToList(), SelectedAction));
				break;
			case SkillTarget.AllEnemies:
				Commands.Add(new BattleCommand(CurrentParty[CurrentPartyMember].Actor, GetAllEnemies(), SelectedAction));
				break;
			default:
				GD.PrintErr("Unhandled SelectTarget case: " + SelectedAction.Target);
				break;
		}

		CurrentEnemyTarget = -1;
		CurrentPartyMemberTarget = -1;
		CurrentPartyMember++;
		SelectedAction = null;
		if (CurrentPartyMember >= CurrentParty.Count)
		{
			BattleLogManager.Instance.ClearBattleLog();
			PrepareCommandExecution();
			SetPhase(BattlePhase.PreCommand);
		}
		else
		{
			MenuManager.Instance.ShowMenu(MenuState.Battle);
			SetPhase(BattlePhase.PlayerCommand);
		}
	}

	private async void HandleCommandExecute()
	{
		while (Commands[CommandIndex].Actor.CurrentState == "toast")
		{
			if (Commands[CommandIndex].Action is Item item)
			{
				// refund items if the character died before using it
				string name = CaptializeItemName(item);
				Items[name]++;
			}
			CommandIndex++;
			if (CommandIndex >= Commands.Count)
			{
				EndOfTurn();
				return;
			}
		}

		BattleCommand currentAction = Commands[CommandIndex];

		if (currentAction.Actor is Enemy enemy && currentAction.Action == null)
		{
			// overwrite the empty enemy skill with an actual command
			currentAction = enemy.ProcessAI();
			Commands[CommandIndex] = currentAction;
		}

		BattleLogManager.Instance.ClearBattleLog();
		GD.Print("Processing action " + currentAction.Action.Name);
		List<Actor> resolvedTargets = [];
		switch (currentAction.Action.Target)
		{
			case SkillTarget.AllAllies:
				resolvedTargets.AddRange(currentAction.Actor is Enemy ? GetAllEnemies() : GetAlivePartyMembers().Select(x => x.Actor));
				break;
			case SkillTarget.AllEnemies:
				resolvedTargets.AddRange(currentAction.Actor is Enemy ? GetAlivePartyMembers().Select(x => x.Actor) : GetAllEnemies());
				break;
			case SkillTarget.XRandomEnemies:
				foreach (Actor target in currentAction.Targets)
				{
					if (target.CurrentState is "toast")
						resolvedTargets.Add(target is Enemy ? GetRandomAliveEnemy() : GetRandomAlivePartyMember());
					else
					{
						resolvedTargets.Add(target);
					}
				}
				break;
			case SkillTarget.Ally:
			case SkillTarget.Enemy:
			case SkillTarget.AllyOrEnemy:
				if (currentAction.Targets[0].CurrentState is "toast")
					resolvedTargets.Add(currentAction.Targets[0] is Enemy ? GetRandomAliveEnemy() : GetRandomAlivePartyMember());
				else
					resolvedTargets.Add(currentAction.Targets[0]);
				break;
			case SkillTarget.DeadAlly:
				if (currentAction.Targets[0].CurrentState is not "toast")
				{
					Actor newTarget = currentAction.Targets[0] is Enemy
						? null
						: GetRandomAlivePartyMember();
					if (newTarget == null)
					{
						BattleLogManager.Instance.QueueMessage(currentAction.Actor.Name.ToUpper() + "'s skill did nothing.");
						SetPhase(BattlePhase.WaitForBattleLog);
						return;
					}
					resolvedTargets.Add(newTarget);
				}
				else
					resolvedTargets.Add(currentAction.Targets[0]);
				break;
			case SkillTarget.AllDeadAllies:
				List<PartyMember> deadAllies = GetDeadPartyMembers().Select(x=> x.Actor).ToList();
				if (currentAction.Actor is Enemy || deadAllies.Count == 0)
				{
					BattleLogManager.Instance.QueueMessage(currentAction.Actor.Name.ToUpper() + "'s skill did nothing.");
					SetPhase(BattlePhase.WaitForBattleLog);
					return;
				}
				resolvedTargets.AddRange(deadAllies);
				break;
			case SkillTarget.AllyNotSelf:
				if (currentAction.Targets[0].CurrentState is "toast")
				{
					Actor newTarget = currentAction.Targets[0] is Enemy
						? GetRandomAliveUniqueEnemy(currentAction.Actor)
						: GetRandomUniqueAlivePartyMember(currentAction.Actor);
					if (newTarget == null)
					{
						BattleLogManager.Instance.QueueMessage(currentAction.Actor.Name.ToUpper() + "'s skill did nothing.");
						SetPhase(BattlePhase.WaitForBattleLog);
						return;
					}
					resolvedTargets.Add(newTarget);
				}
				else
					resolvedTargets.Add(currentAction.Targets[0]);
				break;
			default:
				resolvedTargets.AddRange(currentAction.Targets);
				break;
		}
		
		if (currentAction.Action is Skill skill)
		{
			if (skill.Cost > 0)
			{
				if (currentAction.Actor.CurrentJuice < skill.Cost)
				{
					BattleLogManager.Instance.QueueMessage(currentAction.Actor.Name.ToUpper() + " does not have enough JUICE!");
					SetPhase(BattlePhase.WaitForBattleLog);
					return;
				}

				currentAction.Actor.CurrentJuice -= skill.Cost;
			}
			if (currentAction.Actor is PartyMember && currentAction.Action.Name.EndsWith("Attack") && !(currentAction.Actor.CurrentState == "afraid" || currentAction.Actor.CurrentState == "stressed"))
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
						component.FadeInFollowups();
						FollowupActive = true;
					}
				}
			}
		}

		await currentAction.Action.Effect(currentAction.Actor, resolvedTargets);

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
		if (target.Actor.CurrentState == "toast")
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

		// we already checked for this but oh well
		if (skill.Target is SkillTarget.AllEnemies)
			ForceCommand(current.Actor, GetAllEnemies(), skill);
		else
			ForceCommand(current.Actor, Commands[CommandIndex].Targets, skill);
		return true;
	}

	/// <summary>
	/// Forces a skill command to be executed after the current one.
	/// </summary>
	/// <param name="self">The actor that the command is being forced upon.</param>
	/// <param name="target">The target of the command.</param>
	/// <param name="skill">The skill that is being forced.</param>
	public void ForceCommand(Actor self, Actor target, Skill skill)
	{
		ForceCommand(self, [target], skill);
	}

	/// <summary>
	/// Forces a skill command to be executed after the current one.
	/// </summary>
	/// <param name="self">The actor that the command is being forced upon.</param>
	/// <param name="targets">The targets of the command.</param>
	/// <param name="skill">The skill that is being forced.</param>
	public void ForceCommand(Actor self, IReadOnlyList<Actor> targets, Skill skill)
	{
		if (self is PartyMember && skill.Name.EndsWith("Attack"))
			// if the forced skill is an attack, hide the followup bubbles
			ForceHideFollowup = true;
		if (CommandIndex == Commands.Count)
			Commands.Add(new BattleCommand(self, targets, skill));
		else
			Commands.Insert(CommandIndex + 1, new BattleCommand(self, targets, skill));
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

	private async void EndOfTurn()
	{
		if (!ProcessedEndOfTurn)
		{
			// tick down stat turn timers
			// vanilla omori bug: stats decrease before of end of turn enemy skills
			CurrentParty.ForEach(x =>
			{
				x.Actor.DecreaseStatTurnCounter();
				x.UpdateStateIcons();
			});
			Enemies.ForEach(x => x.Actor.DecreaseStatTurnCounter());
			
			for (int i = 0; i < Enemies.Count; i++)
			{
				await Enemies[i].Actor.ProcessEndOfTurn();
			}
			ProcessedEndOfTurn = true;
		}

		// if any commands were added during the ProcessEndOfTurn, we need to run those still
		if (CommandIndex < Commands.Count)
		{
			SetPhase(BattlePhase.PreCommand);
			return;
		}

		CheckBattleOver();

		ProcessedStartOfTurn = false;
		SetPhase(BattlePhase.FightRun);
	}

	internal void OnBattleLogFinished()
	{
		if (Phase == BattlePhase.WaitForBattleLog)
			SetPhase(BattlePhase.PostCommand);
	}

	private void HandleEnemyDying()
	{
		Tween tween = CreateTween();
		tween.TweenInterval(0.75f);
		foreach (Node2D enemy in DyingEnemies)
		{
			tween.Parallel().TweenProperty(enemy, "position", enemy.Position + new Vector2(0, 400f), 0.50f);
		}
		tween.TweenCallback(Callable.From(EnemiesDoneDying));
	}

	private void EnemiesDoneDying()
	{
		DyingEnemies.ForEach(x => x.QueueFree());
		DyingEnemies.Clear();
		SetPhase(BattlePhase.PreCommand);
	}

	/// <summary>
	/// Runs a check to see if the battle is over.
	/// </summary>
	public async void CheckBattleOver()
	{
		if (Enemies.Count == 0)
		{
			SetPhase(BattlePhase.BattleOver);
			await EndOfBattle(true);
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
			await EndOfBattle(false);
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
			await EndOfBattle(false);
			BattleLogManager.Instance.ClearAndShowMessage(CurrentParty[0].Actor.Name.ToUpper() + "'s party was defeated...");
			MenuButtonContainer.Visible = true;
		}
	}

	private async Task EndOfBattle(bool victory)
	{
		foreach (PartyMemberComponent p in CurrentParty)
			await p.Actor.OnEndOfBattle(victory);
		foreach (EnemyComponent e in Enemies)
			await e.Actor.OnEndOfBattle(victory);
	}

	/// <summary>
	/// Calculates damage. Misses, critical hits, emotion effectiveness, sad juice loss, and stat modifiers are all taken into account.
	/// </summary>
	/// <remarks>
	/// On top of calculating damage, this function also handles displaying damage numbers, playing sound effects, and queuing battle log messages for misses and critical hits.
	/// </remarks>
	/// <param name="self">The attacker.</param>
	/// <param name="target">The target/defender.</param>
	/// <param name="damageFunc">The damage function to use in the damage calculation.<br/><br/>
	/// A common example is the calculation for basic attacks, as shown by this example:<br/>
	/// <c>() => self.CurrentStats.ATK * 2 - target.CurrentStats.DEF;</c></param>
	/// <param name="neverMiss">If this attack should never miss.</param>
	/// <param name="variance">The damage variance. Damage will be multiplied between (1 - variance) and (1 + variance).</param>
	/// <param name="guaranteeCrit">If this attack should guarantee a critical hit.</param>
	/// <param name="neverCrit">If this attack should never be a critical hit.</param>
	/// <param name="ignoreEmotion">If this attack should ignore emotion advantage.</param>
	/// <returns>The final damage after all critical, emotion, juice loss, and stat modifications have been applied.</returns>
	public int Damage(Actor self, Actor target, Func<float> damageFunc, bool neverMiss = true, float variance = 0.2f, bool guaranteeCrit = false, bool neverCrit = false, bool ignoreEmotion = false)
	{
		if (!neverMiss)
		{
			bool miss = self.CurrentStats.HIT < GameManager.Instance.Random.RandiRange(0, 100);
			if (miss)
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor]'s attack missed...");
				AudioManager.Instance.PlaySFX("BA_miss");
				// Miss text spawns a little further down
				SpawnDamageNumber(-1, target.CenterPoint, DamageType.Miss);
				return -1;
			}
		}
		float damage = Math.Max(0, damageFunc());
		string selfState = self.CurrentState;
		string targetState = target.CurrentState;

		if (self.HasLockedEmotion())
		{
			selfState = (self.StateStatModifier as EmotionLockStatModifier).OverrideEmotion();
		}

		// TODO: replace once plot armor is improved
		if (selfState is "plotarmor")
		{
			selfState = (self as Omori).OldEmotion;
		}

		if (target.HasLockedEmotion())
		{
			targetState = (target.StateStatModifier as EmotionLockStatModifier).OverrideEmotion();
		}

		ApplyOverrides(DamagePhase.PreEmotion, ref damage, self, target, false);
		
		int effectiveness = 0;
		if (!ignoreEmotion)
			damage = CalculateEmotionModifiers(selfState, targetState, damage, out effectiveness);
		bool critical =
			(self.CurrentStats.LCK * .01f >= GameManager.Instance.Random.Randf() || guaranteeCrit ||
			 target.HasStatModifier("Tickle")) && !neverCrit;
		
		// even though we know if the attack is a crit or not at this stage, pass false
		// this may change in the future depending on feedback and use case
		ApplyOverrides(DamagePhase.PreCrit, ref damage, self, target, false);
		
		if (critical)
		{
			damage *= 1.5f;
			BattleLogManager.Instance.QueueMessage("IT HIT RIGHT IN THE HEART!");
			AudioManager.Instance.PlaySFX("BA_CRITICAL_HIT", volume: 2f);
		}
		
		ApplyOverrides(DamagePhase.PreFlatCrit, ref damage, self, target, critical);

		if (critical)
		{
			damage += 1.5f;
		}
		
		ApplyOverrides(DamagePhase.PreVariance, ref damage, self, target, critical);

		damage = CalculateVariance(damage, variance);

		ApplyOverrides(DamagePhase.PreRounding, ref damage, self, target, critical);
		
		float rounded = (float)Math.Round(damage, MidpointRounding.AwayFromZero);
				
		if (rounded < 0)
			rounded = 0;
		if (!SettingsMenuManager.Instance.DisableDamageLimit && rounded > 9999)
			rounded = 9999;

		ApplyOverrides(DamagePhase.PreJuice, ref rounded, self, target, critical);
		
		int juiceLost = 0;
		switch (target.CurrentState)
		{
			case "miserable":
				juiceLost = (int)Math.Min(rounded, target.CurrentJuice);
				rounded -= juiceLost;
				break;
			case "depressed":
				juiceLost = Math.Min((int)Math.Floor(rounded * 0.5f), target.CurrentJuice);
				rounded -= juiceLost;
				break;
			case "sad":
				juiceLost = Math.Min((int)Math.Floor(rounded * 0.3f), target.CurrentJuice);
				rounded -= juiceLost;
				break;
		}
		target.CurrentJuice -= juiceLost;
				
		if (rounded < 0)
			rounded = 0;
		if (!SettingsMenuManager.Instance.DisableDamageLimit && rounded > 9999)
			rounded = 9999;
		
		rounded = (float)Math.Round(rounded, MidpointRounding.AwayFromZero);
		
		ApplyOverrides(DamagePhase.PreApply, ref rounded, self, target, critical);

		int roundedInt = (int)rounded;
		target.Damage(roundedInt);
		if (target is PartyMember)
		{
			Energy = Math.Min(10, Energy + 1);
		}
		SpawnDamageNumber(roundedInt, target.CenterPoint, critical: critical);
		// we don't need to play a hitsound if the attack is a critical or if there's no damage
		if (!critical && roundedInt > 0)
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
		BattleLogManager.Instance.QueueMessage(self, target, "[target] takes " + roundedInt + " damage!");
		
		if (juiceLost > 0)
		{
			BattleLogManager.Instance.QueueMessage(self, target, "[target] lost " + juiceLost + " JUICE...");
			SpawnDamageNumber(juiceLost, target.CenterPoint, DamageType.JuiceLoss);
		}
		
		ApplyOverrides(DamagePhase.PostApply, ref rounded, self, target, critical);

		return roundedInt;
	}

	private void ApplyOverrides(DamagePhase phase, ref float damage, Actor attacker, Actor defender, bool isCritical)
	{
		foreach (StatModifier mod in attacker.StatModifiers.Values)
			mod.OverrideDamage(phase, ref damage, attacker, defender, true, isCritical);
		foreach (StatModifier mod in defender.StatModifiers.Values)
			mod.OverrideDamage(phase, ref damage, attacker, defender, false, isCritical);
	}

	/// <summary>
	/// Calculates juice damage. Misses, critical hits, emotion effectiveness, and stat modifiers are all taken into account. Sadness damage reduction, however, is not.
	/// </summary>
	/// /// <remarks>
	/// Unlike <see cref="Damage(Actor, Actor, Func{float}, bool, float, bool, bool, bool)"/>, this method does not play hit sounds, however it does display damage numbers and queues the battle log.
	/// </remarks>
	/// <param name="self">The attacker.</param>
	/// <param name="target">The target/defender.</param>
	/// <param name="damageFunc">The damage function to use in the damage calculation.</param>
	/// <param name="neverMiss">If this attack should never miss.</param>
	/// <param name="variance">The damage variance. Damage will be multiplied between (1 - variance) and (1 + variance).</param>
	/// <param name="guaranteeCrit">If this attack should guarantee a critical hit.</param>
	/// <param name="neverCrit">If this attack should never be a critical hit.</param>
	/// <param name="ignoreEmotion">If this attack should ignore emotion advantage.</param>
	/// <returns>The final juice damage after all critical, emotion, and stat modifications have been applied.</returns>
	public int DamageJuice(Actor self, Actor target, Func<float> damageFunc, bool neverMiss = true, float variance = 0.2f, bool guaranteeCrit = false, bool neverCrit = false, bool ignoreEmotion = false)
	{
		if (!neverMiss)
		{
			bool miss = self.CurrentStats.HIT < GameManager.Instance.Random.RandiRange(0, 100);
			if (miss)
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor]'s attack missed...");
				AudioManager.Instance.PlaySFX("BA_miss");
				// Miss text spawns a little further down
				SpawnDamageNumber(-1, target.CenterPoint, DamageType.Miss);
				return -1;
			}
		}
		float damage = damageFunc();
		string selfState = self.CurrentState;
		string targetState = target.CurrentState;

		if (self.HasLockedEmotion())
		{
			selfState = (self.StateStatModifier as EmotionLockStatModifier).OverrideEmotion();
		}
		
		// TODO: replace once plot armor is improved
		if (selfState is "plotarmor")
		{
			selfState = (self as Omori).OldEmotion;
		}

		if (target.HasLockedEmotion())
		{
			targetState = (target.StateStatModifier as EmotionLockStatModifier).OverrideEmotion();
		}
		
		ApplyOverrides(DamagePhase.PreEmotion, ref damage, self, target, false);
		
		damage = CalculateEmotionModifiers(selfState, targetState, damage, out _);
		bool critical =
			(self.CurrentStats.LCK * .01f >= GameManager.Instance.Random.Randf() || guaranteeCrit ||
			 target.HasStatModifier("Tickle")) && !neverCrit;
		
		ApplyOverrides(DamagePhase.PreCrit, ref damage, self, target, false);
		
		if (critical)
		{
			damage *= 1.5f;
			BattleLogManager.Instance.QueueMessage("IT HIT RIGHT IN THE HEART!");
			AudioManager.Instance.PlaySFX("BA_CRITICAL_HIT", volume: 2f);
		}
		
		ApplyOverrides(DamagePhase.PreFlatCrit, ref damage, self, target, critical);

		if (critical)
		{
			damage += 1.5f;
		}
		
		ApplyOverrides(DamagePhase.PreVariance, ref damage, self, target, critical);

		damage = CalculateVariance(damage, variance);

		ApplyOverrides(DamagePhase.PreRounding, ref damage, self, target, critical);
		
		float rounded = (float)Math.Round(damage, MidpointRounding.AwayFromZero);
				
		if (rounded < 0)
			rounded = 0;
		if (!SettingsMenuManager.Instance.DisableDamageLimit && rounded > 9999)
			rounded = 9999;
		
		ApplyOverrides(DamagePhase.PreJuice, ref rounded, self, target, critical);
		ApplyOverrides(DamagePhase.PreApply, ref rounded, self, target, critical);
		
		int roundedInt = (int)rounded;
		target.DamageJuice(roundedInt);
		SpawnDamageNumber(roundedInt, target.CenterPoint, DamageType.JuiceLoss);
		BattleLogManager.Instance.QueueMessage(self, target, "[target] lost " + roundedInt + " JUICE...");
		ApplyOverrides(DamagePhase.PostApply, ref rounded, self, target, critical);
		return roundedInt;
	}

	// some healing and juice skills are affected by emotion

	/// <summary>
	/// Calculates emotion-based healing to the <paramref name="target"/>.
	/// </summary>
	/// <remarks>
	/// Some healing in OMORI is "bugged" and is influenced by emotion, which this method replicates.
	/// </remarks>
	/// <param name="self">The healer.</param>
	/// <param name="target">The target being healed.</param>
	/// <param name="healFunc">The function to use in the heal calculation.</param>
	/// <param name="variance">The healing variance. Healed HP will be multiplied between (1 - variance) and (1 + variance).</param>
	public void Heal(Actor self, Actor target, Func<float> healFunc, float variance = 0.2f)
	{
		float baseHealing = healFunc();
		baseHealing = CalculateEmotionModifiers(self.CurrentState, target.CurrentState, baseHealing, out _);
		baseHealing = CalculateVariance(baseHealing, variance);
		int rounded = (int)Math.Round(baseHealing, MidpointRounding.AwayFromZero);
		target.Heal(rounded);
		SpawnDamageNumber(rounded, target.CenterPoint, DamageType.Heal);
		BattleLogManager.Instance.QueueMessage(self, target, $"[target] recovered {rounded} HEART!");
	}

	/// <summary>
	/// Calculates emotion-based juice healing to the <paramref name="target"/>.
	/// </summary>
	/// /// <remarks>
	/// Some juice healing in OMORI is "bugged" and is influenced by emotion, which this method replicates.
	/// </remarks>
	/// <param name="self">The healer.</param>
	/// <param name="target">The target being healed.</param>
	/// <param name="healFunc">The healing variance. Healed Juice will be multiplied between (1 - variance) and (1 + variance).</param>
	public void HealJuice(Actor self, Actor target, Func<float> healFunc)
	{
		float baseJuice = healFunc();
		float finalJuice = CalculateEmotionModifiers(self.CurrentState, target.CurrentState, baseJuice, out _);
		int rounded = (int)Math.Round(finalJuice, MidpointRounding.AwayFromZero);
		target.HealJuice(rounded);
		SpawnDamageNumber(rounded, target.CenterPoint, DamageType.JuiceGain);
		BattleLogManager.Instance.QueueMessage(self, target, $"[target] recovered {rounded} JUICE!");
	}

	// RPGMaker applyVariance method
	private float CalculateVariance(float damage, float variance)
	{
		int amp = (int)Math.Floor(Math.Max(Math.Abs(damage) * variance, 0));
		int v = GameManager.Instance.Random.RandiRange(0, amp) + GameManager.Instance.Random.RandiRange(0, amp) - amp;
		return damage + v;
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
		// exploit emotion type
		if (self == "emotion" && target != "neutral")
		{
			effect = 1;
			if (target == "afraid")
				return damage * 1.5f;
			return damage * weakness[GetEmotionTier(target)];
		}

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

	/// <summary>
	/// Gives the provided <see cref="Actor"/> a random emotion.<br/>If the actor already has that emotion, it will be upgraded.
	/// </summary>
	/// <param name="who"></param>
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

	/// <summary>
	/// Spawns a damage number at the specified <paramref name="position"/>.
	/// </summary>
	/// <remarks>
	/// If a damage number already exists at the given <paramref name="position"/>, it will be moved down until an empty space is found<br/>
	/// This can be useful to spawn multiple damage numbers without having to calculate offsets yourself.
	/// </remarks>
	/// <param name="damage">The number to display.</param>
	/// <param name="position">The screen position to spawn the damage number at.</param>
	/// <param name="type">The <see cref="DamageType"/> of the damage. This value will modify the color of the damage number.</param>
	/// <param name="critical">If true, the damage number will blink red when spawned.</param>
	public void SpawnDamageNumber(int damage, Vector2 position, DamageType type = DamageType.Damage, bool critical = false)
	{
		if (DamageNumbersDisabled)
			return;
		
		DamageNumber dmg = new(damage, type, critical);
		while (DamageNumbers.Contains(position))
		{
			position.Y += 40;
		}
		dmg.Position = position;
		AddChild(dmg);
		DamageNumbers.Add(position);

		GetTree().CreateTimer(1.5f).Timeout += () =>
		{
			dmg.Despawn();
			DamageNumbers.Remove(position);
		};
	}
	
	/// <summary>
	/// Spawns an enemy at the given screen <paramref name="position"/>.
	/// </summary>
	/// <param name="who">Which enemy to spawn.</param>
	/// <param name="position">The screen position to spawn the enemy at. The enemy will spawn centered at this position.</param>
	/// <param name="startingEmotion">The enemy's starting emotion.</param>
	/// <param name="fallsOffScreen">Whether this enemy should fall off-screen when defeated.</param>
	/// <param name="layer">The layer to spawn this enemy on.</param>
	/// <returns>The <see cref="EnemyComponent"/> of the spawned enemy.</returns>
	public EnemyComponent SummonEnemy(string who, Vector2 position, string startingEmotion = "neutral", bool fallsOffScreen = true, int layer = 0)
	{
		while (Enemies.Any(x => x.Actor.CenterPoint == position))
		{
			// if this enemy is going to spawn on top of another, nudge them a little
			// this way the targeting system still works
			position.X += 0.1f;
		}
		
		EnemyComponent enemy = GameManager.Instance.SpawnEnemy(who, position, startingEmotion, fallsOffScreen, layer);
		Enemies.Add(enemy);
		return enemy;
	}
	
	/// <summary>
	/// Adds the given <paramref name="amount"/> to the energy bar, up to a maximum of 10.
	/// </summary>
	/// <param name="amount">The amount of energy to add.</param>
	public void AddEnergy(int amount)
	{
		Energy = Math.Min(Energy + amount, 10);
	}

	/// <returns>A random alive <see cref="PartyMember"/>.</returns>
	public PartyMember GetRandomAlivePartyMember()
	{
		IEnumerable<PartyMemberComponent> alive = CurrentParty.Where(x => x.Actor.CurrentHP > 0);
		return alive.ElementAt(GameManager.Instance.Random.RandiRange(0, alive.Count() - 1)).Actor;
	}
	
	/// <returns>Returns a random alive <see cref="PartyMember"/> that's not the provided actor.</returns>
	public PartyMember GetRandomUniqueAlivePartyMember(Actor not)
	{
		IEnumerable<PartyMemberComponent> alive = CurrentParty.Where(x => x.Actor.CurrentHP > 0 && x.Actor != not);
		return alive.ElementAt(GameManager.Instance.Random.RandiRange(0, alive.Count() - 1)).Actor;
	}

	/// <returns>A random <see cref="PartyMember"/> that is toast.</returns>
	public PartyMember GetRandomDeadPartyMember()
	{
		PartyMemberComponent result = CurrentParty.FirstOrDefault(x => x.Actor.CurrentHP <= 0);
		return result?.Actor;
	}

	/// <returns>A random alive <see cref="Enemy"/>.</returns>
	public Enemy GetRandomAliveEnemy()
	{
		IEnumerable<EnemyComponent> alive = Enemies.Where(x => x.Actor.CurrentHP > 0);
		return alive.ElementAt(GameManager.Instance.Random.RandiRange(0, alive.Count() - 1)).Actor;
	}

	/// <returns>A random alive <see cref="Enemy"/> that's not the provided actor.</returns>
	public Enemy GetRandomAliveUniqueEnemy(Actor not)
	{
		IEnumerable<EnemyComponent> alive = Enemies.Where(x => x.Actor.CurrentHP > 0 && x.Actor != not);
		return alive.ElementAt(GameManager.Instance.Random.RandiRange(0, alive.Count() - 1)).Actor;
	}

	/// <returns>All currently alive <see cref="Enemy"/>s.</returns>
	public List<Enemy> GetAllEnemies()
	{
		return Enemies.Select(x => x.Actor).ToList();
	}

	/// <returns>The <see cref="BattleCommand"/> that is currently being processed.</returns>
	public BattleCommand GetCurrentCommand()
	{
		if (CommandIndex < 0 || CommandIndex >= Commands.Count)
			return null;
		return Commands[CommandIndex];
	}

	/// <summary>
	/// Gets the <see cref="PartyMemberComponent"/> of all party members who are not toast.
	/// </summary>
	public List<PartyMemberComponent> GetAlivePartyMembers()
	{
		return CurrentParty.Where(x => x.Actor.CurrentHP > 0).ToList();
	}

	/// <summary>
	/// Gets the <see cref="PartyMemberComponent"/> of all party members who are currently toast.
	/// </summary>
	public List<PartyMemberComponent> GetDeadPartyMembers()
	{
		return CurrentParty.Where(x => x.Actor.CurrentState == "toast").ToList();
	}

	/// <summary>
	/// Gets all party members, including ones who are toast.
	/// </summary>
	/// <remarks>
	/// In most situations, such as skill logic, use <see cref="GetAlivePartyMembers"/> instead.
	/// </remarks>
	public List<PartyMemberComponent> GetAllPartyMembers()
	{
		return CurrentParty;
	}

	/// <summary>
	/// Retrieves the <see cref="PartyMember"/> at the given <paramref name="index"/> in the party.
	/// </summary>
	/// <remarks>
	/// Valid <paramref name="index"/> values include 0 (Bottom Left), 1 (Top Left), 2 (Bottom Right), and 3 (Top Right).
	/// </remarks>
	/// <param name="index"></param>
	public PartyMember GetPartyMember(int index)
	{
		// eh who needs bounds checks these days
		return CurrentParty[Math.Clamp(index, 0, 3)].Actor;
	}

	/// <summary>
	/// Retrieves the <see cref="PartyMember"/> who is currently selecting their action.
	/// </summary>
	/// <returns>The <see cref="PartyMember"/> who is currently selecting their action, otherwise null.</returns>
	public PartyMember GetCurrentPartyMember()
	{
		return CurrentParty.ElementAtOrDefault(CurrentPartyMember)?.Actor;
	}

	/// <summary>
	/// Adds an item to the party's inventory.
	/// </summary>
	/// <param name="name">The database name of the item.</param>
	/// <param name="quantity">The item quantity to give.</param>
	public void AddItem(string name, int quantity)
	{
		if (!Database.TryGetItem(name, out Item item))
		{
			GD.PrintErr("Unknown item: " + name);
			return;
		}
		if (!Items.TryAdd(name, quantity))
			Items[name] += quantity;
	}

	/// <summary>
	/// Retrieves all snacks in the inventory, as well as their quantities.
	/// </summary>
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

	/// <summary>
	/// Retrieves all toys in the inventory, as well as their quantities.
	/// </summary>
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

	// converts item name to CamelCase for dictionary lookup
	private string CaptializeItemName(Item item)
	{
		// Godot's Captialize treats '-' as a regular character and puts a space after it
		// manually fix that for sno-cone
		return item.Name == "SNO-CONE" ? "Sno-Cone" : item.Name.Capitalize();
	}
}

internal enum BattlePhase
{
	PreBattle,
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
