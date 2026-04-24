using Godot;
using DiceCombat.scripts.card;

namespace DiceCombat.scripts.ui;

[GlobalClass]
[Tool]
public partial class CombatInfoPanel : PanelContainer
{
	[Export] public Card BoundCard { get; set; }
	[Export] public bool PreviewCropInEditor { get; set; } = true;

	private const string EmptyPreviewSnapshot = "<empty>";

	private RichTextLabel _descriptionLabel;
	private TextureProgressBar _healthBar;
	private RichTextLabel _healthLabel;
	private TextureRect _avatarRect;
	private RichTextLabel _attackLabel;
	private RichTextLabel _defenseLabel;
	private string _editorPreviewSnapshot = EmptyPreviewSnapshot;
	private bool _editorPreviewDirty = true;

	public override void _Ready()
	{
		CacheNodes();
		SetProcess(Engine.IsEditorHint());
		MarkPreviewDirty();
		RefreshFromCard();
	}

	public override void _Process(double delta)
	{
		TryRefreshEditorPreview();
	}

	public void BindCard(Card card)
	{
		BoundCard = card;
		MarkPreviewDirty();
		RefreshFromCard();
	}

	public void RefreshFromCard()
	{
		CacheNodes();

		CardData cardData = BoundCard?.CardData;
		if (cardData == null)
		{
			ApplyEmptyState();
			CapturePreviewSnapshot();
			return;
		}

		SetDescriptionText(cardData.Description ?? string.Empty);
		SetHealthValue(BoundCard != null ? BoundCard.CurrentHealth : 0, Mathf.Max(cardData.MaxHealth, 1));
		SetAvatarTexture(cardData.InfoAvatar ?? cardData.CardAvatar);
		SetStatValue(_attackLabel, cardData.Attack);
		SetStatValue(_defenseLabel, cardData.Defense);
		CapturePreviewSnapshot();
	}

	private void ApplyEmptyState()
	{
		SetDescriptionText(string.Empty);
		SetHealthValue(0, 1);
		SetAvatarTexture(null);
		SetStatValue(_attackLabel, 0);
		SetStatValue(_defenseLabel, 0);
	}

	private void CacheNodes()
	{
		_descriptionLabel ??= GetNodeOrNull<RichTextLabel>("HBoxContainer/RichTextLabel_Description");
		_healthBar ??= GetNodeOrNull<TextureProgressBar>("HBoxContainer/Control_Health/TextureProgressBar");
		_healthLabel ??= GetNodeOrNull<RichTextLabel>("HBoxContainer/Control_Health/RichTextLabel");
		_avatarRect ??= GetNodeOrNull<TextureRect>("HBoxContainer/Control_Avatar/TextureRect_Avatar");
		_attackLabel ??= GetNodeOrNull<RichTextLabel>("HBoxContainer/Control_Avatar/TextureRect_Attack/RichTextLabel");
		_defenseLabel ??= GetNodeOrNull<RichTextLabel>("HBoxContainer/Control_Avatar/TextureRect_Defence/RichTextLabel");
	}

	private void SetDescriptionText(string text)
	{
		if (_descriptionLabel != null)
		{
			_descriptionLabel.Text = text;
		}
	}

	private void SetHealthValue(int currentHealth, int maxHealth)
	{
		currentHealth = Mathf.Max(currentHealth, 0);
		maxHealth = Mathf.Max(maxHealth, 1);

		if (_healthBar != null)
		{
			_healthBar.MaxValue = maxHealth;
			_healthBar.Value = Mathf.Min(currentHealth, maxHealth);
		}

		if (_healthLabel != null)
		{
			_healthLabel.Text = $"[b]{currentHealth}[/b]";
		}
	}

	private void SetAvatarTexture(Texture2D texture)
	{
		if (_avatarRect != null)
		{
			_avatarRect.Texture = texture;
		}
	}

	private static void SetStatValue(RichTextLabel label, int value)
	{
		if (label != null)
		{
			label.Text = $"[b]{Mathf.Max(value, 0)}[/b]";
		}
	}

	private void TryRefreshEditorPreview()
	{
		if (!PreviewCropInEditor || !Engine.IsEditorHint())
		{
			return;
		}

		string currentSnapshot = BuildPreviewSnapshot();
		if (_editorPreviewDirty || currentSnapshot != _editorPreviewSnapshot)
		{
			RefreshFromCard();
		}
	}

	private void MarkPreviewDirty()
	{
		_editorPreviewDirty = true;
	}

	private void CapturePreviewSnapshot()
	{
		_editorPreviewSnapshot = BuildPreviewSnapshot();
		_editorPreviewDirty = false;
	}

	private string BuildPreviewSnapshot()
	{
		Card card = BoundCard;
		CardData cardData = card?.CardData;

		if (cardData == null)
		{
			return EmptyPreviewSnapshot;
		}

		return string.Join("|",
			card.GetInstanceId(),
			cardData.GetInstanceId(),
			card.CurrentHealth,
			cardData.Description ?? string.Empty,
			cardData.MaxHealth,
			cardData.Attack,
			cardData.Defense,
			cardData.InfoAvatar?.GetInstanceId() ?? 0,
			cardData.CardAvatar?.GetInstanceId() ?? 0);
	}
}

