using Newtonsoft.Json;

namespace OmoriSandbox.Modding;
internal struct JsonEnemyMod
{
    public string Name { get; set; }
    public string Atlas { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public JsonModAnimationData[] Animation { get; set; }
    public int HP { get; set; }
    public int Juice { get; set; }
    public int ATK { get; set; }
    public int DEF { get; set; }
    public int SPD { get; set; }
    public int LCK { get; set; }
    public int HIT { get; set; }
    public string[] InvalidStates { get; set; }
    public string[] EquippedSkills { get; set; }
    public string ObserveMultiSkill { get; set; }
    public string ObserveSingleSkill { get; set; }
    public JsonEnemyAIData[] AI { get; set; }
}

internal struct JsonEnemyAIData
{
    public string Emotion { get; set; }
    public JsonEnemyAIEntry[] Entries { get; set; }
}

internal struct JsonEnemyAIEntry
{
    public int Chance { get; set; }
    public string Skill { get; set; }
    public int? NumTargets { get; set; }
}