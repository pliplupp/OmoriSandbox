using OmoriSandbox.Actors;
using System;
using System.Threading.Tasks;

namespace OmoriSandbox.Battle;

/// <summary>
/// A skill, including regular attacks
/// </summary>
public class Skill : BattleAction
{
	/// <summary>
	/// The cost of the skill in Juice.
	/// </summary>
	public int Cost { get; private set; }
	/// <summary>
	/// Whether or not this skill is performed first.
	/// </summary>
	public bool GoesFirst { get; private set; }
	/// <summary>
	/// Whether or not this skill is hidden in the skill menu.
	/// </summary>
	public bool Hidden { get; private set; }

	/// <summary>
	/// Creates a new skill. Must be registered via <see cref="Modding.Mod.RegisterSkill(string, Skill)"/> to appear in-game.
	/// </summary>
	/// <param name="name">The name of the skill.</param>
	/// <param name="description">The description of the skill.<br/>You can use the [actor] tag to place the actor's name in the description.</param>
	/// <param name="target">What this skill targets. Mainly used for visual targeting.</param>
	/// <param name="effect">The code that runs when this skill is used.</param>
	/// <param name="cost">How much juice this skill costs to use.</param>
	/// <param name="hidden">Whether this skill should show up in the actor's skill list.</param>
	/// <param name="goesFirst">Whether this skill should always go first.</param>
	public Skill(string name, string description, SkillTarget target, Func<Actor, Actor, Task> effect, int cost, bool hidden = false, bool goesFirst = false)
		: base(name, description, target, effect)
	{
		Cost = cost;
		Hidden = hidden;
		GoesFirst = goesFirst;
	}
}
