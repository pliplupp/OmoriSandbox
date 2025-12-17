using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using OmoriSandbox.Actors;

namespace OmoriSandbox;

/// <summary>
/// Handles displaying dialogue messages to the player. Mainly used for boss dialogue.
/// </summary>
public partial class DialogueManager : Node2D
{
	[Signal] public delegate void FinishedDialogueEventHandler();
	[Signal] public delegate void ChoiceSelectedEventHandler(bool choice);
	
	[Export] private Label Text;
	[Export] private NinePatchRect Box;
	[Export] private Sprite2D SpeakerSprite;
	[Export] private Sprite2D Cursor;

	[Export] private NinePatchRect ChoiceBox;
	[Export] private Control ChoiceTextParent;

	private Queue<MessageBox> MessageQueue = [];
	private string CurrentMessage = "";
	private bool HasChoice = false;
	
	private const float TEXT_SPEED = 0.02f;
	private const int WIDTH = 328;
	private bool WaitingForInput = false;
	private bool IsTyping = false;
	private bool WaitingForAnimation = false;
	private bool WaitingForChoice = false;
	private double CharTimer = 0;
	private int CharIndex = 0;
	private int CharsTillSound = 2;

	private Vector2I CursorNormalPos = new(145, 35);
	private Vector2I YesPos = new(100, -115);
	private Vector2I NoPos = new(100, -85);
	
    /// <summary>
    /// If dialogue is disabled in the current preset.<br/>
    /// Setting this value should be avoided unless necessary, as it can override preset settings.
    /// </summary>
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
					Cursor.Visible = false;
					SpeakerSprite.Visible = false;
					Text.Visible = false;
					WaitingForAnimation = true;
					AnimateClose();
				}
				else
				{
					BeginMessage();
				}
			}
		}
		else if (WaitingForChoice)
		{
			if (Input.IsActionJustPressed("Accept"))
			{
				AudioManager.Instance.PlaySFX("SYS_select");
				WaitingForChoice = false;
				if (MessageQueue.Count == 0)
				{
					CharIndex = 0;
					Cursor.Visible = false;
					SpeakerSprite.Visible = false;
					Text.Visible = false;
					ChoiceBox.Visible = false;
					ChoiceTextParent.Visible = false;
					ChoiceBox.CustomMinimumSize = new Vector2(110, 20);
					AnimateClose();
				}
			}
			else if (Input.IsActionJustPressed("MenuUp") || Input.IsActionJustPressed("MenuDown"))
			{
				AudioManager.Instance.PlaySFX("SYS_move");
				Cursor.Position = Cursor.Position == YesPos ? NoPos : YesPos;
			}
		}
	}
	
	private void TypeChar()
	{
		if (CharIndex >= CurrentMessage.Length)
		{
			IsTyping = false;
			if (HasChoice)
				WaitForChoice();
			else
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
		WaitingForAnimation = false;
		Text.Visible = true;
		Cursor.Visible = false;
		Cursor.Position = CursorNormalPos;
		MessageBox current = MessageQueue.Dequeue();
		if (current.Speaker != null)
		{
			SpeakerSprite.Visible = true;        
			Vector2 local = ToLocal(current.SpeakerPos);
			SpeakerSprite.Position = new Vector2(Mathf.Clamp(local.X, -160, 160), SpeakerSprite.Position.Y);
			Text.Text = current.Speaker + ": ";
		}
		else
		{
			SpeakerSprite.Visible = false;
			Text.Text = "";
		}
		CurrentMessage = current.Message;
		HasChoice = current.HasChoice;
		IsTyping = true;
	}

	private void WaitForInput()
	{
		WaitingForInput = true;
		Cursor.Visible = true;
	}

	private void WaitForChoice()
	{
		WaitingForChoice = true;
		ChoiceBox.Visible = true;
		AnimateChoiceOpen();
	}

    /// <summary>
    /// Waits for the current dialogue to finish and for the player to dismiss it.
    /// </summary>
	/// <remarks>
	/// If this method is not called after <see cref="QueueMessage"/>, the battle will continue while the dialogue is still on screen.
	/// </remarks>
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

    /// <summary>
    /// Waits for the user to select Yes/No.
    /// </summary>
    /// <remarks>If this method is not called after <see cref="QueueMessage"/>, the battle will continue while the choice is still on screen.<br/>
    /// If dialogue is disabled, this will always return true (yes).</remarks>
    /// <returns>True if the user picks Yes, False if the user picks No.</returns>
	public Task<bool> WaitForUserChoice()
	{
		if (DialogueDisabled)
			return Task.FromResult(true);
		
		TaskCompletionSource<bool> tcs = new();

		void Handle(bool result)
		{
			ChoiceSelected -= Handle;
			tcs.SetResult(result);
		}
		
		ChoiceSelected += Handle;
		return tcs.Task;
	}

	/// <summary>
	/// Queues a message to be displayed in the dialogue box.
	/// </summary>
	/// <param name="message">The message to display. The @ symbol can be used to pause mid-message.</param>
	/// <param name="hasChoice">If true, the message will come with a yes/no choice.</param>
	public void QueueMessage(string message, bool hasChoice = false)
	{
		QueueMessage(null, Vector2.Zero, message, hasChoice);
	}

    /// <summary>
    /// Queues a message to be displayed in the dialogue box, with an <see cref="Enemy"/> name as the speaker.<br/>
	/// The speaker arrow will point to the <see cref="Enemy"/>'s position on screen.
    /// </summary>
    /// <param name="speaker">The <see cref="Enemy"/> to show as the speaker.</param>
    /// <param name="message">The message to display. The @ symbol can be used to pause mid-message.</param>
    /// <param name="hasChoice">If true, the message will come with a yes/no choice.</param>
    public void QueueMessage(Enemy speaker, string message, bool hasChoice = false)
	{
		QueueMessage(speaker.Name, speaker.CenterPoint, message, hasChoice);
	}

    /// <summary>
    /// Queues a message to be displayed in the dialogue box, with a custom speaker name and position.<br/>
    /// </summary>
    /// <param name="speaker">The name of the speaker.</param>
    /// <param name="speakerPos">The position on screen to use as the speaker target.</param>
    /// <param name="message">The message to display. The @ symbol can be used to pause mid-message.</param>
    /// <param name="hasChoice">If true, the message will come with a yes/no choice.</param>
    public void QueueMessage(string speaker, Vector2 speakerPos, string message, bool hasChoice = false)
	{
		if (DialogueDisabled)
			return;

		MessageQueue.Enqueue(new MessageBox(speaker, speakerPos, message, hasChoice));
		
		if (WaitingForAnimation || IsTyping || WaitingForInput) 
			return;
		
		Visible = true;
		WaitingForAnimation = true;
		AnimateOpen();
	}

	private void AnimateOpen()
	{
		Tween tween = CreateTween();
		tween.TweenProperty(Box, "custom_minimum_size:y", 110, 0.1f);
		tween.TweenCallback(Callable.From(BeginMessage));
	}

	private void AnimateChoiceOpen()
	{
		Tween tween = CreateTween();
		tween.TweenProperty(ChoiceBox, "custom_minimum_size:y", 85, 0.1f);
		tween.TweenCallback(Callable.From(() =>
		{
			ChoiceTextParent.Visible = true;
			Cursor.Visible = true;
			Cursor.Position = YesPos;
		}));
	}

	private void AnimateClose()
	{
		Tween tween = CreateTween();
		tween.TweenProperty(Box, "custom_minimum_size:y", 20, 0.1f);
		tween.TweenCallback(Callable.From(FinishMessage));
	}
	
	private void FinishMessage()
	{
		Visible = false;
		WaitingForAnimation = false;
		EmitSignal(SignalName.FinishedDialogue);
		if (HasChoice)
			EmitSignal(SignalName.ChoiceSelected, Cursor.Position == YesPos);
	}

	private record MessageBox(string Speaker, Vector2 SpeakerPos, string Message, bool HasChoice);
}
