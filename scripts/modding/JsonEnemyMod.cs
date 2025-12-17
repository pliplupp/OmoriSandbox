namespace OmoriSandbox.Modding;
internal struct JsonEnemyMod
{
    public string Name { get; }
    public string Atlas { get; }
    public int Width { get; }
    public int Height { get; }
    public JsonModAnimationData[] Animation { get; }
    public int HP { get; }
    public int Juice { get; }
    public int ATK { get; }
    public int DEF { get; }
    public int SPD { get; }
    public int LCK { get; }
    public int HIT { get; }
    public string[] InvalidStates { get; }
    public string[] EquippedSkills { get; }
    public JsonEnemyAIData[] AI { get; }
}

internal struct JsonEnemyAIData
{
    public string Emotion { get; }
    public JsonEnemyAIEntry[] Entries { get; }
}

internal struct JsonEnemyAIEntry
{
    public int Chance { get; }
    public string Skill { get; }
}