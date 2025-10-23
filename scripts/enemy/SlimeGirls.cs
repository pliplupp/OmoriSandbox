using Godot;
using System.Threading.Tasks;

using OmoriSandbox.Battle;

namespace OmoriSandbox.Actors;
internal sealed class SlimeGirls : Enemy
{
	public override SpriteFrames Animation => ResourceLoader.Load<SpriteFrames>("res://animations/slimegirls.tres");

	public override string Name => "SLIME GIRLS";

	protected override Stats Stats => new(5700, 1750, 57, 32, 52, 10, 95);

	protected override string[] EquippedSkills => ["ComboAttack", "StrangeGas", "Dynamite", "StingRay", "Swap", "Chainsaw", "SlimeUltimateAttack"];

	public override bool IsStateValid(string state)
	{
		return state == "neutral" || state == "sad" || state == "happy"
			|| state == "angry" || state == "hurt" || state == "toast";
	}

	private int Stage = 0;

	public override BattleCommand ProcessAI()
	{
		Actor target = SelectTarget();
        switch (CurrentState)
		{
			case "happy":
				if (Roll() < 16)
					goto combo;
				if (Roll() < 21)
					goto gas;
				if (Roll() < 16)
					goto dynamite;
				if (Roll() < 21)
					goto stingray;
				goto chainsaw;
			case "sad":
				if (Roll() < 16)
					goto combo;
				if (Roll() < 16)
					goto gas;
				if (Roll() < 21)
					goto dynamite;
				if (Roll() < 16)
					goto stingray;
				goto chainsaw;
			case "angry":
				if (Roll() < 21)
					goto combo;
				if (Roll() < 16)
					goto gas;
				if (Roll() < 21)
					goto dynamite;
				if (Roll() < 16)
					goto stingray;
				goto chainsaw;
			default:
				if (Roll() < 16)
					goto combo;
				if (Roll() < 16)
					goto gas;
				if (Roll() < 16)
					goto dynamite;
				if (Roll() < 16)
					goto stingray;
				goto chainsaw;
		}

	combo:
		return new BattleCommand(this, target, Skills["ComboAttack"]);
	gas:
		return new BattleCommand(this, target, Skills["StrangeGas"]);
	dynamite:
		return new BattleCommand(this, target, Skills["Dynamite"]);
	stingray:
		return new BattleCommand(this, target, Skills["StingRay"]);
	chainsaw:
		return new BattleCommand(this, target, Skills["Chainsaw"]);
	}

	public override async Task ProcessBattleConditions()
	{
		if (CurrentHP <= 0)
		{
            DialogueManager.Instance.QueueMessage("MARINA", CenterPoint, "You kids...@ are a lot tougher than you look.");
            DialogueManager.Instance.QueueMessage("MOLLY", CenterPoint, "Hmph...@ This is much more trouble than it's worth.");
            DialogueManager.Instance.QueueMessage("MEDUSA", CenterPoint, "Sigh...@ What a predicament...@ How will we feed HUMPHREY now?");
            await DialogueManager.Instance.WaitForDialogue();
			return;
        }

		if (Stage > 2)
			return;

        if (CurrentHP < 1425 && Stage <= 2)
        {
            BattleManager.Instance.ForceCommand(this, null, Skills["SlimeUltimateAttack"]);
            Stage = 3;
        }

        if (CurrentHP < 2850 && Stage <= 1)
        {
            DialogueManager.Instance.QueueMessage("MARINA", CenterPoint, "Hey, MEDUSA!@ Are you thinkin' what I'm thinkin'?");
            DialogueManager.Instance.QueueMessage("MEDUSA", CenterPoint, "Yes, sister...@ I think it's about time we switched things up.");
            DialogueManager.Instance.QueueMessage("MOLLY", CenterPoint, "Just relax, children...@ This won't hurt a bit~");
            await DialogueManager.Instance.WaitForDialogue();
            BattleManager.Instance.ForceCommand(this, null, Skills["Swap"]);
            Stage++;
        }

        if (CurrentHP < 4275 && Stage == 0)
		{
			DialogueManager.Instance.QueueMessage("MEDUSA", CenterPoint, "Hmph...@ You kids are more resilient than expected.");
            DialogueManager.Instance.QueueMessage("MARINA", CenterPoint, "You know what that means.@ It's time to get serious!");
            DialogueManager.Instance.QueueMessage("MOLLY", CenterPoint, "Oh...@ I'm having so much fun~!");
			await DialogueManager.Instance.WaitForDialogue();
            ForceState("angry");
			BattleLogManager.Instance.ClearAndShowMessage("SLIME GIRLS becomes ANGRIER!");
			Stage++;
		}
    }

    public override async Task OnEndOfBattle(bool victory)
    {
        if (!victory)
		{
            DialogueManager.Instance.QueueMessage("MARINA", CenterPoint, "Now you belong to us!");
            DialogueManager.Instance.QueueMessage("MOLLY", CenterPoint, "Hush, hush, darlings...@ Don't cry...@ You'll get used to your new life soon~");
            DialogueManager.Instance.QueueMessage("MEDUSA", CenterPoint, "It's time to take apart the small one.@ Let's get started, dolls.");
            await DialogueManager.Instance.WaitForDialogue();
        }
    }
}
