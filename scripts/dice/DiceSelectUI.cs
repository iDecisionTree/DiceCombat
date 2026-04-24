using Godot;
using DiceCombat.scripts;
using DiceCombat.scripts.rich_text_3d;

#pragma warning disable IDE1006
namespace DiceCombat.scripts.dice;

[GlobalClass]
[Tool]
public partial class DiceSelectUI : Control
{
	[Signal] public delegate void ConfirmClickedEventHandler();

	[Export] public Button ButtonConfirm { get; set; }
	[Export] public RichTextLabel RichTextInfo { get; set; }
	[Export] public MeshInstance3D RichTextDiceSelect { get; set; }

	private Control _containerDiceSelect;
	private bool _confirmPressed;
	private int _selectedCount;
	private int _requiredCount;
	private int _selectedTotalPoints;
	private int _selectionPreviewBonus;
	private RichText3DWrapper _richTextDiceSelect;

	public override void _Ready()
	{
		CacheNodes();

		if (ButtonConfirm != null)
		{
			ButtonConfirm.Pressed += OnConfirmPressed;
		}

		if (_richTextDiceSelect == null && RichTextDiceSelect != null)
		{
			_richTextDiceSelect = new RichText3DWrapper(RichTextDiceSelect);
		}

		ResetConfirmState();
		RefreshSelectionText();
		SetSelectionDisplayVisible(false);
	}

	public override void _ExitTree()
	{
		if (ButtonConfirm != null)
		{
			ButtonConfirm.Pressed -= OnConfirmPressed;
		}
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
		SetConfirmVisible(true);
		ResetConfirmState();
		RefreshConfirmState();
	}

	public void Close()
	{
		SetConfirmVisible(false);
		ResetConfirmState();
		SetConfirmEnabled(false);
	}

	public void SetConfirmVisible(bool visible)
	{
		CacheNodes();

		if (_containerDiceSelect != null)
		{
			_containerDiceSelect.Visible = visible;
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
		CacheNodes();

		if (ButtonConfirm != null)
		{
			ButtonConfirm.Disabled = !enabled;
		}
	}

	public void SetInfoText(string text)
	{
		CacheNodes();

		if (RichTextInfo != null)
		{
			RichTextInfo.Text = $"[b]{text}[/b]";
		}
	}

	public void SetSelection3DText(string text)
	{
		CacheNodes();

		if (_richTextDiceSelect != null)
		{
			_richTextDiceSelect.Text = text;
		}
	}

	public void SetSelection3DVisible(bool visible)
	{
		CacheNodes();
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

	public void SetSelectionDamagePreview(int previewBonus)
	{
		_selectionPreviewBonus = previewBonus;
		RefreshSelectionText();
	}

	public void ResetSelectionProgress()
	{
		_selectedCount = 0;
		_requiredCount = 0;
		_selectedTotalPoints = 0;
		_selectionPreviewBonus = 0;
		RefreshSelectionText();
		RefreshConfirmState();
	}

	public void ResetConfirmState()
	{
		_confirmPressed = false;
	}

	private void CacheNodes()
	{
		_containerDiceSelect ??= GetNodeOrNull<Control>("VBoxContainer") ?? NodeSearch.FindDescendantByName<Control>(this, "VBoxContainer");
		ButtonConfirm ??= GetNodeOrNull<Button>("VBoxContainer/Button_Confirm") ?? NodeSearch.FindDescendantByName<Button>(this, "Button_Confirm") ?? NodeSearch.FindFirstDescendant<Button>(this);
		RichTextInfo ??= GetNodeOrNull<RichTextLabel>("VBoxContainer/Button_Confirm/RichTextLabel") ?? NodeSearch.FindFirstDescendant<RichTextLabel>(ButtonConfirm);
		RichTextDiceSelect ??= FindSelectionMesh();

		if (_richTextDiceSelect == null && RichTextDiceSelect != null)
		{
			_richTextDiceSelect = new RichText3DWrapper(RichTextDiceSelect);
		}
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

		SetSelection3DText(FormatSelectionValueText());
	}

	private string FormatSelectionValueText()
	{
		if (_selectionPreviewBonus > 0)
		{
			return $"{_selectedTotalPoints}+{_selectionPreviewBonus}";
		}

		if (_selectionPreviewBonus < 0)
		{
			return $"{_selectedTotalPoints}{_selectionPreviewBonus}";
		}

		return _selectedTotalPoints.ToString();
	}

	private void SetSelectionDisplayVisible(bool visible)
	{
		if (RichTextDiceSelect != null)
		{
			RichTextDiceSelect.Visible = visible;
		}
	}

	private MeshInstance3D FindSelectionMesh()
	{
		Node sceneRoot = GetTree()?.CurrentScene;
		if (sceneRoot == null)
		{
			return null;
		}

		return sceneRoot.FindChild("RichText3D_DiceSelect", true, false) as MeshInstance3D
			?? sceneRoot.FindChild("RichTextDiceSelect", true, false) as MeshInstance3D;
	}

}
#pragma warning restore IDE1006
