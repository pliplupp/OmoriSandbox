public class GuardStatModifier : StatModifier
{
    public GuardStatModifier(int turns, params StatBonus[] bonuses) : base(turns, bonuses) { }
    public override void OverrideDamage(ref float damage, Actor attacker, Actor defender, bool isAttacking)
    {
        if (!isAttacking)
            damage *= 0.5f;
    }
}