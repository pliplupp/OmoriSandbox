using System;

public class MrJawsumStatModifier : StatModifier
{
    public override void OverrideDamage(ref float damage, Actor attacker, Actor defender, bool isAttacking)
    {
        if (isAttacking)
            return;

        if (defender is not MrJawsum jawsum)
            return;

        if (jawsum.GatorGuys.Count == 0)
            return;

        int shared = (int)Math.Round(damage / jawsum.GatorGuys.Count);
        for (int i = jawsum.GatorGuys.Count - 1; i >= 0; i--)
        {
            EnemyComponent gator = jawsum.GatorGuys[i];
            gator.Actor.Damage(shared);
            BattleManager.Instance.SpawnDamageNumber(shared, gator.Actor.CenterPoint);
            AnimationManager.Instance.PlayAnimation(123, gator.Actor);
            BattleLogManager.Instance.QueueMessage(attacker, gator.Actor, "[target] takes " + shared + " damage!");
            if (gator.Actor.CurrentHP <= 0)
            {
                jawsum.GatorGuys.RemoveAt(i);
            }
        }
        damage = 0f;
    }
}