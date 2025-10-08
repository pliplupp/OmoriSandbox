using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
public class MrJawsum : Enemy
{
    public override string Name => "MR. JAWSUM";
    public override SpriteFrames Animation => ResourceLoader.Load<SpriteFrames>("res://animations/mr_jawsum.tres");
    protected override Stats Stats => new(500, 250, 999, 20, 1, 10, 95);
    protected override string[] EquippedSkills => ["MJSummonGator", "MJAttackOrder"];

    public override bool IsStateValid(string state)
    {
        return state == "neutral" || state == "happy" || state == "sad"
               || state == "angry" || state == "hurt" || state == "toast";
    }

    public readonly List<EnemyComponent> GatorGuys = [];
    private int Stage = 0;

    public override BattleCommand ProcessAI()
    {
        if (GatorGuys.Count == 0)
            return new BattleCommand(this, null, Skills["MJSummonGator"]);
        if (Roll() < 21)
            return new BattleCommand(this, null, Skills["MJAttackOrder"]);
        if (GatorGuys.Count < 2)
            return new BattleCommand(this, null, Skills["MJSummonGator"]);
        return new BattleCommand(this, null, Skills["MJAttackOrder"]);
    }

    public void SpawnGatorGuy()
    {
        if (GatorGuys.Count == 0)
           GatorGuys.Add(BattleManager.Instance.SummonEnemy("GatorGuyJawsum", new Vector2(CenterPoint.X - 145, CenterPoint.Y + 65)));
        else if (GatorGuys.Count == 1)
            GatorGuys.Add(BattleManager.Instance.SummonEnemy("GatorGuyJawsum", new Vector2(CenterPoint.X + 145, CenterPoint.Y + 65)));
        else
        {
            GD.PushWarning("Tried to summon more than 2 gator guys!");
            return;
        }
    }

    public override async Task ProcessBattleConditions()
    {
        GatorGuys.RemoveAll(x => x.Actor.CurrentHP <= 0);

        if (CurrentHP <= 0)
        {
            DialogueManager.Instance.QueueMessage(this, "You let yourselves be foiled by a bunch of children!?");
            DialogueManager.Instance.QueueMessage(this, "WHAT DID I EVEN HIRE YOU FOR!?");
            await DialogueManager.Instance.WaitForDialogue();
        }

        if (Stage > 2) 
            return;

        if (CurrentHP < 150 && Stage <= 2)
        {
            DialogueManager.Instance.QueueMessage(this, "What do you mean we're running low on henchmen!?@ That's impossible!");
            await DialogueManager.Instance.WaitForDialogue();
            Stage++;
        }

        if (CurrentHP < 225 && Stage <= 1)
        {
            DialogueManager.Instance.QueueMessage(this, "The GATOR GUY who runs them out gets free pizza...@ on me!");
            await DialogueManager.Instance.WaitForDialogue();
            Stage++;
        }

        if (CurrentHP < 297 && Stage == 0)
        {
            DialogueManager.Instance.QueueMessage(this, "I WANT THESE KIDS GONE YOU UNDERSTAND!?");
            await DialogueManager.Instance.WaitForDialogue();
            Stage++;
        }
    }

    public override async Task OnStartOfBattle()
    {
        AddStatModifier("MrJawsumBarrier");
        SpawnGatorGuy();
        SpawnGatorGuy();
        DialogueManager.Instance.QueueMessage(this, "Boys...@ would you be so kind as to show these kids the way out?");
        await DialogueManager.Instance.WaitForDialogue();
    }
    public override async Task OnEndOfBattle(bool victory)
    {
        if (!victory) {
            DialogueManager.Instance.QueueMessage(this, "JAWHAW HAW HAW!!!");
            DialogueManager.Instance.QueueMessage(this, "That's what happens when you mess with Mr. Jawsum!");
            await DialogueManager.Instance.WaitForDialogue();
        }
    }
}
