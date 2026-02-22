using Godot;
using OmoriSandbox.Animation;

namespace OmoriSandbox.Editor;

internal partial class AnimationViewer : Control
{
    [Export] private SpinBox AnimationIdSelector;
    [Export] private Button PlayButton;
    [Export] private Node PreviewRoot;

    private PlayingAnimation Animation;

    public override void _Ready()
    {
        PlayButton.Pressed += async () =>
        {
            Animation = AnimationManager.Instance.PreviewAnimation((int)AnimationIdSelector.Value);
            if (Animation != null)
            {
                // we need to play the animation on this canvas instead of the battle one
                PreviewRoot.AddChild(Animation);
                PlayButton.Disabled = true;
            }
        };

        AnimationManager.Instance.AnimationFinished += () =>
        {
            PlayButton.Disabled = false;
            Animation = null;
        };
    }
}