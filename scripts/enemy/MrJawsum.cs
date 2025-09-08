public class MrJawsum : Enemy
{
    public override string Name => "MR. JAWSUM";
    public override string AnimationPath => "res://animations/mr_jawsum.tres";
    protected override Stats Stats => new(500, 250, 999, 20, 1, 10, 95);
    protected override string[] EquippedSkills => [];

    public override bool IsStateValid(string state)
    {
        return state == "neutral" || state == "happy" || state == "sad"
               || state == "angry" || state == "hurt" || state == "toast";
    }

    public override BattleCommand ProcessAI()
    {
        return new BattleCommand(null, null, null);
    }
}
