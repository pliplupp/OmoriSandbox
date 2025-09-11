public class SpaceExBoyfriend : Enemy
{
    public override string Name => "SPACE EX-BOYFRIEND";
    public override string AnimationPath => "res://animations/space_ex_boyfriend.tres";
    protected override Stats Stats => new(1350, 750, 15, 16, 25, 10, 95);
    protected override string[] EquippedSkills => ["SEBAttack", "SEBDoNothing", "AngstySong", "AngrySong", "SpaceLaser", "BulletHell"];
    public override bool IsStateValid(string state)
    {
        if (state == "toast")
            return true;

        if (EmotionLocked)
            return false;

        return state == "neutral" || state == "sad" || state == "happy"
            || state == "angry" || state == "hurt";
    }

    private bool EmotionLocked = false;
    private int Stage = 0;
    public override BattleCommand ProcessAI()
    {
        int roll;
        Actor target = BattleManager.Instance.GetRandomAlivePartyMember();
        switch (CurrentState)
        {
            case "se_furious":
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 36)
                    goto attack;
                goto bullet;
            case "se_enraged":
            case "se_angry":
            case "angry":
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 46)
                    goto attack;
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 21)
                    goto nothing;
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 21)
                    goto angsty;
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 31)
                    goto angry;
                goto laser;
            case "sad":
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 31)
                    goto attack;
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 21)
                    goto nothing;
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 41)
                    goto angsty;
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 21)
                    goto angry;
                goto laser;
            case "happy":
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 36)
                    goto attack;
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 21)
                    goto nothing;
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 21)
                    goto angsty;
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 21)
                    goto angry;
                goto laser;
            default:
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 36)
                    goto attack;
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 31)
                    goto nothing;
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 31)
                    goto angsty;
                roll = GameManager.Instance.Random.RandiRange(0, 100);
                if (roll < 31)
                    goto angry;
                goto laser;

        }
    attack:
        return new BattleCommand(this, target, Skills["SEBAttack"]);
    nothing:
        return new BattleCommand(this, null, Skills["SEBDoNothing"]);
    angsty:
        return new BattleCommand(this, target, Skills["AngstySong"]);
    angry:
        return new BattleCommand(this, target, Skills["AngrySong"]);
    laser:
        return new BattleCommand(this, target, Skills["SpaceLaser"]);
    bullet:
        return new BattleCommand(this, target, Skills["BulletHell"]);
    }

    public override void ProcessBattleConditions()
    {
        if (Stage > 2 || CurrentHP <= 0)
            return;

        if (CurrentHP < 338 && Stage <= 2)
        {
            EmotionLocked = false;
            ForceState("SpaceExFurious", "furious");
            EmotionLocked = true;
            Stage = 3;
        }

        if (CurrentHP < 675 && Stage <= 1)
        {
            EmotionLocked = false;
            ForceState("SpaceExEnraged", "enraged");
            EmotionLocked = true;
            Stage = 2;
        }

        if (CurrentHP < 1013 && Stage == 0)
        {
            ForceState("SpaceExAngry", "angry");
            EmotionLocked = true;
            Stage = 1;
        }
    }
}