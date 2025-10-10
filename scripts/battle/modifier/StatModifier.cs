using OmoriSandbox.Actors;
using System;

namespace OmoriSandbox.Battle.Modifier;

/// <summary>
/// A generic stat modifier that can provide a set of <see cref="StatBonus"/>es.
/// </summary>
public class StatModifier
{
    protected StatBonus[] Bonuses;

    public int TurnsLeft { get; protected set; } = -1;

    protected int MaxTurns = -1;

    /// <summary>
    /// A generic stat modifier that has no turn counter
    /// </summary>
    /// <param name="bonuses">A list of stat bonuses that will all be applied at once</param>
    public StatModifier(params StatBonus[] bonuses)
    {
        Bonuses = bonuses;
    }

    /// <summary>
    /// A generic stat modifier that has a turn counter. The <paramref name="turns"/> value is also used to set the max turns.
    /// </summary>
    /// <param name="turns">The number of turns this stat modifier lasts for.</param>
    /// <param name="bonuses">A list of stat bonuses that will all be applied at once</param>
    public StatModifier(int turns, params StatBonus[] bonuses) : this(bonuses)
    {
        TurnsLeft = turns;
        MaxTurns = TurnsLeft;
    }

    /// <summary>
    /// Called when a stat modifier is first given to an actor.
    /// </summary>
    public virtual void OnAdd() { }

    /// <summary>
    /// Called when the stats are applied.
    /// </summary>
    /// <param name="stats">A reference to the <see cref="Stats"/> before any stats are applied.</param>
    public virtual void ApplyStats(ref Stats stats)
    {
        // base class simply applies all stat bonuses to the provided stats
        foreach (StatBonus bonus in Bonuses)
        {
            int stat = stats.GetStat(bonus.Type);
            stat = (int)Math.Round(stat * bonus.Multiplier + bonus.FlatBonus);
            stats.SetStat(bonus.Type, stat);
        }
    }

    /// <summary>
    /// Called during damage calculation to override the damage result.
    /// </summary>
    /// <param name="damage">A reference to the current damage at this point in the damage calculation.</param>
    /// <param name="attacker">The <see cref="Actor"/> that is attacking in the calculation.</param>
    /// <param name="defender">The <see cref="Actor"/> that is defending in the calculation.</param>
    /// <param name="isAttacking">The attack context from whenever this function is called.<br/>For instance, this value will be <c>true</c> whenever the <paramref name="attacker"/>'s modifiers are being calculated.</param>
    public virtual void OverrideDamage(ref float damage, Actor attacker, Actor defender, bool isAttacking) { }

    /// <summary>
    /// Called during emotion checks in damage calculation.
    /// </summary>
    /// <returns>The overriden emotion that should be used.</returns>
    public virtual string OverrideEmotion() { return "neutral"; }

    /// <summary>
    /// Sets the number of turns left on this modifier. Will be clamped to the max turns set previously.
    /// </summary>
    /// <param name="turnsLeft">The number of turns left to set this modifier to.</param>
    public void SetTurnsLeft(int turnsLeft)
    {
        TurnsLeft = Math.Min(turnsLeft, MaxTurns);
    }

    /// <summary>
    /// Decrement the number of turns left on this modifier by one.
    /// </summary>
    public void DecreaseTurns()
    {
        if (TurnsLeft > 0)
            TurnsLeft--;
    }
}