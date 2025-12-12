using System;
using System.Threading.Tasks;
using Godot;
using OmoriSandbox.Animation;
using OmoriSandbox.Battle;

namespace OmoriSandbox.Actors;

internal sealed class KingCrawler : Enemy
{
    public override string Name => "KING CRAWLER";
    public override SpriteFrames Animation =>
        ResourceLoader.Load<SpriteFrames>("res://animations/king_crawler.tres");
    protected override Stats Stats => new(730, 250, 25, 10, 18, 10, 200);
    protected override string[] EquippedSkills => ["KCAttack", "KCDoNothing", "KCCrunch", "KCRam", "KCEat", "KCRecover"];
    
    public override bool IsStateValid(string state)
    {
        return state == "neutral" || state == "hurt" || state == "toast" || state == "sad" || state == "angry" || state == "happy";
    }

    public override BattleCommand ProcessAI()
    {
        if (CurrentState == "angry")
        {
            if (Roll() < 41)
                return new BattleCommand(this, SelectTarget(), Skills["KCAttack"]);
            if (Roll() < 31)
                return new BattleCommand(this, SelectTarget(), Skills["KCCrunch"]);
            return new BattleCommand(this, null, Skills["KCRam"]);
        }

        if (Roll() < 41)
            return new BattleCommand(this, SelectTarget(), Skills["KCAttack"]);
        if (Roll() < 26)
            return new BattleCommand(this, SelectTarget(), Skills["KCDoNothing"]);
        if (Roll() < 31)
            return new BattleCommand(this, SelectTarget(), Skills["KCCrunch"]);
        return new BattleCommand(this, null, Skills["KCRam"]);
    }

    private bool HasSpoken = false;
    public override async Task ProcessBattleConditions()
    {
        if (CurrentHP < 365 && !HasSpoken)
        {
            DialogueManager.Instance.QueueMessage(this, "Ssssssssssssss...");
            await DialogueManager.Instance.WaitForDialogue();
            HasSpoken = true;
        }
    }

    private EnemyComponent SproutMole;
    private bool AteSproutMoleLastTurn = false;
    
    public override async Task ProcessEndOfTurn()
    {
        if (AteSproutMoleLastTurn)
        {
            AteSproutMoleLastTurn = false;
            return;
        }
        
        if (SproutMole == null || SproutMole.Actor.CurrentState == "toast")
        {
            SproutMole =
                BattleManager.Instance.SummonEnemy("LostSproutMole", CenterPoint - new Vector2(100, 0), layer: Layer + 1);
            DialogueManager.Instance.QueueMessage("A SPROUT MOLE appears!");
            await DialogueManager.Instance.WaitForDialogue();
        }
        else
        {
            DialogueManager.Instance.QueueMessage("KING CRAWLER eats a SPROUT MOLE!");
            await DialogueManager.Instance.WaitForDialogue();
            BattleManager.Instance.ForceCommand(this, SproutMole.Actor, Skills["KCEat"]);
            BattleManager.Instance.ForceCommand(this, null, Skills["KCRecover"]);
            AteSproutMoleLastTurn = true;
        }
    }

    public override async Task OnEndOfBattle(bool victory)
    {
        if (!victory)
        {
            DialogueManager.Instance.QueueMessage(this, "KISHKISHKISHKISH!!");
            await DialogueManager.Instance.WaitForDialogue();
        }
    }
}