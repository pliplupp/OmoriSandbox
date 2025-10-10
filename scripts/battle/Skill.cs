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


	public Skill(string name, string description, SkillTarget target, Func<Actor, Actor, Task> effect, int cost, bool hidden = false, bool goesFirst = false)
		: base(name, description, target, effect)
	{
		Cost = cost;
		Hidden = hidden;
		GoesFirst = goesFirst;
	}
}
