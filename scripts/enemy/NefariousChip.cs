using Godot;
using System.Threading.Tasks;

using OmoriSandbox.Battle;

namespace OmoriSandbox.Actors;
internal sealed class NefariousChip : Enemy
{
    public override string Name => "NEFARIOUS CHIP";
    public override SpriteFrames Animation => ResourceLoader.Load<SpriteFrames>("res://animations/nefarious_chip.tres");
    protected override Stats Stats => new(3456, 1700, 43, 47, 10, 15, 95);
    protected override string[] EquippedSkills => ["NCAttack", "NCDoNothing", "NCLaugh", "NCCookies", "NCCookiesHappy"];
    public override bool IsStateValid(string state)
    {
        return state == "neutral" || state == "happy" || state == "sad"
               || state == "angry" || state == "hurt" || state == "toast";
    }

    public override BattleCommand ProcessAI()
    {
        switch (CurrentState)
        {
            case "happy":
                if (Roll() < 26)
                    goto attack;
                if (Roll() < 26)
                    goto nothing;
                goto happy;
            case "sad":
                if (Roll() < 36)
                    goto attack;
                if (Roll() < 51)
                    goto nothing;
                if (Roll() < 26)
                    goto laugh;
                goto cookies;
            case "angry":
                if (Roll() < 61)
                    goto attack;
                if (Roll() < 26)
                    goto nothing;
                if (Roll() < 26)
                    goto laugh;
                goto cookies;
            default:
                if (Roll() < 41)
                    goto attack;
                if (Roll() < 31)
                    goto nothing;
                if (Roll() < 36)
                    goto laugh;
                goto cookies;
        }
    attack:
        return new BattleCommand(this, SelectTarget(), Skills["NCAttack"]);
    nothing:
        return new BattleCommand(this, null, Skills["NCDoNothing"]);
    laugh:
        return new BattleCommand(this, null, Skills["NCLaugh"]);
    cookies:
        return new BattleCommand(this, null, Skills["NCCookies"]);
    happy:
        return new BattleCommand(this, null, Skills["NCCookiesHappy"]);
    }

    private bool HasSpoken = false;
    public override async Task ProcessBattleConditions()
    {
        if (CurrentHP <= 0)
        {
            DialogueManager.Instance.QueueMessage(this, "Molto triste...");
            await DialogueManager.Instance.WaitForDialogue();
            return;
        }

        if (CurrentHP < 1728 && !HasSpoken)
        {
            DialogueManager.Instance.QueueMessage(this, "Mamma-mia...");
            DialogueManager.Instance.QueueMessage(this, "Is...@ Is getting hot in here, no?");
            await DialogueManager.Instance.WaitForDialogue();
            HasSpoken = true;
        }
    }

    public override async Task OnEndOfBattle(bool victory)
    {
        if (!victory)
        {
            DialogueManager.Instance.QueueMessage(this, "YAHOO!@ WAA-HAA!!");
            await DialogueManager.Instance.WaitForDialogue();
        }
    }
}