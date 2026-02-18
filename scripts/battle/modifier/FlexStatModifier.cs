using OmoriSandbox.Actors;

namespace OmoriSandbox.Battle.Modifier;

/// <summary>
/// The modifier used by the Flex skill.
/// </summary>
public sealed class FlexStatModifier : StatModifier
{
    /// <inheritdoc/>
    public FlexStatModifier(params StatBonus[] bonuses) : base(bonuses) { }
    
    /// <inheritdoc/>
    public override void OverrideDamage(DamagePhase phase, ref float damage, Actor attacker, Actor defender, bool isAttacking, bool isCritical)
    {
        if (phase is DamagePhase.PreJuice && isAttacking)
        {
            damage *= 2.5f;
            attacker.RemoveStatModifier("Flex");
        }
    }
}