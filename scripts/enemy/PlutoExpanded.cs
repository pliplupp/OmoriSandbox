using System.Threading.Tasks;
using Godot;
using OmoriSandbox.Battle;

namespace OmoriSandbox.Actors;

internal sealed class PlutoExpanded : Enemy
{
    public override string Name => "PLUTO (EXPANDED)";
    public override SpriteFrames Animation =>
        ResourceLoader.Load<SpriteFrames>("res://animations/pluto_expanded.tres");
    protected override Stats Stats => new(3000, 1500, 52, 32, 22, 10, 95);
    protected override string[] EquippedSkills => ["PEAttack", "PESubmissionHold", "PEHeadbutt", "PEDoNothing", "PEExpandFurther", "PEEarthsFinale"];

    public override bool IsStateValid(string state)
    {
        return state == "neutral" || state == "hurt" || state == "toast" || state == "sad" || state == "angry" || state == "happy";
    }

    public override BattleCommand ProcessAI()
    {
        if (CurrentHP < 300)
            return new BattleCommand(this, null, Skills["PEEarthsFinale"]);

        if (Roll() < 31)
            return new BattleCommand(this, SelectTarget(), Skills["PEAttack"]);
        if (Roll() < 31)
            return new BattleCommand(this, SelectTarget(), Skills["PESubmissionHold"]);
        if (Roll() < 31)
            return new BattleCommand(this, null, Skills["PEDoNothing"]);
        if (CurrentHP < 1200)
            return new BattleCommand(this, null, Skills["PEExpandFurther"]);
        return new BattleCommand(this, SelectTarget(), Skills["PEHeadbutt"]);
    }

    public override async Task OnStartOfBattle()
    {
        DialogueManager.Instance.QueueMessage("PLUTO", CenterPoint, "Behold...@\nThis is my final form.");
        DialogueManager.Instance.QueueMessage("PLUTO", CenterPoint, "Can you...@ feel the heat?");
        await DialogueManager.Instance.WaitForDialogue();
    }

    private bool HasSpoken = false;
    public override async Task ProcessBattleConditions()
    {
        if (CurrentHP <= 0)
        {
            DialogueManager.Instance.QueueMessage("PLUTO", CenterPoint, "Hm. Well done, children.@\nYou've come a long way.");
            DialogueManager.Instance.QueueMessage("PLUTO", CenterPoint, "But...@\nI am not finished yet.");
            await DialogueManager.Instance.WaitForDialogue();
            return;
        }
        
        if (CurrentHP < 1500 && !HasSpoken)
        {
            DialogueManager.Instance.QueueMessage("PLUTO", CenterPoint, "... Ah, I see.");
            DialogueManager.Instance.QueueMessage("PLUTO", CenterPoint, "You have all gotten stronger.");
            DialogueManager.Instance.QueueMessage("PLUTO", CenterPoint, "But...@ so have I.");
            await DialogueManager.Instance.WaitForDialogue();
            HasSpoken = true;
        }
    }

    public override async Task OnEndOfBattle(bool victory)
    {
        if (!victory)
        {
            DialogueManager.Instance.QueueMessage("PLUTO", CenterPoint, "I apologize, children.");
            DialogueManager.Instance.QueueMessage("PLUTO", CenterPoint, "You should applaud yourselves for your effort.");
            await DialogueManager.Instance.WaitForDialogue();
        }
    }
}