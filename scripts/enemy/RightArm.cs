using Godot;
using OmoriSandbox.Battle;

namespace OmoriSandbox.Actors;

internal sealed class RightArm : Enemy
{
    public override string Name => "RIGHT ARM";
    public override SpriteFrames Animation => ResourceLoader.Load<SpriteFrames>("res://animations/right_arm.tres");
    protected override string[] EquippedSkills => ["RAAttack", "RAFlex", "RAGrab"];
    protected override Stats Stats => new(175, 75, 12, 5, 5, 10, 95);

    public override bool IsStateValid(string state)
    {
        return state == "neutral" || state == "happy" || state == "sad"
               || state == "angry" || state == "hurt" || state == "toast";
    }

    public override BattleCommand ProcessAI()
    {
        switch (CurrentState)
        {
            case "angry":
                if (Roll() < 61)
                    goto attack;
                if (Roll() < 51)
                    goto flex;
                goto grab;
            case "sad":
                if (Roll() < 61)
                    goto attack;
                if (Roll() < 31)
                    goto flex;
                goto grab;
            case "happy":
                if (Roll() < 46)
                    goto attack;
                if (Roll() < 41)
                    goto flex;
                goto grab;
            default:
                if (Roll() < 61)
                    goto attack;
                if (Roll() < 51)
                    goto flex;
                goto grab;     
        }
        
        attack:
        return new BattleCommand(this, SelectTarget(), Skills["RAAttack"]);
        flex:
        return new BattleCommand(this, this, Skills["RAFlex"]);
        grab:
        return new BattleCommand(this, SelectTarget(), Skills["RAGrab"]);
    }
}