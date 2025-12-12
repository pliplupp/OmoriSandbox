using Godot;
using OmoriSandbox.Battle;

namespace OmoriSandbox.Actors;

internal sealed class KidsKite : Enemy
{
    public override string Name => "KID'S KITE";
    public override SpriteFrames Animation => ResourceLoader.Load<SpriteFrames>("res://animations/kids_kite.tres");
    protected override Stats Stats => new(175, 90, 26, 10, 40, 10, 95);
    protected override string[] EquippedSkills => ["KSKAttack", "KSKDoNothing", "KSKFly"];

    public override bool IsStateValid(string state)
    {
        return state == "neutral" || state == "sad" || state == "happy" || state == "angry" || state == "hurt" || state == "toast";
    }

    public override BattleCommand ProcessAI()
    {
        switch (CurrentState)
        {
            case "angry":
                if (Roll() < 51)
                    goto fly;
                if (Roll() < 51)
                    goto attack;
                goto nothing;
            case "sad":
                if (Roll() < 21)
                    goto fly;
                if (Roll() < 31)
                    goto attack;
                goto nothing;
            case "happy":
                if (Roll() < 31)
                    goto fly;
                if (Roll() < 31)
                    goto attack;
                goto nothing;
            default:
                if (Roll() < 26)
                    goto fly;
                if (Roll() < 46)
                    goto attack;
                goto nothing;
        }
        fly:
        return new BattleCommand(this, null, Skills["KSKFly"]);
        attack:
        return new BattleCommand(this, SelectTarget(), Skills["KSKAttack"]);
        nothing:
        return new BattleCommand(this, null, Skills["KSKDoNothing"]);
    }
}