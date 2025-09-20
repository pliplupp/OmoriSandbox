using System;

public class TierStatModifier : StatModifier
{
	private int Tier;
	private readonly int MaxTier;
	public string SuccessMessage { get; private set; }
	public string FailureMessage { get; private set; }
	public int CurrentTier => Tier;

	/// <summary>
	/// Represents a tiered stat bonus with no turn counter. Defaults to starting at tier 1.
	/// Use <see cref="WithTier(int)"/> to modify the starting value.
	/// </summary>
	/// <param name="bonuses">A list of stat bonuses. Each index of is list is mapped to the stat to provide at that tier.</param>
	public TierStatModifier(params StatBonus[] bonuses) : base(bonuses)
	{
		Tier = 1;
		MaxTier = bonuses.Length;
	}

	/// <summary>
	/// Represents a tiered stat bonus with a turn counter. Defaults to starting at tier 1.
	/// Use <see cref="WithTier(int)"/> and <see cref="WithTurnsLeft(int)"/> to modify these starting values.
	/// </summary>
	/// <param name="bonuses">A list of stat bonuses. Each index of is list is mapped to the stat to provide at that tier.</param>
	public TierStatModifier(int turns, params StatBonus[] bonuses) : base(turns, bonuses)
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

	public bool SetTier(int tier)
	{
		if (Tier == MaxTier)
			return false;
		Tier = Math.Min(Tier + tier, MaxTier);
		return true;
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
