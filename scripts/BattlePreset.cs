using System.Collections.Generic;
using Godot;

public struct BattlePreset
{
    public string Name;
    public string Battleback;
    public string BGM;
    public int FollowupTier;
    public bool BasilFollowups;
    public bool BasilReleaseEnergy;
    public List<(string ItemName, int Quantity)> StartingItems;
    public List<BattlePresetActor> Actors;
    public List<BattlePresetEnemy> Enemies;

    public BattlePreset(string name, string battleback, string bgm, int followupTier, bool basilFollowups, bool basilReleaseEnergy, List<(string ItemName, int Quantity)> startingItems, List<BattlePresetActor> actors, List<BattlePresetEnemy> enemies)
    {
        Name = name;
        Battleback = battleback;
        BGM = bgm;
        FollowupTier = followupTier;
        BasilFollowups = basilFollowups;
        BasilReleaseEnergy = basilReleaseEnergy;
        StartingItems = startingItems;
        Actors = actors;
        Enemies = enemies;
    }
}



public struct BattlePresetActor
{
    public string Name;
    public int Level;
    public string Weapon;
    public string Charm;
    public string Emotion;
    public List<string> Skills;
    
    public BattlePresetActor(string name, int level, string weapon, string charm, string emotion, List<string> skills)
    {
        Name = name;
        Level = level;
        Weapon = weapon;
        Charm = charm;
        Emotion = emotion;
        Skills = skills;
    }
}

public struct BattlePresetEnemy
{
    public string Name;
    public Vector2 Position;
    public string Emotion;
    public BattlePresetEnemy(string name, Vector2 position, string emotion)
    {
        Name = name;
        Position = position;
        Emotion = emotion;
    }
}