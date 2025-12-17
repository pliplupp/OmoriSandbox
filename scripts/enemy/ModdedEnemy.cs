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
            return new BattleCommand(this, [], null);
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

                switch (skill.Target)
                {
                    case SkillTarget.Self:
                        return new BattleCommand(this, this, skill);
                    case SkillTarget.AllAllies:
                        return new BattleCommand(this, SelectAllEnemies(), skill);
                    case SkillTarget.AllEnemies:
                        return new BattleCommand(this, SelectAllTargets(), skill);
                    case SkillTarget.Ally:
                    case SkillTarget.AllyNotSelf:    
                        return new BattleCommand(this, SelectEnemy(), skill);
                    case SkillTarget.Enemy:
                    case SkillTarget.AllyOrEnemy:
                        return new BattleCommand(this, SelectTarget(), skill);
                    default:
                        GD.PrintErr($"Skill {skill.Name} is not supported for enemies.");
                        return new BattleCommand(this, [], null);
                }
            }
        }
        GD.PrintErr($"Modded enemy {Name} ProcessAI failed due to an error.");
        return new BattleCommand(this, [], null);
    }
}