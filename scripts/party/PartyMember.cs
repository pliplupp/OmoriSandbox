using Godot;
using System.Linq;
using System.Threading.Tasks;
using OmoriSandbox.Battle;

namespace OmoriSandbox.Actors;

/// <summary>
/// An <see cref="Actor"/> that is considered a party member. Can be inherited to make a new party member.
/// </summary>
public abstract class PartyMember : Actor
{
	internal void Init(AnimatedSprite2D face, string initialState, int level, string weapon, string charm, string[] skills)
	{
		SpriteFrames animation = Animation;
        if (animation == null)
        {
            GD.PrintErr("Failed to load Face animations for PartyMember: " + Name);
            return;
        }
        // init animation
        Sprite = face;
		Sprite.SpriteFrames = animation;
		Sprite.Animation = initialState;
		Sprite.Play();
		CurrentState = initialState;

        // init stats
        Level = level;
        int idx = level - 1;
		BaseStats = new Stats(HPTree[idx], JuiceTree[idx], ATKTree[idx], DEFTree[idx], SPDTree[idx], BaseLuck, 0);
		if (!Database.TryGetWeapon(weapon, out Weapon w))
		{
			GD.PrintErr("Failed to find Weapon: " + weapon);
			return;
		}
		Weapon = w;
		
		if (!charm.Equals("none", System.StringComparison.CurrentCultureIgnoreCase))
		{
			if (!Database.TryGetCharm(charm, out Charm c))
			{
				GD.PrintErr("Failed to find Charm: " + charm);
				return;
			}
			Charm = c;
		}

		if (initialState == "toast")
			CurrentHP = 0;
		else
			CurrentHP = CurrentStats.HP;
		CurrentJuice = CurrentStats.Juice;

		EquippedSkills = skills;

		foreach (string s in EquippedSkills)
		{
			if (string.IsNullOrWhiteSpace(s))
				continue;

			if (Database.TryGetSkill(s, out var skill))
			{
				Skills.Add(s, skill);
				continue;
			}
			GD.PrintErr("Unknown skill: " + s);
		}
	}

	/// <summary>
	/// The party member's base stats, plus any stats given by a <see cref="Battle.Weapon"/> and/or <see cref="Battle.Charm"/>.
	/// </summary>
	/// <returns></returns>
	protected override Stats GetBaseStats()
	{
		Stats stats = BaseStats + Weapon.Stats;
		if (Charm != null)
		{
			stats += Charm.Apply();
		}
		return stats;
	}

	public override bool IsStateValid(string state)
	{
		return !(InvalidStates.Any(x => x == state) || (Charm != null && Charm.Name == "Paper Bag"));
	}

    public override async Task OnStartOfBattle()
    {
        if (Weapon.Name == "LOL Sword")
		{
			SetState("happy", true);
		}
		Charm?.StartOfBattle(this);
		await Task.CompletedTask;
    }

	public abstract SpriteFrames Animation { get; }
	/// <summary>
	/// The party member's HP scaling, stat by level.
	/// </summary>
	public abstract int[] HPTree { get; }
    /// <summary>
    /// The party member's Juice stat scaling, by level.
    /// </summary>
    public abstract int[] JuiceTree { get; }
    /// <summary>
    /// The party member's ATK stat scaling, by level.
    /// </summary>
    public abstract int[] ATKTree { get; }
    /// <summary>
    /// The party member's DEF stat scaling, by level.
    /// </summary>
    public abstract int[] DEFTree { get; }
    /// <summary>
    /// The party member's SPD stat scaling, by level.
    /// </summary>
    public abstract int[] SPDTree { get; }
    /// <summary>
    /// The party member's LCK stat.
    /// </summary>
    public abstract int BaseLuck { get; }
	/// <summary>
	/// The party member's equipped charm. Will be null if no charm is equipped.
	/// </summary>
	public Charm Charm { get; private set; }
	/// <summary>
	/// The party member's equipped weapon.
	/// </summary>
	public Weapon Weapon { get; private set; }
	public string[] EquippedSkills { get; protected set; }
	/// <summary>
	/// A list of invalid states this party member cannot feel. Used in <see cref="IsStateValid(string)"/>
	/// </summary>
	public abstract string[] InvalidStates { get; }
	/// <summary>
	/// If this party member is considered to be a "real world" member. Mainly used to change the UI buttons.
	/// </summary>
	public abstract bool IsRealWorld { get; }
}
