public class TurnStatModifier : StatModifier
{
    public int TurnsLeft { get; protected set; }

    /// <summary>
    /// Represents a stat bonus with a turn counter. Defaults to starting at 6 turns remaining.
    /// Use <see cref="SetTurnsLeft(int)"/> to modify these starting values.
    /// </summary>
    /// <param name="bonuses">A list of stat bonuses</param>
    public TurnStatModifier(params StatBonus[] bonuses) : base(bonuses)
    {
        TurnsLeft = 6;
    }

    public void SetTurnsLeft(int turnsLeft)
    {
        TurnsLeft = turnsLeft;
    }

    public void DecreaseTurns()
    {
        TurnsLeft--;
    }
}