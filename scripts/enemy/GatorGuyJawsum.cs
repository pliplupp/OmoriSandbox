public class GatorGuyJawsum : Enemy
{
    public override string Name => "GATOR GUY";
    public override string AnimationPath => "res://animations/gator_guy.tres";
    protected override Stats Stats => new(300, 150, 40, 10, 30, 10, 95);
    protected override string[] EquippedSkills => ["GGAttack", "GGDoNothing", "GGRoughUp"];
    public override bool IsStateValid(string state)
    {
        return state == "neutral" || state == "happy" || state == "sad"
               || state == "angry" || state == "hurt" || state == "toast";
    }
    public override BattleCommand ProcessAI()
    {
        int roll;
        switch (CurrentState)
        {
            case "happy":
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 31)
                    goto attack;
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 31)
                    goto nothing;
                goto rough;
            case "sad":
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 26)
                    goto attack;
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 41)
                    goto nothing;
                goto rough;
            case "angry":
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 46)
                    goto attack;
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 26)
                    goto nothing;
                goto rough;
            default:
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 36)
                    goto attack;
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 26)
                    goto nothing;
                goto rough;

        }
    attack:
        return new BattleCommand(this, SelectTarget(), Skills["GGAttack"]);
    nothing:
        return new BattleCommand(this, null, Skills["GGDoNothing"]);
    rough:
        return new BattleCommand(this, SelectTarget(), Skills["GGRoughUp"]);
    }
}