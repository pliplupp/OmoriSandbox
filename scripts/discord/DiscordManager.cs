using Godot;
using System;

namespace Discord;

internal class DiscordManager
{
    private Discord DiscordSDK;
    private Activity Activity;
    private readonly bool DiscordDisabled = false;
    public DiscordManager()
    {
        try
        {
            DiscordSDK = new Discord(1410108043525488812, (ulong)CreateFlags.NoRequireDiscord);
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
                    LargeText = "OmoriSandbox v0.7.0"
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
        catch
        {
            GD.PushWarning("Failed to initialize Discord SDK, disabling Discord integration!");
            DiscordDisabled = true;
        }
    }

    public void Tick()
    {
        if (DiscordDisabled) return;
        DiscordSDK.RunCallbacks();
    }

    public void SetMainMenu()
    {
        if (DiscordDisabled) return;
        Activity.Details = "On the Main Menu";
        DiscordSDK.GetActivityManager().UpdateActivity(Activity, (_) => { });
    }

    public void SetEditingPreset()
    {
        if (DiscordDisabled) return;
        Activity.Details = "Editing a Preset";
        DiscordSDK.GetActivityManager().UpdateActivity(Activity, (_) => { });
    }

    public void SetBattling(int enemies)
    {
        if (DiscordDisabled) return;
        Activity.Details = $"Battling {enemies} Enemies";
        DiscordSDK.GetActivityManager().UpdateActivity(Activity, (_) => { });
    }

    public void Shutdown()
    {
        if (DiscordDisabled) return;
        DiscordSDK.Dispose();
    }
}