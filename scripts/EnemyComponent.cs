using Godot;

public partial class EnemyComponent : Node
{
    private Enemy Enemy;
    private Control AboveHead;
    private TextureProgressBar HPBar;
    private Label NameLabel;
    private NinePatchRect NameRect;

    public Enemy Actor => Enemy;

    public void SetEnemy(Enemy enemy, string initialState, bool fallsOffScreen)
    {
        Enemy = enemy;
        AnimatedSprite2D sprite = GetNode<AnimatedSprite2D>("../Sprite");
        Enemy.Init(sprite, initialState, fallsOffScreen);
        AboveHead = GetNode<Control>("../AboveHead");
        NameRect = GetNode<NinePatchRect>("../AboveHead/Infobox");
        NameLabel = GetNode<Label>("../AboveHead/Infobox/Name");
        HPBar = GetNode<TextureProgressBar>("../AboveHead/Infobox/Health");
        HPBar.MaxValue = Enemy.BaseStats.HP;
        HPBar.Value = Enemy.CurrentHP;
        NameLabel.Text = Enemy.Name;
        float width = Mathf.Max(160f, NameLabel.GetMinimumSize().X + 15);
        NameRect.Size = new Vector2(width, NameRect.Size.Y);
        NameRect.Position = new Vector2(-width / 2f, NameRect.Position.Y);
        AboveHead.Visible = false;

        Enemy.CenterPoint = GetParent<Node2D>().GlobalPosition;
    }

    public override void _Process(double delta)
    {
        HPBar.Value = Enemy.CurrentHP;
    }

    public void ShowInfoBox(bool show)
    {
        AboveHead.Visible = show;
    }

    public void Despawn()
    {
        GetParent().QueueFree();
    }
}