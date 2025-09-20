public class LostSproutMole : Enemy
{
	public override string Name => "LOST SPROUT MOLE";

	public override string AnimationPath => "res://animations/sprout_mole.tres";

	protected override Stats Stats => new(170, 75, 22, 10, 13, 5, 95);
	protected override string[] EquippedSkills => ["LSMAttack", "LSMDoNothing", "LSMRunAround"];

	public override bool IsStateValid(string state)
	{
		return state == "neutral" || state == "sad" || state == "happy" || state == "angry" || state == "hurt" || state == "toast";
	}
    public override BattleCommand ProcessAI()
	{
		switch (CurrentState)
		{
			case "happy":
				if (Roll() < 36)
					goto attack;
				if (Roll() < 36)
					goto nothing;
				goto run;
			case "sad":
				if (Roll() < 31)
					goto attack;
				if (Roll() < 56)
					goto nothing;
				goto run;
			case "angry":
				if (Roll() < 61)
					goto attack;
				if (Roll() < 21)
					goto nothing;
				goto run;
			default:
				if (Roll() < 56)
					goto attack;
				if (Roll() < 36)
					goto nothing;
				goto run;

		}
	attack:
		return new BattleCommand(this, SelectTarget(), Skills["LSMAttack"]);
	nothing:
		return new BattleCommand(this, null, Skills["LSMDoNothing"]);
	run:
		return new BattleCommand(this, SelectTarget(), Skills["LSMRunAround"]);
	}
}
