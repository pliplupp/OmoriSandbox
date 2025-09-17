public class CharmStatModifier : StatModifier
{
    public CharmStatModifier(int turns) : base(turns) { }
    public PartyMember CharmedBy { get; private set; }
    public override void OnAdd()
    {
        CharmedBy = BattleManager.Instance.GetCurrentCommand().Actor as PartyMember;
    }
}