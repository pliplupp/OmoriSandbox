using Godot;
using System.Linq;
using OmoriSandbox.Battle;
using OmoriSandbox.Modding;

namespace OmoriSandbox.Actors;

internal class ModdedEnemy : Enemy
{
    private JsonEnemyMod JsonEnemy;
    private SpriteFrames BuiltFrames;

    public ModdedEnemy(JsonEnemyMod jsonEnemy, SpriteFrames builtFrames)
    {
        JsonEnemy = jsonEnemy;
        BuiltFrames = builtFrames;
    }

    public override SpriteFrames Animation => BuiltFrames;

    public override string Name => JsonEnemy.Name.ToUpper();

    protected override Stats Stats => new Stats(JsonEnemy.HP, JsonEnemy.Juice, JsonEnemy.ATK, JsonEnemy.DEF, JsonEnemy.SPD, JsonEnemy.LCK, JsonEnemy.HIT);

    protected override string[] EquippedSkills => JsonEnemy.EquippedSkills;

    public override bool IsStateValid(string state)
    {
        return !JsonEnemy.InvalidStates.Contains(state);
    }

    public override BattleCommand ProcessAI()
    {
        JsonEnemyAIData data = JsonEnemy.AI.FirstOrDefault(x => x.Emotion == CurrentState);
        if (data.Equals(default(JsonEnemyAIData)))
        {
            GD.PrintErr($"Modded enemy {Name} is missing AI data for emotion {CurrentState}");
            return new BattleCommand(this, null, null);
        }
        foreach (JsonEnemyAIEntry entry in data.Entries)
        {
            if (Roll() <= entry.Chance)
            {
                if (!Database.TryGetSkill(entry.Skill, out Skill skill))
                {
                    GD.PrintErr($"Unknown skill {entry.Skill} for modded enemy {Name}!");
                    continue;
                }
                if (!Skills.TryGetValue(entry.Skill, out skill))
                {
                    GD.PrintErr($"Modded enemy {Name} does not have the {entry.Skill} skill equipped!");
                    continue;
                }
                if (skill.Target == SkillTarget.AllEnemies ||
                    skill.Target == SkillTarget.AllAllies ||
                    skill.Target == SkillTarget.AllDeadAllies)
                    return new BattleCommand(this, null, skill);
                else if (skill.Target == SkillTarget.Self)
                    return new BattleCommand(this, this, skill);
                else return new BattleCommand(this, SelectTarget(), skill);
            }
        }
        GD.PrintErr($"Modded enemy {Name} ProcessAI failed due to an error.");
        return new BattleCommand(this, null, null);
    }
}