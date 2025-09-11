using Discord;
using Godot;
using System;

public class DiscordManager
{
    private Discord.Discord DiscordSDK;
    private Activity Activity;
    public DiscordManager()
    {
        DiscordSDK = new Discord.Discord(1410108043525488812, (ulong)CreateFlags.NoRequireDiscord);
        Activity = new Activity()
        {
            Details = "On the Main Menu",
            Timestamps =
            {
                Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            },
            Assets =
            {
                LargeImage = "icon",
                LargeText = "OmoriSandbox v0.6.0"
            },
            Instance = false
        };

        DiscordSDK.GetActivityManager().UpdateActivity(Activity, (result) =>
        {
            if (result == Result.Ok)
            {
                GD.Print("Initialized Discord Activity");
            }
            else
            {
                GD.PushWarning("Failed to init Discord Activity, got result: " + result);
            }
        });
    }

    public void Tick()
    {
        DiscordSDK.RunCallbacks();
    }

    public void SetMainMenu()
    {
        Activity.Details = "On the Main Menu";
        DiscordSDK.GetActivityManager().UpdateActivity(Activity, (_) => { });
    }

    public void SetEditingPreset()
    {
        Activity.Details = "Editing a Preset";
        DiscordSDK.GetActivityManager().UpdateActivity(Activity, (_) => { });
    }

    public void SetBattling(int enemies)
    {
        Activity.Details = $"Battling {enemies} Enemies";
        DiscordSDK.GetActivityManager().UpdateActivity(Activity, (_) => { });
    }

    public void Shutdown()
    {
        DiscordSDK.Dispose();
    }
}