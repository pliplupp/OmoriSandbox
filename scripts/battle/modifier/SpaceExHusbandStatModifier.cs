using Godot;
using OmoriSandbox.Actors;

namespace OmoriSandbox.Battle.Modifier;

/// <summary>
/// The modifier used by Space Ex-Husband to prevent damage while neutral.
/// </summary>
public sealed class SpaceExHusbandStatModifier : StatModifier
{
    public override void OverrideDamage(ref float damage, Actor attacker, Actor defender, bool isAttacking)
    {
        if (isAttacking)
            return;

        if (defender.CurrentState == "neutral")
        {
            BattleCommand command = BattleManager.Instance.GetCurrentCommand();
            // recreate the Omori bug where items and certain skills can deal damage to him in neutral
            if (command.Action is Skill skill && 
                skill.Name != "TRICK" && 
                !skill.Name.StartsWith("Pass To Aubrey") && 
                skill.Name != "FLOWER CROWN")
                damage = 0f;
        }
    }
}
