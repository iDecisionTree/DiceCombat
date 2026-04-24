using Godot;

namespace DiceCombat.scripts.dice;

[GlobalClass]
[Tool]
public partial class DiceRerollUI : Control
{
	[Signal] public delegate void RerollClickedEventHandler();

	[Export] public Button ButtonReroll { get; set; }
	[Export] public RichTextLabel RichTextRerollCount { get; set; }

	public override void _Ready()
	{
		CacheNodes();

		if (ButtonReroll != null)
		{
			ButtonReroll.Pressed += OnRerollPressed;
		}
	}

	public override void _ExitTree()
	{
		if (ButtonReroll != null)
		{
			ButtonReroll.Pressed -= OnRerollPressed;
		}
	}

	public void Open()
	{
		SetPanelVisible(true);
	}

	public void Close()
	{
		SetPanelVisible(false);
		SetEnabled(false);
	}

	public void SetPanelVisible(bool visible)
	{
		CacheNodes();
		Visible = visible;
	}

	public void SetEnabled(bool enabled)
	{
		CacheNodes();

		if (ButtonReroll != null)
		{
			ButtonReroll.Disabled = !enabled;
		}
	}

	public void SetRerollCount(int remainingRerolls)
	{
		CacheNodes();

		if (RichTextRerollCount != null)
		{
			RichTextRerollCount.Text = $"[b]{Mathf.Max(remainingRerolls, 0)}[/b]";
		}
	}

	private void OnRerollPressed()
	{
		EmitSignal(SignalName.RerollClicked);
	}

	private void CacheNodes()
	{
		ButtonReroll ??= GetNodeOrNull<Button>("VBoxContainer/Button_Reroll") ?? NodeSearch.FindDescendantByName<Button>(this, "Button_Reroll") ?? NodeSearch.FindFirstDescendant<Button>(this);
		RichTextRerollCount ??= GetNodeOrNull<RichTextLabel>("Panel_Reroll/RichTextLabel") ?? NodeSearch.FindDescendantByName<RichTextLabel>(this, "RichTextLabel");
	}
}



