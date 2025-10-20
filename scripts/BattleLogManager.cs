using Godot;
using OmoriSandbox.Actors;
using System.Collections.Generic;

namespace OmoriSandbox;

/// <summary>
/// Handles displaying messages in the battle log during battles.
/// </summary>
public partial class BattleLogManager : Control
{
	/// <summary>
	/// Fired whenever the battle log has finished logging messages.
	/// </summary>
	[Signal] public delegate void FinishedLoggingEventHandler();

	[Export] private PackedScene LogLine;
	[Export] private Label ImmediateLabel;
	[Export] private Font Font;

	private readonly List<string> MessageQueue = [];
	private readonly List<Control> ActiveLines = [];
	private const int HEIGHT = 26;
	/// <summary>
	/// Returns true when the battle log is busy with messages.
	/// </summary>
	public bool IsProcessingMessage { get; private set; } = false;
	public static BattleLogManager Instance { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;
	}

    /// <summary>
    /// Queues a message to be displayed in the battle log that shows the names of actors.
    /// </summary>
	/// <remarks>
	/// You can use [actor] and [target] in the message string to replace them with <paramref name="self"/> and <paramref name="target"/> respectively.
	/// </remarks>
    /// <param name="self">The actor to replace [actor] with in the <paramref name="message"/>.</param>
    /// <param name="target">The actor to replace [target] with in the <paramref name="message"/>.</param>
    /// <param name="message">The message to display. Occurences of the \n character will split the message up into different logs.</param>
    public void QueueMessage(Actor self, Actor target, string message)
	{
		QueueMessage(ParseMessage(self, target, message));
	}

    /// <summary>
    /// Queues a message to be displayed in the battle log.
    /// </summary>
    /// <param name="message">The message to display. Occurences of the \n character will split the message up into different logs.</param>
    public void QueueMessage(string message)
	{
		MessageQueue.Add(message);

		if (!IsProcessingMessage)
			ProcessMessage();
	}

    /// <summary>
    /// Immediately shows a message in the battle log, bypassing the queue.<br/>
	/// Will automatically resize the font to fit the message in the box.
    /// </summary>
    /// <param name="message">The message to display.</param>
    public void ShowMessage(string message)
	{
		ImmediateLabel.Text = message;
		int fontSize = 24;
		while (Font.GetMultilineStringSize(ImmediateLabel.Text, ImmediateLabel.HorizontalAlignment, -1, fontSize).X > ImmediateLabel.Size.X)
		{
			fontSize--;
		}
		ImmediateLabel.AddThemeFontSizeOverride("font_size", fontSize);
	}

    /// <summary>
    /// Immediately shows a message in the battle log, clearing any queued or active messages first.
    /// </summary>
    /// <param name="message">The message to display.</param>
    public void ClearAndShowMessage(string message)
	{
		ClearBattleLog();
		ShowMessage(message);
	}

    /// <summary>
    /// Immediately shows a message in the battle log, clearing any queued or active messages first.
    /// </summary>
    /// <remarks>
	/// You can use [actor] and [target] in the message string to replace them with <paramref name="self"/> and <paramref name="target"/> respectively.
	/// </remarks>
    /// <param name="self">The actor representing the sender of the message.</param>
    /// <param name="target">The actor representing the recipient of the message.</param>
    /// <param name="message">The raw message to be formatted and displayed.</param>
    public void ClearAndShowMessage(Actor self, Actor target, string message)
	{
		ClearAndShowMessage(ParseMessage(self, target, message));
	}

    /// <summary>
    /// Clears the battle log of all queued and active messages.
    /// </summary>
	/// <remarks>
	/// See <see cref="ClearAndShowMessage(string)"/> for a shorthand to clear the log and show a message.
	/// </remarks>
    public void ClearBattleLog()
	{
		MessageQueue.Clear();
		ActiveLines.ForEach(x => x.QueueFree());
		ActiveLines.Clear();
		ImmediateLabel.Text = "";
		IsProcessingMessage = false;
	}

	private string ParseMessage(Actor self, Actor target, string message)
	{
		return message.Replace("[actor]", self == null ? "" : self.Name.ToUpper()).Replace("[target]", target == null ? "" : target.Name.ToUpper());
	}

	private async void ProcessMessage()
	{
		if (MessageQueue.Count == 0)
		{
			IsProcessingMessage = false;
			EmitSignal(SignalName.FinishedLogging);
			return;
		}

		IsProcessingMessage = true;
		string next = MessageQueue[0];

		// if text overruns the border of the log box
		if (next.Length > 35)
		{
			int lastSpace = next.LastIndexOf(' ');
			// if there's no spaces in the message I guess we're fucked
			if (lastSpace > -1)
			{
				char[] arr = next.ToCharArray();
				arr[lastSpace] = '\n';
				next = new string(arr);
			}
		}

		string[] lines = next.Split('\n');

		while (ActiveLines.Count >= 3)
		{
			MoveOffScreen(ActiveLines[0]);
			ActiveLines.RemoveAt(0);
		}

		for (int i = 1; i < lines.Length; i++)
		{
			// fixes a bug where some text like "cannot go any lower" would get split across messages if multiple messages are queued at once
			if (MessageQueue.Count >= 2)
				MessageQueue.Insert(i, lines[i]);
			else
				MessageQueue.Add(lines[i]);
        }
		MessageQueue.RemoveAt(0);

		Control newLine = LogLine.Instantiate<Control>();
		newLine.GetNode<Label>("Label").Text = lines[0];
		newLine.Position = new Vector2(11, ActiveLines.Count * HEIGHT);
		newLine.Modulate = new Color(1f, 1f, 1f, 0f);
		AddChild(newLine);
		ActiveLines.Add(newLine);

		Tween tween = GetTree().CreateTween();
		tween.TweenProperty(newLine, "modulate:a", 1f, 0.15f);

		for (int i = 0; i < ActiveLines.Count; i++)
		{
			Control line = ActiveLines[i];
			Vector2 target = new(11, i * HEIGHT);
			Tween repositionTween = GetTree().CreateTween();
			repositionTween.TweenProperty(line, "position", target, 0.15f)
						   .SetTrans(Tween.TransitionType.Sine);
		}

		await ToSignal(GetTree().CreateTimer(0.4f), "timeout");

		ProcessMessage();
	}

	private void MoveOffScreen(Control line)
	{
		Tween tween = GetTree().CreateTween();
		tween.TweenProperty(line, "position", new Vector2(11, -HEIGHT), 0.15f)
			.SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(line, "modulate:a", 0f, 0.15f)
			.SetTrans(Tween.TransitionType.Sine);
		tween.TweenCallback(Callable.From(() => line.QueueFree()));
	}

}
