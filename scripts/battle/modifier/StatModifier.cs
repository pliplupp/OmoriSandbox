using System;

/// <summary>
/// Represents a generic stat modifier that has no turn counter or tier
/// </summary>
public class StatModifier
{
    protected StatBonus[] Bonuses;

    /// <summary>
    /// Represents a generic stat modifier that has no turn counter or tier
    /// </summary>
    /// <param name="bonuses">A list of stat bonuses that will all be applied at once</param>
    public StatModifier(StatBonus[] bonuses)
    {
        Bonuses = bonuses;
    }

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

    public virtual void OverrideDamage(ref float damage, Actor attacker, Actor defender) { }
    public virtual string OverrideEmotion() { return "neutral"; }
}