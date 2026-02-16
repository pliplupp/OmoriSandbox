using Godot;
using OmoriSandbox.Actors;

namespace OmoriSandbox;

public partial class EnemyInfoBox : Control
{
	internal protected Enemy Enemy;

	[Export] private NinePatchRect Infobox;
	[Export] private Label NameLabel;
	[Export] private TextureProgressBar HPBar;
	
	internal virtual void SetEnemy(Enemy enemy)
	{
		Enemy = enemy;
		HPBar.MaxValue = Enemy.BaseStats.HP;
		HPBar.Value = Enemy.CurrentHP;
		NameLabel.Text = Enemy.Name;
		float width = Mathf.Max(Infobox.CustomMinimumSize.X, NameLabel.GetMinimumSize().X + 15);
		Infobox.Size = new Vector2(width, Infobox.Size.Y);
		Infobox.Position = new Vector2(-width / 2f, Infobox.Position.Y);
	}

	internal virtual void Show(bool show)
	{
		HPBar.Value = Enemy.CurrentHP;
		Visible = show;
	}
}
