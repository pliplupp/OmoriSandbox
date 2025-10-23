using OmoriSandbox.Actors;

namespace OmoriSandbox.Battle.Modifier;

/// <summary>
/// The modifier used by the Guard skill, as well as any other skill that has a Guard as a side effect.
/// </summary>
public sealed class GuardStatModifier : StatModifier
{
    public GuardStatModifier(int turns, params StatBonus[] bonuses) : base(turns, bonuses) { }
    public override void OverrideDamage(ref float damage, Actor attacker, Actor defender, bool isAttacking)
    {
        if (!isAttacking)
            damage *= 0.5f;
    }
}