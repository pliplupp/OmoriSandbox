using Godot;
using System;

public class TierStatModifier : TurnStatModifier
{
    private int Tier;
    private readonly int MaxTier;
    public string SuccessMessage { get; private set; }
    public string FailureMessage { get; private set; }

    /// <summary>
    /// Represents a tiered stat bonus with a turn counter. Defaults to starting at tier 1 and 6 turns remaining.
    /// Use <see cref="WithTier(int)"/> and <see cref="WithTurnsLeft(int)"/> to modify these starting values.
    /// </summary>
    /// <param name="bonuses">A list of stat bonuses. Each index of is list is mapped to the stat to provide at that tier.</param>
    public TierStatModifier(StatBonus[] bonuses) : base(bonuses)
    {
        Tier = 1;
        MaxTier = bonuses.Length;
    }

    public TierStatModifier WithMessages(string success, string failure)
    {
        SuccessMessage = success;
        FailureMessage = failure;
        return this;
    }

    public void SetTier(int tier)
    {
        if (tier > MaxTier)
        {
            GD.PrintErr("Cannot set a tier greater than the max tier");
            return;
        }
        Tier = tier;
    }

    public bool IncreaseTier()
    {
        if (Tier < MaxTier)
        {
            Tier++;
            TurnsLeft = 6;
            return true;
        }
        return false;
    }
 
    public override void ApplyStats(ref Stats stats)
    {
        StatBonus bonus = Bonuses[Tier - 1];
        int val = stats.GetStat(bonus.Type);
        val = (int)Math.Round(val * bonus.Multiplier + bonus.FlatBonus);
        stats.SetStat(bonus.Type, val);
    }
}