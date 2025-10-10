using Godot;
using OmoriSandbox.Battle;

namespace OmoriSandbox.Actors;
internal sealed class FearOfSpiders : Enemy
{
    public override string Name => "SOMETHING";
    public override SpriteFrames Animation => ResourceLoader.Load<SpriteFrames>("res://animations/fear_of_spiders.tres");
    protected override Stats Stats => new(7500, 3000, 115, 35, 110, 30, 95);
    protected override string[] EquippedSkills => ["FOSAttack", "FOSDoNothing", "FOSSpinWeb", "FOSAttackAll"];

    public override bool IsStateValid(string state)
    {
        return state == "neutral" || state == "hurt" || state == "toast";
    }

    public override BattleCommand ProcessAI()
    {
        if (Roll() < 26)
            return new BattleCommand(this, SelectTarget(), Skills["FOSAttack"]);
        if (Roll() < 16)
            return new BattleCommand(this, null, Skills["FOSDoNothing"]);
        if (Roll() < 19)
            return new BattleCommand(this, SelectTarget(), Skills["FOSSpinWeb"]);
        return new BattleCommand(this, null, Skills["FOSAttackAll"]);
    }
}