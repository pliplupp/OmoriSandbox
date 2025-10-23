using OmoriSandbox.Actors;

namespace OmoriSandbox.Battle;

/// <summary>
/// A command that an actor can be given in battle.
/// </summary>
public class BattleCommand
{
    /// <summary>
    /// The <see cref="Actor"/> this command is being given to.
    /// </summary>
    public Actor Actor;

    /// <summary>
    /// The <see cref="Actor"/> this command is targeting.
    /// </summary>
    public Actor Target;

    /// <summary>
    /// The <see cref="BattleAction"/> that is carried out when this command is performed.
    /// </summary>
    public BattleAction Action;

	/// <summary>
	/// Creates a new <see cref="BattleCommand"/> context.
	/// </summary>
	/// <param name="actor">The <see cref="Actor"/> this command is being given to.</param>
	/// <param name="target">The <see cref="Actor"/> this command is targeting.</param>
	/// <param name="action">The <see cref="BattleAction"/> that is carried out when this command is performed.</param>
    public BattleCommand(Actor actor, Actor target, BattleAction action)
	{
		Actor = actor;
		Target = target;
		Action = action;
	}
}
