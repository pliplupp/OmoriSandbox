using System;

namespace OmoriSandbox.Battle.Modifier;

/// <summary>
/// A generic stat modifier that can provide a different <see cref="StatBonus"/> at each tier.
/// </summary>
public class TierStatModifier : StatModifier
{
	private int Tier;
	private int MaxTier;
    /// <summary>
    /// The message to display on success.
    /// </summary>
	/// <remarks>
	/// Only requires the last portion of the sentence. As shown in the following example:
	/// <code>
	/// "ATTACK rose!"
	/// </code>
	/// </remarks>
    public string SuccessMessage { get; private set; }
    /// <summary>
    /// The message to display on failure.
    /// </summary>
    /// <remarks>
    /// Only requires the last portion of the sentence. As shown in the following example:
    /// <code>
    /// "ATTACK cannot go any higher!"
    /// </code>
    /// </remarks>
    public string FailureMessage { get; private set; }
	/// <summary>
	/// The current tier of this modifier.
	/// </summary>
	public int CurrentTier => Tier;

    /// <summary>
    /// A tiered stat bonus with no turn counter. Defaults to starting at tier 1.
    /// </summary>
    /// <param name="bonuses"></param>
    public TierStatModifier(params StatBonus[] bonuses) : base(bonuses)
	{
		Tier = 1;
		MaxTier = bonuses.Length;
	}

	/// <summary>
	/// Represents a tiered stat bonus with a turn counter. Defaults to starting at tier 1.
	/// Use <see cref="SetTier(int)"/> to modify the modifier's tier.
	/// </summary>
	/// <param name="turns">The number of turns to give this stat bonus for.</param>
	/// <param name="bonuses">A list of stat bonuses. Each index of is list is mapped to the stat to provide at that tier.</param>
	public TierStatModifier(int turns, params StatBonus[] bonuses) : base(turns, bonuses)
	{
		Tier = 1;
		MaxTier = bonuses.Length;
	}

	internal override StateIcon[] GetStateIcons()
	{
		return [StateIcons[Math.Min(Tier - 1, StateIcons.Length - 1)]];
	}

	/// <summary>
	/// Sets the messages that display in the battle log when the stat modifier is given or the tier is changed.
	/// </summary>
	/// <remarks>
	/// Only requires the last portion of the sentence. As shown in the following example:
	/// <code>
	/// WithMessages("ATTACK rose!", "ATTACK cannot go any higher!");
	/// </code>
	/// </remarks>
	/// <param name="success">The message to display on success.</param>
	/// <param name="failure">The message to display on failure.</param>
	public TierStatModifier WithMessages(string success, string failure)
	{
		SuccessMessage = success;
		FailureMessage = failure;
		return this;
	}

	/// <summary>
	/// Overrides the max tier for this stat modifier. By default, the max tier will be the length of the stat bonuses.
	/// </summary>
	/// <param name="tier">The max tier to use.</param>
	public TierStatModifier WithMaxTier(int tier)
	{
		MaxTier = tier;
		return this;
	}
	
	/// <summary>
	/// Directly sets the tier of the stat modifier.
	/// </summary>
	/// <param name="tier">The tier to set this stat modifier to.</param>
	/// <returns>If the change is successful.</returns>
	public bool SetTier(int tier)
	{
		if (Tier == MaxTier)
			return false;
		Tier = Math.Min(tier, MaxTier);
		return true;
	}

	/// <summary>
	/// Increases the tier of this stat modifier by one. Also resets the turns left counter.
	/// </summary>
	/// <returns>If the increase is successful.</returns>
	public bool IncreaseTier()
	{
		if (Tier < MaxTier)
		{
			Tier++;
			TurnsLeft = MaxTurns;
			return true;
		}

		return false;
	}

	/// <inheritdoc/>
	public override void ApplyStats(ref Stats stats)
	{
		if (Bonuses.Length == 0)
			return;
		StatBonus bonus = Bonuses[Math.Min(Tier - 1, Bonuses.Length - 1)];
		int val = stats.GetStat(bonus.Type);
		val = (int)Math.Round(val * bonus.Multiplier + bonus.FlatBonus);
		stats.SetStat(bonus.Type, val);
	}
}
