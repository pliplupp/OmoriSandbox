using Godot;
using OmoriSandbox.Battle;

namespace OmoriSandbox.Actors;

internal sealed class Tentacle : Enemy
{
    public override string Name => "TENTACLE";
    public override SpriteFrames Animation => ResourceLoader.Load<SpriteFrames>("res://animations/tentacle.tres");
    protected override Stats Stats => new(1200, 600, 72, 50, 110, 25, 95);
    protected override string[] EquippedSkills => ["TEAttack", "TEWeaken", "TEGrab", "TEGoop"];
    public override bool IsStateValid(string state)
    {
        return state == "neutral" || state == "sad" || state == "happy" || state == "angry" || state == "hurt" || state == "toast";
    }

    public override BattleCommand ProcessAI()
    {
        switch (CurrentState)
        {
            case "angry":
                if (Roll() < 46)
                    goto attack;
                if (Roll() < 21)
                    goto weaken;
                if (Roll() < 36)
                    goto grab;
                goto goop;
            case "sad":
                if (Roll() < 31)
                    goto attack;
                if (Roll() < 26)
                    goto weaken;
                if (Roll() < 46)
                    goto grab;
                goto goop;
            case "happy":
                if (Roll() < 26)
                    goto attack;
                if (Roll() < 36)
                    goto weaken;
                if (Roll() < 36)
                    goto grab;
                goto goop;   
            default:
                if (Roll() < 41)
                    goto attack;
                if (Roll() < 31)
                    goto weaken;
                if (Roll() < 31)
                    goto grab;
                goto goop;    
        }
        
        attack:
        return new BattleCommand(this, SelectTarget(), Skills["TEAttack"]);
        weaken:
        return new BattleCommand(this, SelectTarget(), Skills["TEWeaken"]);
        grab:
        return new BattleCommand(this, SelectTarget(), Skills["TEGrab"]);
        goop:
        return new BattleCommand(this, SelectTarget(), Skills["TEGoop"]);
    }
}