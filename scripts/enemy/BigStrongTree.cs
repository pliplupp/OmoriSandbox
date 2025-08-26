public class BigStrongTree : Enemy
{
    public override string Name => "BIG STRONG TREE";
    public override string AnimationPath => "res://animations/big_strong_tree.tres";
    protected override Stats Stats => new(1000, 500, 999, 999, 1, 999, 100);
    public override bool IsStateValid(string state)
    {
        return state == "neutral" || state == "toast";
    }

    protected override string[] EquippedSkills => ["BSTDoNothing"];

    public override BattleCommand ProcessAI()
    {
        return new BattleCommand(this, null, Skills["BSTDoNothing"]);
    }
}