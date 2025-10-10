namespace OmoriSandbox.Battle;

/// <summary>
/// What a <see cref="Skill"/> can target. Mainly used for visual targeting.
/// </summary>
public enum SkillTarget
{
    Self,
    Ally,
    AllyNotSelf,
    AllAllies,
    Enemy,
    AllEnemies,
    AllyOrEnemy,
    DeadAlly,
    AllDeadAllies
}
