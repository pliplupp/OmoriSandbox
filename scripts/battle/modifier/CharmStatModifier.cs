using OmoriSandbox.Actors;

namespace OmoriSandbox.Battle.Modifier;

/// <summary>
/// The modifier used by the Charm skill.
/// </summary>
public sealed class CharmStatModifier : StatModifier
{
    public CharmStatModifier(int turns) : base(turns) { }
    /// <summary>
    /// The <see cref="PartyMember"/> that the enemy will target.
    /// </summary>
    public PartyMember CharmedBy { get; private set; }
    public override void OnAdd()
    {
        CharmedBy = BattleManager.Instance.GetCurrentCommand().Actor as PartyMember;
    }
}