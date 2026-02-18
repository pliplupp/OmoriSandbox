using System;
using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmoriSandbox.Battle;
using OmoriSandbox.Battle.Modifier;

namespace OmoriSandbox.Actors;

// TODO: Remaining enemies
// Humphrey - done?
// Faraway Town Enemies
// Omori

/* TODO: Alt enemies
- Kite Kid - done?
- Ye Old Sprout - done
- Download Window - done
- Space Ex-Bf - done
- Earth - done
- King Crawler - done
- Sir Maximus I/II/III
- Sweetheart - done
- Unbread Twins - done
- Mr Jawsum + Gator Guys - done
- Pluto (Expanded) - done?
- Slime Girls - done

!!! Fix HP requirements on alt enemies !!!
*/

/// <summary>
/// An <see cref="Actor"/> that is considered an enemy. Can be inherited to create a new enemy.
/// </summary>
public abstract class Enemy : Actor
{
	internal void Init(AnimatedSprite2D sprite, string initialState, bool fallsOffScreen, int layer)
	{
		SpriteFrames animation = Animation;
		if (animation == null)
		{
			GD.PrintErr("Failed to load Sprite animations for Enemy: " + Name);
			return;
		}
		// init animation
		Sprite = sprite;
		Sprite.SpriteFrames = animation;
		Sprite.Animation = initialState;
		Sprite.Play();
		CurrentState = initialState;
		BaseStats = Stats;
		CurrentHP = BaseStats.HP;
		CurrentJuice = BaseStats.Juice;

		FallsOffScreen = fallsOffScreen;
		Layer = layer;

		foreach (string s in EquippedSkills)
		{
			if (Database.TryGetSkill(s, out var skill))
			{
				Skills.Add(s, skill);
				continue;
			}
			GD.PrintErr("Unknown skill: " + s);
		}
	}

	/// <summary>
	/// Selects a target. Mainly used in <see cref="ProcessAI"/> for single-target skills. Can be overriden for custom targeting behavior.
	/// </summary>
	/// <remarks>This only includes alive party members by default.</remarks>
	/// <returns>The <see cref="PartyMember"/> that will be targeted.</returns>
	protected virtual PartyMember SelectTarget()
	{
		if (HasStatModifier("Charm"))
			return (StatModifiers["Charm"] as CharmStatModifier).CharmedBy;
		List<PartyMemberComponent> members = BattleManager.Instance.GetAlivePartyMembers();
		List<PartyMemberComponent> taunting = members.FindAll(x => x.Actor.HasStatModifier("Taunt"));
		if (taunting.Count == 0)
		{
			// if nobody is taunting, pick a random target
			return members[GameManager.Instance.Random.RandiRange(0, members.Count - 1)].Actor;
		}
		return taunting[GameManager.Instance.Random.RandiRange(0, taunting.Count - 1)].Actor;
	}

	/// <summary>
	/// Selects X random targets. Can include duplicates. Mainly used in <see cref="ProcessAI"/> for multi-target skills. Can be overriden for custom targeting behavior.
	/// </summary>
	/// <param name="amount">The amount of targets to select.</param>
	/// <remarks>This only includes alive party members by default. </remarks>
	/// <returns>The <see cref="PartyMember"/>s that will be targeted.</returns>
	protected virtual IReadOnlyList<PartyMember> SelectTargets(int amount)
	{
		List<PartyMemberComponent> targets = BattleManager.Instance.GetAlivePartyMembers();
		List<PartyMember> result = [];
		for (int i = 0; i < Math.Min(amount, targets.Count); i++)
			result.Add(targets[GameManager.Instance.Random.RandiRange(0, targets.Count - 1)].Actor);
		return result;
	}

	/// <summary>
	/// Selects all targets. Mainly used in <see cref="ProcessAI"/> for multi-target skills. Can be overriden for custom targeting behavior.
	/// </summary>
	/// <remarks>This only includes alive party members by default.</remarks>
	/// <returns>The <see cref="PartyMember"/>s that will be targeted.</returns>
	protected virtual IReadOnlyList<PartyMember> SelectAllTargets()
	{
		return BattleManager.Instance.GetAlivePartyMembers().Select(x => x.Actor).ToList();
	}

	/// <summary>
	/// Selects an enemy target. Mainly used in <see cref="ProcessAI"/> for single-target skills that target an enemy. Can be overriden for custom targeting behavior.<br/>
	/// <remarks>For targeting party members, use <see cref="SelectTarget"/>.</remarks>
	/// </summary>
	/// <returns>The <see cref="Enemy"/> that will be targeted.</returns>
	protected virtual Enemy SelectEnemy()
	{
		return BattleManager.Instance.GetRandomAliveEnemy();
	}

	/// <summary>
	/// Selects all enemy targets. Mainly used in <see cref="ProcessAI"/> for multi-target skills that target all enemies. Can be overriden for custom targeting behavior.
	/// </summary>
	/// <remarks>For targeting all party members, use <see cref="SelectAllTargets"/>.</remarks>
	/// <returns></returns>
	protected virtual IReadOnlyList<Enemy> SelectAllEnemies()
	{
		return BattleManager.Instance.GetAllEnemies();
	}

	/// <summary>
	/// Rolls a number between 0 and 100 (inclusive). Mainly a helper function for calculating skill chances in <see cref="ProcessAI"/>
	/// </summary>
	/// <returns></returns>
	protected int Roll()
	{
		return GameManager.Instance.Random.RandiRange(0, 100);
	}

	/// <summary>
	/// The enemy's stats.
	/// </summary>
	protected abstract Stats Stats { get; }
	/// <summary>
	/// A list of skills that this enemy has equipped.
	/// </summary>
	protected abstract string[] EquippedSkills { get; }
	public abstract SpriteFrames Animation { get; }
	/// <summary>
	/// Called right before an enemy takes their turn.
	/// </summary>
	/// <returns>The <see cref="BattleCommand"/> that the enemy will perform on their turn.</returns>
	public abstract BattleCommand ProcessAI();
	/// <summary>
	/// Whether the enemy falls off the screen when killed.
	/// </summary>
	/// <remarks>
	/// Setting this value directly should be avoided as the player can set this manually in the preset settings.
	/// </remarks>
	public bool FallsOffScreen = true;
	/// <summary>
	/// The layer this enemy is on
	/// </summary>
	public int Layer { get; protected set; } = 0;
	/// <summary>
	/// Called after each action finishes. Mainly used for boss events.
	/// </summary>
	public virtual async Task ProcessBattleConditions() { await Task.CompletedTask; }
	/// <summary>
	/// Called at the very start of the turn.
	/// </summary>
	public virtual async Task ProcessStartOfTurn() { await Task.CompletedTask; }
	/// <summary>
	/// Called at the very end of the turn, but before it officially ends.
	/// </summary>
	public virtual async Task ProcessEndOfTurn() { await Task.CompletedTask; }
}
