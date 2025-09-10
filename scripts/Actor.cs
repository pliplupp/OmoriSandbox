using Godot;
using System;
using System.Collections.Generic;
public abstract class Actor
{
	public event EventHandler OnStateChanged;
	public event EventHandler OnHPChanged;
	public event EventHandler OnJuiceChanged;

	public abstract string Name { get; }
	public AnimatedSprite2D Sprite;
	public Vector2 CenterPoint = Vector2.Zero;
	public string CurrentState;
	public Dictionary<string, Skill> Skills = [];
	public bool IsHurt = false;
	public int Level = 1;

	public Stats BaseStats;

	public Dictionary<string, StatModifier> StatModifiers = [];
	public StatModifier StateStatModifier { get; private set; } = new StatModifier([]);

	private int _CurrentHP = 0;
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
	public int CurrentJuice
	{
		get { return _CurrentJuice; }
		set
		{
			_CurrentJuice = value;
			OnJuiceChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	protected virtual Stats GetBaseStats() { return BaseStats; }

	/// <summary>
	/// The Actor's base stats, any adjusted stats from weapons or buffs, and emotion stats.
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

	public void AddStatModifier(string modifier, bool silent = false)
	{
		StatModifier mod = Database.CreateModifier(modifier);
		if (mod == null)
		{
			GD.PrintErr("Unknown stat modifier: " + modifier);
			return;
		}
		if (mod is TierStatModifier && StatModifiers.TryGetValue(modifier, out StatModifier m))
		{
			TierStatModifier existing = m as TierStatModifier;
			bool success = existing.IncreaseTier();
			if (!silent)
			{
				ShowStatMessage(modifier, success ? existing.SuccessMessage : existing.FailureMessage);
			}
			return;
		}
		StatModifiers.Add(modifier, mod);
	}

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
			bool success = existing.IncreaseTier();
			if (!silent)
			{
				ShowStatMessage(modifier, success ? existing.SuccessMessage : existing.FailureMessage);
			}
			return;
		}
		t.SetTier(tier);
		t.SetTurnsLeft(turns);
		StatModifiers.Add(modifier, t);
	}

	public void RemoveStatModifier(string modifier)
	{
		StatModifiers.Remove(modifier);
	}

	private void ShowStatMessage(string modifier, string message)
	{
		BattleLogManager.Instance.QueueMessage($"{Name.ToUpper()}'s {modifier} {message}");
	}

	public void DecreaseStatTurnCounter()
	{
		foreach (var mod in StatModifiers)
		{
			if (this is Omori omori && mod.Key == "PlotArmor")
			{
				// set the emotion background back to what it was before
				SetState(omori.OldEmotion, true);
				omori.OldEmotion = null;
			}
			if (mod.Value is TierStatModifier tier)
			{
				tier.DecreaseTurns();
				if (tier.TurnsLeft <= 0)
				{
					StatModifiers.Remove(mod.Key);
					continue;
				}
			}
		}

	}

	public bool HasStatModifier(string modifier)
	{
		return StatModifiers.ContainsKey(modifier);
	}

	public bool HasLockedEmotion()
	{
		return StateStatModifier is EmotionLockStatModifier;
	}

	public void Damage(int damage)
	{
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
			AddTierStatModifier("PlotArmor", 1, 1);
			omori.HasUsedPlotArmor = true;
		}
	}

	public void Heal(int health)
	{
		CurrentHP += health;
		if (CurrentHP > CurrentStats.MaxHP)
			CurrentHP = CurrentStats.MaxHP;
	}

	public void HealJuice(int juice)
	{
		CurrentJuice += juice;
		if (CurrentJuice > CurrentStats.MaxJuice)
			CurrentJuice = CurrentStats.MaxJuice;
	}


	public void SetHurt(bool hurt)
	{
		Sprite.Animation = hurt ? "hurt" : CurrentState;
		IsHurt = hurt;
	}

	public virtual bool IsStateValid(string state) { return true; }

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


	// forces a state without any validity checks
	// mainly used for bosses like Sweetheart
	public void ForceState(string state, bool silent = false)
	{
		Sprite.Animation = state;
		CurrentState = state;
		StatModifier mod = Database.CreateModifier(Capitalize(CurrentState));
		if (mod != null)
		{
			StateStatModifier = mod;
		}
		if (!silent)
			BattleLogManager.Instance.QueueMessage(Name.ToUpper() + " became " + state.Replace("se_", "").ToUpper() + "!");
	}

	private string Capitalize(string s)
	{
		// using ToCharArray avoids an extra string allocation
		char[] a = s.ToCharArray();
		a[0] = char.ToUpper(a[0]);
		return new string(a);
	}
}
