using Godot;
using OmoriSandbox.Editor;

namespace OmoriSandbox.Menu;
internal partial class PartyMenu : Menu
{
	public override void _Ready()
	{
		Options = ["Fight", "Run"];
		CursorPositions = [new Vector2I(-125, -20), new Vector2I(-125, 20)];
	}

	protected override void MoveCursor(Vector2I direction)
	{
		CursorIndex = (CursorIndex + direction.Y + Options.Count) % Options.Count;
		UpdateCursor();
		AudioManager.Instance.PlaySFX("SYS_move");
	}

	protected override void OnSelect()
	{
		if (CursorIndex == 0)
		{
			BattleManager.Instance.OnFightSelected();
			AudioManager.Instance.PlaySFX("SYS_select");        
		}
		else
		{
			BattleManager.Instance.Reset();
			MainMenuManager.Instance.ReturnToTitle();
			AudioManager.Instance.PlaySFX("SYS_select");
		}
	}

    public override void MoveUp(bool immediate)
    {
        Tween?.Kill();
        if (immediate)
        {
            Position = new Vector2(Position.X, 429);
        }
        else
        {
            Tween = CreateTween();
            Tween.TweenProperty(this, "position", new Vector2(Position.X, 429), 0.2f).SetTrans(Tween.TransitionType.Sine);
        }
    }

    public override void MoveDown(bool immediate)
    {
        Tween?.Kill();
        if (immediate)
        {
            Position = new Vector2(Position.X, 529);
        }
        else
        {
            Tween = CreateTween();
            Tween.TweenProperty(this, "position", new Vector2(Position.X, 529), 0.2f).SetTrans(Tween.TransitionType.Sine);
        }
    }
}
