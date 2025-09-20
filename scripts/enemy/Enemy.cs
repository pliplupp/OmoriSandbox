using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public abstract class Enemy : Actor
{
    public void Init(AnimatedSprite2D sprite, string initialState, bool fallsOffScreen)
    {
        SpriteFrames animation = ResourceLoader.Load<SpriteFrames>(AnimationPath);
        if (animation == null)
        {
            GD.PrintErr("Failed to load Sprite animations for Enemy: " + Name);
            return;
        }
        // init animation
        Sprite = sprite;
        Sprite.SpriteFrames = animation;
        Sprite.Animation = initialState;
        Sprite.Play();
        CurrentState = initialState;
        BaseStats = Stats;
        CurrentHP = BaseStats.HP;
        CurrentJuice = BaseStats.Juice;

        FallsOffScreen = fallsOffScreen;

        foreach (string s in EquippedSkills)
        {
            if (Database.TryGetSkill(s, out var skill))
            {
                Skills.Add(s, skill);
                continue;
            }
            GD.PrintErr("Unknown skill: " + s);
        }
    }

    protected virtual PartyMember SelectTarget()
    {
        if (HasStatModifier("Charm"))
            return (StatModifiers["Charm"] as CharmStatModifier).CharmedBy;
        List<PartyMemberComponent> members = BattleManager.Instance.GetAlivePartyMembers();
        List<PartyMemberComponent> taunting = members.FindAll(x => x.Actor.HasStatModifier("Taunt"));
        if (taunting.Count == 0)
        {
            // if nobody is taunting, pick a random target
            return members[GameManager.Instance.Random.RandiRange(0, members.Count - 1)].Actor;
        }
        return taunting[GameManager.Instance.Random.RandiRange(0, taunting.Count - 1)].Actor;
    }

    protected int Roll()
    {
        return GameManager.Instance.Random.RandiRange(0, 100);
    }

    protected abstract Stats Stats { get; }
    protected abstract string[] EquippedSkills { get; }
    public abstract string AnimationPath { get; }
    public abstract BattleCommand ProcessAI();
    public bool FallsOffScreen = true;
    public virtual async Task ProcessBattleConditions() { await Task.CompletedTask; }
    public virtual async Task ProcessStartOfTurn() { await Task.CompletedTask; }
    public virtual async Task ProcessEndOfTurn() { await Task.CompletedTask; }
}