public class FlexStatModifier : StatModifier
{
    public FlexStatModifier(params StatBonus[] bonuses) : base(bonuses) { }
    public override void OverrideDamage(ref float damage, Actor attacker, Actor defender, bool isAttacking)
    {
        if (isAttacking)
            damage *= 2.5f;
    }
}