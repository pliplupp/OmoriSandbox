using Godot;
using System.Collections.Generic;

public partial class DialogueManager : Node2D
{
    [Signal] public delegate void FinishedDialogueEventHandler();

    [Export] public Label Text;
    [Export] public Sprite2D SpeakerSprite;
    [Export] public Sprite2D Cursor;

    private Queue<(Enemy, string)> MessageQueue = [];
    private string CurrentMessage = "";
    private const float TEXT_SPEED = 0.02f;
    private const int WIDTH = 328;
    private bool WaitingForInput = false;
    private bool IsTyping = false;
    private double CharTimer = 0;
    private int CharIndex = 0;
    private int CharsTillSound = 2;
    public bool IsProcessingMessage => IsTyping || WaitingForInput;
    public static DialogueManager Instance { get; private set; }
    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("TestAnim"))
        {
            List<Enemy> speakers = BattleManager.Instance.GetAllEnemies();
            QueueMessage(speakers[1], "I'm currently speaking right now, as you can see by the arrow.");
            QueueMessage(speakers[2], "I don't really have much to say...@ it's just nice to be able to speak finally.");
            QueueMessage(speakers[0], "Anyway, back to the battle.");
            QueueMessage("The enemies are finished talking...");
        }
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
            Vector2 local = ToLocal(current.Item1.CenterPoint);
            SpeakerSprite.Position = new Vector2(Mathf.Clamp(local.X, -160, 160), SpeakerSprite.Position.Y);
            Text.Text = current.Item1.Name + ": ";
        }
        else
        {
            SpeakerSprite.Visible = false;
            Text.Text = "";
        }
        CurrentMessage = current.Item2;
        IsTyping = true;
    }

    private void WaitForInput()
    {
        WaitingForInput = true;
        Cursor.Visible = true;
    }

    public void QueueMessage(string message)
    {
        QueueMessage(null, message);
    }

    public void QueueMessage(Enemy speaker, string message)
    {
        MessageQueue.Enqueue((speaker, message));

        if (!IsTyping && !WaitingForInput)
        {
            Visible = true;
            BeginMessage();
        }
    }
}