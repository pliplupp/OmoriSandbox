public class FearOfSpiders : Enemy
{
    public override string Name => "SOMETHING";
    public override string AnimationPath => "res://animations/fear_of_spiders.tres";
    protected override Stats Stats => new(7500, 3000, 115, 35, 110, 30, 95);
    protected override string[] EquippedSkills => ["FOSAttack", "FOSDoNothing", "FOSSpinWeb", "FOSAttackAll"];

    public override bool IsStateValid(string state)
    {
        return state == "neutral" || state == "hurt" || state == "toast";
    }

    public override BattleCommand ProcessAI()
    {
        Actor target = BattleManager.Instance.GetRandomAlivePartyMember();
        int roll = GameManager.Instance.Random.RandiRange(0, 100);
        if (roll < 26)
            return new BattleCommand(this, target, Skills["FOSAttack"]);
        roll = GameManager.Instance.Random.RandiRange(0, 100);
        if (roll < 16)
            return new BattleCommand(this, null, Skills["FOSDoNothing"]);
        roll = GameManager.Instance.Random.RandiRange(0, 100);
        if (roll < 19)
            return new BattleCommand(this, target, Skills["FOSSpinWeb"]);
        else
            return new BattleCommand(this, null, Skills["FOSAttackAll"]);
    }
}