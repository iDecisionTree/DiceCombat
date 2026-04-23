using Godot;
using DiceCombat.scripts.rich_text_3d;

#pragma warning disable IDE1006
namespace DiceCombat.scripts.dice;

[GlobalClass]
[Tool]
public partial class DiceSelectUI : Control
{
	[Signal] public delegate void ConfirmClickedEventHandler();

	[Export] public Control ContainerDiceSelect { get; set; }
	[Export] public Button ButtonConfirm { get; set; }
	[Export] public RichTextLabel RichTextInfo { get; set; }
	[Export] public MeshInstance3D RichTextDiceSelect { get; set; }
	
	private bool _confirmPressed;
	private int _selectedCount;
	private int _requiredCount;
	private int _selectedTotalPoints;
	private RichText3DWrapper _richTextDiceSelect;

	public override void _Ready()
	{
		if (ButtonConfirm != null)
		{
			ButtonConfirm.Pressed += OnConfirmPressed;
		}

		if (RichTextDiceSelect != null)
		{
			_richTextDiceSelect = new RichText3DWrapper(RichTextDiceSelect);
		}

		ResetConfirmState();
		RefreshSelectionText();
		SetSelectionDisplayVisible(false);
	}

	private void OnConfirmPressed()
	{
		if (_confirmPressed)
		{
			return;
		}

		_confirmPressed = true;
		EmitSignal(SignalName.ConfirmClicked);
	}

	public void Open()
	{
		Visible = true;
		SetSelectionDisplayVisible(true);
		ResetConfirmState();
		RefreshConfirmState();
	}

	public void Close()
	{
		ResetConfirmState();
		SetConfirmEnabled(false);
		SetSelectionDisplayVisible(false);
		Visible = false;
	}

	public void SetConfirmVisible(bool visible)
	{
		if (ContainerDiceSelect != null)
		{
			ContainerDiceSelect.Visible = visible;
		}
		else
		{
			Visible = visible;
		}

		SetSelectionDisplayVisible(visible);

		if (visible)
		{
			ResetConfirmState();
			RefreshConfirmState();
		}
	}

	public void SetConfirmEnabled(bool enabled)
	{
		if (ButtonConfirm != null)
		{
			ButtonConfirm.Disabled = !enabled;
		}
	}

	public void SetInfoText(string text)
	{
		if (RichTextInfo != null)
		{
			RichTextInfo.Text = $"[b]{text}[/b]";
		}
	}

	public void SetSelection3DText(string text)
	{

		if (_richTextDiceSelect != null)
		{
			_richTextDiceSelect.Text = text;
		}
	}

	public void SetSelection3DVisible(bool visible)
	{
		SetSelectionDisplayVisible(visible);
	}

	public void SetSelectionProgress(int selectedCount, int requiredCount, int selectedTotalPoints)
	{
		_selectedCount = Mathf.Max(selectedCount, 0);
		_requiredCount = Mathf.Max(requiredCount, 0);
		_selectedTotalPoints = Mathf.Max(selectedTotalPoints, 0);
		RefreshSelectionText();
		RefreshConfirmState();
	}

	public void ResetSelectionProgress()
	{
		_selectedCount = 0;
		_requiredCount = 0;
		_selectedTotalPoints = 0;
		RefreshSelectionText();
		RefreshConfirmState();
	}

	public void ResetConfirmState()
	{
		_confirmPressed = false;
	}

	private void RefreshConfirmState()
	{
		SetConfirmEnabled(_selectedCount == _requiredCount);
	}

	private void RefreshSelectionText()
	{
		if (RichTextInfo != null)
		{
			RichTextInfo.Text = $"[b]{_selectedCount}/{_requiredCount}[/b]";
		}

		SetSelection3DText(_selectedTotalPoints.ToString());
	}

	private void SetSelectionDisplayVisible(bool visible)
	{
		if (RichTextDiceSelect != null)
		{
			RichTextDiceSelect.Visible = visible;
		}
	}
}
#pragma warning restore IDE1006
