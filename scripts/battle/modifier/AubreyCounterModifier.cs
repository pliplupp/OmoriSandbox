using OmoriSandbox.Actors;
using System.Linq;

namespace OmoriSandbox.Battle.Modifier;

/// <summary>
/// The modifier used by the Counter skill.
/// </summary>
public sealed class AubreyCounterModifier : StatModifier
{
    public AubreyCounterModifier(int turns) : base(turns) { }
    public override void OverrideDamage(ref float damage, Actor attacker, Actor defender, bool isAttacking)
    {
        if (isAttacking)
            return;

        BattleCommand command = BattleManager.Instance.GetCurrentCommand();

        if (attacker is Enemy && command.Action is Skill skill && skill.Target == SkillTarget.Enemy)
        {
            BattleManager.Instance.ForceCommand(defender, attacker, defender.Skills.First().Value);
        }
    }
}