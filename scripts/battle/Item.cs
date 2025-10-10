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

	public Item(string name, string description, SkillTarget target, Func<Actor, Actor, Task> effect, bool isToy = false)
		: base(name, description, target, effect)
	{
		IsToy = isToy;
	}
}
