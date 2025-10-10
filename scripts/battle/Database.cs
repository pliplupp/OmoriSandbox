using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmoriSandbox.Animation;
using OmoriSandbox.Battle.Modifier;
using OmoriSandbox.Actors;
using OmoriSandbox.Modding;

namespace OmoriSandbox.Battle;

/// <summary>
/// The database where all game related data is stored.
/// </summary>
public class Database
{
	// TODO: abstract these into different registries
	private static readonly SortedDictionary<string, Func<PartyMember>> PartyMembers = [];
	private static readonly SortedDictionary<string, Func<Enemy>> Enemies = [];
	private static readonly Dictionary<string, Skill> Skills = [];
	private static readonly SortedDictionary<string, Item> Items = [];
	private static readonly SortedDictionary<string, Weapon> Weapons = [];
	private static readonly SortedDictionary<string, Charm> Charms = [];
	private static readonly Dictionary<string, Func<StatModifier>> Modifiers = [];

	static Database()
	{
		Init();
	}

	/// <summary>
	/// Tries to get a <see cref="Skill"/> of the given <paramref name="name"/> from the database.
	/// </summary>
	/// <param name="name">The name of the skill to search for.</param>
	/// <param name="skill">The returned skill, if a match is found.</param>
	/// <returns>Whether or not the skill exists in the database.</returns>
	public static bool TryGetSkill(string name, out Skill skill)
	{
		return Skills.TryGetValue(name, out skill);
	}

    /// <summary>
    /// Tries to get an <see cref="Item"/> of the given <paramref name="name"/> from the database.
    /// </summary>
    /// <param name="name">The name of the item to search for.</param>
    /// <param name="item">The returned item, if a match is found.</param>
    /// <returns>Whether or not the item exists in the database.</returns>
    public static bool TryGetItem(string name, out Item item)
	{
		return Items.TryGetValue(name, out item);
	}

    /// <summary>
    /// Tries to get a <see cref="Weapon"/> of the given <paramref name="name"/> from the database.
    /// </summary>
    /// <param name="name">The name of the weapon to search for.</param>
    /// <param name="weapon">The returned weapon, if a match is found.</param>
    /// <returns>Whether or not the weapon exists in the database.</returns>
    public static bool TryGetWeapon(string name, out Weapon weapon)
	{
		return Weapons.TryGetValue(name, out weapon);
	}

    /// <summary>
    /// Tries to get a <see cref="Charm"/> of the given <paramref name="name"/> from the database.
    /// </summary>
    /// <param name="name">The name of the charm to search for.</param>
    /// <param name="charm">The returned charm, if a match is found.</param>
    /// <returns>Whether or not the charm exists in the database.</returns>
    public static bool TryGetCharm(string name, out Charm charm)
	{
		return Charms.TryGetValue(name, out charm);
	}

	internal static void RegisterJsonPartyMember(JsonActorMod jsonActor, SpriteFrames builtFrames)
	{
		if (builtFrames == null)
			return;

		if (PartyMembers.ContainsKey(jsonActor.Name))
		{
			GD.PrintErr("Party member with name " +  jsonActor.Name + " already exists!");
			return;
		}
		PartyMembers[jsonActor.Name] = () => new ModdedPartyMember(jsonActor, builtFrames);
	}

    internal static void RegisterJsonEnemy(JsonEnemyMod jsonEnemy, SpriteFrames builtFrames)
	{
		if (builtFrames == null)
			return;

		if (Enemies.ContainsKey(jsonEnemy.Name))
		{
            GD.PrintErr("Enemy with name " + jsonEnemy.Name + " already exists!");
            return;
        }
		Enemies[jsonEnemy.Name] = () => new ModdedEnemy(jsonEnemy, builtFrames);
 	}

	internal static void RegisterModdedPartyMember<T>(string id) where T : PartyMember, new()
	{
		if (!PartyMembers.TryAdd(id, () => new T()))
		{
            GD.PrintErr("PartyMember with ID " + id + " already exists!");
        }
	}

	internal static void RegisterModdedEnemy<T>(string id) where T : Enemy, new()
	{
		if (!Enemies.TryAdd(id, () => new T()))
		{
			GD.PrintErr("Enemy with ID " + id + " already exists!");
		}
    }

	internal static void RegisterModdedStatModifier(string id, Func<StatModifier> func)
	{
		if (!Modifiers.TryAdd(id, func))
		{
			GD.PrintErr("StatModifier with ID " + id + " already exists!");
        }
    }

	internal static void RegisterModdedSkill(string id, Skill skill)
	{
		if (!Skills.TryAdd(id, skill))
		{
			GD.PrintErr("Skill with ID " + id + " already exists!");
		}
    }

	internal static void RegisterModdedItem(string id, Item item)
	{
		if (!Items.TryAdd(id, item))
		{
			GD.PrintErr("Item with ID " + id + " already exists!");
        }
    }

	internal static void RegisterModdedWeapon(string id, Weapon weapon)
	{
		if (!Weapons.TryAdd(id, weapon))
		{
			GD.PrintErr("Weapon with ID " + id + " already exists!");
        }
	}

	internal static void RegisterModdedCharm(string id, Charm charm)
	{
		if (!Charms.TryAdd(id, charm))
		{
			GD.PrintErr("Charm with ID " + id + " already exists!");
        }
    }

    internal static PartyMember CreatePartyMember(string who)
	{
		if (!PartyMembers.TryGetValue(who, out Func<PartyMember> member))
		{
			GD.PrintErr("Unknown party member: " + who);
			return null;
		}
		return member();
	}

    internal static Enemy CreateEnemy(string who)
	{
		if (!Enemies.TryGetValue(who, out Func<Enemy> enemy))
		{
			GD.PrintErr("Unknown enemy: " + who);
			return null;
		}
		return enemy();
	}

    internal static StatModifier CreateModifier(string what)
	{
		if (!Modifiers.TryGetValue(what, out Func<StatModifier> modifier))
		{
			return null;
		}
		return modifier();
	}

    internal static IEnumerable<string> GetAllWeaponNames()
	{
		return Weapons.Keys;
	}

    internal static IEnumerable<string> GetAllCharmNames()
	{
		return Charms.Keys;
	}

    internal static IEnumerable<string> GetAllItemNames()
	{
		return Items.Keys;
	}

    internal static IEnumerable<string> GetAllPartyMemberNames()
	{
		return PartyMembers.Keys;
	}

    internal static IEnumerable<string> GetAllEnemyNames()
	{
		return Enemies.Keys;
	}

    internal static IEnumerable<string> GetAllSkillNames()
	{
		return Skills.Keys;
	}

	private static void Init()
	{
		#region PARTY MEMBERS
		PartyMembers.Add("Omori", () => new Omori());
		PartyMembers.Add("Aubrey", () => new Aubrey());
		PartyMembers.Add("Hero", () => new Hero());
		PartyMembers.Add("Kel", () => new Kel());
		PartyMembers.Add("AubreyRW", () => new AubreyRW());
		PartyMembers.Add("KelRW", () => new KelRW());
		PartyMembers.Add("HeroRW", () => new HeroRW());
		PartyMembers.Add("Sunny", () => new Sunny());
		PartyMembers.Add("Basil", () => new Basil());
		#endregion

		#region ENEMIES
		Enemies.Add("LostSproutMole", () => new LostSproutMole());
		Enemies.Add("ForestBunny?", () => new ForestBunnyQuestion());
		Enemies.Add("Sweetheart", () => new Sweetheart());
		Enemies.Add("SlimeGirls", () => new SlimeGirls());
		Enemies.Add("HumphreyUvula", () => new HumphreyUvula());
		Enemies.Add("AubreyEnemy", () => new AubreyEnemy());
		Enemies.Add("BigStrongTree", () => new BigStrongTree());
		Enemies.Add("DownloadWindow", () => new DownloadWindow());
		Enemies.Add("SpaceExBoyfriend", () => new SpaceExBoyfriend());
		Enemies.Add("GatorGuyJawsum", () => new GatorGuyJawsum());
		Enemies.Add("MrJawsum", () => new MrJawsum());
		Enemies.Add("FearOfSpiders", () => new FearOfSpiders());
		Enemies.Add("UnbreadTwins", () => new UnbreadTwins());
		Enemies.Add("BunBunny", () => new BunBunny());
		Enemies.Add("Creepypasta", () => new Creepypasta());
		Enemies.Add("Slice", () => new Slice());
		Enemies.Add("Sourdough", () => new Sourdough());
		Enemies.Add("Sesame", () => new Sesame());
		Enemies.Add("LivingBread", () => new LivingBread());
		Enemies.Add("Boss", () => new Boss());
		Enemies.Add("YeOldSprout", () => new YeOldSprout());
		Enemies.Add("Mutantheart", () => new Mutantheart());
		Enemies.Add("NefariousChip", () => new NefariousChip());
		Enemies.Add("TheEarth", () => new TheEarth());
		Enemies.Add("Perfectheart", () => new Perfectheart());
		Enemies.Add("Roboheart", () => new Roboheart());
		Enemies.Add("FearOfHeights", () => new FearOfHeights());
		#endregion

		#region SKILLS
		Skills["Guard"] = new Skill(
			name: "GUARD",
			description: "Acts first, reducing damage taken for 1 turn.\nCost: 0",
			target: SkillTarget.Self,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] guards.");
				await AnimationManager.Instance.WaitForAnimation(115, self, false);
				self.AddStatModifier("Guard");
			},
			goesFirst: true
		);

		// OMORI //
		Skills["OAttack"] = new Skill(
			name: "OAttack",
			description: "Basic Attack",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForAnimation(3, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] attacks [target]!");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false);
			},
			hidden: true
		);

		Skills["SadPoem"] = new Skill(
			name: "SAD POEM",
			description: "Inflicts SAD on a friend or foe.\nCost: 5",
			target: SkillTarget.AllyOrEnemy,
			cost: 5,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] reads a sad poem.");
				await AnimationManager.Instance.WaitForAnimation(5, self, false);
				MakeSad(target);
			}
		);
		Skills["LuckySlice"] = new Skill(
			name: "LUCKY SLICE",
			description: "Acts first. An attack that's stronger\nwhen [actor] is HAPPY. Cost: 15",
			target: SkillTarget.Enemy,
			cost: 15,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForAnimation(8, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] lunges at [target]!");
				if (self.CurrentState == "happy" || self.CurrentState == "ecstatic" || self.CurrentState == "manic")
					BattleManager.Instance.Damage(self, target, () => { return (self.CurrentStats.ATK + self.CurrentStats.LCK) * 2f - target.CurrentStats.DEF; }, false);
				else
					BattleManager.Instance.Damage(self, target, () => { return (self.CurrentStats.ATK + self.CurrentStats.LCK) * 1.5f - target.CurrentStats.DEF; }, false);
			},
			goesFirst: true
		);
		Skills["Stab"] = new Skill(
			name: "STAB",
			description: "Always deals a critical hit.\nIgnores DEFENSE when [actor] is sad. Cost: 13",
			target: SkillTarget.Enemy,
			cost: 13,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForAnimation(9, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] stabs [target].");
				if (self.CurrentState == "sad" || self.CurrentState == "depressed" || self.CurrentState == "miserable")
					BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2f; }, false, guaranteeCrit: true);
				else
					BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 1.5f - target.CurrentStats.DEF; }, false, guaranteeCrit: true);
			}
		);

		Skills["Trick"] = new Skill(
			name: "TRICK",
			description: "Deals damage. If the foe is HAPPY, greatly\nreduce its SPEED. Cost: 20",
			target: SkillTarget.Enemy,
			cost: 20,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForAnimation(10, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] tricks [target].");
				if (target.CurrentState == "happy" || target.CurrentState == "ecstatic" || target.CurrentState == "manic")
				{
					AnimationManager.Instance.PlayAnimation(219, target);
					target.AddTierStatModifier("SpeedDown", 3);
				}
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 3f - target.CurrentStats.DEF; }, true);
				await Task.Delay(334);
			}
		);

		Skills["HackAway"] = new Skill(
			name: "HACK AWAY",
			description: "Attacks 3 times, hitting random foes.\nCost: 30",
			target: SkillTarget.AllEnemies,
			cost: 30,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForScreenAnimation(6, true);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] slashes wildly!");
				List<Enemy> allEnemies = BattleManager.Instance.GetAllEnemies();
				List<Enemy> targets = [];
				for (int i = 0; i < 3; i++)
				{
					targets.Add(allEnemies[GameManager.Instance.Random.RandiRange(0, allEnemies.Count - 1)]);
				}
				foreach (Enemy enemy in targets)
				{
					BattleManager.Instance.Damage(self, enemy, () =>
					{
						if (self.CurrentState == "angry" || self.CurrentState == "enraged" || self.CurrentState == "furious")
						{
							return self.CurrentStats.ATK * 2.25f - enemy.CurrentStats.DEF;
						}
						return self.CurrentStats.ATK * 2f - enemy.CurrentStats.DEF;
					}, false);
				}
			}
		);

		Skills["PainfulTruth"] = new Skill(
			name: "PAINFUL TRUTH",
			description: "Deals damage to a foe. [actor] and the foe\nbecome SAD. Cost: 10",
			target: SkillTarget.Enemy,
			cost: 10,
			effect: async (self, target) =>
			{
				AnimationManager.Instance.PlayAnimation(5, self, false);
				AnimationManager.Instance.PlayAnimation(19, target);

				MakeSad(self);
				MakeSad(target);

				await Task.Delay(1000);

				BattleLogManager.Instance.QueueMessage(self, target, "[actor] whispers something\nto [target].");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false);
			}
		);

		Skills["Mock"] = new Skill(
			name: "MOCK",
			description: "Deals damage. If the foe is ANGRY, greatly\nreduce it's ATTACK. Cost: 20",
			target: SkillTarget.Enemy,
			cost: 20,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForAnimation(12, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] mocks [target].");
				if (target.CurrentState == "angry" || target.CurrentState == "enraged" || target.CurrentState == "furious")
				{
					AnimationManager.Instance.PlayAnimation(219, target);
					target.AddTierStatModifier("AttackDown", 3);
				}
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 3f - target.CurrentStats.DEF; }, false);
				await Task.Delay(334);
			}
		);

		Skills["Shun"] = new Skill(
			name: "SHUN",
			description: "Deals damage. If the foe is SAD, greatly\nreduce its DEFENSE. Cost: 20",
			target: SkillTarget.Enemy,
			cost: 20,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForAnimation(11, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] shuns [target].");
				if (target.CurrentState == "sad" || target.CurrentState == "depressed" || target.CurrentState == "miserable")
				{
					AnimationManager.Instance.PlayAnimation(219, target);
					target.AddTierStatModifier("DefenseDown", 3);
				}
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 3f - target.CurrentStats.DEF; }, false);
				await Task.Delay(334);
			}
		);

		Skills["Stare"] = new Skill(
			name: "STARE",
			description: "Reduces all of a foe's STATS.\nCost: 45",
			target: SkillTarget.Enemy,
			cost: 45,
			effect: async (self, target) =>
			{
				AnimationManager.Instance.PlayAnimation(18, target);
				await Task.Delay(1660);
				AnimationManager.Instance.PlayAnimation(219, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] stares at [target].");
				BattleLogManager.Instance.QueueMessage(self, target, "[target] feels uncomfortable.");
				target.AddStatModifier("AttackDown");
				target.AddStatModifier("DefenseDown");
				target.AddStatModifier("SpeedDown");
				await Task.Delay(334);
			}
		);

		Skills["Exploit"] = new Skill(
			name: "EXPLOIT",
			description: "Deals extra damage to a HAPPY, SAD, or\nANGRY foe. Cost: 30",
			target: SkillTarget.Enemy,
			cost: 30,
			effect: async (self, target) =>
			{
				if (target.CurrentState == "happy" || target.CurrentState == "ecstatic" || target.CurrentState == "manic")
				{
					await AnimationManager.Instance.WaitForAnimation(10, target);
				}
				else if (target.CurrentState == "sad" || target.CurrentState == "depressed" || target.CurrentState == "miserable")
				{
					await AnimationManager.Instance.WaitForAnimation(11, target);
				}
				else if (target.CurrentState == "angry" || target.CurrentState == "enraged" || target.CurrentState == "furious")
				{
					await AnimationManager.Instance.WaitForAnimation(12, target);
				}
				else
				{
					await AnimationManager.Instance.WaitForAnimation(123, target);
				}
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] exploits [target]'s EMOTIONS!");
				if (target.CurrentState != "neutral")
				{
					BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 3.5f - target.CurrentStats.DEF; }, false);
				}
				else
				{
					BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2.5f - target.CurrentStats.DEF; }, false);
				}
			}
		);

		Skills["FinalStrike"] = new Skill(
			name: "FINAL STRIKE",
			description: "Strikes all foes. Deals more damage if [actor]\nhas a higher stage of EMOTION. Cost: 50",
			target: SkillTarget.AllEnemies,
			cost: 50,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] releases his ultimate\nattack!");
				await AnimationManager.Instance.WaitForScreenAnimation(13, true);
				float multiplier = 3f;
				if (self.CurrentState == "manic" || self.CurrentState == "miserable" || self.CurrentState == "furious")
					multiplier = 6f;
				else if (self.CurrentState == "ecstatic" || self.CurrentState == "depressed" || self.CurrentState == "enraged")
					multiplier = 5f;
				else if (self.CurrentState == "happy" || self.CurrentState == "sad" || self.CurrentState == "angry")
					multiplier = 5f;
				foreach (Enemy enemy in BattleManager.Instance.GetAllEnemies())
				{
					BattleManager.Instance.Damage(self, enemy, () => { return self.CurrentStats.ATK * multiplier - enemy.CurrentStats.DEF; }, false);
				}
			}
		);

		Skills["RedHands"] = new Skill(
			name: "RED HANDS",
			description: "Deals big damage 4 times.\nCost: 75",
			target: SkillTarget.Enemy,
			cost: 75,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForRedHands();
				for (int i = 0; i < 4; i++)
				{
					BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 3f - target.CurrentStats.DEF; }, false);
				}
			}
		);

		Skills["Vertigo"] = new Skill(
			name: "VERTIGO",
			description: "Deals damage to all foes based on user's\nSPEED and greatly reduces their ATTACK.",
			target: SkillTarget.AllEnemies,
			cost: 45,
			effect: async (self, target) =>
			{
				AudioManager.Instance.PlaySFX("SE_bs_scare4", 0.5f, 0.9f);
				await AnimationManager.Instance.WaitForOmoriSpecialAnimation(
						"res://assets/pictures/dark_overlay.png",
						"res://assets/pictures/fear_hands_effect.png"
					);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] throws the foes off balance!");
				BattleLogManager.Instance.QueueMessage("All foes' ATTACK fell!");
				foreach (Enemy enemy in BattleManager.Instance.GetAllEnemies())
				{
					enemy.AddTierStatModifier("AttackDown", 3, silent: true);
					AnimationManager.Instance.PlayAnimation(219, enemy);
					BattleManager.Instance.Damage(self, enemy, () => { return self.CurrentStats.SPD * 3f - enemy.CurrentStats.DEF; }, false);
				}
			}
		);

		Skills["Cripple"] = new Skill(
			name: "CRIPPLE",
			description: "Deals big damage to all foes and\ngreatly reduces their SPEED.",
			target: SkillTarget.AllEnemies,
			cost: 45,
			effect: async (self, target) =>
			{
				AudioManager.Instance.PlaySFX("SE_something_ALT");
				await AnimationManager.Instance.WaitForOmoriSpecialAnimation(
						"res://assets/pictures/dark_overlay.png",
						"res://assets/pictures/fear_spiders_effect.png"
					);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] cripples the foes!");
				BattleLogManager.Instance.QueueMessage("All foes' SPEED fell!");
				foreach (Enemy enemy in BattleManager.Instance.GetAllEnemies())
				{
					enemy.AddTierStatModifier("SpeedDown", 3, silent: true);
					AnimationManager.Instance.PlayAnimation(219, enemy);
					BattleManager.Instance.Damage(self, enemy, () => { return self.CurrentStats.ATK * 3.5f - enemy.CurrentStats.DEF; }, false);
				}
			}
		);

		Skills["Suffocate"] = new Skill(
		   name: "SUFFOCATE",
		   description: "Deals 400 damage to all foes and\ngreatly reduces their DEFENSE.",
		   target: SkillTarget.AllEnemies,
		   cost: 45,
		   effect: async (self, target) =>
		   {
			   AudioManager.Instance.PlaySFX("SE_reverse_swell", 0.8f, 0.9f);
			   await AnimationManager.Instance.WaitForOmoriSpecialAnimation(
					   "res://assets/pictures/dark_overlay.png",
					   "res://assets/pictures/fear_hair.png"
				   );
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] suffocates the foes!");
			   BattleLogManager.Instance.QueueMessage("All foes feel a shortness of breath.");
			   BattleLogManager.Instance.QueueMessage("All foes' DEFENSE fell!");
			   foreach (Enemy enemy in BattleManager.Instance.GetAllEnemies())
			   {
				   AnimationManager.Instance.PlayAnimation(219, enemy);
				   BattleManager.Instance.Damage(self, enemy, () => { return 400; }, false, 0f, neverCrit: true);
				   enemy.AddTierStatModifier("DefenseDown", 3, silent: true);
			   }
		   }
		);

		Skills["AttackAgain1"] = new Skill(
			name: "Attack Again 1",
			description: "Omori Followup",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] readies his blade.");
				await Task.Delay(1000);
				BattleLogManager.Instance.ClearBattleLog();
				await AnimationManager.Instance.WaitForAnimation(3, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] attacks again!");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false);
			},
			hidden: true
		);

		Skills["AttackAgain2"] = new Skill(
			name: "Attack Again 2",
			description: "Omori Followup",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] readies his blade.");
				await Task.Delay(1000);
				BattleLogManager.Instance.ClearBattleLog();
				await AnimationManager.Instance.WaitForAnimation(3, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] attacks again!");
				BattleManager.Instance.Damage(self, target, () => { return (self.CurrentStats.ATK * 2) + self.CurrentStats.LCK - target.CurrentStats.DEF; }, false);
			},
			hidden: true
		);

		Skills["AttackAgain3"] = new Skill(
			name: "Attack Again 3",
			description: "Omori Followup",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] readies his blade.");
				await Task.Delay(1000);
				BattleLogManager.Instance.ClearBattleLog();
				await AnimationManager.Instance.WaitForAnimation(290, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] attacks again!");
				BattleManager.Instance.Damage(self, target, () => { return (self.CurrentStats.ATK * 2) + self.CurrentStats.LCK - target.CurrentStats.DEF; }, false);
				await Task.Delay(500);
				BattleManager.Instance.Damage(self, target, () => { return (self.CurrentStats.ATK * 2) + self.CurrentStats.LCK - target.CurrentStats.DEF; }, false);
			},
			hidden: true
		);

		Skills["Trip1"] = new Skill(
			name: "Trip 1",
			description: "Omori Followup",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] walks forward.");
				await Task.Delay(1000);
				BattleLogManager.Instance.ClearBattleLog();
				await AnimationManager.Instance.WaitForAnimation(14, target);
				AnimationManager.Instance.PlayAnimation(219, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] trips [target]!");
				target.AddStatModifier("SpeedDown");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK + self.CurrentStats.LCK - target.CurrentStats.DEF; }, false);
			},
			hidden: true
		);

		Skills["Trip2"] = new Skill(
			name: "Trip 2",
			description: "Omori Followup",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] walks forward.");
				await Task.Delay(1000);
				BattleLogManager.Instance.ClearBattleLog();
				await AnimationManager.Instance.WaitForAnimation(14, target);
				AnimationManager.Instance.PlayAnimation(219, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] trips [target]!");
				target.AddTierStatModifier("SpeedDown", 2);
				target.SetState("sad");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK + self.CurrentStats.LCK - target.CurrentStats.DEF; }, false);
			},
			hidden: true
		);

		Skills["Trip3"] = new Skill(
			name: "Trip 3",
			description: "Omori Followup",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] walks forward.");
				await Task.Delay(1000);
				BattleLogManager.Instance.ClearBattleLog();
				await AnimationManager.Instance.WaitForAnimation(14, target);
				AnimationManager.Instance.PlayAnimation(219, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] trips [target]!");
				target.AddTierStatModifier("SpeedDown", 3);
				target.SetState("sad");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK + self.CurrentStats.LCK - target.CurrentStats.DEF; }, false);
			},
			hidden: true
		);

		Skills["ReleaseEnergy1"] = new Skill(
			name: "Release Energy 1",
			description: "Omori Followup",
			target: SkillTarget.AllEnemies,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] and friends come together and\nuse their ultimate attack!");
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					AnimationManager.Instance.PlayAnimation(243, member.Actor, false);
				}
				await AnimationManager.Instance.WaitForReleaseEnergy();
				BattleLogManager.Instance.ClearBattleLog();
				await AnimationManager.Instance.WaitForScreenAnimation(15, true);
				foreach (Enemy enemy in BattleManager.Instance.GetAllEnemies())
				{
					BattleManager.Instance.Damage(self, enemy, () => { return 300; }, true, 0f, false, true);
				}
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					member.Actor.AddStatModifier("ReleaseEnergy");
				}
			},
			hidden: true
		);

		Skills["ReleaseEnergy2"] = new Skill(
			name: "Release Energy 2",
			description: "Omori Followup",
			target: SkillTarget.AllEnemies,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] and friends come together and\nuse their ultimate attack!");
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					AnimationManager.Instance.PlayAnimation(243, member.Actor, false);
				}
				await AnimationManager.Instance.WaitForReleaseEnergy();
				BattleLogManager.Instance.ClearBattleLog();
				await AnimationManager.Instance.WaitForScreenAnimation(15, true);
				foreach (Enemy enemy in BattleManager.Instance.GetAllEnemies())
				{
					BattleManager.Instance.Damage(self, enemy, () => { return 600; }, true, 0f, false, true);
				}
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					member.Actor.AddStatModifier("ReleaseEnergy");
				}
			},
			hidden: true
		);

		Skills["ReleaseEnergy3"] = new Skill(
			name: "Release Energy 3",
			description: "Omori Followup",
			target: SkillTarget.AllEnemies,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] and friends come together and\nuse their ultimate attack!");
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					AnimationManager.Instance.PlayAnimation(243, member.Actor, false);
				}
				await AnimationManager.Instance.WaitForReleaseEnergy();
				BattleLogManager.Instance.ClearBattleLog();
				await AnimationManager.Instance.WaitForScreenAnimation(15, true);
				foreach (Enemy enemy in BattleManager.Instance.GetAllEnemies())
				{
					BattleManager.Instance.Damage(self, enemy, () => { return 1000; }, true, 0f, false, true);
				}
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					member.Actor.AddStatModifier("ReleaseEnergy");
				}
			},
			hidden: true
		);

		// SUNNY

		Skills["SRWAttack"] = new Skill(
			name: "Attack",
			description: "Basic Attack",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForAnimation(108, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] attacks [target]!");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false, neverCrit: true);
			},
			hidden: true
		);

		Skills["CalmDown"] = new Skill(
			name: "CALM DOWN",
			description: "Removes EMOTIONS and heals some HEART.\nCost: 0",
			target: SkillTarget.Self,
			cost: 0,
			effect: async (self, target) =>
			{
				AudioManager.Instance.FadeBGMTo(10f);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] calms down.");
				AnimationManager.Instance.PlayScreenAnimation(104, false);
				await Task.Delay(2500);
				self.Heal((int)Math.Round(self.BaseStats.MaxHP * 0.5, MidpointRounding.AwayFromZero));
				self.SetState("neutral", true);
				AudioManager.Instance.FadeBGMTo(100f);
			},
			goesFirst: true
		);

		// BASIL //
		Skills["BAttack"] = new Skill(
			name: "BAttack",
			description: "Basic Attack",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForAnimation(142, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] attacks [target]!");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false);
			},
			hidden: true
		);

		Skills["BodySlam"] = new Skill(
			name: "BODY SLAM",
			description: "Deals damage that increases with more ENERGY.\nCost: 40",
			target: SkillTarget.Enemy,
			cost: 40,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForAnimation(124, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] body slams [target]!");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 + (BattleManager.Instance.Energy * self.Level) - target.CurrentStats.DEF; }, false);
			}
		);

		Skills["Cheer"] = new Skill(
			name: "CHEER",
			description: "Heals all friends JUICE by 20%. Grealtly increases\na STAT if [actor] is feeling an EMOTION. Cost: 80",
			target: SkillTarget.AllAllies,
			cost: 80,
			effect: async (self, target) =>
			{
				AnimationManager.Instance.PlayScreenAnimation(340, false);
				await Task.Delay(1000);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] cheers!");
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					BattleManager.Instance.HealJuice(self, member.Actor, () => { return member.Actor.CurrentStats.MaxJuice * 0.2f; });
					string modifier = member.Actor.CurrentState switch
					{
						"happy" or "ecstatic" or "manic" => "SpeedUp",
						"sad" or "depressed" or "miserable" => "DefenseUp",
						"angry" or "enraged" or "furious" => "AttackUp",
						_ => null
					};
					if (modifier != null)
					{
						member.Actor.AddTierStatModifier(modifier, 3);
						AnimationManager.Instance.PlayAnimation(214, member.Actor, false);
					}
				}
			}
		);

		Skills["Photograph"] = new Skill(
			name: "PHOTOGRAPH",
			description: "Acts first, reducing HIT RATE for all foes for 1\nturn. All foes target [actor] for 1 turn. Cost: 50",
			target: SkillTarget.AllEnemies,
			cost: 50,
			effect: async (self, target) =>
			{
				AudioManager.Instance.PlaySFX("SYS_tag1", volume: 0.9f);
				AnimationManager.Instance.PlayPhotograph();
				await Task.Delay(500);
				self.AddStatModifier("Taunt");
				foreach (Enemy enemy in BattleManager.Instance.GetAllEnemies())
				{
					AnimationManager.Instance.PlayAnimation(219, enemy);
					enemy.AddStatModifier("PhotographHitRateDown");
				}
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] takes a picture.");
				BattleLogManager.Instance.QueueMessage("The foe's HIT RATE fell!");
			},
			goesFirst: true
		);

		Skills["HerbalRemedy"] = new Skill(
			name: "HERBAL REMEDY",
			description: "Heals a friend for 75% of their HEART. Also\nincreases ENERGY by 1. Cost: 35",
			target: SkillTarget.Ally,
			cost: 35,
			effect: async (self, target) =>
			{
				AnimationManager.Instance.PlayScreenAnimation(341, false);
				await Task.Delay(1000);
				AnimationManager.Instance.PlayAnimation(342, target, false);
				await Task.Delay(1000);
				AnimationManager.Instance.PlayAnimation(212, target, false);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] brings out a remedy.");
				BattleManager.Instance.Heal(self, target, () => { return target.CurrentStats.MaxHP * 0.75f; });
				BattleManager.Instance.AddEnergy(1);
			}
		);

		Skills["Tulip"] = new Skill(
			name: "TULIP",
			description: "Deals damage to all foes based on [first]'s\nSTATS. Cost: 40",
			target: SkillTarget.AllEnemies,
			cost: 40,
			effect: async (self, target) =>
			{
				AudioManager.Instance.PlaySFX("GEN_shine", 0.5f, 0.9f);
				await AnimationManager.Instance.WaitForBasilSpecialAnimation("res://assets/pictures/border_tulip.png", 326);
				PartyMember first = BattleManager.Instance.GetPartyMember(0);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] plants a TULIP.");
				foreach (Enemy enemy in BattleManager.Instance.GetAllEnemies())
				{
					BattleManager.Instance.Damage(first, enemy, () => { return (first.CurrentStats.ATK + first.CurrentStats.DEF + first.CurrentStats.SPD + (first.CurrentStats.LCK * 5)) - enemy.CurrentStats.DEF; }, false);
				}
			}
		);

		Skills["Gladiolus"] = new Skill(
			name: "GLADIOLUS",
			description: "Deals big damage that ignores DEFENSE.\nAlways hits right in the HEART. Cost: 40",
			target: SkillTarget.Enemy,
			cost: 40,
			effect: async (self, target) =>
			{
				AudioManager.Instance.PlaySFX("GEN_shine", 0.5f, 0.9f);
				await AnimationManager.Instance.WaitForBasilSpecialAnimation("res://assets/pictures/border_gladiolus.png", 290);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] plants a GLADIOLUS.");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 4; }, false, 0.1f, true);
			}
		);

		Skills["Cactus"] = new Skill(
			name: "CACTUS",
			description: "Deals damage based on DEFENSE and HEART\ninstead of ATTACK. Cost: 40",
			target: SkillTarget.Enemy,
			cost: 40,
			effect: async (self, target) =>
			{
				AudioManager.Instance.PlaySFX("GEN_shine", 0.5f, 0.9f);
				await AnimationManager.Instance.WaitForBasilSpecialAnimation("res://assets/pictures/border_cactus.png", 124);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] plants a CACTUS.");
				BattleManager.Instance.Damage(self, target, () => { return (self.CurrentStats.DEF * 2) + self.CurrentHP - target.CurrentStats.DEF; }, false, 0.1f);
			}
		);

		Skills["Rose"] = new Skill(
			name: "ROSE",
			description: "Acts first, reducing all foes' ATTACK. Heals\nall friends for 40% of their HEART. Cost: 50",
			target: SkillTarget.AllEnemies,
			cost: 50,
			goesFirst: true,
			effect: async (self, target) =>
			{
				AudioManager.Instance.PlaySFX("GEN_shine", 0.5f, 0.9f);
				await AnimationManager.Instance.WaitForBasilSpecialAnimation("res://assets/pictures/border_rose.png", 335);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] plants a ROSE.");
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					AnimationManager.Instance.PlayAnimation(212, member.Actor, false);
					int heal = (int)Math.Round(self.CurrentStats.MaxHP * 0.4f, MidpointRounding.AwayFromZero);
					member.Actor.Heal(heal);
					BattleManager.Instance.SpawnDamageNumber(heal, member.Actor.CenterPoint, DamageType.Heal);
				}
				await Task.Delay(500);
				foreach (Enemy enemy in BattleManager.Instance.GetAllEnemies())
				{
					AnimationManager.Instance.PlayAnimation(219, enemy);
					enemy.AddStatModifier("AttackDown", silent: true);
				}
			}
		);

		Skills["FlowerCrown"] = new Skill(
			name: "FLOWER CROWN",
			description: "Deals big damage 4 times.\nCost: 75",
			target: SkillTarget.Enemy,
			cost: 75,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForFlowerCrown();
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] makes a FLOWER CROWN.");
				for (int i = 0; i < 4; i++)
				{
					BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2.5f - target.CurrentStats.DEF; }, false);
				}
			}
		);

		Skills["Vent"] = new Skill(
			name: "Vent",
			description: "Basil Followup",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				AnimationManager.Instance.PlayAnimation(142, target);
				await Task.Delay(60);
				await AnimationManager.Instance.WaitForAnimation(3, target);
				PartyMember first = BattleManager.Instance.GetPartyMember(0);
				BattleLogManager.Instance.QueueMessage(self, first, "[actor] and [target] vent their ANGER!");
				BattleManager.Instance.Damage(self, target, () => { return ((first.CurrentStats.ATK * 1.5f) + (self.CurrentStats.ATK * 1.5f) - target.CurrentStats.DEF); }, true, 0.1f);
				MakeAngry(first);
				MakeAngry(self);
			}
		);

		Skills["Mull"] = new Skill(
			name: "Mull",
			description: "Basil Followup",
			target: SkillTarget.Ally,
			cost: 0,
			effect: async (self, target) =>
			{
				PartyMember first = BattleManager.Instance.GetPartyMember(0);
				BattleLogManager.Instance.QueueMessage(self, first, "[actor] and [target] mull over SAD thoughts.");
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					AnimationManager.Instance.PlayAnimation(213, member.Actor, false);
					BattleManager.Instance.HealJuice(self, member.Actor, () => { return member.Actor.CurrentStats.MaxJuice * 0.25f; });
				}
				// only character 1 and basil become sad
				MakeSad(first);
				MakeSad(self);
				await Task.CompletedTask;
			}
		);

		Skills["Comfort"] = new Skill(
			name: "Comfort",
			description: "Basil Followup",
			target: SkillTarget.Ally,
			cost: 0,
			effect: async (self, target) =>
			{
				PartyMember first = BattleManager.Instance.GetPartyMember(0);
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					AnimationManager.Instance.PlayAnimation(212, member.Actor, false);
					BattleManager.Instance.Heal(self, member.Actor, () => { return member.Actor.CurrentStats.MaxJuice * 0.25f; });
				}
				BattleLogManager.Instance.QueueMessage(self, first, "[actor] and [target] comfort each other.");
				// only character 1 and basil become happy
				MakeHappy(first);
				MakeHappy(self);
				await Task.CompletedTask;
			}
		);

		Skills["ReleaseEnergyBasil"] = new Skill(
			name: "Release Energy Basil",
			description: "Omori Followup",
			target: SkillTarget.AllEnemies,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] and friends come together and\nuse their ultimate attack!");
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					AnimationManager.Instance.PlayAnimation(243, member.Actor, false);
				}
				await AnimationManager.Instance.WaitForReleaseEnergyBasil();
				BattleLogManager.Instance.ClearBattleLog();
				await AnimationManager.Instance.WaitForRedHands();
				await AnimationManager.Instance.WaitForFlowerCrown();
				await AnimationManager.Instance.WaitForScreenAnimation(344, true);
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					AnimationManager.Instance.PlayAnimation(212, member.Actor, false);
					member.Actor.Heal(member.Actor.CurrentStats.MaxHP);
					member.Actor.HealJuice(member.Actor.CurrentStats.MaxJuice);
				}
				await Task.Delay(1000);
				AnimationManager.Instance.PlayPhotograph();
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					AnimationManager.Instance.PlayAnimation(214, member.Actor, false);
					member.Actor.AddStatModifier("ReleaseEnergyBasil", silent: true);
				}
				foreach (Enemy enemy in BattleManager.Instance.GetAllEnemies())
				{
					BattleManager.Instance.Damage(self, enemy, () => { return 1000; }, true, 0f, false, true);
				}
			},
			hidden: true
		);



		// AUBREY //
		Skills["AAttack"] = new Skill(
			name: "AAttack",
			description: "Basic Attack",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForAnimation(28, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] attacks [target]!");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false);
			},
			hidden: true
		);
		Skills["PepTalk"] = new Skill(
			name: "PEP TALK",
			description: "Makes a friend or foe HAPPY.\nCost: 5",
			target: SkillTarget.AllyOrEnemy,
			cost: 5,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] cheers on [target]!");
				await AnimationManager.Instance.WaitForScreenAnimation(29, false);
				MakeHappy(target);
			}
		);
		Skills["Headbutt"] = new Skill(
			name: "HEADBUTT",
			description: "Deals big damage, but [actor] also takes damage.\nStronger when [actor] is ANGRY. Cost: 5",
			target: SkillTarget.Enemy,
			cost: 5,
			effect: async (self, target) =>
			{
				double neededHp = Math.Floor(self.CurrentStats.MaxHP * 0.2);
				if (self.CurrentHP < neededHp)
				{
					BattleLogManager.Instance.QueueMessage(self, target, "[actor] does not have enough HP!");
					// refund juice
					self.CurrentJuice += Skills["Headbutt"].Cost;
					return;
				}
				await AnimationManager.Instance.WaitForScreenAnimation(30, true);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] headbutts [target]!");
				if (self.CurrentState == "angry" || self.CurrentState == "enraged")
					BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 3f - target.CurrentStats.DEF; }, false);
				else
					BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2.5f - target.CurrentStats.DEF; }, false);
				self.CurrentHP = (int)Math.Max(1f, self.CurrentHP - Math.Floor(self.CurrentStats.MaxHP * 0.2));
			}
		);

		Skills["Counter"] = new Skill(
			name: "COUNTER",
			description: "All foes target [actor] for 1 turn.\nIf [actor] is attacked, she attacks. Cost: 5",
			target: SkillTarget.Self,
			cost: 5,
			effect: async (self, target) =>
			{
				AudioManager.Instance.PlaySFX("BA_protect", volume: 0.9f);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] readies her bat!");
				self.AddStatModifier("Taunt");
				self.AddStatModifier("AubreyCounter");
				await Task.CompletedTask;
			},
			goesFirst: true
		);

		Skills["PowerHit"] = new Skill(
			name: "POWER HIT",
			description: "An attack that ignore's a foe's DEFENSE,\nthen reduces the foe's DEFENSE. Cost: 20",
			target: SkillTarget.Enemy,
			cost: 20,
			effect: async (self, target) =>
			{
				AnimationManager.Instance.PlayAnimation(31, target);
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForAnimation(219, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] smashes [target]!");
				target.AddStatModifier("DefenseDown");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2f; }, false);
			}
		);

		Skills["Twirl"] = new Skill(
			name: "TWIRL",
			description: "[actor] attacks a foe and becomes HAPPY.\nCost: 10",
			target: SkillTarget.Enemy,
			cost: 10,
			effect: async (self, target) =>
			{
				AnimationManager.Instance.PlayAnimation(45, target);
				await Task.Delay(500);
				AnimationManager.Instance.PlayAnimation(28, target);
				await Task.Delay(500);
				int damage = BattleManager.Instance.Damage(self, target, () => { return (self.CurrentStats.ATK * 2f + self.CurrentStats.LCK) - target.CurrentStats.DEF; }, false);
				if (damage > -1)
				{
					MakeHappy(self);
				}

			}
		);

		Skills["MoodWrecker"] = new Skill(
			name: "MOOD WRECKER",
			description: "A swing that doesn't miss. Deals extra damage to\nHAPPY foes. Cost: 10",
			target: SkillTarget.Enemy,
			cost: 10,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForAnimation(46, target, true);
				await Task.Delay(500);
				if (target.CurrentState == "happy" || target.CurrentState == "ecstatic" || target.CurrentState == "manic")
				{
					// very nice
					if (target.CurrentState == "ecstatic" || target.CurrentState == "manic")
						await AnimationManager.Instance.WaitForAnimation(279, target, true);
					else
						await AnimationManager.Instance.WaitForAnimation(278, target, true);
					BattleLogManager.Instance.QueueMessage(self, target, "[actor] attacks [target]!");
					BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 3f - target.CurrentStats.DEF; }, true);
				}
				else
				{
					BattleLogManager.Instance.QueueMessage(self, target, "[actor] attacks [target]!");
					BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2.25f - target.CurrentStats.DEF; }, true);
				}
			}
		);

		Skills["TeamSpirit"] = new Skill(
			name: "TEAM SPIRIT",
			description: "Makes [actor] and a friend HAPPY.\nCost: 10",
			target: SkillTarget.AllyNotSelf,
			cost: 10,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] cheers on [target]!");
				AnimationManager.Instance.PlayAnimation(49, self, false);
				await Task.Delay(500);
				AnimationManager.Instance.PlayScreenAnimation(29, false);
				MakeHappy(target);
				MakeHappy(self);
			}
		);

		Skills["WindUpThrow"] = new Skill(
			name: "WIND-UP THROW",
			description: "Damages all foes. Deals more damage the less\nenemies there are. Cost: 20",
			target: SkillTarget.AllEnemies,
			cost: 20,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] throws her weapon!");
				await AnimationManager.Instance.WaitForScreenAnimation(33, true);
				int enemies = BattleManager.Instance.GetAllEnemies().Count;
				foreach (Enemy enemy in BattleManager.Instance.GetAllEnemies())
				{
					if (enemies == 1)
						BattleManager.Instance.Damage(self, enemy, () => { return self.CurrentStats.ATK * 3f - enemy.CurrentStats.DEF; }, false);
					else if (enemies == 2)
						BattleManager.Instance.Damage(self, enemy, () => { return self.CurrentStats.ATK * 2.5f - enemy.CurrentStats.DEF; }, false);
					else
						BattleManager.Instance.Damage(self, enemy, () => { return self.CurrentStats.ATK * 2f - enemy.CurrentStats.DEF; }, false);
				}
			}
		);

		Skills["Mash"] = new Skill(
			name: "MASH",
			description: "If this skill defeats a foe, recover 100% JUICE.\nCost: 15",
			target: SkillTarget.Enemy,
			cost: 15,
			effect: async (self, target) =>
			{
				AnimationManager.Instance.PlayAnimation(28, target);
				await Task.Delay(500);
				AnimationManager.Instance.PlayAnimation(213, target);
				await Task.Delay(500);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] attacks [target]!");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2.5f - target.CurrentStats.DEF; }, false);
				if (target.CurrentHP == 0)
				{
					AnimationManager.Instance.PlayAnimation(213, self);
					self.HealJuice(self.CurrentStats.MaxJuice);
					BattleManager.Instance.SpawnDamageNumber(self.CurrentStats.MaxJuice, target.CenterPoint, DamageType.JuiceGain);
				}
			}
		);

		Skills["Beatdown"] = new Skill(
			name: "BEATDOWN",
			description: "Attacks a foe 3 times.\nCost: 30",
			target: SkillTarget.Enemy,
			cost: 30,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] furiously attacks!");
				await AnimationManager.Instance.WaitForAnimation(17, target);
				for (int i = 0; i < 3; i++)
				{
					BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2f - target.CurrentStats.DEF; }, false);
					await Task.Delay(1000);
				}
			}
		);

		Skills["LastResort"] = new Skill(
			name: "LAST RESORT",
			description: "Deals damage based on [actor]'s HEART,\nbut [actor] becomes TOAST. Cost: 50",
			target: SkillTarget.Enemy,
			cost: 50,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForAnimation(34, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] strikes [target]\nwith all her strength!");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentHP * 4f; }, false);
				self.Damage(self.CurrentHP);
			}
		);

		Skills["LookAtOmori1"] = new Skill(
			name: "Look At Omori 1",
			description: "Aubrey Followup",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				PartyMember other = BattleManager.Instance.GetPartyMember(0);
				BattleLogManager.Instance.QueueMessage(self, other, "[actor] looks at [target].");
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForScreenAnimation(35, false);
				await AnimationManager.Instance.WaitForAnimation(28, target);
				BattleLogManager.Instance.QueueMessage(self, other, "[target] didn't notice [actor], so\n[actor] attacks again!");
				BattleManager.Instance.Damage(self, target, () => { return (self.CurrentStats.ATK * 2 + self.CurrentStats.LCK) - target.CurrentStats.DEF; }, false);
			},
			hidden: true
		);

		Skills["LookAtOmori2"] = new Skill(
			name: "Look At Omori 2",
			description: "Aubrey Followup",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				PartyMember other = BattleManager.Instance.GetPartyMember(0);
				BattleLogManager.Instance.QueueMessage(self, other, "[actor] looks at [target].");
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForScreenAnimation(36, false);
				await AnimationManager.Instance.WaitForAnimation(28, target);
				BattleLogManager.Instance.QueueMessage(self, other, "[target] still didn't notice [actor], so\n[actor] attacks harder!");
				BattleManager.Instance.Damage(self, target, () => { return (self.CurrentStats.ATK * 3 + self.CurrentStats.LCK) - target.CurrentStats.DEF; }, false);
			},
			hidden: true
		);

		Skills["LookAtOmori3"] = new Skill(
			name: "Look At Omori 3",
			description: "Aubrey Followup",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				PartyMember other = BattleManager.Instance.GetPartyMember(0);
				BattleLogManager.Instance.QueueMessage(self, other, "[actor] looks at [target].");
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForScreenAnimation(37, false);
				await AnimationManager.Instance.WaitForAnimation(44, target);
				BattleLogManager.Instance.QueueMessage(self, other, "[target] finally notices [actor]!\n[actor] swings her bat in happiness!");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 3 + self.CurrentStats.LCK; }, false);
			},
			hidden: true
		);

		Skills["LookAtKel1"] = new Skill(
			name: "Look At Kel 1",
			description: "Aubrey Followup",
			target: SkillTarget.Self,
			cost: 0,
			effect: async (self, target) =>
			{
				PartyMember other = BattleManager.Instance.GetPartyMember(2);
				BattleLogManager.Instance.QueueMessage(self, other, "[actor] looks at [target].");
				await Task.Delay(1000);
				AnimationManager.Instance.PlayScreenAnimation(38, false);
				await Task.Delay(2000);
				BattleLogManager.Instance.QueueMessage(self, other, "[target] eggs [actor] on!");
				MakeAngry(self);
			},
			hidden: true
		);

		Skills["LookAtKel2"] = new Skill(
		   name: "Look At Kel 2",
		   description: "Aubrey Followup",
		   target: SkillTarget.Self,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   PartyMember other = BattleManager.Instance.GetPartyMember(2);
			   BattleLogManager.Instance.QueueMessage(self, other, "[actor] looks at [target].");
			   await Task.Delay(1000);
			   AnimationManager.Instance.PlayScreenAnimation(39, false);
			   await Task.Delay(2000);
			   BattleLogManager.Instance.QueueMessage(self, other, "[target] eggs [actor] on!");
			   self.AddStatModifier("AttackUp", silent: true);
			   BattleLogManager.Instance.QueueMessage(self, other, "[target] and [actor]'s ATTACK ROSE!");
			   AnimationManager.Instance.PlayAnimation(214, self, false);
			   AnimationManager.Instance.PlayAnimation(214, other, false);
			   MakeAngry(self);
			   MakeAngry(other);
		   },
		   hidden: true
	   );

		Skills["LookAtKel3"] = new Skill(
		  name: "Look At Kel 3",
		  description: "Aubrey Followup",
		  target: SkillTarget.Self,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  PartyMember other = BattleManager.Instance.GetPartyMember(2);
			  BattleLogManager.Instance.QueueMessage(self, other, "[actor] looks at [target].");
			  await Task.Delay(1000);
			  AnimationManager.Instance.PlayScreenAnimation(40, false);
			  await Task.Delay(2000);
			  BattleLogManager.Instance.QueueMessage(self, other, "[target] eggs [actor] on!");
			  self.AddTierStatModifier("AttackUp", 3, silent: true);
			  BattleLogManager.Instance.QueueMessage(self, other, "[target] and [actor]'s ATTACK ROSE!");
			  AnimationManager.Instance.PlayAnimation(214, self, false);
			  AnimationManager.Instance.PlayAnimation(214, other, false);
			  self.SetState("enraged");
			  other.SetState("enraged");
		  },
		  hidden: true
	  );

		Skills["LookAtHero1"] = new Skill(
			name: "Look At Hero 1",
			description: "Aubrey Followup",
			target: SkillTarget.Self,
			cost: 0,
			effect: async (self, target) =>
			{
				PartyMember other = BattleManager.Instance.GetPartyMember(3);
				BattleLogManager.Instance.QueueMessage(self, other, "[actor] looks at [target].");
				await Task.Delay(1000);
				AnimationManager.Instance.PlayScreenAnimation(41, false);
				await Task.Delay(2000);
				AnimationManager.Instance.PlayAnimation(214, self, false);
				await Task.Delay(1000);
				BattleLogManager.Instance.QueueMessage(self, other, "[target] tells [actor] to focus!");
				self.AddStatModifier("DefenseUp");
				MakeHappy(self);
			},
			hidden: true
		);

		Skills["LookAtHero2"] = new Skill(
			name: "Look At Hero 2",
			description: "Aubrey Followup",
			target: SkillTarget.Self,
			cost: 0,
			effect: async (self, target) =>
			{
				PartyMember other = BattleManager.Instance.GetPartyMember(3);
				BattleLogManager.Instance.QueueMessage(self, other, "[actor] looks at [target].");
				await Task.Delay(1000);
				AnimationManager.Instance.PlayScreenAnimation(42, false);
				await Task.Delay(2000);
				AnimationManager.Instance.PlayAnimation(214, self, false);
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForAnimation(212, self, false);
				int heal = (int)Math.Round(self.CurrentStats.MaxHP * 0.25f, MidpointRounding.AwayFromZero);
				BattleLogManager.Instance.QueueMessage(self, other, "[target] cheers [actor]!");
				self.Heal(heal);
				self.AddTierStatModifier("DefenseUp", 2);
				MakeHappy(self);
			},
			hidden: true
		);

		Skills["LookAtHero3"] = new Skill(
			name: "Look At Hero 3",
			description: "Aubrey Followup",
			target: SkillTarget.Self,
			cost: 0,
			effect: async (self, target) =>
			{
				PartyMember other = BattleManager.Instance.GetPartyMember(3);
				BattleLogManager.Instance.QueueMessage(self, other, "[actor] looks at [target].");
				await Task.Delay(1000);
				AnimationManager.Instance.PlayScreenAnimation(43, false);
				await Task.Delay(2000);
				AnimationManager.Instance.PlayAnimation(214, self, false);
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForAnimation(212, self, false);
				int heal = (int)Math.Round(self.CurrentStats.MaxHP * 0.75f, MidpointRounding.AwayFromZero);
				int juice = (int)Math.Round(self.CurrentStats.MaxJuice * 0.5f, MidpointRounding.AwayFromZero);
				BattleLogManager.Instance.QueueMessage(self, other, "[target] cheers [actor]!");
				self.Heal(heal);
				self.HealJuice(juice);
				self.AddTierStatModifier("DefenseUp", 3);
				self.SetState("ecstatic");
			},
			hidden: true
		);


		Skills["ARWAttack"] = new Skill(
			name: "ARWAttack",
			description: "Basic Attack",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForAnimation(48, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] attacks [target]!");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false, neverCrit: true);
			},
			hidden: true
		);

		Skills["Homerun"] = new Skill(
			name: "HOMERUN",
			description: "Has a chance to instantly defeat a\nfoe. [actor] also takes damage. Cost: 25",
			target: SkillTarget.Enemy,
			cost: 25,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForAnimation(32, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] hits a home run!");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 4f - target.CurrentStats.DEF; }, neverCrit: true);
				int roll = GameManager.Instance.Random.RandiRange(0, 100);
				if (roll < 11)
				{
					target.CurrentHP = 0;
				}
				self.CurrentHP = Math.Max(0, (int)Math.Round(self.CurrentHP - self.BaseStats.MaxHP * 0.2f, MidpointRounding.AwayFromZero));
			}
		);

		// KEL //
		Skills["KAttack"] = new Skill(
			name: "KAttack",
			description: "Basic Attack",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForAnimation(54, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] attacks [target]!");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false);
			},
			hidden: true
		);
		Skills["Annoy"] = new Skill(
			name: "ANNOY",
			description: "Makes a friend or foe ANGRY.\nCost: 5",
			target: SkillTarget.AllyOrEnemy,
			cost: 5,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] annoys [target]!");
				await AnimationManager.Instance.WaitForScreenAnimation(55, false);
				MakeAngry(target);
			}
		);
		Skills["Rebound"] = new Skill(
			name: "REBOUND",
			description: "Deals damage to all foes.\nCost: 15",
			target: SkillTarget.AllEnemies,
			cost: 15,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor]'s ball bounces everywhere!");
				await AnimationManager.Instance.WaitForScreenAnimation(56, true);
				foreach (Enemy enemy in BattleManager.Instance.GetAllEnemies())
					BattleManager.Instance.Damage(self, enemy, () => { return self.CurrentStats.ATK * 2.5f - enemy.CurrentStats.DEF; }, false);
			}
		);

		Skills["RunNGun"] = new Skill(
			name: "RUN 'N GUN",
			description: "[actor] does an attack based on his SPEED\ninstead of his ATTACK. Cost: 15",
			target: SkillTarget.Enemy,
			cost: 15,
			effect: async (self, target) =>
			{
				AnimationManager.Instance.PlayAnimation(72, self, false);
				await Task.Delay(500);
				AnimationManager.Instance.PlayAnimation(54, target);
				await Task.Delay(500);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] attacks [target]!");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.SPD * 1.5f - target.CurrentStats.DEF; }, false);
			}
		);

		Skills["CantCatchMe"] = new Skill(
			name: "CAN'T CATCH ME",
			description: "Attracts attention and reduces all foes'\nHIT RATE for the turn. Cost: 50",
			target: SkillTarget.Self,
			cost: 50,
			effect: async (self, target) =>
			{
				AudioManager.Instance.PlaySFX("BA_dodge", volume: 0.9f);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] starts taunting all the foes!");
				BattleLogManager.Instance.QueueMessage(self, target, "All foes' HIT RATE fell for the turn!");
				self.AddStatModifier("Taunt");
				foreach (Enemy enemy in BattleManager.Instance.GetAllEnemies())
					enemy.AddStatModifier("HitRateDown");
				await Task.CompletedTask;
			},
			goesFirst: true
		);

		Skills["Curveball"] = new Skill(
			name: "CURVEBALL",
			description: "Makes a foe feel a random EMOTION. Deals\nextra damage to foes with EMOTION. Cost: 20",
			target: SkillTarget.Enemy,
			cost: 20,
			effect: async (self, target) =>
			{
				AnimationManager.Instance.PlayScreenAnimation(73, true);
				await Task.Delay(1000);
				AnimationManager.Instance.PlayAnimation(67, target);
				await Task.Delay(500);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] throws a curveball...");
				int damage;
				if (target.CurrentState != "neutral")
					damage = BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 3f - target.CurrentStats.DEF; }, false);
				else
					damage = BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2f - target.CurrentStats.DEF; }, false);
				if (damage > -1)
				{
					BattleManager.Instance.RandomEmotion(target);
				}
			}
		);

		Skills["Ricochet"] = new Skill(
			name: "RICOCHET",
			description: "Deals damage to a foe 3 times.\nCost: 30",
			target: SkillTarget.Enemy,
			cost: 30,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] does a fancy ball trick!");
				await AnimationManager.Instance.WaitForScreenAnimation(58, true);
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false, 0.3f);
				await Task.Delay(1000);
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false, 0.3f);
				await Task.Delay(1000);
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false, 0.3f);
			}
		);

		Skills["Megaphone"] = new Skill(
			name: "MEGAPHONE",
			description: "Makes all friends ANGRY.\nCost: 45",
			target: SkillTarget.AllAllies,
			cost: 45,
			effect: async (self, target) =>
			{
				AnimationManager.Instance.PlayScreenAnimation(74, false);
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForScreenAnimation(55, false);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] runs around and annoys everyone!");
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					MakeAngry(member.Actor);
				}
			}
		);

		Skills["Rally"] = new Skill(
			name: "RALLY",
			description: "[actor] becomes HAPPY. [actor]'s friends recover\nsome ENERGY and JUICE. Cost: 50",
			target: SkillTarget.Self,
			cost: 50,
			effect: async (self, target) =>
			{
				AnimationManager.Instance.PlayScreenAnimation(61, false);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] gets everyone pumped up!");
				MakeHappy(self);
				BattleLogManager.Instance.QueueMessage(self, target, "Everyone gains ENERGY!");
				BattleManager.Instance.AddEnergy(4);
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAllPartyMembers())
				{
					AnimationManager.Instance.PlayAnimation(213, member.Actor, false);
					int rounded = (int)Math.Round(member.Actor.CurrentStats.MaxJuice * 0.3f, MidpointRounding.AwayFromZero);
					target.HealJuice(rounded);
					BattleLogManager.Instance.QueueMessage(self, target, $"[target] recovered {rounded} JUICE!");
				}
				await Task.Delay(500);
			}
		);

		Skills["Comeback"] = new Skill(
			name: "COMEBACK",
			description: "Makes [actor] HAPPY. If SAD was removed,\n[actor] gains FLEX. Cost: 25",
			target: SkillTarget.Self,
			cost: 25,
			effect: async (self, target) =>
			{
				if (self.CurrentState == "sad" || self.CurrentState == "depressed" || self.CurrentState == "miserable")
				{
					AnimationManager.Instance.PlayAnimation(76, self, false);
					await Task.Delay(1000);
					self.AddStatModifier("Flex");
					AnimationManager.Instance.PlayAnimation(214, self, false);
				}
				else
				{
					AnimationManager.Instance.PlayAnimation(75, self, false);
				}
				MakeHappy(self);
			}
		);

		Skills["Tickle"] = new Skill(
			name: "TICKLE",
			description: "All attacks on a foe will hit right\nin the HEART for the turn. Cost: 55",
			target: SkillTarget.Enemy,
			cost: 55,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] tickles [target]!");
				BattleLogManager.Instance.QueueMessage(self, target, "[target] let their guard down!");
				target.AddStatModifier("Tickle");
				await Task.CompletedTask;
			}
		);

		Skills["JuiceMe"] = new Skill(
			name: "JUICE ME",
			description: "Heals a lot of JUICE to a friend, but\nalso hurts the friend. Cost: 10",
			target: SkillTarget.Ally,
			cost: 10,
			effect: async (self, target) =>
			{
				string weapon = (self as PartyMember).Weapon.Name;
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] passes the " + weapon + " to [target]!");
				AnimationManager.Instance.PlayAnimation(123, target, false);
				int rounded = (int)Math.Round(target.CurrentStats.MaxJuice * 0.3f, MidpointRounding.AwayFromZero);
				target.HealJuice(rounded);
				BattleLogManager.Instance.QueueMessage(self, target, $"[target] recovered {rounded} JUICE!");
				// can juice me miss???
				BattleManager.Instance.Damage(self, target, () => { return target.CurrentHP * .25f; }, true, 0f, neverCrit: true);
				await Task.CompletedTask;
			}
		);

		Skills["Snowball"] = new Skill(
			name: "SNOWBALL",
			description: "Makes a foe SAD.\nAlso deals big damage to SAD foes. Cost: 20",
			target: SkillTarget.Enemy,
			cost: 20,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForAnimation(60, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] throws a snowball at [target]!");
				if (target.CurrentState == "sad" || target.CurrentState == "depressed" || target.CurrentState == "miserable")
				{
					BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 3f - target.CurrentStats.DEF; }, false);
				}
				else
				{
					BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2.5f - target.CurrentStats.DEF; }, false);
					MakeSad(target);
				}
			}
		);

		Skills["Flex"] = new Skill(
			name: "FLEX",
			description: "[actor] deals more damage next turn and increases\nHIT RATE for his next attack. Cost: 10",
			target: SkillTarget.Self,
			cost: 10,
			effect: async (self, target) =>
			{
				AnimationManager.Instance.PlayScreenAnimation(57, true);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] flexes and feels his best!");
				BattleLogManager.Instance.QueueMessage(self, target, "[actor]'s HIT RATE rose!");
				self.AddStatModifier("Flex");
				await Task.CompletedTask;
			}
		);

		Skills["KRWAttack"] = new Skill(
			name: "KRWAttack",
			description: "Basic Attack",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForAnimation(77, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] attacks [target]!");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false, neverCrit: true);
			},
			hidden: true
		);

		Skills["Encourage"] = new Skill(
			name: "ENCOURAGE",
			description: "[actor] encourages a friend.\nRaises their attack. No cost.",
			target: SkillTarget.Ally,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] gives some encouragement!");
				AnimationManager.Instance.PlayAnimation(214, target, false);
				await Task.Delay(1000);
				target.AddStatModifier("AttackUp");
			}
		);
		Skills["PassToOmori1"] = new Skill(
			name: "Pass To Omori 1",
			description: "Kel Followup",
			target: SkillTarget.Ally,
			cost: 0,
			effect: async (self, target) =>
			{
				PartyMember first = BattleManager.Instance.GetPartyMember(0);
				BattleLogManager.Instance.QueueMessage(self, first, "[actor] passes to [target].");
				await Task.Delay(1000);
				AnimationManager.Instance.PlayScreenAnimation(62, false);
				await Task.Delay(1000);
				BattleLogManager.Instance.QueueMessage(self, first, "[target] wasn't looking and gets bopped!");
				BattleManager.Instance.Damage(self, first, () => { return 1; }, true, 0f, false, true);
				first.SetState("sad");
			},
			hidden: true
		);
		Skills["PassToOmori2"] = new Skill(
			name: "Pass To Omori 2",
			description: "Kel Followup",
			target: SkillTarget.Ally,
			cost: 0,
			effect: async (self, target) =>
			{
				PartyMember first = BattleManager.Instance.GetPartyMember(0);
				BattleLogManager.Instance.QueueMessage(self, first, "[actor] passes to [target].");
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForScreenAnimation(63, false);
				BattleLogManager.Instance.QueueMessage(self, first, "[target] catches [actor]'s ball!");
				BattleLogManager.Instance.QueueMessage(first, target, "[actor] throws the ball at\n[target]!");
				BattleManager.Instance.Damage(self, target, () => { return (first.CurrentStats.ATK * 1.5f) + (self.CurrentStats.ATK * 1.5f) - target.CurrentStats.DEF; }, false);
				first.SetState("happy");
			},
			hidden: true
		);
		Skills["PassToOmori3"] = new Skill(
		   name: "Pass To Omori 3",
		   description: "Kel Followup",
		   target: SkillTarget.Ally,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   PartyMember first = BattleManager.Instance.GetPartyMember(0);
			   BattleLogManager.Instance.QueueMessage(self, first, "[actor] passes to [target].");
			   await Task.Delay(1000);
			   await AnimationManager.Instance.WaitForScreenAnimation(64, false);
			   BattleLogManager.Instance.QueueMessage(self, first, "[target] catches [actor]'s ball!");
			   BattleLogManager.Instance.QueueMessage(first, target, "[actor] throws the ball at\n[target]!");
			   BattleManager.Instance.Damage(self, target, () => { return (first.CurrentStats.ATK * 2f) + (self.CurrentStats.ATK * 2f) - target.CurrentStats.DEF; }, false);
			   first.SetState("ecstatic");
		   },
		   hidden: true
	   );
		Skills["PassToAubrey1"] = new Skill(
			name: "Pass To Aubrey 1",
			description: "Kel Followup",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				target = BattleManager.Instance.GetRandomAliveEnemy();
				PartyMember second = BattleManager.Instance.GetPartyMember(1);
				BattleLogManager.Instance.QueueMessage(self, second, "[actor] passes to [target].");
				await Task.Delay(1000);
				AnimationManager.Instance.PlayScreenAnimation(65, true);
				await Task.Delay(2000);
				await AnimationManager.Instance.WaitForAnimation(66, target);
				BattleLogManager.Instance.QueueMessage(self, second, "[target] knocks the ball out of the park!");
				BattleManager.Instance.Damage(self, target, () => { return second.CurrentStats.ATK + self.CurrentStats.ATK - target.CurrentStats.DEF; }, true);
			},
			hidden: true
		);
		Skills["PassToAubrey2"] = new Skill(
			name: "Pass To Aubrey 2",
			description: "Kel Followup",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				target = BattleManager.Instance.GetRandomAliveEnemy();
				PartyMember second = BattleManager.Instance.GetPartyMember(1);
				BattleLogManager.Instance.QueueMessage(self, second, "[actor] passes to [target].");
				await Task.Delay(1000);
				AnimationManager.Instance.PlayScreenAnimation(65, true);
				await Task.Delay(2000);
				await AnimationManager.Instance.WaitForAnimation(67, target);
				BattleLogManager.Instance.QueueMessage(self, second, "[target] knocks the ball out of the park!");
				BattleManager.Instance.Damage(self, target, () => { return (second.CurrentStats.ATK * 2f) + self.CurrentStats.ATK - target.CurrentStats.DEF; }, true);
			},
			hidden: true
		);
		Skills["PassToAubrey3"] = new Skill(
			name: "Pass To Aubrey 3",
			description: "Kel Followup",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				target = BattleManager.Instance.GetRandomAliveEnemy();
				PartyMember second = BattleManager.Instance.GetPartyMember(1);
				BattleLogManager.Instance.QueueMessage(self, second, "[actor] passes to [target].");
				await Task.Delay(1000);
				AnimationManager.Instance.PlayScreenAnimation(79, true);
				await Task.Delay(2000);
				await AnimationManager.Instance.WaitForAnimation(68, target);
				BattleLogManager.Instance.QueueMessage(self, second, "[target] knocks the ball out of the park!");
				BattleManager.Instance.Damage(self, target, () => { return (second.CurrentStats.ATK * 2f) + (self.CurrentStats.ATK * 2f) - target.CurrentStats.DEF; }, true);
			},
			hidden: true
		);
		Skills["PassToHero1"] = new Skill(
			name: "Pass To Hero 1",
			description: "Kel Followup",
			target: SkillTarget.AllEnemies,
			cost: 0,
			effect: async (self, target) =>
			{
				PartyMember second = BattleManager.Instance.GetPartyMember(1);
				PartyMember third = BattleManager.Instance.GetPartyMember(3);
				BattleLogManager.Instance.QueueMessage(self, third, "[actor] passes to [target].");
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForScreenAnimation(69, true);
				BattleLogManager.Instance.QueueMessage(self, third, "[actor] dunks on the foes!");
				foreach (Enemy enemy in BattleManager.Instance.GetAllEnemies())
				{
					// VANILLA BUG: uses Aubrey's attack instead of Hero's
					BattleManager.Instance.Damage(self, enemy, () => { return second.CurrentStats.ATK + self.CurrentStats.ATK - enemy.CurrentStats.DEF; }, false);
				}
			},
			hidden: true
		);
		Skills["PassToHero2"] = new Skill(
			name: "Pass To Hero 2",
			description: "Kel Followup",
			target: SkillTarget.AllEnemies,
			cost: 0,
			effect: async (self, target) =>
			{
				PartyMember second = BattleManager.Instance.GetPartyMember(1);
				PartyMember third = BattleManager.Instance.GetPartyMember(3);
				BattleLogManager.Instance.QueueMessage(self, third, "[actor] passes to [target].");
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForScreenAnimation(70, true);
				BattleLogManager.Instance.QueueMessage(self, third, "[actor] dunks on the foes!");
				foreach (Enemy enemy in BattleManager.Instance.GetAllEnemies())
				{
					// VANILLA BUG: uses Aubrey's attack instead of Hero's
					BattleManager.Instance.Damage(self, enemy, () => { return second.CurrentStats.ATK + (self.CurrentStats.ATK * 1.5f) - enemy.CurrentStats.DEF; }, false);
				}
			},
			hidden: true
		);
		Skills["PassToHero3"] = new Skill(
		   name: "Pass To Hero 3",
		   description: "Kel Followup",
		   target: SkillTarget.AllEnemies,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   PartyMember second = BattleManager.Instance.GetPartyMember(1);
			   PartyMember third = BattleManager.Instance.GetPartyMember(3);
			   BattleLogManager.Instance.QueueMessage(self, third, "[actor] passes to [target].");
			   await Task.Delay(1000);
			   await AnimationManager.Instance.WaitForScreenAnimation(71, true);
			   BattleLogManager.Instance.QueueMessage(self, third, "[actor] dunks on the foes with style!");
			   foreach (Enemy enemy in BattleManager.Instance.GetAllEnemies())
			   {
				   // VANILLA BUG: uses Aubrey's attack instead of Hero's
				   BattleManager.Instance.Damage(self, enemy, () => { return (second.CurrentStats.ATK * 1.5f) + (self.CurrentStats.ATK * 1.5f) - enemy.CurrentStats.DEF; }, false);
				   AnimationManager.Instance.PlayAnimation(219, enemy, false);
				   enemy.AddStatModifier("AttackDown");
			   }
		   },
		   hidden: true
	   );

		// HERO //
		Skills["HAttack"] = new Skill(
			name: "HAttack",
			description: "Basic Attack",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForAnimation(83, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] attacks [target]!");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false);
			},
			hidden: true
		);
		Skills["Massage"] = new Skill(
			name: "MASSAGE",
			description: "Removes a friend or foe's EMOTION.\nCost: 5",
			target: SkillTarget.AllyOrEnemy,
			cost: 5,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] massages [target]!");
				await AnimationManager.Instance.WaitForScreenAnimation(86, false);
				target.SetState("neutral", true);
				if (target.CurrentState == "neutral")
					BattleLogManager.Instance.QueueMessage(target.Name.ToUpper() + " calms down...");
			}
		);
		Skills["Charm"] = new Skill(
			name: "CHARM",
			description: "Acts first, a foe targets [actor] for 1 turn.\nCost: 10",
			target: SkillTarget.Enemy,
			cost: 10,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] draws [target]'s\nattention.");
				await AnimationManager.Instance.WaitForScreenAnimation(90, false);
				target.AddStatModifier("Charm");
				await Task.Delay(2000);
			},
			goesFirst: true
		);
		Skills["Enchant"] = new Skill(
			name: "ENCHANT",
			description: "Acts first. A foe targets [actor] for 1 turn\nand becomes HAPPY. Cost: 15",
			target: SkillTarget.Enemy,
			cost: 15,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] draws the foe's attention\nwith a smile.");
				await AnimationManager.Instance.WaitForScreenAnimation(90, false);
				target.AddStatModifier("Charm");
				MakeHappy(target);
				await Task.Delay(2000);
			},
			goesFirst: true
		);
		Skills["Captivate"] = new Skill(
			name: "CAPTIVATE",
			description: "Acts first. All foes target [actor] for 1 turn.\nCost: 20",
			target: SkillTarget.Self,
			cost: 20,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] draws the foe's attention.");
				await AnimationManager.Instance.WaitForScreenAnimation(91, false);
				self.AddStatModifier("Taunt");
				await Task.Delay(1000);
			},
			goesFirst: true
		);
		Skills["Mesmerize"] = new Skill(
			name: "MESMERIZE",
			description: "Acts first. All foes target [actor] for 1 turn.\n[actor] takes less damage. Cost: 30",
			target: SkillTarget.Self,
			cost: 30,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] draws the foe's attention.");
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] prepares to block enemy attacks.");
				await AnimationManager.Instance.WaitForScreenAnimation(92, false);
				self.AddStatModifier("Taunt");
				self.AddStatModifier("Guard");
				await Task.Delay(1000);
			},
			goesFirst: true
		);
		Skills["SpicyFood"] = new Skill(
			name: "SPICY FOOD",
			description: "Damages a foe and makes them ANGRY.\nCost: 15",
			target: SkillTarget.Enemy,
			cost: 15,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForAnimation(98, target);
				MakeAngry(target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] cooks some spicy food!");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2f - target.CurrentStats.DEF; }, false, neverCrit: true);
			}
		);
		Skills["Tenderize"] = new Skill(
			name: "TENDERIZE",
			description: "Deals big damage to a foe and reduces\ntheir DEFENSE. Cost: 30",
			target: SkillTarget.Enemy,
			cost: 30,
			effect: async (self, target) =>
			{
				AnimationManager.Instance.PlayScreenAnimation(86, true);
				await Task.Delay(332);
				AnimationManager.Instance.PlayAnimation(124, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] intensely massages\n[target]!");
				target.AddStatModifier("DefenseDown");
				AnimationManager.Instance.PlayAnimation(219, target);
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 4f - target.CurrentStats.DEF; }, false);
			}
		);
		Skills["Smile"] = new Skill(
		   name: "SMILE",
		   description: "Acts first, reducing a foe's ATTACK.\nCost: 25",
		   target: SkillTarget.Enemy,
		   cost: 25,
		   goesFirst: true,
		   effect: async (self, target) =>
		   {
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] smiles.");
			   await AnimationManager.Instance.WaitForScreenAnimation(87, false);
			   await Task.Delay(332);
			   target.AddStatModifier("AttackDown");
			   await AnimationManager.Instance.WaitForAnimation(219, target);
		   }
		);
		Skills["Dazzle"] = new Skill(
		   name: "DAZZLE",
		   description: "Acts first. Reduces all foes' ATTACK and\nmakes them HAPPY. Cost: 35",
		   target: SkillTarget.AllEnemies,
		   cost: 35,
		   goesFirst: true,
		   effect: async (self, target) =>
		   {
			   AnimationManager.Instance.PlayAnimation(90, self, false);
			   await Task.Delay(500);
			   foreach (Enemy enemy in BattleManager.Instance.GetAllEnemies())
			   {
				   BattleLogManager.Instance.QueueMessage(self, enemy, "[actor] smiles at [target]!");
				   AnimationManager.Instance.PlayAnimation(276, enemy);
				   enemy.AddStatModifier("AttackDown");
				   MakeHappy(enemy);
				   AnimationManager.Instance.PlayAnimation(219, enemy);
			   }
		   }
		);
		Skills["FastFood"] = new Skill(
		   name: "FAST FOOD",
		   description: "Acts first, healing a friend for 40% of\ntheir HEART. Cost: 15",
		   target: SkillTarget.Ally,
		   cost: 15,
		   goesFirst: true,
		   effect: async (self, target) =>
		   {
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] prepares a quick meal for [target].");
			   await AnimationManager.Instance.WaitForAnimation(85, target, false);
			   int rounded = (int)Math.Round(target.CurrentStats.MaxHP * .4f, MidpointRounding.AwayFromZero);
			   target.Heal(rounded);
			   BattleManager.Instance.SpawnDamageNumber(rounded, target.CenterPoint, DamageType.Heal);
			   BattleLogManager.Instance.QueueMessage(self, target, $"[target] recovered {rounded} HEART!");
			   AnimationManager.Instance.PlayAnimation(212, target);
			   await Task.Delay(1000);
		   }
		);
		Skills["ShareFood"] = new Skill(
		   name: "SHARE FOOD",
		   description: "[actor] and a friend recover some HEART.\nCost: 15",
		   target: SkillTarget.Ally,
		   cost: 15,
		   effect: async (self, target) =>
		   {
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] shares food with [target]!");
			   AnimationManager.Instance.PlayAnimation(85, target, false);
			   AnimationManager.Instance.PlayAnimation(85, self, false);

			   int rounded = (int)Math.Round(target.CurrentStats.MaxHP * .5f, MidpointRounding.AwayFromZero);
			   target.Heal(rounded);
			   BattleManager.Instance.SpawnDamageNumber(rounded, target.CenterPoint, DamageType.Heal);
			   AnimationManager.Instance.PlayAnimation(212, target);

			   rounded = (int)Math.Round(self.CurrentStats.MaxHP * .5f, MidpointRounding.AwayFromZero);
			   self.Heal(rounded);
			   BattleManager.Instance.SpawnDamageNumber(rounded, self.CenterPoint, DamageType.Heal);
			   AnimationManager.Instance.PlayAnimation(212, self);
			   await Task.Delay(1000);
		   }
		);
		Skills["SnackTime"] = new Skill(
		   name: "SNACK TIME",
		   description: "Heals all friends for 40% of their HEART.\nCost: 25",
		   target: SkillTarget.AllAllies,
		   cost: 25,
		   effect: async (self, target) =>
		   {
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] made snacks for everyone!");
			   AnimationManager.Instance.PlayScreenAnimation(88, false);
			   await Task.Delay(1666);
			   foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
			   {
				   BattleManager.Instance.Heal(self, member.Actor, () => { return member.Actor.CurrentStats.MaxHP * 0.4f; }, 0f);
				   AnimationManager.Instance.PlayAnimation(212, member.Actor, false);
			   }
		   }
		);
		Skills["GatorAid"] = new Skill(
		   name: "GATOR AID",
		   description: "Boosts all friends' DEFENSE.\nCost: 15",
		   target: SkillTarget.AllAllies,
		   cost: 15,
		   effect: async (self, target) =>
		   {
			   await AnimationManager.Instance.WaitForScreenAnimation(100, false);
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] gets a little help from a friend.");
			   BattleLogManager.Instance.QueueMessage("Everyone's DEFENSE rose!");
			   foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
			   {
					member.Actor.AddStatModifier("DefenseUp", silent: true);
					AnimationManager.Instance.PlayAnimation(214, member.Actor, false);
			   }
		   }
		);
		Skills["TeaTime"] = new Skill(
			name: "TEA TIME",
			description: "Heals some of a friend's HEART and JUICE.\nCost: 25",
			target: SkillTarget.AllyNotSelf,
			cost: 10,
			effect: async (self, target) =>
			{
				AnimationManager.Instance.PlayAnimation(89, target, false);
				await Task.Delay(2000);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] brings out some tea for a break.");
				BattleLogManager.Instance.QueueMessage(self, target, "[target] feels refreshed!");
				int heartHeal = (int)Math.Round(target.CurrentStats.MaxHP * 0.3f, MidpointRounding.AwayFromZero);
				target.Heal(heartHeal);
				BattleLogManager.Instance.QueueMessage(self, target, $"[target] recovers {heartHeal} HEART!");
				BattleManager.Instance.SpawnDamageNumber(heartHeal, target.CenterPoint, DamageType.Heal);
				int juiceHeal = (int)Math.Round(target.CurrentStats.MaxJuice * 0.2f, MidpointRounding.AwayFromZero);
				target.HealJuice(juiceHeal);
				BattleManager.Instance.SpawnDamageNumber(juiceHeal, target.CenterPoint + new Vector2(0, 50), DamageType.JuiceGain);
				BattleLogManager.Instance.QueueMessage(self, target, $"[target] recovers {juiceHeal} JUICE!");
			}
		);
		Skills["Cook"] = new Skill(
			name: "COOK",
			description: "Heals a friend for 75% of their HEART.\nCost: 10",
			target: SkillTarget.Ally,
			cost: 10,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] makes a cookie just for [target]!");
				await AnimationManager.Instance.WaitForAnimation(85, target, false);
				BattleManager.Instance.Heal(self, target, () => { return target.CurrentStats.MaxHP * 0.75f; });
				AnimationManager.Instance.PlayAnimation(212, target, false);
				await Task.Delay(1000);
			}
		);
		Skills["Refresh"] = new Skill(
			name: "REFRESH",
			description: "Heals 50% of a friend's JUICE.\nCost: 40",
			target: SkillTarget.Ally,
			cost: 40,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] makes a refreshment for [target].");
				AnimationManager.Instance.PlayAnimation(213, target, false);
				BattleManager.Instance.HealJuice(self, target, () => { return target.CurrentStats.MaxJuice * 0.5f; });
				await Task.Delay(1000);
			}
		);
		Skills["HomemadeJam"] = new Skill(
			name: "HOMEMADE JAM",
			description: "Brings back a friend that is TOAST.\nCost: 40",
			target: SkillTarget.DeadAlly,
			cost: 40,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] makes HOMEMADE JAM!");
				if (target.CurrentState != "toast")
				{
					target = BattleManager.Instance.GetRandomDeadPartyMember();
					if (target == null)
					{
						BattleLogManager.Instance.QueueMessage("It had no effect.");
						return;
					}
				}
				await AnimationManager.Instance.WaitForAnimation(269, target, false);
				target.SetState("neutral", true);
				int heal = (int)Math.Round(target.CurrentStats.MaxHP * 0.7f, MidpointRounding.AwayFromZero);
				target.Heal(heal);
				BattleManager.Instance.SpawnDamageNumber(heal, target.CenterPoint, DamageType.Heal);
				BattleLogManager.Instance.QueueMessage(self, target, $"[target] recovered {heal} HEART!");
				BattleLogManager.Instance.QueueMessage(self, target, "[target] rose again!");
				await Task.Delay(1000);
			}
		);

		Skills["CallOmori1"] = new Skill(
			name: "Call Omori 1",
			description: "Hero Followup",
			target: SkillTarget.Ally,
			cost: 0,
			effect: async (self, target) =>
			{
				PartyMember first = BattleManager.Instance.GetPartyMember(0);
				BattleLogManager.Instance.QueueMessage(self, first, "[actor] calls out to [target].");
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForScreenAnimation(93, false);
				await AnimationManager.Instance.WaitForAnimation(212, first, false);
				int heal = (int)Math.Round(first.CurrentStats.MaxHP * 0.15f, MidpointRounding.AwayFromZero);
				first.Heal(heal);
				BattleLogManager.Instance.QueueMessage(self, first, "[actor] signals to [target]!");
				BattleLogManager.Instance.QueueMessage(self, first, $"[target] recovers {heal} HEART!");
				BattleManager.Instance.ForceCommand(first, BattleManager.Instance.GetRandomAliveEnemy(), Skills["OAttack"]);
			},
			hidden: true
		);
		Skills["CallOmori2"] = new Skill(
			name: "Call Omori 2",
			description: "Hero Followup",
			target: SkillTarget.Ally,
			cost: 0,
			effect: async (self, target) =>
			{
				PartyMember first = BattleManager.Instance.GetPartyMember(0);
				BattleLogManager.Instance.QueueMessage(self, first, "[actor] calls out to [target].");
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForScreenAnimation(93, false);
				await AnimationManager.Instance.WaitForAnimation(212, first, false);
				int heal = (int)Math.Round(first.CurrentStats.MaxHP * 0.25f, MidpointRounding.AwayFromZero);
				int juice = (int)Math.Round(first.CurrentStats.MaxJuice * 0.1f, MidpointRounding.AwayFromZero);
				first.Heal(heal);
				first.HealJuice(juice);
				BattleLogManager.Instance.QueueMessage(self, first, "[actor] signals to [target]!");
				BattleLogManager.Instance.QueueMessage(self, first, $"[target] recovers {heal} HEART!");
				BattleLogManager.Instance.QueueMessage(self, first, $"[target] recovers {juice} JUICE!");
				BattleManager.Instance.ForceCommand(first, BattleManager.Instance.GetRandomAliveEnemy(), Skills["OAttack"]);
			},
			hidden: true
		);
		Skills["CallOmori3"] = new Skill(
			name: "Call Omori 3",
			description: "Hero Followup",
			target: SkillTarget.Ally,
			cost: 0,
			effect: async (self, target) =>
			{
				PartyMember first = BattleManager.Instance.GetPartyMember(0);
				BattleLogManager.Instance.QueueMessage(self, first, "[actor] calls out to [target].");
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForScreenAnimation(93, false);
				await AnimationManager.Instance.WaitForAnimation(212, first, false);
				int heal = (int)Math.Round(first.CurrentStats.MaxHP * 0.4f, MidpointRounding.AwayFromZero);
				int juice = (int)Math.Round(first.CurrentStats.MaxJuice * 0.2f, MidpointRounding.AwayFromZero);
				first.Heal(heal);
				first.HealJuice(juice);
				BattleLogManager.Instance.QueueMessage(self, first, "[actor] signals to [target]!");
				BattleLogManager.Instance.QueueMessage(self, first, $"[target] recovers {heal} HEART!");
				BattleLogManager.Instance.QueueMessage(self, first, $"[target] recovers {juice} JUICE!");
				BattleManager.Instance.ForceCommand(first, BattleManager.Instance.GetRandomAliveEnemy(), Skills["OAttack"]);
			},
			hidden: true
		);

		Skills["CallAubrey1"] = new Skill(
			name: "Call Aubrey 1",
			description: "Hero Followup",
			target: SkillTarget.Ally,
			cost: 0,
			effect: async (self, target) =>
			{
				PartyMember second = BattleManager.Instance.GetPartyMember(1);
				BattleLogManager.Instance.QueueMessage(self, second, "[actor] calls out to [target].");
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForScreenAnimation(94, false);
				await AnimationManager.Instance.WaitForAnimation(212, second, false);
				int heal = (int)Math.Round(second.CurrentStats.MaxHP * 0.15f, MidpointRounding.AwayFromZero);
				second.Heal(heal);
				BattleLogManager.Instance.QueueMessage(self, second, "[actor] encourages [target]!");
				BattleLogManager.Instance.QueueMessage(self, second, $"[target] recovers {heal} HEART!");
				BattleManager.Instance.ForceCommand(second, BattleManager.Instance.GetRandomAliveEnemy(), Skills["AAttack"]);
			},
			hidden: true
		);

		Skills["CallAubrey2"] = new Skill(
			name: "Call Aubrey 2",
			description: "Hero Followup",
			target: SkillTarget.Ally,
			cost: 0,
			effect: async (self, target) =>
			{
				PartyMember second = BattleManager.Instance.GetPartyMember(1);
				BattleLogManager.Instance.QueueMessage(self, second, "[actor] calls out to [target].");
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForScreenAnimation(94, false);
				await AnimationManager.Instance.WaitForAnimation(212, second, false);
				int heal = (int)Math.Round(second.CurrentStats.MaxHP * 0.25f, MidpointRounding.AwayFromZero);
				int juice = (int)Math.Round(second.CurrentStats.MaxJuice * 0.1f, MidpointRounding.AwayFromZero);
				second.Heal(heal);
				second.HealJuice(juice);
				BattleLogManager.Instance.QueueMessage(self, second, "[actor] encourages [target]!");
				BattleLogManager.Instance.QueueMessage(self, second, $"[target] recovers {heal} HEART!");
				BattleLogManager.Instance.QueueMessage(self, second, $"[target] recovers {juice} JUICE!");
				BattleManager.Instance.ForceCommand(second, BattleManager.Instance.GetRandomAliveEnemy(), Skills["AAttack"]);
			},
			hidden: true
		);

		Skills["CallAubrey3"] = new Skill(
			name: "Call Aubrey 3",
			description: "Hero Followup",
			target: SkillTarget.Ally,
			cost: 0,
			effect: async (self, target) =>
			{
				PartyMember second = BattleManager.Instance.GetPartyMember(1);
				BattleLogManager.Instance.QueueMessage(self, second, "[actor] calls out to [target].");
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForScreenAnimation(94, false);
				await AnimationManager.Instance.WaitForAnimation(212, second, false);
				int heal = (int)Math.Round(second.CurrentStats.MaxHP * 0.40f, MidpointRounding.AwayFromZero);
				int juice = (int)Math.Round(second.CurrentStats.MaxJuice * 0.2f, MidpointRounding.AwayFromZero);
				second.Heal(heal);
				second.HealJuice(juice);
				BattleLogManager.Instance.QueueMessage(self, second, "[actor] encourages [target]!");
				BattleLogManager.Instance.QueueMessage(self, second, $"[target] recovers {heal} HEART!");
				BattleLogManager.Instance.QueueMessage(self, second, $"[target] recovers {juice} JUICE!");
				BattleManager.Instance.ForceCommand(second, BattleManager.Instance.GetRandomAliveEnemy(), Skills["AAttack"]);
			},
			hidden: true
		);

		Skills["CallKel1"] = new Skill(
			name: "Call Kel 1",
			description: "Hero Followup",
			target: SkillTarget.Ally,
			cost: 0,
			effect: async (self, target) =>
			{
				PartyMember fourth = BattleManager.Instance.GetPartyMember(2);
				BattleLogManager.Instance.QueueMessage(self, fourth, "[actor] calls out to [target].");
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForScreenAnimation(95, false);
				await AnimationManager.Instance.WaitForAnimation(212, fourth, false);
				int heal = (int)Math.Round(fourth.CurrentStats.MaxHP * 0.15f, MidpointRounding.AwayFromZero);
				fourth.Heal(heal);
				BattleLogManager.Instance.QueueMessage(self, fourth, "[actor] psyches up [target]!");
				BattleLogManager.Instance.QueueMessage(self, fourth, $"[target] recovers {heal} HEART!");
				BattleManager.Instance.ForceCommand(fourth, BattleManager.Instance.GetRandomAliveEnemy(), Skills["KAttack"]);
			},
			hidden: true
		);

		Skills["CallKel2"] = new Skill(
			name: "Call Kel 2",
			description: "Hero Followup",
			target: SkillTarget.Ally,
			cost: 0,
			effect: async (self, target) =>
			{
				PartyMember fourth = BattleManager.Instance.GetPartyMember(2);
				BattleLogManager.Instance.QueueMessage(self, fourth, "[actor] calls out to [target].");
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForScreenAnimation(95, false);
				await AnimationManager.Instance.WaitForAnimation(212, fourth, false);
				int heal = (int)Math.Round(fourth.CurrentStats.MaxHP * 0.25f, MidpointRounding.AwayFromZero);
				int juice = (int)Math.Round(fourth.CurrentStats.MaxJuice * 0.1f, MidpointRounding.AwayFromZero);
				fourth.Heal(heal);
				fourth.HealJuice(juice);
				BattleLogManager.Instance.QueueMessage(self, fourth, "[actor] psyches up [target]!");
				BattleLogManager.Instance.QueueMessage(self, fourth, $"[target] recovers {heal} HEART!");
				BattleLogManager.Instance.QueueMessage(self, fourth, $"[target] recovers {juice} JUICE!");
				BattleManager.Instance.ForceCommand(fourth, BattleManager.Instance.GetRandomAliveEnemy(), Skills["KAttack"]);
			},
			hidden: true
		);

		Skills["CallKel3"] = new Skill(
			name: "Call Kel 3",
			description: "Hero Followup",
			target: SkillTarget.Ally,
			cost: 0,
			effect: async (self, target) =>
			{
				PartyMember fourth = BattleManager.Instance.GetPartyMember(2);
				BattleLogManager.Instance.QueueMessage(self, fourth, "[actor] calls out to [target].");
				await Task.Delay(1000);
				await AnimationManager.Instance.WaitForScreenAnimation(95, false);
				await AnimationManager.Instance.WaitForAnimation(212, fourth, false);
				int heal = (int)Math.Round(fourth.CurrentStats.MaxHP * 0.4f, MidpointRounding.AwayFromZero);
				int juice = (int)Math.Round(fourth.CurrentStats.MaxJuice * 0.2f, MidpointRounding.AwayFromZero);
				fourth.Heal(heal);
				fourth.HealJuice(juice);
				BattleLogManager.Instance.QueueMessage(self, fourth, "[actor] psyches up [target]!");
				BattleLogManager.Instance.QueueMessage(self, fourth, $"[target] recovers {heal} HEART!");
				BattleLogManager.Instance.QueueMessage(self, fourth, $"[target] recovers {juice} JUICE!");
				BattleManager.Instance.ForceCommand(fourth, BattleManager.Instance.GetRandomAliveEnemy(), Skills["KAttack"]);
			},
			hidden: true
		);

		Skills["HRWAttack"] = new Skill(
			name: "Attack",
			description: "Basic Attack",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForAnimation(99, target);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] attacks [target]!");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false, neverCrit: true);
			},
			hidden: true
		);

		Skills["FirstAid"] = new Skill(
			name: "FIRST AID",
			description: "Heals a friend for 25% of their HEART.\nCost: 10",
			target: SkillTarget.Ally,
			cost: 10,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] provides first aid!");
				await AnimationManager.Instance.WaitForAnimation(114, target, false);
				float heal = target.CurrentStats.MaxHP * 0.25f;
				float variance = GameManager.Instance.Random.RandfRange(0.8f, 1.2f);
				int finalHeal = (int)Math.Round(heal * variance, MidpointRounding.AwayFromZero);
				target.Heal(finalHeal);
				BattleManager.Instance.SpawnDamageNumber(finalHeal, target.CenterPoint, DamageType.Heal);
				AnimationManager.Instance.PlayAnimation(212, target, false);
				BattleLogManager.Instance.QueueMessage(self, target, $"[target] recovered {finalHeal} HEART!");
				await Task.Delay(1000);
			}
		);

		// LOST SPROUT MOLE //
		Skills["LSMAttack"] = new Skill(
			name: "Attack",
			description: "Basic Attack",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForAnimation(123, target, false);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] bumps into [target]!");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false);
			},
			hidden: true
		);

		Skills["LSMDoNothing"] = new Skill(
			name: "Do Nothing",
			description: "Does nothing",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				AudioManager.Instance.PlaySFX("BA_do_nothing_dance");
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] is rolling around.");
				await Task.CompletedTask;
			},
			hidden: true
		);

		Skills["LSMRunAround"] = new Skill(
			name: "Run Around",
			description: "Run Around",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				AnimationManager.Instance.PlayScreenAnimation(200, false);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] runs around!");
				await Task.Delay(100);
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 1.5f - target.CurrentStats.DEF; }, false);
				await Task.Delay(917);
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 1.5f - target.CurrentStats.DEF; }, false);
			},
			hidden: true
		);

		// FOREST BUNNY? //
		Skills["FBQAttack"] = new Skill(
			name: "Attack",
			description: "Basic Attack",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForAnimation(123, target, false);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] nibbles at [target]?");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false);
			},
			hidden: true
		);

		Skills["FBQDoNothing"] = new Skill(
			name: "Do Nothing",
			description: "Does nothing",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				AudioManager.Instance.PlaySFX("BA_do_nothing_falls_over");
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] is hopping around?");
				await Task.CompletedTask;
			},
			hidden: true
		);

		Skills["FBQBeCute"] = new Skill(
			name: "Be Cute",
			description: "Be Cute",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] winks at [target]?");
				await AnimationManager.Instance.WaitForAnimation(148, self);
				await AnimationManager.Instance.WaitForAnimation(215, target, false);
				target.AddStatModifier("AttackDown");
			},
			hidden: true
		);

		Skills["FBQSadEyes"] = new Skill(
			name: "Sad Eyes",
			description: "Sad Eyes",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] looks sadly at [target]?");
				await AnimationManager.Instance.WaitForAnimation(149, self);
				MakeSad(target);
			},
			hidden: true
		);

		// SWEETHEART //
		Skills["SHAttack"] = new Skill(
			name: "Attack",
			description: "Basic Attack",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForAnimation(132, target, false);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] slaps [target].");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false);
			},
			hidden: true
		);

		Skills["SharpInsult"] = new Skill(
			name: "Sharp Insult",
			description: "Sharp Insult",
			target: SkillTarget.AllEnemies,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] insults everyone!");
				await AnimationManager.Instance.WaitForScreenAnimation(183, false);
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers()) {
					BattleManager.Instance.Damage(self, member.Actor, () => { return self.CurrentStats.ATK; }, false, 0.1f, neverCrit: true);
					MakeAngry(member.Actor);
				}
			},
			hidden: true
		);

		Skills["SwingMace"] = new Skill(
			name: "Swing Mace",
			description: "Swing Mace",
			target: SkillTarget.AllEnemies,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] swings her mace!");
				await AnimationManager.Instance.WaitForScreenAnimation(206, false);
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					BattleManager.Instance.Damage(self, member.Actor, () => { return self.CurrentStats.ATK * 2.5f - member.Actor.CurrentStats.DEF; }, false);
				}
			},
			hidden: true
		);

		Skills["Brag"] = new Skill(
			name: "Brag",
			description: "Brag",
			target: SkillTarget.Self,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] boasts about one of her\nmany, many talents!");
				await AnimationManager.Instance.WaitForScreenAnimation(162, false);
				MakeHappy(self);
			},
			hidden: true
		);

		// SLIME GIRLS //
		Skills["ComboAttack"] = new Skill(
			name: "ComboAttack",
			description: "ComboAttack",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "The [actor] attack all at once!");
				AnimationManager.Instance.PlayAnimation(133, target, false);
				await Task.Delay(580);
				AnimationManager.Instance.PlayAnimation(134, target, false);
				await Task.Delay(580);
				AnimationManager.Instance.PlayAnimation(135, target, false);
				await Task.Delay(580);
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2f - target.CurrentStats.DEF; }, false, neverCrit: true);
			},
			hidden: true
		);

		Skills["StrangeGas"] = new Skill(
			name: "StrangeGas",
			description: "StrangeGas",
			target: SkillTarget.AllEnemies,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage("MEDUSA threw a bottle...");
				AnimationManager.Instance.PlayScreenAnimation(194, false);
				await Task.Delay(1500);
				AnimationManager.Instance.PlayScreenAnimation(181, false);
				BattleLogManager.Instance.QueueMessage("A strange gas fills the room.");
				await Task.Delay(2000);

				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					BattleManager.Instance.RandomEmotion(member.Actor);
				}
			},
			hidden: true
		);

		Skills["Dynamite"] = new Skill(
			name: "Dynamite",
			description: "Dynamite",
			target: SkillTarget.AllEnemies,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage("MEDUSA threw a bottle...");
				AnimationManager.Instance.PlayScreenAnimation(194, false);
				await Task.Delay(1500);
				AnimationManager.Instance.PlayScreenAnimation(172, false);
				BattleLogManager.Instance.QueueMessage("And it explodes!");
				await Task.Delay(2000);

				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					BattleManager.Instance.Damage(self, member.Actor, () => { return 75; }, false, 0f, false, true);
				}
			},
			hidden: true
		);

		Skills["StingRay"] = new Skill(
			name: "StingRay",
			description: "StingRay",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "MOLLY fires her stingers!\n[target] gets struck!");
				await AnimationManager.Instance.WaitForAnimation(193, target, false);
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2; }, false, neverCrit: true);
				AnimationManager.Instance.PlayAnimation(215, target, false);
				target.AddTierStatModifier("SpeedDown", 3);
			},
			hidden: true
		);

		Skills["Chainsaw"] = new Skill(
			name: "Chainsaw",
			description: "Chainsaw",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] pulls out a chainsaw!");
				await AnimationManager.Instance.WaitForAnimation(208, target, false);
				for (int i = 0; i < 3; i++)
				{
					BattleManager.Instance.Damage(self, target, () => { return 40; }, false, 0.75f, false, true);
					await Task.Delay(500);
				}
			},
			hidden: true
		);

		Skills["Swap"] = new Skill(
			name: "Swap",
			description: "Swap",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] did their thing!\nHEART and JUICE were swapped!");
				await AnimationManager.Instance.WaitForScreenAnimation(191, false);
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					int hp = member.Actor.CurrentHP;
					int juice = member.Actor.CurrentJuice;
					member.Actor.CurrentHP = Math.Min(member.Actor.CurrentStats.MaxHP, juice + 1);
					member.Actor.CurrentJuice = Math.Min(member.Actor.CurrentStats.MaxJuice, hp);
				}
			},
			hidden: true
		);

		Skills["SlimeUltimateAttack"] = new Skill(
			name: "SlimeUltimateAttack",
			description: "SlimeUltimateAttack",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] throw everything they have!");
				AnimationManager.Instance.PlayScreenAnimation(293, false);
				await Task.Delay(1162);
				AnimationManager.Instance.PlayScreenAnimation(181, false);
				await Task.Delay(332);
				foreach (PartyMemberComponent partyMember in BattleManager.Instance.GetAlivePartyMembers())
				{
					BattleManager.Instance.SpawnDamageNumber(partyMember.Actor.CurrentJuice, partyMember.Actor.CenterPoint, DamageType.JuiceLoss);
					partyMember.Actor.CurrentJuice = 0;
				}
				await Task.Delay(1660);
				// TODO: screen tint
				await Task.Delay(332);
				foreach (PartyMemberComponent partyMember in BattleManager.Instance.GetAlivePartyMembers())
					AnimationManager.Instance.PlayAnimation(193, partyMember.Actor, false);
				await Task.Delay(664);
				foreach (PartyMemberComponent partyMember in BattleManager.Instance.GetAlivePartyMembers())
				{
					partyMember.Actor.AddTierStatModifier("AttackDown", 3, silent: true);
					partyMember.Actor.AddTierStatModifier("DefenseDown", 3, silent: true);
					partyMember.Actor.AddTierStatModifier("SpeedDown", 3, silent: true);
					AnimationManager.Instance.PlayAnimation(215, partyMember.Actor, false);
				}
				BattleLogManager.Instance.QueueMessage("Everyone's ATTACK fell.");
				await Task.Delay(166);
				BattleLogManager.Instance.QueueMessage("Everyone's DEFENSE fell.");
				await Task.Delay(166);
				BattleLogManager.Instance.QueueMessage("Everyone's SPEED fell.");
				await Task.Delay(1660);
				AnimationManager.Instance.PlayScreenAnimation(172, false);
				await Task.Delay(332);
				foreach (PartyMemberComponent partyMember in BattleManager.Instance.GetAlivePartyMembers())
				{
					BattleManager.Instance.Damage(self, partyMember.Actor, () => { return partyMember.Actor.CurrentStats.MaxHP * 0.4f; }, false, 0f, neverCrit: true);
					BattleManager.Instance.RandomEmotion(partyMember.Actor);
				}
				await Task.Delay(664);
			},
			hidden: true
		);

		// BIG STRONG TREE //
		Skills["BSTDoNothing"] = new Skill(
			name: "BSTDoNothing",
			description: "BSTDoNothing",
			target: SkillTarget.AllEnemies,
			cost: 0,
			effect: async (self, target) =>
			{
				int roll = GameManager.Instance.Random.RandiRange(0, 1);
				if (roll == 0)
					BattleLogManager.Instance.QueueMessage("A gentle breeze blows across the leaves.");
				else
					BattleLogManager.Instance.QueueMessage("BIG STRONG TREE stands firm\nbecause it is a tree.");
				await Task.CompletedTask;
			},
			hidden: true
		);

		// DOWNLOAD WINDOW //
		Skills["DWDoNothing1"] = new Skill(
			name: "DWDoNothing1",
			description: "DWDoNothing1",
			target: SkillTarget.AllEnemies,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage("DOWNLOAD WINDOW is at 99%.");
				await Task.CompletedTask;
			},
			hidden: true
		);
		Skills["DWDoNothing2"] = new Skill(
			name: "DWDoNothing2",
			description: "DWDoNothing2",
			target: SkillTarget.AllEnemies,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage("DOWNLOAD WINDOW is still at 99%.");
				await Task.CompletedTask;
			},
			hidden: true
		);
		Skills["Crash"] = new Skill(
			name: "Crash",
			description: "Crash",
			target: SkillTarget.AllEnemies,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] crashes and burns!");
				AnimationManager.Instance.PlayScreenAnimation(165, false);
				await Task.Delay(3652);
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					BattleManager.Instance.Damage(self, member.Actor, () => { return member.Actor.CurrentStats.MaxHP * 0.8f; }, true, 0f, false, true);
				}
			},
			hidden: true
		);

		// SPACE EX BOYFRIEND //
		Skills["SEBAttack"] = new Skill(
			name: "SEBAttack",
			description: "SEBAttack",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForAnimation(123, target, false);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] kicks [target]!");
				BattleManager.Instance.Damage(self, target, () => { return (self.CurrentStats.ATK * 2) + 5 - target.CurrentStats.DEF; }, false);
			},
			hidden: true
		);

		Skills["SEBDoNothing"] = new Skill(
			name: "SEBDoNothing",
			description: "SEBDoNothing",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] looks wistfully\ninto the distance.");
				await Task.CompletedTask;
			},
			hidden: true
		);

		Skills["AngstySong"] = new Skill(
			name: "AngstySong",
			description: "AngstySong",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] sings sadly...");
				await AnimationManager.Instance.WaitForScreenAnimation(154, false);
				MakeSad(target);
			},
			hidden: true
		);

		Skills["AngrySong"] = new Skill(
			name: "AngrySong",
			description: "AngrySong",
			target: SkillTarget.AllEnemies,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] wails wildly!");
				await AnimationManager.Instance.WaitForScreenAnimation(153, false);
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					BattleManager.Instance.Damage(self, member.Actor, () => { return self.CurrentStats.ATK * 2 - member.Actor.CurrentStats.DEF; }, false);
				}
			},
			hidden: true
		);

		Skills["SpaceLaser"] = new Skill(
			name: "SpaceLaser",
			description: "SpaceLaser",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForAnimation(160, target, false);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] fires his laser!");
				BattleManager.Instance.Damage(self, target, () => { return (self.CurrentStats.ATK * 2.5f) - target.CurrentStats.DEF; }, false);
			},
			hidden: true
		);

		Skills["BulletHell"] = new Skill(
			name: "BulletHell",
			description: "BulletHell",
			target: SkillTarget.AllEnemies,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] fires wildly!");
				await AnimationManager.Instance.WaitForScreenAnimation(168, false);
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					BattleManager.Instance.Damage(self, member.Actor, () => { return 20; }, false, neverCrit: true);
				}
			},
			hidden: true
		);

		// AUBREY (Enemy) //

		Skills["AEAttack"] = new Skill(
			name: "AEAttack",
			description: "AEAttack",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForAnimation(28, target, false);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] attacks [target]!");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false, neverCrit: true);
			},
			hidden: true
		);

		Skills["AEDoNothing"] = new Skill(
			name: "AEDoNothing",
			description: "AEDoNothing",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] spits on your shoe.");
				await Task.CompletedTask;
			},
			hidden: true
		);

		Skills["AEHeadbutt"] = new Skill(
			name: "AEHeadbutt",
			description: "AEHeadbutt",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForAnimation(124, target, false);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] headbutts [target]!");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 3 - target.CurrentStats.DEF; }, false, neverCrit: true);
			},
			hidden: true
		);

		// Gator Guy //

		Skills["GGAttack"] = new Skill(
			name: "GGAttack",
			description: "GGAttack",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForAnimation(123, target, false);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] karate chops [target]!");
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false, neverCrit: true);
			},
			hidden: true
		);

		Skills["GGDoNothing"] = new Skill(
			name: "GGDoNothing",
			description: "GGDoNothing",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] cracks his knuckles.");
				await Task.CompletedTask;
			},
			hidden: true
		);

		Skills["GGRoughUp"] = new Skill(
			name: "GGRoughUp",
			description: "GGRoughUp",
			target: SkillTarget.Enemy,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] gets rough.");
				await Task.Delay(100);
				AnimationManager.Instance.PlayAnimation(123, target, false);
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 1.5f - target.CurrentStats.DEF; }, false, neverCrit: true);
				await Task.Delay(917);
				AnimationManager.Instance.PlayAnimation(123, target, false);
				BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 1.5f - target.CurrentStats.DEF; }, false, neverCrit: true);
			},
			hidden: true
		);

		// Mr. Jawsum //
		Skills["MJAttackOrder"] = new Skill(
			name: "MJAttackOrder",
			description: "MJAttackOrder",
			target: SkillTarget.AllAllies,
			cost: 0,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] gives orders to attack!");
				AudioManager.Instance.PlaySFX("SE_dinosaur", 0.8f, 1f);
				await Task.Delay(250);
				foreach (Enemy enemy in BattleManager.Instance.GetAllEnemies())
				{
					MakeAngry(enemy);
				}
			},
			hidden: true
		);

		Skills["MJSummonGator"] = new Skill(
			name: "MJSummonGator",
			description: "MJSummonGator",
			target: SkillTarget.Self,
			cost: 0,
			effect: async (self, target) =>
			{
				await AnimationManager.Instance.WaitForScreenAnimation(146, true);
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] picks up the phone and\ncalls a GATOR GUY!");
				if (self is MrJawsum jawsum)
				{
					jawsum.SpawnGatorGuy();
				}
			},
			hidden: true
		);

		// Fear of Spiders //
		Skills["FOSAttack"] = new Skill(
		   name: "FOSAttack",
		   description: "FOSAttack",
		   target: SkillTarget.Enemy,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   await AnimationManager.Instance.WaitForAnimation(287, target, false);
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] wraps up and eats [target].");
			   BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false);
		   },
		   hidden: true
		);

		Skills["FOSDoNothing"] = new Skill(
		   name: "FOSDoNothing",
		   description: "FOSDoNothing",
		   target: SkillTarget.Self,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] is trying to talk to you...");
			   await Task.CompletedTask;
		   },
		   hidden: true
		);

		Skills["FOSSpinWeb"] = new Skill(
		   name: "FOSSpinWeb",
		   description: "FOSSpinWeb",
		   target: SkillTarget.Enemy,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   AnimationManager.Instance.PlayScreenAnimation(176, false);
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] entangles [target]\nin sticky webs.");
			   target.AddStatModifier("SpeedDown");
			   await Task.CompletedTask;
		   },
		   hidden: true
		);

		Skills["FOSAttackAll"] = new Skill(
		   name: "FOSAttackAll",
		   description: "FOSAttackAll",
		   target: SkillTarget.AllEnemies,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] catches everyone!");
			   AnimationManager.Instance.PlayScreenAnimation(176, false);
			   await Task.Delay(1000);
			   foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
			   {
				   BattleManager.Instance.Damage(self, member.Actor, () => { return self.CurrentStats.ATK * 2f - member.Actor.CurrentStats.DEF; }, false);
				   AnimationManager.Instance.PlayAnimation(287, member.Actor, false);
			   }
		   },
		   hidden: true
		);

		// Unbread Twins //
		Skills["UBTAttack"] = new Skill(
		   name: "UBTAttack",
		   description: "UBTAttack",
		   target: SkillTarget.Enemy,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] attack together!");
			   await AnimationManager.Instance.WaitForAnimation(124, target, false);
			   BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false);
			   await Task.Delay(500);
			   BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false);
		   },
		   hidden: true
		);

		Skills["UBTDoNothing"] = new Skill(
		   name: "UBTDoNothing",
		   description: "UBTDoNothing",
		   target: SkillTarget.Self,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] forget something\nin the oven!");
			   await Task.CompletedTask;
		   },
		   hidden: true
		);

		Skills["UBTCheerUp"] = new Skill(
		   name: "UBTCheerUp",
		   description: "UBTCheerUp",
		   target: SkillTarget.Self,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   await AnimationManager.Instance.WaitForAnimation(180, self);
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] do their best to not\nbe SAD.");
			   self.SetState("neutral", true);
		   },
		   hidden: true
		);

		Skills["UBTCook"] = new Skill(
		   name: "UBTCook",
		   description: "UBTCook",
		   target: SkillTarget.Ally,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   await AnimationManager.Instance.WaitForAnimation(85, target);
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] makes a cookie!");
			   BattleManager.Instance.Heal(self, target, () =>
			   {
				   if (self.CurrentJuice <= 0)
					   return 1;
				   return self.CurrentJuice * 0.4f;
			   }, 0f);
			   await AnimationManager.Instance.WaitForAnimation(216, target);
		   },
		   hidden: true
		);

		Skills["UBTBakeBread"] = new Skill(
		   name: "UBTBakeBread",
		   description: "UBTBakeBread",
		   target: SkillTarget.Self,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   await AnimationManager.Instance.WaitForAnimation(145, self);
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] pull out some\nBREAD from the oven!");
			   if (self is UnbreadTwins twins)
				   twins.SpawnBread();
		   },
		   hidden: true
		);

		// Bun Bunny //
		Skills["BBAttack"] = new Skill(
		  name: "BBAttack",
		  description: "BBAttack",
		  target: SkillTarget.Enemy,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  await AnimationManager.Instance.WaitForAnimation(122, target, false);
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] bumps buns with [target]!");
			  BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false, neverCrit: true);
		  },
		  hidden: true
		);

		Skills["BBDoNothing"] = new Skill(
		   name: "BBDoNothing",
		   description: "BBDoNothing",
		   target: SkillTarget.Self,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] is loafing around.");
			   AudioManager.Instance.PlaySFX("BA_Drink", volume: 0.9f);
			   await Task.CompletedTask;
		   },
		   hidden: true
		);

		Skills["BBHide"] = new Skill(
		   name: "BBHide",
		   description: "BBHide",
		   target: SkillTarget.Self,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   await AnimationManager.Instance.WaitForAnimation(178, self);
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] hides in its bun.");
			   self.AddStatModifier("Guard");
		   },
		   hidden: true,
		   goesFirst: true
		);

		// Creepypasta //
		Skills["CPAttack"] = new Skill(
		  name: "CPAttack",
		  description: "CPAttack",
		  target: SkillTarget.Enemy,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  await AnimationManager.Instance.WaitForAnimation(123, target, false);
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] makes [target] feel uncomfortable.");
			  BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false, neverCrit: true);
		  },
		  hidden: true
		);

		Skills["CPDoNothing"] = new Skill(
		   name: "CPDoNothing",
		   description: "CPDoNothing",
		   target: SkillTarget.Self,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] does nothing...menacingly!");
			   AudioManager.Instance.PlaySFX("SE_evil5", volume: 0.9f);
			   await Task.CompletedTask;
		   },
		   hidden: true
		);

		Skills["CPScare"] = new Skill(
		   name: "CPScare",
		   description: "CPScare",
		   target: SkillTarget.AllEnemies,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   AnimationManager.Instance.PlayAnimation(195, self);
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] shows everyone their worst nightmare!");
			   await Task.Delay(1500);
			   foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
			   {
				   member.Actor.SetState("afraid");
			   }
		   },
		   hidden: true
		);

		// Slice //
		Skills["SLAttack"] = new Skill(
		  name: "SLAttack",
		  description: "SLAttack",
		  target: SkillTarget.Enemy,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  await AnimationManager.Instance.WaitForAnimation(123, target, false);
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] charges into [target].");
			  BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false, neverCrit: true);
		  },
		  hidden: true
		);

		Skills["SLDoNothing"] = new Skill(
		   name: "SLDoNothing",
		   description: "SLDoNothing",
		   target: SkillTarget.Self,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] picks its nose.");
			   AudioManager.Instance.PlaySFX("BA_Drink", volume: 0.9f);
			   await Task.CompletedTask;
		   },
		   hidden: true
		);

		Skills["SLRile"] = new Skill(
		   name: "SLRile",
		   description: "SLRile",
		   target: SkillTarget.AllAllies,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] gives a controvesial speech!");
			   foreach (Enemy enemy in BattleManager.Instance.GetAllEnemies())
			   {
				   MakeAngry(enemy);
				   enemy.AddStatModifier("AttackUp", true);
			   }
			   await Task.CompletedTask;
		   },
		   hidden: true
		);;

		// Sourdough //
		Skills["SDAttack"] = new Skill(
		  name: "SDAttack",
		  description: "SDAttack",
		  target: SkillTarget.Enemy,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  await AnimationManager.Instance.WaitForAnimation(123, target, false);
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] steps on [target].");
			  BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false, neverCrit: true);
		  },
		  hidden: true
		);

		Skills["SDDoNothing"] = new Skill(
		   name: "SLDoNothing",
		   description: "SLDoNothing",
		   target: SkillTarget.Self,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] kicks some dirt.");
			   AudioManager.Instance.PlaySFX("BA_INK", volume: 0.9f);
			   await Task.CompletedTask;
		   },
		   hidden: true
		);

		Skills["SDBadWord"] = new Skill(
		   name: "SDBadWord",
		   description: "SDBadWord",
		   target: SkillTarget.Enemy,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   AnimationManager.Instance.PlayAnimation(188, self);
			   BattleLogManager.Instance.QueueMessage(self, target, "Oh no! [actor] says a bad word!");
			   await Task.Delay(1500);
			   BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK; }, false, neverCrit: true);
		   },
		   hidden: true
		);

		// Sesame //
		Skills["SESAttack"] = new Skill(
		  name: "SESAttack",
		  description: "SESAttack",
		  target: SkillTarget.Enemy,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  await AnimationManager.Instance.WaitForAnimation(123, target, false);
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] throws seeds at [target].");
			  BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false, neverCrit: true);
		  },
		  hidden: true
		);

		Skills["SESDoNothing"] = new Skill(
		   name: "SESDoNothing",
		   description: "SESDoNothing",
		   target: SkillTarget.Self,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] scratches their head.");
			   AudioManager.Instance.PlaySFX("BA_do_nothing_dance", volume: 0.9f);
			   await Task.CompletedTask;
		   },
		   hidden: true
		);

		Skills["SESBreadRoll"] = new Skill(
		   name: "SESBreadRoll",
		   description: "SESBreadRoll",
		   target: SkillTarget.AllEnemies,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   await AnimationManager.Instance.WaitForScreenAnimation(207, false);
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] rolls over everyone!");
			   foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				   BattleManager.Instance.Damage(self, member.Actor, () => { return self.CurrentStats.ATK * 2 - member.Actor.CurrentStats.DEF; }, false, neverCrit: true);
		   },
		   hidden: true
		);

		// Living Bread //
		Skills["LBAttack"] = new Skill(
		  name: "LBAttack",
		  description: "LBAttack",
		  target: SkillTarget.Enemy,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  await AnimationManager.Instance.WaitForAnimation(123, target, false);
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] bites at [target].");
			  BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false, neverCrit: true);
		  },
		  hidden: true
		);

		Skills["LBDoNothing"] = new Skill(
		   name: "LBDoNothing",
		   description: "LBDoNothing",
		   target: SkillTarget.Self,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] slowly inches towards [target]!");
			   AudioManager.Instance.PlaySFX("BA_do_nothing_space_out", volume: 0.9f);
			   await Task.CompletedTask;
		   },
		   hidden: true
		);

		Skills["LBBite"] = new Skill(
		   name: "LBBite",
		   description: "LBBite",
		   target: SkillTarget.Enemy,
		   cost: 0,
		   effect: async (self, target) =>
		   {
			   await AnimationManager.Instance.WaitForAnimation(156, target, false);
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] bites [target]!");
			   BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 3 - target.CurrentStats.DEF; }, false, neverCrit: true);
		   },
		   hidden: true
		);

		// Boss //
		Skills["BSSAttack"] = new Skill(
		  name: "BSSAttack",
		  description: "BSSAttack",
		  target: SkillTarget.Enemy,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  await AnimationManager.Instance.WaitForAnimation(139, target, false);
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] punches [target]!");
			  BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false);
		  },
		  hidden: true
		);

		Skills["BSSAttackTwice"] = new Skill(
		  name: "BSSAttackTwice",
		  description: "BSSAttackTwice",
		  target: SkillTarget.AllEnemies,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  target = BattleManager.Instance.GetRandomAlivePartyMember();
			  await AnimationManager.Instance.WaitForAnimation(139, target, false);
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] punches [target]!");
			  BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false);
			  await Task.Delay(1000);
			  target = BattleManager.Instance.GetRandomAlivePartyMember();
			  await AnimationManager.Instance.WaitForAnimation(139, target, false);
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] punches [target]!");
			  BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false);
		  },
		  hidden: true
		);

		Skills["BSSDoNothing"] = new Skill(
		  name: "BSSDoNothing",
		  description: "BSSDoNothing",
		  target: SkillTarget.AllEnemies,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] cracks his knuckles.");
			  await Task.CompletedTask;
		  },
		  hidden: true
		);

		Skills["BSSAttackAll"] = new Skill(
		  name: "BSSAttackAll",
		  description: "BSSAttackAll",
		  target: SkillTarget.AllEnemies,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
			  {
				  await Task.Delay(1000);
				  BattleManager.Instance.Damage(self, member.Actor, () => { return 100; }, true, 0f, neverCrit: true);
			  }
		  },
		  hidden: true
		);

		// Ye Old Sprout //
		Skills["YOSRollOver"] = new Skill(
		  name: "YOSRollOver",
		  description: "YOSRollOver",
		  target: SkillTarget.AllEnemies,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] rolls over!");
			  foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
			  {
				  AnimationManager.Instance.PlayAnimation(124, member.Actor, false);
				  BattleManager.Instance.Damage(self, member.Actor, () => { return 4; }, false, 0.5f, neverCrit: true);
			  }
			  await Task.CompletedTask;
		  },
		  hidden: true
		);

		// Mutantheart //
		Skills["MHWink"] = new Skill(
		  name: "MHWink",
		  description: "MHWink",
		  target: SkillTarget.Enemy,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  await AnimationManager.Instance.WaitForAnimation(298, self);
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] winks at [target]!\nIt was kind of cute...");
			  MakeHappy(target);
		  },
		  hidden: true
		);

		Skills["MHCry"] = new Skill(
		  name: "MHCry",
		  description: "MHCry",
		  target: SkillTarget.Enemy,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  await AnimationManager.Instance.WaitForAnimation(297, self);
			  BattleLogManager.Instance.QueueMessage(self, target, "Tears well up in [actor]'s eyes.");
			  MakeSad(target);
		  },
		  hidden: true
		);

		Skills["MHInsult"] = new Skill(
		  name: "MHInsult",
		  description: "MHInsult",
		  target: SkillTarget.Enemy,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  AudioManager.Instance.PlaySFX("BA_INK", volume: 0.9f);
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] accidentally says\nsomething mean.");
			  MakeAngry(target);
			  await Task.CompletedTask;
		  },
		  hidden: true
		);

		Skills["MHInstakill"] = new Skill(
		  name: "MHInstakill",
		  description: "MHInstakill",
		  target: SkillTarget.Enemy,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  await AnimationManager.Instance.WaitForAnimation(122, target, false);
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] slaps [target]!");
			  BattleManager.Instance.Damage(self, target, () => { return 999; }, true, 0f, neverCrit: true);
		  },
		  hidden: true
		);

		// Nefarious Chip //
		Skills["NCAttack"] = new Skill(
		  name: "NCAttack",
		  description: "NCAttack",
		  target: SkillTarget.Enemy,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  await AnimationManager.Instance.WaitForAnimation(123, target, false);
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] charges into [target]!");
			  BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false);
		  },
		  hidden: true
		);

		Skills["NCDoNothing"] = new Skill(
		  name: "NCDoNothing",
		  description: "NCDoNothing",
		  target: SkillTarget.Self,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] strokes his evil\nmoustache!");
			  await Task.CompletedTask;
		  },
		  hidden: true
		);

		Skills["NCLaugh"] = new Skill(
		  name: "NCLaugh",
		  description: "NCLaugh",
		  target: SkillTarget.Self,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  await AnimationManager.Instance.WaitForAnimation(162, self);
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] laughs like the evil villain he is!");
			  MakeHappy(self);
		  },
		  hidden: true
		);

		Skills["NCCookies"] = new Skill(
		  name: "NCCookies",
		  description: "NCCookies",
		  target: SkillTarget.AllEnemies,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] throws OATMEAL\nCOOKIES!");
			  for (int i = 0; i < 3; i++)
			  {
				  target = BattleManager.Instance.GetRandomAlivePartyMember();
				  BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false);
			  }
			  await AnimationManager.Instance.WaitForAnimation(196, self);
		  },
		  hidden: true
		);

		Skills["NCCookiesHappy"] = new Skill(
		  name: "NCCookiesHappy",
		  description: "NCCookiesHappy",
		  target: SkillTarget.AllEnemies,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] launches OATMEAL\nCOOKIES!");
			  for (int i = 0; i < 4; i++)
			  {
				  target = BattleManager.Instance.GetRandomAlivePartyMember();
				  BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false);
			  }
			  await AnimationManager.Instance.WaitForAnimation(196, self);
		  },
		  hidden: true
		);

		// Earth //
		Skills["TEAttack"] = new Skill(
		  name: "TEAttack",
		  description: "TEAttack",
		  target: SkillTarget.Enemy,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  await AnimationManager.Instance.WaitForAnimation(124, target, false);
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] attacks [target]!");
			  BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false);
		  },
		  hidden: true
		);

		Skills["TEDoNothing"] = new Skill(
		  name: "TEDoNothing",
		  description: "TEDoNothing",
		  target: SkillTarget.Self,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] is rotating slowly.");
			  await Task.CompletedTask;
		  },
		  hidden: true
		);

		Skills["TECruel"] = new Skill(
		  name: "TECruel",
		  description: "TECruel",
		  target: SkillTarget.Enemy,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  AnimationManager.Instance.PlayScreenAnimation(169, true);
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] is cruel to [target]!");
			  MakeSad(target);
			  await Task.CompletedTask;
		  },
		  hidden: true
		);

		Skills["TEProtect"] = new Skill(
		  name: "TEProtect",
		  description: "TEProtect",
		  target: SkillTarget.AllEnemies,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] uses her ultimate attack!");
			  AnimationManager.Instance.PlayScreenAnimation(170, true);
			  await Task.Delay(1000);
			  foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				  BattleManager.Instance.Damage(self, member.Actor, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; });
		  },
		  hidden: true
		);

		// Perfectheart //
		Skills["PHStealHeart"] = new Skill(
		 name: "PHStealHeart",
		 description: "PHStealHeart",
		 target: SkillTarget.Enemy,
		 cost: 0,
		 effect: async (self, target) =>
		 {
			 await AnimationManager.Instance.WaitForAnimation(122, target, false);
			 BattleLogManager.Instance.QueueMessage(self, target, "[actor] steals [target]'s heart.");
			 int damage = BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false, neverCrit: true);
			 if (damage > 0)
			 {
				 self.Heal(damage);
				 BattleManager.Instance.SpawnDamageNumber(damage, self.CenterPoint, DamageType.Heal);
			 }
		 },
		 hidden: true
		);

		Skills["PHStealBreath"] = new Skill(
		 name: "PHStealBreath",
		 description: "PHStealBreath",
		 target: SkillTarget.Enemy,
		 cost: 0,
		 effect: async (self, target) =>
		 {
			 await AnimationManager.Instance.WaitForAnimation(122, target, false);
			 BattleLogManager.Instance.QueueMessage(self, target, "[actor] steals [target]'s\nbreath away.");
			 target.CurrentJuice = 0;
			 BattleManager.Instance.SpawnDamageNumber(target.CurrentStats.MaxJuice, target.CenterPoint, DamageType.JuiceLoss);
			 target.SetHurt(true);
			 self.HealJuice(target.CurrentStats.MaxJuice);
			 BattleManager.Instance.SpawnDamageNumber(target.CurrentStats.MaxJuice, self.CenterPoint, DamageType.JuiceGain);
		 },
		 hidden: true
		);

		Skills["PHWrath"] = new Skill(
			 name: "PHWrath",
			 description: "PHWrath",
			 target: SkillTarget.AllEnemies,
			 cost: 0,
			 effect: async (self, target) =>
			 {
				 BattleLogManager.Instance.QueueMessage(self, target, "[actor] unleashes her wrath.");
				 foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
					 AnimationManager.Instance.PlayAnimation(210, member.Actor, false);
				 await Task.Delay(1500);
				 foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				 {
					 BattleManager.Instance.RandomEmotion(member.Actor);
					 BattleManager.Instance.Damage(self, member.Actor, () => { return member.Actor.CurrentStats.MaxHP * 0.75f; }, false, 0.15f, neverCrit: true);
				 }
			 },
			 hidden: true
		 );

		Skills["PHExploitEmotion"] = new Skill(
			 name: "PHExploitEmotion",
			 description: "PHExploitEmotion",
			 target: SkillTarget.Enemy,
			 cost: 0,
			 effect: async (self, target) =>
			 {
				 await AnimationManager.Instance.WaitForAnimation(124, target, false);
				 BattleLogManager.Instance.QueueMessage(self, target, "[actor] exploits [target]'s\nemotions!");
				 string old = self.CurrentState;
				 self.ForceState("EmotionExploit", old);
				 BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false, 0f, neverCrit: true);
				 self.ForceState(old);
			 },
			 hidden: true
		);

		Skills["PHSpare"] = new Skill(
			 name: "PHSpare",
			 description: "PHSpare",
			 target: SkillTarget.Enemy,
			 cost: 0,
			 effect: async (self, target) =>
			 {
				 await AnimationManager.Instance.WaitForAnimation(122, target, false);
				 BattleLogManager.Instance.QueueMessage(self, target, "[actor] decides to let [target] live.");
				 int damage = 1;
				 if (target.CurrentHP > 1)
					 damage = target.CurrentHP - 1;
				 target.Damage(damage);
				 AudioManager.Instance.PlaySFX("SE_dig", 0.7f, 0.9f);
				 BattleManager.Instance.SpawnDamageNumber(damage, target.CenterPoint);
			 },
			 hidden: true
		);

		Skills["PHAngelicVoice"] = new Skill(
			 name: "PHAngelicVoice",
			 description: "PHAngelicVoice",
			 target: SkillTarget.Enemy,
			 cost: 0,
			 effect: async (self, target) =>
			 {
				 AnimationManager.Instance.PlayAnimation(154, self);
				 await Task.Delay(166);
				 AnimationManager.Instance.PlayAnimation(155, self);
				 MakeSad(self);
				 foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				 {
					 BattleManager.Instance.Damage(self, member.Actor, () => { return 175; }, false, 0f, neverCrit: true);
					 MakeHappy(member.Actor);
				 }
			 },
			 hidden: true
		);

		// Roboheart //
		Skills["RHAttack"] = new Skill(
		  name: "RHAttack",
		  description: "RHAttack",
		  target: SkillTarget.Enemy,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  await AnimationManager.Instance.WaitForAnimation(125, target, false);
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] fires rocket hands!");
			  BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false);
		  },
		  hidden: true
		);

		Skills["RHDoNothing"] = new Skill(
		  name: "RHDoNothing",
		  description: "RHDoNothing",
		  target: SkillTarget.Self,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] is buffering...");
			  await Task.CompletedTask;
		  },
		  hidden: true
		);

		Skills["RHLaser"] = new Skill(
		  name: "RHLaser",
		  description: "RHLaser",
		  target: SkillTarget.Enemy,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  await AnimationManager.Instance.WaitForAnimation(160, target, false);
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] opens her mouth and\nfires a laser!");
			  BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 3 - target.CurrentStats.DEF; }, false);
		  },
		  hidden: true
		);

		Skills["RHSnack"] = new Skill(
		  name: "RHSnack",
		  description: "RHSnack",
		  target: SkillTarget.Self,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] opens her mouth!\nA nutritious SNACK appears!");
			  await AnimationManager.Instance.WaitForAnimation(216, self, true);
			  self.Heal(200);
			  BattleManager.Instance.SpawnDamageNumber(200, self.CenterPoint, DamageType.Heal);
		  },
		  hidden: true
		);

		Skills["RHExplode"] = new Skill(
		  name: "RHExplode",
		  description: "RHExplode",
		  target: SkillTarget.AllEnemies,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] sheds a single tear...\nand bids everyone farewell!");
			  await AnimationManager.Instance.WaitForScreenAnimation(216, true);
			  foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
			  {
				  BattleManager.Instance.Damage(self, member.Actor, () => { return member.Actor.CurrentStats.MaxHP * 0.1f; }, false, 0f, neverCrit: true);
			  }
			  self.Damage(self.CurrentStats.MaxHP);
		  },
		  hidden: true
		);

		// Fear of Heights //
		Skills["FOHAttack"] = new Skill(
		  name: "FOHAttack",
		  description: "FOHAttack",
		  target: SkillTarget.Enemy,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  await AnimationManager.Instance.WaitForAnimation(140, target, false);
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] strikes [target].");
			  BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK * 2 - target.CurrentStats.DEF; }, false, neverCrit: true);
		  },
		  hidden: true
		);

		Skills["FOHDoNothing"] = new Skill(
		  name: "FOHDoNothing",
		  description: "FOHDoNothing",
		  target: SkillTarget.Enemy,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] taunts [target] as he falls.");
			  await Task.CompletedTask;
		  },
		  hidden: true
		);

		Skills["FOHGrab"] = new Skill(
		  name: "FOHGrab",
		  description: "FOHGrab",
		  target: SkillTarget.AllEnemies,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  BattleLogManager.Instance.QueueMessage("Hands appear and grab everyone!");
			  foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				  AnimationManager.Instance.PlayAnimation(164, member.Actor, false);
			  await Task.Delay(2000);
			  BattleLogManager.Instance.QueueMessage("Everyone's ATTACK fell!");
			  foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
			  {
				  AnimationManager.Instance.PlayAnimation(215, member.Actor, false);
				  member.Actor.AddStatModifier("AttackDown", true);
			  }
			  await Task.Delay(1000);
		  },
		  hidden: true
		);

		Skills["FOHHands"] = new Skill(
		  name: "FOHHands",
		  description: "FOHHands",
		  target: SkillTarget.Self,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  BattleLogManager.Instance.QueueMessage(self, target, "More hands appear and\nsurround [actor].");
			  AnimationManager.Instance.PlayAnimation(11, self);
			  await Task.Delay(2000);
			  AnimationManager.Instance.PlayAnimation(218, self);
			  self.AddStatModifier("DefenseUp");
			  await Task.Delay(1000);
		  },
		  hidden: true
		);

		Skills["FOHShove"] = new Skill(
		  name: "FOHShove",
		  description: "FOHShove",
		  target: SkillTarget.Enemy,
		  cost: 0,
		  effect: async (self, target) =>
		  {
			  await AnimationManager.Instance.WaitForAnimation(209, target, false);
			  BattleLogManager.Instance.QueueMessage(self, target, "[actor] shoves [target].");
			  BattleManager.Instance.Damage(self, target, () => { return self.CurrentStats.ATK; }, neverCrit: true);
			  target.SetState("afraid");
		  },
		  hidden: true
		);

		#endregion

		#region MODIFIERS
		Modifiers.Add("Neutral", () => new StatModifier());
		Modifiers.Add("Happy", () => new StatModifier(new StatBonus(StatType.LCK, 2f), new StatBonus(StatType.SPD, 1.25f), new StatBonus(StatType.HIT, -10)));
		Modifiers.Add("Ecstatic", () => new StatModifier(new StatBonus(StatType.LCK, 3f), new StatBonus(StatType.SPD, 1.5f), new StatBonus(StatType.HIT, -20)));
		Modifiers.Add("Manic", () => new StatModifier(new StatBonus(StatType.LCK, 4f), new StatBonus(StatType.SPD, 2f), new StatBonus(StatType.HIT, -30)));
		Modifiers.Add("Angry", () => new StatModifier(new StatBonus(StatType.ATK, 1.3f), new StatBonus(StatType.DEF, 0.5f)));
		Modifiers.Add("Enraged", () => new StatModifier(new StatBonus(StatType.ATK, 1.5f), new StatBonus(StatType.DEF, 0.3f)));
		Modifiers.Add("Furious", () => new StatModifier(new StatBonus(StatType.ATK, 2f), new StatBonus(StatType.DEF, 0.15f)));
		Modifiers.Add("Sad", () => new StatModifier(new StatBonus(StatType.DEF, 1.25f), new StatBonus(StatType.SPD, 0.8f)));
		Modifiers.Add("Depressed", () => new StatModifier(new StatBonus(StatType.DEF, 1.35f), new StatBonus(StatType.SPD, 0.65f)));
		Modifiers.Add("Miserable", () => new StatModifier(new StatBonus(StatType.DEF, 1.5f), new StatBonus(StatType.SPD, 0.5f)));
		Modifiers.Add("Stressed", () => new StatModifier(new StatBonus(StatType.ATK, 1.2f), new StatBonus(StatType.DEF, 0.9f)));
		Modifiers.Add("AttackUp", () => new TierStatModifier(6, new StatBonus(StatType.ATK, 1.1f), new StatBonus(StatType.ATK, 1.25f), new StatBonus(StatType.ATK, 1.5f)).WithMessages("ATTACK rose!", "ATTACK cannot go\nany higher!"));
		Modifiers.Add("AttackDown", () => new TierStatModifier(6, new StatBonus(StatType.ATK, 0.9f), new StatBonus(StatType.ATK, 0.8f), new StatBonus(StatType.ATK, 0.7f)).WithMessages("ATTACK fell!", "ATTACK cannot go\nany lower!"));
		Modifiers.Add("DefenseUp", () => new TierStatModifier(6, new StatBonus(StatType.DEF, 1.15f), new StatBonus(StatType.DEF, 1.3f), new StatBonus(StatType.DEF, 1.5f)).WithMessages("DEFENSE rose!", "DEFENSE cannot go\nany higher!"));
		Modifiers.Add("DefenseDown", () => new TierStatModifier(6, new StatBonus(StatType.DEF, 0.75f), new StatBonus(StatType.DEF, 0.5f), new StatBonus(StatType.DEF, 0.25f)).WithMessages("DEFENSE fell!", "DEFENSE cannot go\nany lower!"));
		Modifiers.Add("SpeedUp", () => new TierStatModifier(6, new StatBonus(StatType.SPD, 1.15f), new StatBonus(StatType.SPD, 2f), new StatBonus(StatType.SPD, 5f)).WithMessages("SPEED rose!", "SPEED cannot go\nany higher!"));
		Modifiers.Add("SpeedDown", () => new TierStatModifier(6, new StatBonus(StatType.SPD, 0.8f), new StatBonus(StatType.SPD, 0.5f), new StatBonus(StatType.SPD, 0.25f)).WithMessages("SPEED fell!", "SPEED cannot go\nany lower!"));
		Modifiers.Add("ReleaseEnergy", () => new StatModifier(new StatBonus(StatType.SPD, 1.25f), new StatBonus(StatType.ATK, 1.25f), new StatBonus(StatType.DEF, 1.25f), new StatBonus(StatType.LCK, 1.25f)));
		Modifiers.Add("ReleaseEnergyBasil", () => new StatModifier(new StatBonus(StatType.SPD, 1.25f), new StatBonus(StatType.ATK, 1.25f), new StatBonus(StatType.DEF, 1.25f), new StatBonus(StatType.LCK, 1.25f)));
		Modifiers.Add("SnoCone", () => new StatModifier(new StatBonus(StatType.SPD, 1.2f), new StatBonus(StatType.ATK, 1.2f), new StatBonus(StatType.DEF, 1.2f), new StatBonus(StatType.LCK, 1.2f)));
		Modifiers.Add("Flex", () => new FlexStatModifier(new StatBonus(StatType.HIT, 1000)));
		// see if these even need to be their own classes
		Modifiers.Add("Guard", () => new GuardStatModifier(1));
		Modifiers.Add("PlotArmor", () => new PlotArmorStatModifier(1));
		Modifiers.Add("Tickle", () => new StatModifier(1));
		Modifiers.Add("SweetheartHappy", () => new EmotionLockStatModifier("happy", new StatBonus(StatType.LCK, 2f), new StatBonus(StatType.SPD, 1.25f), new StatBonus(StatType.HIT, -10)));
		Modifiers.Add("SweetheartEcstatic", () => new EmotionLockStatModifier("happy", new StatBonus(StatType.LCK, 20), new StatBonus(StatType.LCK, 3f), new StatBonus(StatType.SPD, 1.5f), new StatBonus(StatType.HIT, -20)));
		Modifiers.Add("SweetheartManic", () => new EmotionLockStatModifier("happy", new StatBonus(StatType.LCK, 20), new StatBonus(StatType.LCK, 4f), new StatBonus(StatType.SPD, 2f), new StatBonus(StatType.HIT, -30)));
		Modifiers.Add("SpaceExAngry", () => new EmotionLockStatModifier("angry", new StatBonus(StatType.ATK, 1.25f), new StatBonus(StatType.DEF, 0.9f)));
		Modifiers.Add("SpaceExEnraged", () => new EmotionLockStatModifier("angry", new StatBonus(StatType.ATK, 1.5f), new StatBonus(StatType.DEF, 0.5f)));
		Modifiers.Add("SpaceExFurious", () => new EmotionLockStatModifier("angry", new StatBonus(StatType.ATK, 2f), new StatBonus(StatType.DEF, 0.3f)));
		Modifiers.Add("UnbreadTwinsSad", () => new EmotionLockStatModifier("sad", new StatBonus(StatType.DEF, 1.25f), new StatBonus(StatType.SPD, 0.8f)));
		Modifiers.Add("UnbreadTwinsDepressed", () => new EmotionLockStatModifier("sad", new StatBonus(StatType.DEF, 1.35f), new StatBonus(StatType.SPD, 0.65f)));
		Modifiers.Add("UnbreadTwinsMiserable", () => new EmotionLockStatModifier("sad", new StatBonus(StatType.DEF, 1.5f), new StatBonus(StatType.SPD, 0.5f)));
		Modifiers.Add("MrJawsumBarrier", () => new MrJawsumStatModifier());
		Modifiers.Add("Taunt", () => new StatModifier(1));
		Modifiers.Add("AubreyCounter", () => new AubreyCounterModifier(1));
		Modifiers.Add("HitRateDown", () => new StatModifier(2, new StatBonus(StatType.HIT, -55)));
		Modifiers.Add("PhotographHitRateDown", () => new StatModifier(1, new StatBonus(StatType.HIT, -25)));
		Modifiers.Add("Charm", () => new CharmStatModifier(1));
		Modifiers.Add("EmotionExploit", () => new EmotionLockStatModifier("emotion"));
		#endregion

		#region SNACKS

		// will most likely be file driven in the future

		AddSnack("Tofu", "Soft cardboard, basically.\nHeals 5 HEART.", 5);
		AddSnack("Candy", "A child's favorite food. Sweet!\nHeals 30 HEART.", 30);
		AddSnack("Smores", "S'more smores, please!\nHeals 50 HEART.", 50);
		AddSnack("Granola Bar", "A healthy stick of grain.\nHeals 60 HEART.", 60);
		AddSnack("Bread", "A slice of life.\nHeals 60 HEART.", 60);
		AddSnack("Nachos", "Suggested serving size: 6-8 nachos.\nHeals 75 HEART.", 75);
		AddSnack("Chicken Wing", "Wing of chicken.\nHeals 80 HEART.", 80);
		AddSnack("Hot Dog", "Better than a cold dog.\nHeals 100 HEART.", 100);
		AddSnack("Waffle", "Designed to hold syrup!\nHeals 150 HEART.", 150);
		AddSnack("Pancake", "Not designed to hold syrup...\nHeals 150 HEART.", 150);
		AddSnack("Pizza Slice", "1/8th of a Whole pizza.\nHeals 175 HEART.", 175);
		AddSnack("Fish Taco", "Aquatic taco.\nHeals 200 HEART.", 200);
		AddSnack("Cheeseburger", "Contains all food groups, so it's healthy!\nHeals 250 HEART.", 250);

		AddSnack("Chocolate", "Chocolate!? Oh, it's baking chocolate...\nHeals 40% of HEART.", 0.4f);
		AddSnack("Donut", "Circular bread with a hole in it.\nHeals 60% of HEART.", 0.6f);
		AddSnack("Ramen", "Now that is a lot of sodium!\nHeals 80% of HEART.", 0.8f);
		AddSnack("Spaghetti", "Wet noodles slathered with chunky sauce.\nFully heals a friend's HEART.", 1.0f);
		AddSnack("Dino Pasta", "Pasta shaped line dinosaurs.\nFully restores a friend's HEART.", 1.0f);

		AddGroupSnack("Popcorn", "9/10 dentists hate it.\nHeals 35 HEART to all friends.", 35);
		AddGroupSnack("Fries", "From France, wherever that is...\nHeals 60 HEART to all friends.", 60);
		AddGroupSnack("Cheese Wheel", "Delicious, yet functional.\nHeals 100 HEART to all friends.", 100);
		AddGroupSnack("Whole Chicken", "An entire chicken, wings and all.\nHeals 175 HEART to all friends.", 175);
		AddGroupSnack("Whole Pizza", "8/8ths of a whole pizza.\nHeals 250 HEART to all friends.", 250);
		AddGroupSnack("Dino Clumps", "Chicken nuggets shaped like dinosaurs.\nHeals 250 HEART to all friends.", 250);

		AddJuiceSnack("Plum Juice", "For seniors. Wait, that's prune juice.\nHeals 15 JUICE.", 15);
		AddJuiceSnack("Apple Juice", "Apparently better than orange juice.\nHeals 25 JUICE.", 25);
		AddJuiceSnack("Breadfruit Juice", "Does not taste like bread.\nHeals 50 JUICE.", 50);
		AddJuiceSnack("Lemonade", "When life gives you lemons, make this!\nHeals 75 JUICE.", 75);
		AddJuiceSnack("Orange Juice", "Apparently better than apple juice.\nHeals 100 JUICE.", 100);
		AddJuiceSnack("Pineapple Juice", "Painful... Why do you drink it?\nHeals 150 JUICE.", 150);
		AddJuiceSnack("Bottled Water", "Water in a bottle.\nHeals 100 JUICE.", 100);
		AddJuiceSnack("Fruit Juice?", "You're not sure what fruit it is.\nHeals 75 JUICE.", 75);


		AddJuiceSnack("Cherry Soda", "Carbonated hell sludge.\nHeals 25% of JUICE.", 0.25f);
		AddJuiceSnack("Star Fruit Soda", "To be shared with a friend.\nHeals 35% of JUICE.", 0.35f);
		AddJuiceSnack("Tasty Soda", "Tasty soda for thirsty people.\nHeals 50% of JUICE.", 0.5f);
		AddJuiceSnack("Peach Soda", "A regular peach soda.\nHeals 60% of JUICE.", 0.6f);
		AddJuiceSnack("Butt Peach Soda", "An irregular peach soda.\nHeals 61% of JUICE.", 0.61f);
		AddJuiceSnack("Watermelon Juice", "Heavenly nectar.\nFully heals a friend's JUICE.", 1.0f);
		AddJuiceSnack("Dino Melon Soda", "Melon soda in a dino-shaped bottle.\nFully heals a friend's JUICE.", 1.0f);

		AddGroupJuiceSnack("Banana Smoothie", "A little bland, but it does the job.\nHeals 20 JUICE to all friends.", 20);
		AddGroupJuiceSnack("Mango Smoothie", "Makes you tango!\nHeals 40 JUICE to all friends.", 40);
		AddGroupJuiceSnack("Berry Smoothie", "A healthy smoothie that tastes like dirt.\nHeals 60 JUICE to all friends.", 60);
		AddGroupJuiceSnack("Melon Smoothie", "Chunky green melon goodness.\nHeals 80 JUICE to all friends.", 80);
		AddGroupJuiceSnack("S.berry Smoothie", "The default smoothie.\nHeals 100 JUICE to all friends.", 100);
		AddGroupJuiceSnack("Dino Smoothie", "Berry smoothie in a dino-shaped cup.\nHeals 150 JUICE to all friends.", 150);

		AddComboSnack("Tomato", "You say tomato, I say tomato.\nHeals 100 HEART and 50 JUICE.", 100, 50);
		AddComboSnack("Combo Meal", "What more could you ask for?\nHeals 250 HEART and 100 JUICE.", 250, 100);

		Items["Grape Soda"] = new Item(
			name: "GRAPE SODA",
			description: "Objectively the best soda.\nHeals 80% of JUICE.",
			target: SkillTarget.Ally,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, $"[actor] uses GRAPE SODA!");
				AnimationManager.Instance.PlayAnimation(212, target, false);
				// grape soda uses emotion due to an oversight
				BattleManager.Instance.HealJuice(self, target, () => { return target.CurrentStats.MaxJuice * 0.8f; });
				await Task.CompletedTask;
			}
		);

		Items["Coffee"] = new Item(
			name: "COFFEE",
			description: "Bitter bean juice.\nIncreases a friend's SPEED.",
			target: SkillTarget.Ally,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, $"[actor] uses COFFEE!");
				AnimationManager.Instance.PlayAnimation(214, target, false);
				// coffee heals, uses emotion, and has a variance due to an oversight
				BattleManager.Instance.Heal(self, target, () => { return target.CurrentStats.MaxJuice * 0.1f; }, 0.2f);
				target.AddTierStatModifier("SpeedUp", 3);
				await Task.CompletedTask;
			}
		);

		Items["☐☐☐"] = new Item(
		   name: "☐☐☐",
		   description: "☐☐☐☐☐☐☐☐☐ ☐☐☐ ☐☐☐",
		   target: SkillTarget.Ally,
		   effect: async (self, target) =>
		   {
			   BattleLogManager.Instance.QueueMessage(self, target, $"[actor] uses ☐☐☐!");
			   AnimationManager.Instance.PlayAnimation(215, target, false);
			   // ☐☐☐ uses emotion due to an oversight
			   BattleManager.Instance.Heal(self, target, () => { return 50; }, 0f);
			   await Task.CompletedTask;
		   }
	   );

		Items["Prune Juice"] = new Item(
			name: "PRUNE JUICE",
			description: "This tastes horrible. Don't drink it.\nHeals 30 JUICE...probably.",
			target: SkillTarget.Ally,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] uses PRUNE JUICE!");
				AnimationManager.Instance.PlayAnimation(213, target, false);
				int total = 30;
				if (BattleManager.Instance.GetAllPartyMembers().Any(x => x.Actor.Weapon.Name == "Blender" || x.Actor.Weapon.Name == "Ol' Reliable"))
					total = 45;
				target.HealJuice(total);
				BattleManager.Instance.SpawnDamageNumber(total, target.CenterPoint, DamageType.JuiceGain);
				BattleLogManager.Instance.QueueMessage(self, target, $"[target] recovered {total} JUICE!");
				int hpLoss = (int)Math.Round(target.CurrentHP * 0.3f, MidpointRounding.AwayFromZero);
				target.Damage(hpLoss);
				// damaging items don't kill
				if (target.CurrentHP == 0)
					target.CurrentHP = 1;
				await Task.CompletedTask;
			}
		);

		Items["Rotten Milk"] = new Item(
			name: "ROTTEN MILK",
			description: "This is bad. Don't drink it.\nHeals 10 juice + ???",
			target: SkillTarget.Ally,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] uses ROTTEN MILK!");
				AnimationManager.Instance.PlayAnimation(213, target, false);
				int total = 10;
				if (BattleManager.Instance.GetAllPartyMembers().Any(x => x.Actor.Weapon.Name == "Blender" || x.Actor.Weapon.Name == "Ol' Reliable"))
					total = 15;
				target.HealJuice(total);
				BattleManager.Instance.SpawnDamageNumber(total, target.CenterPoint, DamageType.JuiceGain);
				BattleLogManager.Instance.QueueMessage(self, target, $"[target] recovered {total} JUICE!");
				int hpLoss = (int)Math.Round(target.CurrentHP * 0.5f, MidpointRounding.AwayFromZero);
				target.Damage(hpLoss);
				// damaging items don't kill
				if (target.CurrentHP == 0)
					target.CurrentHP = 1;
				await Task.CompletedTask;
			}
		);

		Items["Milk"] = new Item(
			name: "MILK",
			description: "Good for your bones. Heals 10 juice\nand increases DEFENSE for the battle.",
			target: SkillTarget.Ally,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] uses MILK!");
				AnimationManager.Instance.PlayAnimation(213, target, false);
				await Task.Delay(2000);
				AnimationManager.Instance.PlayAnimation(214, target, false);
				int total = 10;
				if (BattleManager.Instance.GetAllPartyMembers().Any(x => x.Actor.Weapon.Name == "Blender" || x.Actor.Weapon.Name == "Ol' Reliable"))
					total = 15;
				target.HealJuice(total);
				BattleManager.Instance.SpawnDamageNumber(total, target.CenterPoint, DamageType.JuiceGain);
				BattleLogManager.Instance.QueueMessage(self, target, $"[target] recovered {total} JUICE!");
				target.AddStatModifier("DefenseUp");
				await Task.CompletedTask;
			}
		);

		Items["Sno-Cone"] = new Item(
			name: "SNO-CONE",
			description: "Heals a friend's HEART and JUICE, and\nraises ALL STATS for the battle.",
			target: SkillTarget.Ally,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, $"[actor] uses SNO-CONE!");
				await AnimationManager.Instance.WaitForAnimation(214, target, false);
				target.Heal(target.CurrentStats.MaxHP);
				target.HealJuice(target.CurrentStats.MaxJuice);
				target.AddStatModifier("SnoCone");
				BattleLogManager.Instance.QueueMessage(self, target, "[target]'s ATTACK rose!");
				BattleLogManager.Instance.QueueMessage(self, target, "[target]'s DEFENSE rose!");
				BattleLogManager.Instance.QueueMessage(self, target, "[target]'s SPEED rose!");
				BattleLogManager.Instance.QueueMessage(self, target, "[target]'s LUCK rose!");
			}
		);

		Items["Life Jam"] = new Item(
			name: "LIFE JAM",
			description: "Infused with the spirit of life.\nRevives a friend that is TOAST.",
			target: SkillTarget.DeadAlly,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] uses LIFE JAM!");
				if (target.CurrentState != "toast")
				{
					target = BattleManager.Instance.GetRandomDeadPartyMember();
					if (target == null)
					{
						BattleLogManager.Instance.QueueMessage("It had no effect.");
						return;
					}
				}
				await AnimationManager.Instance.WaitForAnimation(269, target, false);
				if (BattleManager.Instance.GetAllPartyMembers().Any(x => x.Actor.Charm?.Name == "Breadphones"))
					target.CurrentHP = target.CurrentStats.MaxHP;
				else
					target.CurrentHP = target.CurrentStats.MaxHP / 2;
				target.SetState("neutral", true);
				BattleLogManager.Instance.QueueMessage(self, target, "[target] rose again!");
			}
		);

		Items["Dino Jam"] = new Item(
		   name: "DINO JAM",
		   description: "Infused with the spirit of dino life.\nFully revives a friend that is TOAST.",
		   target: SkillTarget.DeadAlly,
		   effect: async (self, target) =>
		   {
			   BattleLogManager.Instance.QueueMessage(self, target, "[actor] uses DINO JAM!");
			   if (target.CurrentState != "toast")
			   {
				   target = BattleManager.Instance.GetRandomDeadPartyMember();
				   if (target == null)
				   {
					   BattleLogManager.Instance.QueueMessage("It had no effect.");
					   return;
				   }
			   }
			   await AnimationManager.Instance.WaitForAnimation(269, target, false);
			   target.CurrentHP = target.CurrentStats.MaxHP;
			   target.SetState("neutral", true);
			   BattleLogManager.Instance.QueueMessage(self, target, "[target] rose again!");
		   }
		);

		Items["Jam Packets"] = new Item(
		   name: "JAM PACKETS",
		   description: "Infused with the spirit of life.\nRevives all friends that are TOAST.",
		   target: SkillTarget.AllDeadAllies,
		   effect: async (self, target) =>
		   {
			   BattleLogManager.Instance.QueueMessage(self, null, "[actor] uses JAM PACKETS!");
			   List<PartyMemberComponent> dead = BattleManager.Instance.GetDeadPartyMembers();
			   if (dead.Count == 0)
			   {
				   BattleLogManager.Instance.QueueMessage("It had no effect.");
				   return;
			   }
			   foreach (PartyMemberComponent member in dead)
			   {
				   AnimationManager.Instance.PlayAnimation(269, member.Actor, false);
				   member.Actor.CurrentHP = member.Actor.CurrentStats.MaxHP / 4;
				   member.Actor.SetState("neutral", true);
				   BattleLogManager.Instance.QueueMessage(self, member.Actor, "[target] rose again!");
			   }
			   await Task.CompletedTask;
		   }
		);

		// TODO: faraway town snacks

		#endregion

		#region TOYS
		Items["Rubber Band"] = new Item(
			name: "RUBBER BAND",
			description: "Deals damage to a foe and reduces\ntheir DEFENSE.",
			target: SkillTarget.Enemy,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] uses RUBBER BAND!");
				BattleManager.Instance.Damage(self, target, () => { return 50; }, true, 0, neverCrit: true);
				await AnimationManager.Instance.WaitForAnimation(219, target);
				target.AddStatModifier("DefenseDown");
			},
			isToy: true
		);

		Items["Big Rubber Band"] = new Item(
			name: "BIG RUBBER BAND",
			description: "Deals big damage to a foe and reduces\ntheir DEFENSE.",
			target: SkillTarget.Enemy,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] uses BIG RUBBER BAND!");
				BattleManager.Instance.Damage(self, target, () => { return 150; }, true, 0, neverCrit: true);
				await AnimationManager.Instance.WaitForAnimation(219, target);
				target.AddStatModifier("DefenseDown");
			},
			isToy: true
		);

		Items["Jacks"] = new Item(
			name: "JACKS",
			description: "Deals small damage to all foes\nand reduces their SPEED.",
			target: SkillTarget.AllEnemies,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] uses JACKS!");
				foreach (Enemy enemy in BattleManager.Instance.GetAllEnemies())
				{
					AnimationManager.Instance.PlayAnimation(122, enemy);
				}
				await Task.Delay(1000);
				foreach (Enemy enemy in BattleManager.Instance.GetAllEnemies())
				{
					BattleManager.Instance.Damage(self, enemy, () => { return 25; }, true, 0, neverCrit: true);
					AnimationManager.Instance.PlayAnimation(219, enemy);
					enemy.AddStatModifier("SpeedDown", silent: true);
				}
				BattleLogManager.Instance.QueueMessage("All foes' SPEED fell.");
				await Task.Delay(500);
			},
			isToy: true
		);

		Items["Dynamite"] = new Item(
			name: "DYNAMITE",
			description: "Actually dangerous...\nDeals heavy damage to all foes.",
			target: SkillTarget.AllEnemies,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] uses DYNAMITE!");
				await AnimationManager.Instance.WaitForScreenAnimation(172, true);
				foreach (Enemy enemy in BattleManager.Instance.GetAllEnemies())
				{
					BattleManager.Instance.Damage(self, enemy, () => { return 150; }, true, 0, neverCrit: true);
				}
			},
			isToy: true
		);

		Items["Air Horn"] = new Item(
			name: "AIR HORN",
			description: "Who would invent this!?\nInflicts ANGER on all friends.",
			target: SkillTarget.AllAllies,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] uses AIR HORN!");
				AudioManager.Instance.PlaySFX("SE_airhorn", 1, 0.9f);
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					MakeAngry(member.Actor);
				}
				await Task.CompletedTask;
			},
			isToy: true
		);

		Items["Rain Cloud"] = new Item(
			name: "RAIN CLOUD",
			description: "Angsty water droplets.\nInflicts SAD on all friends.",
			target: SkillTarget.AllAllies,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] uses RAIN CLOUD!");
				AudioManager.Instance.PlaySFX("BA_sad_level_2", 1, 0.9f);
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					MakeSad(member.Actor);
				}
				await Task.CompletedTask;
			},
			isToy: true
		);

		Items["Confetti"] = new Item(
			name: "CONFETTI",
			description: "Small squares of colorful paper.\nInflicts HAPPY on all friends.",
			target: SkillTarget.AllAllies,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] uses CONFETTI!");
				AudioManager.Instance.PlaySFX("GEN_ta_da", 1, 0.9f);
				foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
				{
					MakeHappy(member.Actor);
				}
				await Task.CompletedTask;
			},
			isToy: true
		);

		Items["Sparkler"] = new Item(
			name: "SPARKLER",
			description: "Little fires.\nInflicts HAPPY on a friend or foe.",
			target: SkillTarget.AllyOrEnemy,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] uses SPARKLER!");
				AudioManager.Instance.PlaySFX("GEN_pahpuh", 1, 0.9f);
				MakeHappy(target);
				await Task.CompletedTask;
			},
			isToy: true
		);

		Items["Poetry Book"] = new Item(
			name: "POETRY BOOK",
			description: "Sad words string together.\nInflicts SAD on a friend or foe.",
			target: SkillTarget.AllyOrEnemy,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] uses POETRY BOOK!");
				AnimationManager.Instance.PlayAnimation(272, target);
				MakeSad(target);
				await Task.CompletedTask;
			},
			isToy: true
		);

		Items["Present"] = new Item(
			name: "PRESENT",
			description: "It's not what you wanted...\nInflicts ANGER on a friend or foe.",
			target: SkillTarget.AllyOrEnemy,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] uses PRESENT!");
				AudioManager.Instance.PlaySFX("SE_shuffle", 1, 0.9f);
				MakeAngry(target);
				await Task.CompletedTask;
			},
			isToy: true
		);

		Items["Dandelion"] = new Item(
			name: "DANDELION",
			description: "Has a calming effect.\nRemoves emotion from a friend or foe.",
			target: SkillTarget.AllyOrEnemy,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, "[actor] uses DANDELION!");
				AudioManager.Instance.PlaySFX("BA_calm_down", 1, 0.9f);
				// this should be changed once boss specific/special states are improved
				if (target.CurrentState == "neutral" || target.HasLockedEmotion())
				{
					BattleLogManager.Instance.QueueMessage("It had no effect.");
				}
				else
				{
					BattleLogManager.Instance.QueueMessage(self, target, "[target] feels NEUTRAL.");
					target.SetState("neutral", true);
				}
				await Task.CompletedTask;
			},
			isToy: true
		);
		#endregion

		#region WEAPONS
		Weapons["Shiny Knife"] = new Weapon("Shiny Knife", new Stats(atk: 5, hit: 100));
		Weapons["Knife"] = new Weapon("Shiny Knife", new Stats(atk: 7, spd:2, hit: 100));
		Weapons["Dull Knife"] = new Weapon("Dull Knife", new Stats(atk: 9, spd: 4, lck: 2, hit: 100));
		Weapons["Rusty Knife"] = new Weapon("Rusty Knife", new Stats(atk: 11, def: 2, spd: 6, lck: 4, hit: 100));
		Weapons["Red Knife"] = new Weapon("Red Knife", new Stats(atk: 13, def: 6, spd: 6, lck: 6, hit: 100));

		Weapons["Fly Swatter"] = new Weapon("Fly Swatter", new Stats(atk: 1, hit: 1000));
		Weapons["Steak Knife"] = new Weapon("Steak Knife", new Stats(atk: 30, hit: 25));
		Weapons["Hands"] = new Weapon("Hands", new Stats(atk: 2, hit: 95));
		Weapons["Steak Knife"] = new Weapon("Steak Knife", new Stats(atk: 30, hit: 25));
		Weapons["Steak Knife"] = new Weapon("Steak Knife", new Stats(atk: 30, hit: 25));
		// potential todo: other violin variants?
		Weapons["Violin"] = new Weapon("Violin", new Stats(atk: 14, hit: 1000));

		Weapons["Stuffed Toy"] = new Weapon("Stuffed Toy", new Stats(atk: 4, hit: 100));
		Weapons["Comet Hammer"] = new Weapon("Comet Hammer", new Stats(atk: 6, lck: 2, hit: 100));
		Weapons["Body Pillow"] = new Weapon("Body Pillow", new Stats(hp: 10, atk: 8, hit: 100));
		Weapons["Pool Noodle"] = new Weapon("Pool Noodle", new Stats(atk: -5, def: -5, spd: -5, lck: -5, hit: 100));
		Weapons["Cool Noodle"] = new Weapon("Cool Noodle", new Stats(atk: 15, hit: 100));
		Weapons["Hero's Trophy"] = new Weapon("Hero's Trophy", new Stats(atk: 10, def: 5, hit: 100));
		Weapons["Mailbox"] = new Weapon("Mailbox", new Stats(atk: 12, hit: 100));
		Weapons["Baguette"] = new Weapon("Baguette", new Stats(atk: 10, def: 10, hit: 100));
		Weapons["Sweetheart Bust"] = new Weapon("Sweetheart Bust", new Stats(atk: 20, spd: -30, hit: 75));
		Weapons["Baseball Bat"] = new Weapon("Baseball Bat", new Stats(hp: 10, atk: 20, spd: 10, lck: 10, hit: 100));

		Weapons["Nail Bat"] = new Weapon("Nail Bat", new Stats(atk: 3, hit: 95));

		Weapons["Rubber Ball"] = new Weapon("Rubber Ball", new Stats(atk: 3, hit: 100));
		Weapons["Meteor Ball"] = new Weapon("Meteor Ball", new Stats(atk: 4, lck: 2, hit: 100));
		Weapons["Blood Orange"] = new Weapon("Blood Orange", new Stats(juice: 30, atk: 6, hit: 100));
		Weapons["Jack"] = new Weapon("Jack", new Stats(atk: 12, def: -6, lck: -6, hit: 100));
		Weapons["Beach Ball"] = new Weapon("Beach Ball", new Stats(atk: 10, spd: 25, hit: 100));
		Weapons["Coconut"] = new Weapon("Coconut", new Stats(juice: 50, atk: 8, hit: 100));
		Weapons["Globe"] = new Weapon("Globe", new Stats(atk: 10, hit: 1000));
		Weapons["Chicken Ball"] = new Weapon("Chicken Ball", new Stats(spd: 200, hit: 100));
		Weapons["Snowball"] = new Weapon("Snowball", new Stats(atk: 13, hit: 100));
		Weapons["Basketball"] = new Weapon("Basketball", new Stats(juice: 50, atk: 15, spd: 100, lck: 15, hit: 100));

		Weapons["Basketball (Real World)"] = new Weapon("Basketball", new Stats(atk: 2, hit: 95));

		Weapons["Spatula"] = new Weapon("Spatula", new Stats(atk: 4, hit: 100));
		Weapons["Rolling Pin"] = new Weapon("Rolling Pin", new Stats(hp: 10, atk: 12, def: 12, hit: 100));
		Weapons["Teapot"] = new Weapon("Teapot", new Stats(juice: 30, atk: 6, hit: 100));
		Weapons["Frying Pan"] = new Weapon("Frying Pan", new Stats(hp: 30, atk: 7, hit: 100));
		Weapons["Blender"] = new Weapon("Blender", new Stats(juice: 30, atk: 7, hit: 100));
		Weapons["Baking Pan"] = new Weapon("Baking Pan", new Stats(hp: 10, atk: 6, hit: 100));
		Weapons["Tenderizer"] = new Weapon("Tenderizer", new Stats(atk: 30, hit: 100));
		Weapons["LOL Sword"] = new Weapon("LOL Sword", new Stats(juice: 10, atk: 14, hit: 100));
		Weapons["Ol' Reliable"] = new Weapon("Ol' Reliable", new Stats(hp: 20, juice: 20, atk: 20, hit: 100));
		Weapons["Shucker"] = new Weapon("Shucker", new Stats(atk: 10, hit: 100));

		Weapons["Fist"] = new Weapon("Fist", new Stats(atk: 1, hit: 95));

		Weapons["Garden Shears"] = new Weapon("Garden Shears", new Stats(atk: 13, def: 6, spd: 6, lck: 6, hit: 100));
		#endregion

		#region CHARMS
		// TODO: missing charms (special behavior/unused): sales tag, chef's chat, contract, abbi's eye, unused charms
		Charms["3-leaf Clover"] = new Charm("3-leaf Clover", new Stats(lck: 3));
		Charms["4-leaf Clover"] = new Charm("4-leaf Clover", new Stats(hp: 4, lck: 4));
		Charms["5-leaf Clover"] = new Charm("5-leaf Clover", () =>
		{
			return new Stats(lck: 2 + BattleManager.Instance.Energy);
		});
		Charms["Backpack"] = new Charm("Backpack", new Stats(def: 2));
		Charms["Baseball Cap"] = new Charm("Baseball Cap", new Stats(def: 10, spd: 15));
		Charms["Binoculars"] = new Charm("Binoculars", new Stats(def: 2, hit: 200));
		Charms["Blanket"] = new Charm("Blanket", new Stats(hp: 10, def: 1));
		Charms["Bow Tie"] = new Charm("Bow Tie", new Stats(def: 4));
		Charms["Bracelet"] = new Charm("Bracelet", new Stats(def: 1));
		Charms["Breadphones"] = new Charm("Breadphones", new Stats(hp: 10, def: 5));
		Charms["Bubble Wrap"] = new Charm("Bubble Wrap", new Stats(def: 3));
		Charms["Bunny Ears"] = new Charm("Bunny Ears", new Stats(def: 3, spd: 12));
		Charms["Cat Ears"] = new Charm("Cat Ears", new Stats(def: 1, spd: 10));
		Charms["Cellphone"] = new Charm("Cellphone", new Stats(def: 10));
		Charms["Cool Glasses"] = new Charm("Cool Glasses", new Stats(atk: 5, def: 5));
		Charms["Cough Mask"] = new Charm("Cough Mask", new Stats(25, 25, 10, 10, 10, 10));
		Charms["Daisy"] = new Charm("Daisy", new Stats(hp: 10), (actor) =>
		{
			actor.SetState("happy", true);
		});
		Charms["Eye Patch"] = new Charm("Eye Patch", new Stats(atk: 7, hit: -25));
		Charms["Faux Tail"] = new Charm("Faux Tail", new Stats(spd: 15));
		Charms["Fedora"] = new Charm("Fedora", new Stats(def: 5, lck: 5));
		Charms["Finger"] = new Charm("Finger", new Stats(atk: 10, def: -5), (actor) =>
		{
			actor.SetState("angry", true);
		});
		Charms["Fox Tail"] = new Charm("Fox Tail", () =>
		{
			return new Stats(spd: 5 + (3 * BattleManager.Instance.Energy));
		});
		Charms["Friendship Bracelet"] = new Charm("Friendship Bracelet", new Stats(10, 10));
		Charms["Nerdy Glasses"] = new Charm("Nerdy Glasses", new Stats(def: 5, hit: 200));
		Charms["Gold Watch"] = new Charm("Gold Watch", new Stats(spd: -10));
		Charms["Hard Hat"] = new Charm("Hard Hat", new Stats(def: 6));
		Charms["Headband"] = new Charm("Headband", new Stats(juice: 20, atk: 10, def: 3, spd: 15));
		Charms["Heart String"] = new Charm("Heart String", new Stats(hp: 30), (actor) =>
		{
			actor.SetState("happy", true);
		});
		Charms["High Heels"] = new Charm("High Heels", new Stats(atk: 10, spd: -10));
		Charms["Homework"] = new Charm("Homework", new Stats(), (actor) =>
		{
			actor.SetState("sad", true);
		});
		Charms["Inner Tube"] = new Charm("Inner Tube", () =>
		{
			return new Stats(def: 2 + BattleManager.Instance.Energy);
		});
		Charms["Magical Bean"] = new Charm("Magical Bean", new Stats(), (actor) =>
		{
			BattleManager.Instance.RandomEmotion(actor);
		});
		Charms["Onion Ring"] = new Charm("Onion Ring", new Stats(20, 20));
		Charms["Paper Bag"] = new Charm("Paper Bag", new Stats(hp: 40, def: 13));
		Charms["Hector"] = new Charm("Hector", new Stats());
		Charms["Pretty Bow"] = new Charm("Pretty Bow", new Stats(hp: 50, atk: 10, def: 3));
		Charms["Punching Bag"] = new Charm("Punching Bag", new Stats(), (actor) =>
		{
			actor.SetState("angry", true);
		});
		Charms["Rabbit Foot"] = new Charm("Rabbit Foot", new Stats(spd: 15, lck: 10));
		Charms["Red Ribbon"] = new Charm("Red Ribbon", () =>
		{
			return new Stats(atk: 1 + (2 * BattleManager.Instance.Energy), def: 5);
		});
		Charms["Deep Poetry Book"] = new Charm("Deep Poetry Book", new Stats(), (actor) =>
		{
			actor.SetState("sad", true);
		});
		Charms["Rubber Duck"] = new Charm("Rubber Duck", new Stats(def: 7));
		Charms["Seer Goggles"] = new Charm("Seer Goggles", new Stats(def: 1, lck: 3, hit: 200));
		Charms["Top Hat"] = new Charm("Top Hat", new Stats(hp: 13, def: 13, lck: 13));
		Charms["Hector Jr."] = new Charm("Hector Jr.", () =>
		{
			int energy = BattleManager.Instance.Energy;
			return new Stats(atk: 1 + energy, def: 1 + energy, spd: 1 + energy, lck: energy);
		});
		Charms["Wedding Ring"] = new Charm("Wedding Ring", new Stats(10, 10, 3, 3, 3, 3), (actor) =>
		{
			actor.SetState("happy", true);
		});
		Charms["Wishbone"] = new Charm("Wishbone", new Stats(lck: 7));
		Charms["Veggie Kid"] = new Charm("Veggie Kid", new Stats(15, 15));
		Charms["Watering Pail"] = new Charm("Watering Pail", new Stats(juice: 10));
		Charms["Sunscreen"] = new Charm("Sunscreen", new Stats(hp: 15));
		Charms["Rake"] = new Charm("Rake", new Stats(atk: 3));
		Charms["Scarf"] = new Charm("Scarf", new Stats(def: 3));
		Charms["Cotton Ball"] = new Charm("Cotton Ball", new Stats(def: 1, spd: 3));
		Charms["Flashlight"] = new Charm("Flashlight", new Stats(def: 4));
		Charms["Universal Remote"] = new Charm("Universal Remote", new Stats(10, 10, 5, 5, 5, 5));
		Charms["TV Remote"] = new Charm("TV Remote", new Stats(hp: 5, def: 2));
		Charms["Flower Crown"] = new Charm("Flower Crown", new Stats(100, 25));
		Charms["Tulip Hairstick"] = new Charm("Tulip Hairstick", new Stats(hp: 50));
		Charms["Gladiolus Hairband"] = new Charm("Gladiolus Hairband", new Stats(atk: 10, lck: 10, hit: 100));
		Charms["Cactus Hairclip"] = new Charm("Cactus Hairclip", new Stats(hp: 15, def: 15));
		Charms["Rose Hairclip"] = new Charm("Rose Hairclip", new Stats(15, 15, 5, 5, 5, 5, 100));
		Charms["Seashell Necklace"] = new Charm("Seashell Necklace", new Stats(hp: 25, juice: 25, def: 5));
		#endregion
	}

	/// <summary>
	/// Adds a snack that provides flat healing
	/// </summary>
	private static void AddSnack(string name, string description, int healing)
	{
		Items[name] = new Item(
			name: name.ToUpper(),
			description: description,
			target: SkillTarget.Ally,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, $"[actor] uses {name.ToUpper()}!");
				AnimationManager.Instance.PlayAnimation(212, target, false);
				int heal = healing;
				if (BattleManager.Instance.GetAllPartyMembers().Any(x => x.Actor.Weapon.Name == "Frying Pan" || x.Actor.Weapon.Name == "Ol' Reliable"))
					heal = (int)Math.Round(heal * 1.5f, MidpointRounding.AwayFromZero);
				target.Heal(heal);
				BattleManager.Instance.SpawnDamageNumber(heal, target.CenterPoint, DamageType.Heal);
				BattleLogManager.Instance.QueueMessage(self, target, $"[target] recovered {heal} HEART!");
				await Task.CompletedTask;
			}
		);
	}

	/// <summary>
	/// Adds a snack that provides flat juice healing
	/// </summary>
	private static void AddJuiceSnack(string name, string description, int juice)
	{
		Items[name] = new Item(
			name: name.ToUpper(),
			description: description,
			target: SkillTarget.Ally,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, $"[actor] uses {name.ToUpper()}!");
				AnimationManager.Instance.PlayAnimation(213, target, false);
				int total = juice;
				if (BattleManager.Instance.GetAllPartyMembers().Any(x => x.Actor.Weapon.Name == "Blender" || x.Actor.Weapon.Name == "Ol' Reliable"))
					total = (int)Math.Round(total * 1.5f, MidpointRounding.AwayFromZero);
				target.HealJuice(total);
				BattleManager.Instance.SpawnDamageNumber(total, target.CenterPoint, DamageType.JuiceGain);
				BattleLogManager.Instance.QueueMessage(self, target, $"[target] recovered {total} JUICE!");
				await Task.CompletedTask;
			}
		);
	}

	/// <summary>
	/// Adds a snack that provides percentage-based juice healing
	/// </summary>
	private static void AddJuiceSnack(string name, string description, float percentage)
	{
		Items[name] = new Item(
			name: name.ToUpper(),
			description: description,
			target: SkillTarget.Ally,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, $"[actor] uses {name.ToUpper()}!");
				AnimationManager.Instance.PlayAnimation(213, target, false);
				float juice = target.CurrentStats.MaxJuice * percentage;
				if (BattleManager.Instance.GetAllPartyMembers().Any(x => x.Actor.Weapon.Name == "Blender" || x.Actor.Weapon.Name == "Ol' Reliable"))
					juice *= 1.5f;
				int finalJuice = (int)Math.Round(juice, MidpointRounding.AwayFromZero);
				target.HealJuice(finalJuice);
				BattleManager.Instance.SpawnDamageNumber(finalJuice, target.CenterPoint, DamageType.JuiceGain);
				BattleLogManager.Instance.QueueMessage(self, target, $"[target] recovered {finalJuice} JUICE!");
				await Task.CompletedTask;
			}
		);
	}

	/// <summary>
	/// Adds a snack that provides percentage-based healing
	/// </summary>
	private static void AddSnack(string name, string description, float percentage)
	{
		Items[name] = new Item(
			name: name.ToUpper(),
			description: description,
			target: SkillTarget.Ally,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, $"[actor] uses {name.ToUpper()}!");
				AnimationManager.Instance.PlayAnimation(212, target, false);
				float heal = target.CurrentStats.MaxHP * percentage;
				if (BattleManager.Instance.GetAllPartyMembers().Any(x => x.Actor.Weapon.Name == "Frying Pan" || x.Actor.Weapon.Name == "Ol' Reliable"))
					heal *= 1.5f;
				int finalHeal = (int)Math.Round(heal, MidpointRounding.AwayFromZero);
				target.Heal(finalHeal);
				BattleManager.Instance.SpawnDamageNumber(finalHeal, target.CenterPoint, DamageType.Heal);
				BattleLogManager.Instance.QueueMessage(self, target, $"[target] recovered {finalHeal} HEART!");
				await Task.CompletedTask;
			}
		);
	}

	/// <summary>
	/// Adds a snack that provides flat healing to all allies
	/// </summary>
	private static void AddGroupSnack(string name, string description, int healing)
	{
		Items[name] = new Item(
		   name: name.ToUpper(),
		   description: description,
		   target: SkillTarget.AllAllies,
		   effect: async (self, target) =>
		   {
			   BattleLogManager.Instance.QueueMessage(self, target, $"[actor] uses {name.ToUpper()}!");
			   int heal = healing;
			   if (BattleManager.Instance.GetAllPartyMembers().Any(x => x.Actor.Weapon.Name == "Frying Pan" || x.Actor.Weapon.Name == "Ol' Reliable"))
				   heal = (int)Math.Round(heal * 1.5f, MidpointRounding.AwayFromZero);
			   foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
			   {
				   AnimationManager.Instance.PlayAnimation(212, member.Actor, false);
				   member.Actor.Heal(heal);
				   BattleManager.Instance.SpawnDamageNumber(heal, member.Actor.CenterPoint, DamageType.Heal);
				   BattleLogManager.Instance.QueueMessage(self, member.Actor, $"[target] recovered {heal} HEART!");
			   }
			   await Task.CompletedTask;
		   }
	   );
	}

	/// <summary>
	/// Adds a snack that provides flat juice healing to all allies
	/// </summary>
	private static void AddGroupJuiceSnack(string name, string description, int juice)
	{
		Items[name] = new Item(
		   name: name.ToUpper(),
		   description: description,
		   target: SkillTarget.AllAllies,
		   effect: async (self, target) =>
		   {
			   BattleLogManager.Instance.QueueMessage(self, target, $"[actor] uses {name.ToUpper()}!");
			   int total = juice;
			   if (BattleManager.Instance.GetAllPartyMembers().Any(x => x.Actor.Weapon.Name == "Blender" || x.Actor.Weapon.Name == "Ol' Reliable"))
				   total = (int)Math.Round(total * 1.5f, MidpointRounding.AwayFromZero);
			   foreach (PartyMemberComponent member in BattleManager.Instance.GetAlivePartyMembers())
			   {
				   AnimationManager.Instance.PlayAnimation(213, member.Actor, false);
				   member.Actor.HealJuice(total);
				   BattleManager.Instance.SpawnDamageNumber(total, member.Actor.CenterPoint, DamageType.JuiceGain);
				   BattleLogManager.Instance.QueueMessage(self, member.Actor, $"[target] recovered {total} JUICE!");
			   }
			   await Task.CompletedTask;
		   }
	   );
	}

	/// <summary>
	/// A snack that provides flat healing and juice
	/// </summary>
	private static void AddComboSnack(string name, string description, int healing, int juice)
	{
		Items[name] = new Item(
			name: name.ToUpper(),
			description: description,
			target: SkillTarget.Ally,
			effect: async (self, target) =>
			{
				BattleLogManager.Instance.QueueMessage(self, target, $"[actor] uses {name.ToUpper()}!");
				AnimationManager.Instance.PlayAnimation(212, target, false);
				int heal = healing;
				int total = juice;
				if (BattleManager.Instance.GetAllPartyMembers().Any(x => x.Actor.Weapon.Name == "Frying Pan" || x.Actor.Weapon.Name == "Ol' Reliable"))
					heal = (int)Math.Round(heal * 1.5f, MidpointRounding.AwayFromZero);
				// donald compiler please come save us donald compiler please save us
				if (BattleManager.Instance.GetAllPartyMembers().Any(x => x.Actor.Weapon.Name == "Blender" || x.Actor.Weapon.Name == "Ol' Reliable"))
					total = (int)Math.Round(total * 1.5f, MidpointRounding.AwayFromZero);
				target.Heal(heal);
				target.HealJuice(total);
				BattleManager.Instance.SpawnDamageNumber(heal, target.CenterPoint, DamageType.Heal);
				BattleLogManager.Instance.QueueMessage(self, target, $"[target] recovered {heal} HEART!");
				BattleLogManager.Instance.QueueMessage(self, target, $"[target] recovered {total} JUICE!");
				await Task.CompletedTask;
			}
		);
	}

	private static void MakeSad(Actor who)
	{
		string state = "sad";
		switch (who.CurrentState)
		{
			case "miserable":
				BattleLogManager.Instance.QueueMessage(null, who, "[target] can't get SADDER!");
				return;
			case "depressed":
				state = "miserable";
				break;
			case "sad":
				state = "depressed";
				break;
		}
		if (who.IsStateValid(state))
			who.SetState(state);
		else
			BattleLogManager.Instance.QueueMessage(null, who, "[target] can't get SADDER!");
	}

	private static void MakeHappy(Actor who)
	{
		string state = "happy";
		switch (who.CurrentState)
		{
			case "manic":
				BattleLogManager.Instance.QueueMessage(null, who, "[target] can't get HAPPIER!");
				return;
			case "ecstatic":
				state = "manic";
				break;
			case "happy":
				state = "ecstatic";
				break;
		}
		if (who.IsStateValid(state))
			who.SetState(state);
		else
			BattleLogManager.Instance.QueueMessage(null, who, "[target] can't get HAPPIER!");
	}

	private static void MakeAngry(Actor who)
	{
		string state = "angry";
		switch (who.CurrentState)
		{
			case "furious":
				BattleLogManager.Instance.QueueMessage(null, who, "[target] can't get ANGRIER!");
				return;
			case "enraged":
				state = "furious";
				break;
			case "angry":
				state = "enraged";
				break;
		}
		if (who.IsStateValid(state))
			who.SetState(state);
		else
			BattleLogManager.Instance.QueueMessage(null, who, "[target] can't get ANGRIER!");
	}
}
