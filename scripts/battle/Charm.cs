using OmoriSandbox.Actors;
using System;

namespace OmoriSandbox.Battle;

/// <summary>
/// A Charm that can be equipped on a <see cref="PartyMember"/>
/// </summary>
public class Charm
{
    /// <summary>
    /// The name of the Charm
    /// </summary>
    public string Name { get; private set; }
    /// <summary>
    /// The Charm's stats
    /// </summary>
    public StatBonus[] Stats { get; }

    /// <summary>
    /// An extra method that allows dynamic StatBonuses to be applied when <see cref="Apply"/>ing a charm.
    /// </summary>
    /// <remarks>
    /// This can be useful for stat changes that rely on game variables, like the Energy bar.
    /// </remarks>
    public Func<StatBonus[]> OnApply { get; }

    /// <summary>
    /// What the charm does to its holder at the start of the battle.
    /// </summary>
    /// <remarks>
    /// Mainly used for emotion effects.
    /// </remarks>
    public Action<Actor> StartOfBattle { get; private set; }

    /// <summary>
    /// A basic Charm that modifies a single stat.
    /// </summary>
    public Charm(string name, StatBonus stat)
    {
        Name = name;
        Stats = [stat];
        OnApply = null;
        // empty action
        StartOfBattle = (_) => { };
    }
    
    /// <summary>
    /// A basic Charm that modifies a multiple stats.
    /// </summary>
    public Charm(string name, StatBonus[] stats)
    {
        Name = name;
        Stats = stats;
        OnApply = null;
        // empty action
        StartOfBattle = (_) => { };
    }

    /// <summary>
    /// A Charm with custom behavior when the stats are applied.
    /// </summary>
    /// <remarks>
    /// Useful for stats based on game variables, like the Energy bar.
    /// </remarks>
    public Charm(string name, Func<StatBonus[]> apply)
    {
        Name = name;
        Stats = [];
        OnApply = apply;
        // empty action
        StartOfBattle = (_) => { };
    }

    /// <summary>
    /// A Charm that does something at the start of the battle, usually giving an emotion to the specified <c>Actor</c>.
    /// </summary>
    public Charm(string name, StatBonus[] stats, Action<Actor> startOfBattle)
    {
        Name = name;
        Stats = stats;
        OnApply = null;
        StartOfBattle = startOfBattle;
    }

    /// <summary>
    /// A Charm that both does something at the start of the battle and when stats are applied.
    /// </summary>
    public Charm(string name, Func<StatBonus[]> apply, Action<Actor> startOfBattle)
    {
        Name = name;
        Stats = [];
        OnApply = apply;
        StartOfBattle = startOfBattle;
    }

    /// <summary>
    /// Applies this Charm's statbonuses to the provided <see cref="Stats"/>
    /// </summary>
    /// <param name="stats"></param>
    public void Apply(ref Stats stats)
    {
        foreach (StatBonus bonus in Stats)
        {
            int stat = stats.GetStat(bonus.Type);
            stat = (int)Math.Round(stat * bonus.Multiplier + bonus.FlatBonus);
            stats.SetStat(bonus.Type, stat);
        }

        if (OnApply != null)
        {
            foreach (StatBonus bonus in OnApply())
            {
                int stat = stats.GetStat(bonus.Type);
                stat = (int)Math.Round(stat * bonus.Multiplier + bonus.FlatBonus);
                stats.SetStat(bonus.Type, stat);
            }
        }
    }
}