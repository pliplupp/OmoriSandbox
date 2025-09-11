public struct StatBonus
{
    public readonly StatType Type;
    public readonly float Multiplier;
    public readonly int FlatBonus;

    public StatBonus(StatType type, float multiplier)
    {
        Type = type;
        Multiplier = multiplier;
        FlatBonus = 0;
    }

    public StatBonus(StatType type, int flatBonus)
    {
        Type = type;
        FlatBonus = flatBonus;
        Multiplier = 1f;
    }

    public StatBonus(StatType type, float multiplier, int flatBonus)
    {
        Type = type;
        Multiplier = multiplier;
        FlatBonus = flatBonus;
    }
}