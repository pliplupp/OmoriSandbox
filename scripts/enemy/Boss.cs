using Godot;
using System.Threading.Tasks;
using OmoriSandbox.Battle;

namespace OmoriSandbox.Actors;
internal sealed class Boss : Enemy
{
    public override SpriteFrames Animation => ResourceLoader.Load<SpriteFrames>("res://animations/boss.tres");

    public override string Name => "BOSS";

    protected override Stats Stats => new Stats(150, 25, 6, 2, 1, 10, 95);

    protected override string[] EquippedSkills => ["BSSAttack", "BSSAttackTwice", "BSSDoNothing", "BSSAttackAll"];

    public override bool IsStateValid(string state)
    {
        return state == "neutral" || state == "sad" || state == "happy" || state == "angry" || state == "hurt" || state == "toast";
    }

    private int Stage = 0;

    public override BattleCommand ProcessAI()
    {
        if (CurrentHP < 23)
            return new BattleCommand(this, null, Skills["BSSDoNothing"]);

        if (Roll() < 31)
            return new BattleCommand(this, SelectTarget(), Skills["BSSAttack"]);

        if (Roll() < 31)
            return new BattleCommand(this, null, Skills["BSSAttackTwice"]);
        return new BattleCommand(this, null, Skills["BSSDoNothing"]);
    }

    public override async Task ProcessBattleConditions()
    {
        if (Stage > 2 || CurrentHP <= 0)
            return;

        if (CurrentHP < 15 && Stage <= 2)
        {
            DialogueManager.Instance.QueueMessage(this, "Grr... Now you've made me angry...");
            DialogueManager.Instance.QueueMessage(this, "It's time for my special move!");
            DialogueManager.Instance.QueueMessage(this, "Body slam!!");
            await DialogueManager.Instance.WaitForDialogue();
            SetState("angry", true);
            BattleManager.Instance.ForceCommand(this, null, Skills["BSSAttackAll"]);
            Stage++;
        }

        if (CurrentHP < 60 && Stage <= 1)
        {
            DialogueManager.Instance.QueueMessage(this, "Hey, that kinda hurt!@ Hmph!@ This isn't fun anymore.");
            await DialogueManager.Instance.WaitForDialogue();
            Stage++;
        }

        if (CurrentHP < 120 && Stage == 0)
        {
            DialogueManager.Instance.QueueMessage(this, "Hwehwehwe!@ You weaklings!@ You call that an attack!?");
            await DialogueManager.Instance.WaitForDialogue();
            Stage++;
        }
    }
}