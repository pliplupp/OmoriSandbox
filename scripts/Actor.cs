using Godot;
using OmoriSandbox.Battle;
using OmoriSandbox.Battle.Modifier;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OmoriSandbox.Actors;

/// <summary>
/// An generic actor. See <see cref="PartyMember"/> and <see cref="Enemy"/>.
/// </summary>
public abstract class Actor
{
	/// <summary>
	/// Fired whenever the actor's state (emotion) changes.
	/// </summary>
	public event EventHandler OnStateChanged;
	/// <summary>
	/// Fired whenever the actor's HP changes.
	/// </summary>
	public event EventHandler OnHPChanged;
	/// <summary>
	/// Fired whenever the actor's Juice changes.
	/// </summary>
	public event EventHandler OnJuiceChanged;

	/// <summary>
	/// The name of the actor.
	/// </summary>
	public abstract string Name { get; }
	/// <summary>
	/// The actor's sprite.
	/// </summary>
	public AnimatedSprite2D Sprite;
	/// <summary>
	/// The center point of the actor, calculated by the center of its sprite.
	/// </summary>
	public Vector2 CenterPoint = Vector2.Zero;
	/// <summary>
	/// The actor's current state (emotion).
	/// </summary>
	public string CurrentState;
	/// <summary>
	/// The skills the actor has equipped.
	/// </summary>
	public Dictionary<string, Skill> Skills = [];

	public Stats BaseStats;

    /// <summary>
    /// The actor's level. Mainly only used for <see cref="PartyMember"/>s.
    /// </summary>
    public int Level { get; protected set; } = 1;

	/// <summary>
	/// The <see cref="StatModifier"/>s the actor currently has.
	/// </summary>
	public Dictionary<string, StatModifier> StatModifiers = [];
	/// <summary>
	/// The <see cref="StatModifier"/> that the actor's emotion gives.
	/// </summary>
	public StatModifier StateStatModifier { get; private set; } = new StatModifier([]);

	private int _CurrentHP = 0;
	/// <summary>
	/// The actor's current HP. Updating this value will fire <see cref="OnHPChanged"/>.
	/// </summary>
	public int CurrentHP
	{
		get { return _CurrentHP; }
		set
		{
			_CurrentHP = value;
			OnHPChanged?.Invoke(this, EventArgs.Empty);
		}
	} 

	private int _CurrentJuice = 0;
    /// <summary>
    /// The actor's current Juice. Updating this value will fire <see cref="OnJuiceChanged"/>.
    /// </summary>
    public int CurrentJuice
	{
		get { return _CurrentJuice; }
		set
		{
			_CurrentJuice = value;
			OnJuiceChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <summary>
	/// The actor's base stats without any modifiers.
	/// </summary>
	/// <returns></returns>
	protected virtual Stats GetBaseStats() { return BaseStats; }

	/// <summary>
	/// The Actor's base stats, any adjusted stats from equips or modifiers, and emotion stats.
	/// </summary>
	public Stats CurrentStats
	{
		get
		{
			Stats current = GetBaseStats();

			StateStatModifier.ApplyStats(ref current);

			foreach (StatModifier mod in StatModifiers.Values)
			{
				mod.ApplyStats(ref current);
			}

			return current;
		}
	}

	/// <summary>
	/// Adds a new <see cref="StatModifier"/> to this actor.
	/// </summary>
	/// <param name="modifier">The name of the modifier to add.</param>
	/// <param name="silent">If true, success/failure messages will not be logged.</param>
	public void AddStatModifier(string modifier, bool silent = false)
	{
		if (StatModifiers.TryGetValue(modifier, out StatModifier m))
		{
			if (m is TierStatModifier tier)
			{
				bool success = tier.IncreaseTier();
				if (success)
				{
                    GD.Print("Increased tier of " + modifier + " on " + Name + " to " + tier.CurrentTier);
                }
				if (!silent && tier.SuccessMessage != null)
					ShowStatMessage(success ? tier.SuccessMessage : tier.FailureMessage);
			}
			// if the actor already has the modifier and it's not tiered, do nothing
		}
		else
		{
			StatModifier mod = Database.CreateModifier(modifier);
			if (mod == null)
			{
				GD.PrintErr("Unknown stat modifier: " + modifier);
				return;
			}
			StatModifiers.Add(modifier, mod);
			mod.OnAdd();
			GD.Print("Added modifier " + modifier + " to " + Name);
			if (mod is TierStatModifier t && t.SuccessMessage != null && !silent)
				ShowStatMessage(t.SuccessMessage);
		}
	}

    /// <summary>
    /// Adds a new <see cref="TierStatModifier"/> to this actor.
    /// </summary>
    /// <param name="modifier">The name of the tier modifier to add.</param>
    /// <param name="tier">The tier that this modifier will start at.</param>
    /// <param name="turns">The number of turns left the modifier will start at.</param>
    /// <param name="silent">If true, success/failure messages will not be logged.</param>
    public void AddTierStatModifier(string modifier, int tier = 1, int turns = 6, bool silent = false)
	{
		StatModifier mod = Database.CreateModifier(modifier);
		if (mod is not TierStatModifier t)
		{
			GD.PushWarning("Tried to add a non-tiered stat modifier with tier and turns: " + modifier);
			AddStatModifier(modifier, silent);
			return;
		}
		if (StatModifiers.TryGetValue(modifier, out StatModifier m))
		{
			TierStatModifier existing = m as TierStatModifier;
			bool success = existing.SetTier(tier);
			if (success)
			{
                GD.Print("Increased tier of " + modifier + " on " + Name + " to " + existing.CurrentTier);
                existing.SetTurnsLeft(turns);
			}
			if (!silent && existing.SuccessMessage != null)
			{
				ShowStatMessage(success ? existing.SuccessMessage : existing.FailureMessage);
			}
			return;
		}
		t.SetTier(tier);
		t.SetTurnsLeft(turns);
		StatModifiers.Add(modifier, t);
		t.OnAdd();
		GD.Print("Added modifier " + modifier + " to " + Name);
		if (!silent && t.SuccessMessage != null)
			ShowStatMessage(t.SuccessMessage);
	}

	/// <summary>
	/// Removes a <see cref="StatModifier"/> of the given name from this actor.
	/// </summary>
	/// <param name="modifier">The name of the modifier to remove.</param>
	public void RemoveStatModifier(string modifier)
	{
		StatModifiers.Remove(modifier);
	}

	/// <summary>
	/// Removes all <see cref="StatModifier"/>s from this actor.
	/// </summary>
	public void RemoveAllStatModifiers()
	{
		StatModifiers.Clear();
	}

	private void ShowStatMessage(string message)
	{
		BattleLogManager.Instance.QueueMessage($"{Name.ToUpper()}'s {message}");
	}

	internal void DecreaseStatTurnCounter()
	{
		foreach (var mod in StatModifiers)
		{
			if (this is Omori omori && mod.Key == "PlotArmor")
			{
				// set the emotion background back to what it was before
				SetState(omori.OldEmotion, true);
				omori.OldEmotion = null;
				StatModifiers.Remove(mod.Key);
			}
			if (mod.Value.TurnsLeft != -1)
			{
				mod.Value.DecreaseTurns();
				if (mod.Value.TurnsLeft == 0)
				{
					GD.Print("Removed modifier " + mod.Key + " from " + Name);
					StatModifiers.Remove(mod.Key);
					continue;
				}
			}
		}

	}

	/// <summary>
	/// Checks if this actor has a certain <see cref="StatModifier"/>.
	/// </summary>
	/// <param name="modifier">The name of the modifier to check for.</param>
	/// <returns>True if the actor has the given <paramref name="modifier"/>.</returns>
	public bool HasStatModifier(string modifier)
	{
		return StatModifiers.ContainsKey(modifier);
	}

	/// <summary>
	/// Checks if this actor currently has a locked emotion.
	/// </summary>
	/// <returns>True if the actor's <see cref="StateStatModifier"/> is a <see cref="EmotionLockStatModifier"/>.</returns>
	public bool HasLockedEmotion()
	{
		return StateStatModifier is EmotionLockStatModifier;
	}

	/// <summary>
	/// Damages this actor by the given amount.
	/// </summary>
	/// <remarks>
	/// Negative values should not be used. See <see cref="Heal(int)"/> for healing actors.
	/// </remarks>
	/// <param name="damage">The amount of damage to deal to this actor.</param>
	public void Damage(int damage)
	{
		if (damage < 0)
			return;

		CurrentHP -= damage;
		if (CurrentHP < 0)
			CurrentHP = 0;
		SetHurt(true);

		// TODO: allow other characters to have plot armor if desired
		if (this is Omori omori && CurrentHP == 0 && !omori.HasUsedPlotArmor)
		{
			CurrentHP = 1;
			SetHurt(false);
			// keep track of omori's old emotion for when plot armor expires
			omori.OldEmotion = omori.CurrentState;
			SetState("plotarmor", true);
			AddStatModifier("PlotArmor");
			omori.HasUsedPlotArmor = true;
		}
	}

	/// <summary>
	/// Heals this actor by the given amount.
	/// </summary>
	/// <param name="health">The amount of health to heal.</param>
	public void Heal(int health)
	{
		CurrentHP += health;
		if (CurrentHP > CurrentStats.MaxHP)
			CurrentHP = CurrentStats.MaxHP;
	}

	/// <summary>
	/// Heals this actor's juice by the given amount.
	/// </summary>
	/// <param name="juice">The amount of juice to heal.</param>
	public void HealJuice(int juice)
	{
		CurrentJuice += juice;
		if (CurrentJuice > CurrentStats.MaxJuice)
			CurrentJuice = CurrentStats.MaxJuice;
	}

	/// <summary>
	/// Makes this actor appear visually hurt. Removed at the end of turn.
	/// </summary>
	/// <param name="hurt">Whether or not this actor should appear hurt.</param>
	public void SetHurt(bool hurt)
	{
		Sprite.Animation = hurt ? "hurt" : CurrentState;
	}

	/// <summary>
	/// Checks if this actor can feel the given state (emotion).
	/// </summary>
	/// <param name="state">The emotion to check.</param>
	/// <returns>True if this actor can feel the given <paramref name="state"/>.</returns>
	public virtual bool IsStateValid(string state) { return true; }

    /// <summary>
    /// Sets this actor's state to the given state (emotion). Will fail and log a battle message if the actor cannot feel the given <paramref name="state"/>.<br/>
    /// See <see cref="IsStateValid(string)"/>.
    /// </summary>
    /// <param name="state">The emotion to set this actor to.</param>
    /// <param name="silent">If true, success/failure messages will not be logged.</param>
    public void SetState(string state, bool silent = false)
	{
		if (IsStateValid(state))
		{
			Sprite.Animation = state;
			CurrentState = state;
			if (!silent)
			{
				BattleLogManager.Instance.QueueMessage(Name.ToUpper() + " feels " + state.ToUpper() + "!");
			}

			// kinda dumb but the rest of the modifiers are capitalized so whatever
			StatModifier mod = Database.CreateModifier(Capitalize(CurrentState));
			if (mod != null)
			{
				StateStatModifier = mod;
			}

			OnStateChanged?.Invoke(this, EventArgs.Empty);

			// bug fix for when omori changes state during the plot armor turn
			// this REAALLLY needs to be handled better soon...
			if (this is Omori omori && omori.OldEmotion != null && state != "plotarmor")
			{
				omori.OldEmotion = state;
			}
		}
		else
		{
			BattleLogManager.Instance.QueueMessage(Name.ToUpper() + " cannot be " + state.ToUpper() + "!");
		}
	}


	/// <summary>
	/// Forces this actor to have a state (emotion) without any validity checks. Mainly used for bosses like Sweetheart. Should be used sparingly.
	/// </summary>
	/// <param name="state">The emotion to force this actor to have.</param>
	/// <param name="fakeState">If set, the actor will feel the <paramref name="fakeState"/> but use the stats of the <paramref name="state"/>.</param>
	public void ForceState(string state, string fakeState = null)
	{
		// TODO: attach emotion/animation info to non-emotion modifiers, like boss specific emotions
		if (fakeState != null)
		{
			Sprite.Animation = fakeState;
			CurrentState = fakeState;
		}
		else
		{
			Sprite.Animation = state;
			CurrentState = state;
		}
		StatModifier mod = Database.CreateModifier(Capitalize(state));
		if (mod != null)
		{
			StateStatModifier = mod;
		}
	}
	/// <summary>
	/// Called at the very start of the battle.
	/// </summary>
	public virtual async Task OnStartOfBattle() { await Task.CompletedTask; }
	/// <summary>
	/// Called when the battle is over, but before the victory screen.
	/// </summary>
	/// <param name="victory">Whether or not the battle was won by the player.</param>
	public virtual async Task OnEndOfBattle(bool victory) { await Task.CompletedTask; }

	private string Capitalize(string s)
	{
		// using ToCharArray avoids an extra string allocation
		char[] a = s.ToCharArray();
		a[0] = char.ToUpper(a[0]);
		return new string(a);
	}
}
