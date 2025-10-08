using Godot;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

internal partial class ModManager : Node
{
	public static ModManager Instance;
	private const int ACTOR_SIZE = 106;

	[Export]
	private Node ModParent;

	public readonly Dictionary<string, Texture2D> Battlebacks = new();
	public readonly List<ModMetadata> LoadedMods = new();

	public override void _Ready()
	{
		Instance = this;

		if (!DirAccess.DirExistsAbsolute("user://mods"))
		{
			using DirAccess access = DirAccess.Open("user://");
			access.MakeDir("mods");
			GD.Print("Created user://mods directory");
		}
	}

	public void LoadMods()
	{
		GD.Print("Beginning mod loading...");
		int processed = 0;
		Stopwatch sw = Stopwatch.StartNew();

		foreach (string dirName in DirAccess.GetDirectoriesAt("user://mods"))
		{
			if (LoadMod(dirName))
				processed++;
		}

		sw.Stop();
		GD.Print($"Loaded {processed} mods in {sw.ElapsedMilliseconds}ms");
		MainMenuManager.Instance.UpdateModsLoaded(processed);
	}

	private bool LoadMod(string dirName)
	{
		if (!FileAccess.FileExists($"user://mods/{dirName}/mod.json"))
		{
			GD.PushWarning($"Missing mod.json in {dirName}, skipping!");
			return false;
		}
		FileAccess access = FileAccess.Open($"user://mods/{dirName}/mod.json", FileAccess.ModeFlags.Read);

		ModMetadata metadata;
		try
		{
			metadata = JsonConvert.DeserializeObject<ModMetadata>(access.GetAsText());
			if (LoadedMods.Any(x => x.Id == metadata.Id))
			{
				GD.PushWarning($"Mod {metadata.Id} is already loaded, skipping!");
				return false;
			}
		}
		catch
		{
			GD.PushError($"Failed to read mod.json from mod {dirName}");
			return false;
		}

		// TODO: Check for .dll

		foreach (string modDir in DirAccess.GetDirectoriesAt("user://mods/" + dirName))
		{
			bool success = true;
			switch (modDir.ToLower())
			{
				case "actors":
					success = LoadActors(dirName);
					break;
				case "bgm":
					success = LoadBGM(dirName);
					break;
				case "sfx":
					success = LoadSFX(dirName);
					break;
				case "battlebacks":
					success = LoadBattlebacks(dirName);
					break;
				case "enemies":
					success = LoadEnemies(dirName);
					break;
			}
			if (!success)
				return false;
		}

		Texture2D icon = null;
		if (!string.IsNullOrEmpty(metadata.Icon) && metadata.Icon.EndsWith(".png"))
		{
			icon = ImageTexture.CreateFromImage(Image.LoadFromFile($"user://mods/{dirName}/{metadata.Icon}"));
		}

		MainMenuManager.Instance.AddModListEntry(metadata, icon);
		LoadedMods.Add(metadata);
		GD.Print($"Loaded {metadata.Name} ({metadata.Id})");
		return true;
	}

	private bool LoadActors(string root)
	{
		string path = "user://mods/" + root + "/actors";
		foreach (string actor in DirAccess.GetDirectoriesAt(path))
		{
			string[] files = DirAccess.GetFilesAt($"{path}/{actor}");
			string json = files.FirstOrDefault(x => x.EndsWith(".json"));
			if (json == null)
			{
				GD.PushWarning($"Missing json file at {path}/{actor}, skipping!");
				return false;
			}
			FileAccess access = FileAccess.Open($"{path}/{actor}/{json}", FileAccess.ModeFlags.Read);
			if (access == null)
			{
				GD.PushError($"Failed to open file {path}/{actor}/{json}");
				return false;
			}
			try
			{
				JsonActorMod mod = JsonConvert.DeserializeObject<JsonActorMod>(access.GetAsText());
				Database.RegisterModdedPartyMember(mod, BuildAnimation(mod, $"{path}/{actor}"));
			}
			catch
			{
				GD.PushError($"Failed to read actor json for {actor} from mod {root}");
				return false;
			}
		}
		return true;
	}

	private SpriteFrames BuildAnimation(JsonActorMod jsonActor, string path)
	{
		if (!FileAccess.FileExists($"{path}/{jsonActor.Atlas}"))
		{
			GD.PushWarning($"Unable to find atlas at {path}/{jsonActor.Atlas}, skipping!");
			return null;
		}
		SpriteFrames spriteFrames = new();
		Texture2D texture = ImageTexture.CreateFromImage(Image.LoadFromFile($"{path}/{jsonActor.Atlas}"));
		int columns = texture.GetWidth() / ACTOR_SIZE;
		foreach (JsonModAnimationData data in jsonActor.Animation)
		{
			spriteFrames.AddAnimation(data.Emotion);
			spriteFrames.SetAnimationSpeed(data.Emotion, data.Fps);
			spriteFrames.SetAnimationLoop(data.Emotion, true);
			foreach (int idx in data.Frames)
			{
				int column = idx % columns;
				int row = idx / columns;
				AtlasTexture tex = new()
				{
					Atlas = texture,
					Region = new Rect2(column * ACTOR_SIZE, row * ACTOR_SIZE, ACTOR_SIZE, ACTOR_SIZE)
				};
				spriteFrames.AddFrame(data.Emotion, tex);
			}
		}
		return spriteFrames;
	}

	private bool LoadEnemies(string root)
	{
		string path = "user://mods/" + root + "/enemies";
		foreach (string enemy in DirAccess.GetDirectoriesAt(path))
		{
			string[] files = DirAccess.GetFilesAt($"{path}/{enemy}");
			string json = files.FirstOrDefault(x => x.EndsWith(".json"));
			if (json == null)
			{
				GD.PushWarning($"Missing json file at {path}/{enemy}, skipping!");
				return false;
			}
			FileAccess access = FileAccess.Open($"{path}/{enemy}/{json}", FileAccess.ModeFlags.Read);
			if (access == null)
			{
				GD.PushError($"Failed to open file {path}/{enemy}/{json}");
				return false;
			}
			try
			{
				JsonEnemyMod mod = JsonConvert.DeserializeObject<JsonEnemyMod>(access.GetAsText());
				Database.RegisterModdedEnemy(mod, BuildAnimation(mod, $"{path}/{enemy}"));
			}
			catch
			{
				GD.PushError($"Failed to read enemy json for {enemy} from mod {root}");
				return false;
			}
		}
		return true;
	}

	private SpriteFrames BuildAnimation(JsonEnemyMod jsonEnemy, string path)
	{
		if (!FileAccess.FileExists($"{path}/{jsonEnemy.Atlas}"))
		{
			GD.PushWarning($"Unable to find atlas at {path}/{jsonEnemy.Atlas}, skipping!");
			return null;
		}
		SpriteFrames spriteFrames = new();
		Texture2D texture = ImageTexture.CreateFromImage(Image.LoadFromFile($"{path}/{jsonEnemy.Atlas}"));
		int width = jsonEnemy.Width;
		int height = jsonEnemy.Height;
		int columns = texture.GetWidth() / width;
		foreach (JsonModAnimationData data in jsonEnemy.Animation)
		{
			spriteFrames.AddAnimation(data.Emotion);
			spriteFrames.SetAnimationSpeed(data.Emotion, data.Fps);
			spriteFrames.SetAnimationLoop(data.Emotion, true);
			foreach (int idx in data.Frames)
			{
                int column = idx % columns;
                int row = idx / columns;
                AtlasTexture tex = new()
				{
					Atlas = texture,
					Region = new Rect2(column * width, row * height, width, height)
				};
				spriteFrames.AddFrame(data.Emotion, tex);
			}
		}
		return spriteFrames;
	}

	private bool LoadBGM(string root)
	{
		string path = "user://mods/" + root + "/bgm";
		foreach (string file in DirAccess.GetFilesAt(path))
		{
			if (file.EndsWith(".ogg"))
			{
				if (!AudioManager.Instance.LoadCustomBGM($"{path}/{file}"))
				{
					GD.PrintErr("Failed to load custom BGM " + file);
					return false;
				}
			}
		}
		return true;
	}


	private bool LoadSFX(string root)
	{
		string path = "user://mods/" + root + "/sfx";
		foreach (string file in DirAccess.GetFilesAt(path))
		{
			if (file.EndsWith(".ogg"))
			{
				if (!AudioManager.Instance.LoadCustomBGM($"{path}/{file}"))
				{
					GD.PrintErr("Failed to load custom SFX " + file);
					return false;
				}
			}
		}
		return true;
	}

	private bool LoadBattlebacks(string root)
	{
		string path = "user://mods/" + root + "/battlebacks";
		foreach (string file in DirAccess.GetFilesAt(path))
		{
			if (file.EndsWith(".png")) {
				if (!Battlebacks.TryAdd(file.GetBaseName(), ImageTexture.CreateFromImage(Image.LoadFromFile($"{path}/{file}"))))
				{
					GD.PrintErr("Failed to load custom battleback " + file);
					return false;
				}
			}
		}
		return true;
	}
}
