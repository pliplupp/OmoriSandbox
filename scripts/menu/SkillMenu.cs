using System.Collections.Generic;
using System.Linq;
using Godot;
using OmoriSandbox.Actors;
using OmoriSandbox.Battle;

namespace OmoriSandbox.Menu;

internal partial class SkillMenu : Menu
{
	[Export] public Label[] SkillLabels;
	[Export] public Label CostText;
	private readonly List<Skill> Skills = [];
	private List<Vector2I> Positions = [new(-145, 5), new(25, 5), new(-145, 25), new(25, 25)];

	private Vector2I GridSize = new(2, 2);
	private Actor Actor;

	public void Populate(Actor actor)
	{
		Skills.Clear();
		Actor = actor;
        CostText.Text = "0";
        foreach (Label l in SkillLabels)
			l.Text = "";
		int idx = 0;
        foreach (Skill s in actor.Skills.Values.Where(x => !x.Hidden))
		{
			if (idx > 3)
				break;
			SkillLabels[idx].Text = s.Name;
			if (actor.CurrentJuice < s.Cost(actor) || !s.MeetsRequirements(actor))
				SkillLabels[idx].AddThemeColorOverride("font_color", Colors.DimGray);
			else
				SkillLabels[idx].RemoveThemeColorOverride("font_color");
			Skills.Add(s);
			idx++;
		}
        if (SkillLabels.All(x => x.Text == ""))
        {
			CursorPositions = Positions.GetRange(0, 1);
            Empty = true;
            return;
        }
		Empty = false;
		CursorPositions = Positions.GetRange(0, Skills.Count);
	}
	
	private void ShowSkillInfo()
	{
        if (Empty) return;
        Skill s = Skills[CursorIndex];
		CostText.Text = s.Cost(Actor).ToString();
		BattleLogManager.Instance.ClearAndShowMessage($"{s.Name}\n{s.Description.Replace("[actor]", Actor.Name.ToUpper()).Replace("[first]", BattleManager.Instance.GetPartyMember(0).Name.ToUpper())}");
	}

	protected override void MoveCursor(Vector2I direction)
	{
        if (Empty) return;
        int old = CursorIndex;
        int x = CursorIndex % 2;
		int y = CursorIndex / 2;
		x = (x + direction.X + GridSize.X) % GridSize.X;
		y = (y + direction.Y + GridSize.Y) % GridSize.Y;
		int newIndex = y * GridSize.X + x;
		newIndex = Mathf.Min(newIndex, Skills.Count - 1);
		CursorIndex = newIndex;
		if (CursorIndex != old)
		{
			UpdateCursor();
			ShowSkillInfo();
			AudioManager.Instance.PlaySFX("SYS_move");
		}
	}

	protected override void OnSelect()
	{
        if (Empty) return;
        Skill selected = Skills[CursorIndex];
		if (BattleManager.Instance.OnSelectSkill(selected))
			CursorSprite.StopBounce();
	}

	public override void OnOpen(SelectionMemory memory)
	{
		if (memory.SavedState == MenuState.Skill)
		{
			CursorIndex = memory.SavedIndex;
			Show();
			UpdateCursor();
		}
		else
			base.OnOpen(memory);
		ShowSkillInfo();
		CursorSprite.StartBounce();
    }
}
