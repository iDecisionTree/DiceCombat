using Godot;

namespace DiceCombat.scripts.dice;

[GlobalClass]
[Tool]
public partial class DiceRerollUI : Control
{
	[Signal] public delegate void RerollClickedEventHandler();

	[Export] public Control ContainerDiceReroll { get; set; }
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
		ContainerDiceReroll ??= GetNodeOrNull<Control>("VBoxContainer") ?? FindDescendantByName<Control>(this, "VBoxContainer");
		ButtonReroll ??= GetNodeOrNull<Button>("VBoxContainer/Button_Reroll") ?? FindDescendantByName<Button>(this, "Button_Reroll") ?? FindFirstDescendant<Button>(this);
		RichTextRerollCount ??= GetNodeOrNull<RichTextLabel>("Panel_Reroll/RichTextLabel") ?? FindDescendantByName<RichTextLabel>(this, "RichTextLabel");
	}

	private static T FindDescendantByName<T>(Node root, string nodeName) where T : Node
	{
		if (root == null)
		{
			return null;
		}

		foreach (Node child in root.GetChildren())
		{
			if (child is T typed && child.Name == nodeName)
			{
				return typed;
			}

			T found = FindDescendantByName<T>(child, nodeName);
			if (found != null)
			{
				return found;
			}
		}

		return null;
	}

	private static T FindFirstDescendant<T>(Node root) where T : Node
	{
		if (root == null)
		{
			return null;
		}

		foreach (Node child in root.GetChildren())
		{
			if (child is T typed)
			{
				return typed;
			}

			T found = FindFirstDescendant<T>(child);
			if (found != null)
			{
				return found;
			}
		}

		return null;
	}
}



