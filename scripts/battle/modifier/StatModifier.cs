using System;

/// <summary>
/// Represents a generic stat modifier that has no turn counter or tier
/// </summary>
public class StatModifier
{
    protected StatBonus[] Bonuses;

    public int TurnsLeft { get; protected set; } = -1;

    protected int MaxTurns = -1;

    /// <summary>
    /// Represents a generic stat modifier that has no turn counter or tier
    /// </summary>
    /// <param name="bonuses">A list of stat bonuses that will all be applied at once</param>
    public StatModifier(params StatBonus[] bonuses)
    {
        Bonuses = bonuses;
    }

    public StatModifier(int turns, params StatBonus[] bonuses) : this(bonuses)
    {
        TurnsLeft = turns;
        MaxTurns = TurnsLeft;
    }

    public virtual void OnAdd() { }

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

    public virtual void OverrideDamage(ref float damage, Actor attacker, Actor defender, bool isAttacking) { }
    public virtual string OverrideEmotion() { return "neutral"; }

    public void SetTurnsLeft(int turnsLeft)
    {
        TurnsLeft = Math.Min(turnsLeft, MaxTurns);
    }

    public void DecreaseTurns()
    {
        if (TurnsLeft > 0)
            TurnsLeft--;
    }
}