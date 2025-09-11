/// <summary>
/// Represents a modifier that modifies damage before it is dealt, and optionally applying additional stats
/// </summary>
public class DamageStatModifier : StatModifier
{
    protected readonly float DamageMultiplier;
    /// <summary>
    /// Represents a modifier that modifies damage before it is dealt, and optionally applying additional stats
    /// </summary>
    /// <param name="damageMultiplier">The damage multipler to apply to the damage</param>
    /// <param name="bonuses">Additional stat bonuses, if any</param>
    public DamageStatModifier(float damageMultiplier, params StatBonus[] bonuses) : base(bonuses)
    {
        DamageMultiplier = damageMultiplier;
    }

    public override void OverrideDamage(ref float damage, Actor attacker, Actor defender)
    {
        damage *= DamageMultiplier;
    }
}