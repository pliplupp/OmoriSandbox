using System.Collections.Generic;
using Godot;

namespace OmoriSandbox.Menu;

internal abstract partial class Menu : Sprite2D
{
	[Export] protected CursorBounce CursorSprite;
	protected List<string> Options = [];
	protected List<Vector2I> CursorPositions = [];
	public int CursorIndex { get; protected set; } = 0;
	protected bool Empty = false;
	protected Tween Tween;

	public void OnInput(Vector2I direction)
	{
		// kind of a wacky way to do this but I didn't feel like making an enum when I can just use the struct for directions
		if (direction == Vector2I.Zero)
			OnSelect();
		else
			MoveCursor(direction);
	}

	protected virtual void MoveCursor(Vector2I direction) {}

	protected virtual void UpdateCursor()
	{
		CursorSprite.Position = CursorPositions[CursorIndex];
	}

	public Vector2I GetCursorPosition()
	{
		if (CursorPositions.Count > CursorIndex)
			return CursorPositions[CursorIndex];
		return Vector2I.Zero;
	}

	protected abstract void OnSelect();
	public virtual void OnOpen(SelectionMemory memory) 
	{
		CursorIndex = 0;
		Show();
		UpdateCursor();
	}

	public virtual void MoveUp(bool immediate)
	{
		Tween?.Kill();
		if (immediate)
		{
			Position = new Vector2(Position.X, 437);
		}
		else
		{
			Tween = CreateTween();
			Tween.TweenProperty(this, "position", new Vector2(Position.X, 437), 0.2f).SetTrans(Tween.TransitionType.Sine);
		}
	}

    public virtual void MoveDown(MenuState newState, bool immediate, bool noHide = false)
    {
        Tween?.Kill();
        if (immediate)
        {
            Position = new Vector2(Position.X, 537);
			Visible = noHide;
        }
        else
        {
            Tween = CreateTween();
            Tween.TweenProperty(this, "position", new Vector2(Position.X, 537), 0.2f).SetTrans(Tween.TransitionType.Sine);
			Tween.TweenCallback(Callable.From(() => Visible = noHide));
        }
    }
}
