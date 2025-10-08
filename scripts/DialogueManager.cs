using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class DialogueManager : Node2D
{
	[Signal] public delegate void FinishedDialogueEventHandler();

	[Export] public Label Text;
	[Export] public Sprite2D SpeakerSprite;
	[Export] public Sprite2D Cursor;

	private Queue<(string, Vector2, string)> MessageQueue = [];
	private string CurrentMessage = "";
	private const float TEXT_SPEED = 0.02f;
	private const int WIDTH = 328;
	private bool WaitingForInput = false;
	private bool IsTyping = false;
	private double CharTimer = 0;
	private int CharIndex = 0;
	private int CharsTillSound = 2;

	public bool DialogueDisabled = false;
	public static DialogueManager Instance { get; private set; }
	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Process(double delta)
	{
		if (IsTyping)
		{
			CharTimer += delta;
			if (CharTimer >= TEXT_SPEED)
			{
				CharTimer = 0;
				TypeChar();
			}
		}
		else if (WaitingForInput)
		{
			if (Input.IsActionJustPressed("Accept"))
			{
				WaitingForInput = false;
				if (CharIndex < CurrentMessage.Length)
				{
					Cursor.Visible = false;
					IsTyping = true;
				}
				else if (MessageQueue.Count == 0)
				{
					CharIndex = 0;
					Visible = false;
					Cursor.Visible = false;
					EmitSignal(SignalName.FinishedDialogue);
				}
				else
				{
					BeginMessage();
				}
			}
		}
	}

	// TODO: Can probably use the `Visible Characters` property of a label instead of doing this manually
	private void TypeChar()
	{
		if (CharIndex >= CurrentMessage.Length)
		{
			IsTyping = false;
			WaitForInput();
			return;
		}

		if (CurrentMessage[CharIndex] == '@')
		{
			CharIndex++;
			IsTyping = false;
			WaitForInput();
			return;
		}

		if (CurrentMessage[CharIndex] == ' ')
		{
			string remaining = CurrentMessage.Substring(CharIndex + 1);
			string nextWord = remaining.Split(' ')[0];
			// only consider the text after the last line break
			string currentLine = Text.Text.Split('\n')[^1];
			string candidate = currentLine + " " + nextWord;
			Vector2 size = Text.GetThemeFont("font").GetStringSize(candidate, fontSize: Text.GetThemeFontSize("font_size"));
			if (size.X > WIDTH)
			{
				Text.Text += "\n";
			}
			else {
				Text.Text += " ";
			}
			CharIndex++;
			PlaySound();
			return;
		}

		Text.Text += CurrentMessage[CharIndex];
		PlaySound();
		CharIndex++;
	}

	private void PlaySound()
	{
		CharsTillSound--;
		if (CharsTillSound == 0)
		{
			AudioManager.Instance.PlaySFX("SYS_text", GameManager.Instance.Random.RandfRange(0.9f, 1.1f), 0.5f);
			CharsTillSound = 2;
		}
	}

	private void BeginMessage()
	{
		CharIndex = 0;
		Cursor.Visible = false;
		var current = MessageQueue.Dequeue();
		if (current.Item1 != null)
		{
			SpeakerSprite.Visible = true;        
			Vector2 local = ToLocal(current.Item2);
			SpeakerSprite.Position = new Vector2(Mathf.Clamp(local.X, -160, 160), SpeakerSprite.Position.Y);
			Text.Text = current.Item1 + ": ";
		}
		else
		{
			SpeakerSprite.Visible = false;
			Text.Text = "";
		}
		CurrentMessage = current.Item3;
		IsTyping = true;
	}

	private void WaitForInput()
	{
		WaitingForInput = true;
		Cursor.Visible = true;
	}

	public Task WaitForDialogue()
	{
		if (DialogueDisabled)
			return Task.CompletedTask;

		TaskCompletionSource tcs = new();

		void Handle()
		{
			FinishedDialogue -= Handle;
			tcs.SetResult();
		}

		FinishedDialogue += Handle;
		return tcs.Task;
	}

	public void QueueMessage(string message)
	{
		QueueMessage(null, Vector2.Zero, message);
	}

	public void QueueMessage(Enemy speaker, string message)
	{
		QueueMessage(speaker.Name, speaker.CenterPoint, message);
	}

	public void QueueMessage(string speaker, Vector2 speakerPos, string message)
	{
		if (DialogueDisabled)
			return;

		MessageQueue.Enqueue((speaker, speakerPos, message));

		if (!IsTyping && !WaitingForInput)
		{
			Visible = true;
			BeginMessage();
		}
	}
}
