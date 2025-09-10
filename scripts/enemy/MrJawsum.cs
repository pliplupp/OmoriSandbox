using System.Collections.Generic;
public class MrJawsum : Enemy
{
    public override string Name => "MR. JAWSUM";
    public override string AnimationPath => "res://animations/mr_jawsum.tres";
    protected override Stats Stats => new(500, 250, 999, 20, 1, 10, 95);
    protected override string[] EquippedSkills => ["MJSummonGator", "MJAttackOrder"];

    public override bool IsStateValid(string state)
    {
        return state == "neutral" || state == "happy" || state == "sad"
               || state == "angry" || state == "hurt" || state == "toast";
    }

    public readonly List<EnemyComponent> GatorGuys = [];

    public override BattleCommand ProcessAI()
    {
        if (GatorGuys.Count == 0)
            return new BattleCommand(this, null, Skills["MJSummonGator"]);
        int roll = GameManager.Instance.Random.RandiRange(0, 100);
        if (roll < 21)
            return new BattleCommand(this, null, Skills["MJAttackOrder"]);
        if (GatorGuys.Count < 2)
            return new BattleCommand(this, null, Skills["MJSummonGator"]);
        return new BattleCommand(this, null, Skills["MJAttackOrder"]);
    }
}
