using OmoriSandbox.Actors;
using System;
using System.Threading.Tasks;

namespace OmoriSandbox.Battle;

/// <summary>
/// An item. Represents both Snacks and Toys.
/// </summary>
public class Item : BattleAction
{
	/// <summary>
	/// Whether or not this item is a Toy.
	/// </summary>
	public bool IsToy { get; private set; }

    /// <summary>
    /// Creates a new item. Must be registered via <see cref="Modding.Mod.RegisterItem(string, Item)"/> to appear in-game.
    /// </summary>
    /// <param name="name">The name of the item.</param>
    /// <param name="description">The description of the item.<br/>You can use the [actor] tag to place the actor's name in the description.</param>
    /// <param name="target">What this item can target. Mainly used for visual targeting.</param>
    /// <param name="effect">The code that runs when this item is used.</param>
    /// <param name="isToy">Whether this item is a Toy or not.</param>
    public Item(string name, string description, SkillTarget target, Func<Actor, Actor, Task> effect, bool isToy = false)
		: base(name, description, target, effect)
	{
		IsToy = isToy;
	}
}
