using Godot;

namespace OmoriSandbox.Modding;

public class SpriteFramesBuilder
{
    private SpriteFrames spriteFrames;
    private Texture2D texture;
    private int Width;
    private int Height;
    private int Columns;
    public SpriteFramesBuilder(string atlasPath, int width, int height)
    {
        if (!FileAccess.FileExists("user://mods/" + atlasPath))
        {
            GD.PushError("Failed to find atlas at path: user://mods/" + atlasPath);
            return;
        }
        spriteFrames = new();
        texture = ImageTexture.CreateFromImage(Image.LoadFromFile("user://mods/" + atlasPath));
        Width = width;
        Height = height;
        Columns = texture.GetWidth() / Width;
    }

    public SpriteFramesBuilder AddEmotion(string emotion, int fps, params int[] indices)
    {
        if (spriteFrames.HasAnimation(emotion))
        {
            GD.PushWarning($"SpriteFrames already has an animation named {emotion}, skipping!");
            return this;
        }
        spriteFrames.AddAnimation(emotion);
        spriteFrames.SetAnimationSpeed(emotion, fps);
        spriteFrames.SetAnimationLoop(emotion, true);
        foreach (int index in indices)
        {
            int column = index % Columns;
            int row = index / Columns;
            AtlasTexture tex = new()
            {
                Atlas = texture,
                Region = new Rect2(column * Width, row * Height, Width, Height)
            };
            spriteFrames.AddFrame(emotion, tex);
        }

        return this;
    }

    public SpriteFrames Build()
    {
        return spriteFrames;
    }
}