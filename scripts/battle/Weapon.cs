namespace OmoriSandbox.Battle;

/// <summary>
/// A weapon that can be equipped by a <see cref="PartyMember"/>
/// </summary>
/// <param name="name">The name of the weapon.</param>
/// <param name="stats">The stats that this weapon provides.</param>
public struct Weapon(string name, Stats stats)
{
    /// <summary>
    /// The name of the weapon.
    /// </summary>
    public string Name { get; private set; } = name;
    /// <summary>
    /// The stats that this weapon provides.
    /// </summary>
    public Stats Stats { get; private set; } = stats;
}