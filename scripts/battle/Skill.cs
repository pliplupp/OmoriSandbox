using OmoriSandbox.Actors;
using System;
using System.Collections.Generic;
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
	/// Whether this skill is performed first.
	/// </summary>
	public bool GoesFirst { get; private set; }
	/// <summary>
	/// Whether this skill is hidden in the skill menu.
	/// </summary>
	public bool Hidden { get; private set; }

	/// <summary>
	/// Whether the given <param name="actor"> meets the requirements to use this skill.</param>
	/// </summary>
	/// <remarks>
	/// By default, all skills have the requirement that the user is not afraid or stressed.
	/// Juice cost is an inherent requirement for all skills and cannot be modified.
	/// </remarks>
	/// <param name="actor">The actor to check.</param>
	/// <returns>True if the actor can use this skill at the given moment.</returns>
	public bool MeetsRequirements(Actor actor) => Requirement(actor);
	
	private Func<Actor, bool> Requirement = actor => actor.CurrentState is not "afraid" and not "stressed";

	/// <summary>
	/// Creates a new single-target skill. Must be registered via <see cref="Modding.Mod.RegisterSkill(string, Skill)"/> to appear in-game.
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
	
	/// <summary>
	/// Creates a new multi-target skill. Must be registered via <see cref="Modding.Mod.RegisterSkill(string, Skill)"/> to appear in-game.
	/// </summary>
	/// <param name="name">The name of the skill.</param>
	/// <param name="description">The description of the skill.<br/>You can use the [actor] tag to place the actor's name in the description.</param>
	/// <param name="target">What this skill targets. Mainly used for visual targeting.</param>
	/// <param name="effect">The code that runs when this skill is used.</param>
	/// <param name="cost">How much juice this skill costs to use.</param>
	/// <param name="hidden">Whether this skill should show up in the actor's skill list.</param>
	/// <param name="goesFirst">Whether this skill should always go first.</param>
	public Skill(string name, string description, SkillTarget target, Func<Actor, IReadOnlyList<Actor>, Task> effect, int cost, bool hidden = false, bool goesFirst = false)
		: base(name, description, target, effect)
	{
		Cost = cost;
		Hidden = hidden;
		GoesFirst = goesFirst;
	}

	/// <summary>
	/// Adds a custom requirement for this skill to be used.
	/// If the actor does not meet the requirements to use the skill, the skill will be greyed out in the menu.
	/// </summary>
	/// <remarks>
	/// By default, all skills have the requirement that the user is not afraid or stressed.
	/// By adding a custom requirement, you must also re-add those checks in yourself if necessary.<br/>
	/// Juice cost is an inherent requirement for all skills and cannot be modified.
	/// </remarks>
	public Skill WithCustomRequirement(Func<Actor, bool> requirement)
	{
		Requirement = requirement;
		return this;
	}
}
