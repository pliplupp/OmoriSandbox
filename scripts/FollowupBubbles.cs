using Godot;

namespace OmoriSandbox;

internal partial class FollowupBubbles : Node2D
{
    [Export] private FollowupDirection[] Directions;

    public void ShowBubbles()
    {
        foreach (FollowupDirection direction in Directions)
            direction.ShowBubble();
    }

    public void HideBubbles()
    {
        foreach (FollowupDirection direction in Directions)
            direction.HideBubble();
    }
}