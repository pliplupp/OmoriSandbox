using System.Numerics;
using Godot;
using Vector2 = Godot.Vector2;

namespace OmoriSandbox.Animation;

internal partial class PlayingAnimation : Node2D
{
    public int CurrentFrame { get; private set; } = 0;
    public readonly RPGMAnimatedSprite Animation;
    public Vector2 DrawPosition { get; private set; }

    public PlayingAnimation(RPGMAnimatedSprite animation, Vector2 drawPosition, int layer)
    {
        Animation = animation;
        DrawPosition = drawPosition;
        ZIndex = layer;
        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (Frame frame in Animation.GetFrame(CurrentFrame))
        {
            DrawSetTransform(DrawPosition + new Vector2(frame.X, frame.Y), frame.Rotation, new Vector2(frame.Scale / 100f, frame.Scale / 100f));
            AtlasTexture texture = Animation.GetTextureAt(frame.Pattern);
            if (frame.Mirror)
            {
                Image img = texture.GetImage();
                img.FlipX();
                DrawTexture(ImageTexture.CreateFromImage(img), Vector2.Zero, new Color(1f, 1f, 1f, frame.Opacity / 255f));
                continue;
            }
            
            DrawTexture(texture, Vector2.Zero, new Color(1f, 1f, 1f, frame.Opacity / 255f));
        }
    }

    public bool AdvanceFrame()
    {
        CurrentFrame++;
        if (CurrentFrame >= Animation.FrameCount)
            return true;
        QueueRedraw();
        return false;
    }
}
