using Godot;
using System.Collections.Generic;
using DiceCombat.scripts.card;

namespace DiceCombat.scripts.state_machine;

[GlobalClass]
[Tool]
public partial class CombatEffectDirector3D : CombatEffectDirector
{
	[Export] public CanvasItem DimOverlay { get; set; }
	[Export] public int IsolatedRenderLayer { get; set; } = 2;
	[Export] public NodePath[] AdditionalIsolatedRootPaths = [];

	[ExportGroup("Resolution Effect")]
	[Export] public float OverlayAlpha { get; set; } = 0.65f;
	[Export] public float OverlayFadeInDuration { get; set; } = 0.24f;
	[Export] public float RevealAnimationDuration { get; set; } = 0.40f;
	[Export] public float OverlayHoldDuration { get; set; } = 0.12f;
	[Export] public float OverlayFadeOutDuration { get; set; } = 0.20f;
	[Export] public StringName RevealAnimationName { get; set; } = "reveal";
	[Export] public StringName PlayerRevealAnimationName { get; set; } = "card/player_reveal";
	[Export] public StringName EnemyRevealAnimationName { get; set; } = "card/enemy_reveal";
	[Export] public StringName DamageAnimationName { get; set; } = "damage";
	[Export] public StringName ResetAnimationName { get; set; } = "reset";

	private Tween _overlayTween;
	private readonly Dictionary<VisualInstance3D, uint> _capturedInstanceLayers = new();
	private SubViewport _isolatedViewport;
	private Camera3D _isolatedCamera;
	private TextureRect _isolatedPresenter;
	private Camera3D _capturedMainCamera;
	private Card _capturedCard;
	private uint _capturedMainCameraCullMask;
	private bool _isIsolatedCaptureActive;

	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			return;
		}

		EnsureIsolatedCaptureNodes();
	}

	public override void _Process(double delta)
	{
		if (!_isIsolatedCaptureActive)
		{
			return;
		}

		SyncIsolatedViewportSize();
		SyncIsolatedCamera();
	}

	public override void ResetEffects()
	{
		KillTweens();
		EndIsolatedCapture();
		SetOverlayAlpha(0f, false);
	}

	public override float GetResolutionDuration()
	{
		float total = OverlayFadeInDuration + RevealAnimationDuration + OverlayHoldDuration + OverlayFadeOutDuration;
		return total + 0.05f;
	}

	public override void PlayAttackEffect(CombatTurn turn, Card sourceCard, Card targetCard, int damage)
	{
		KillTweens();
		SetCardVisible(sourceCard, true);
		SetCardVisible(targetCard, false);
		BeginIsolatedCapture(sourceCard);
		SetOverlayAlpha(0f, true);
		TweenOverlayTo(OverlayAlpha, OverlayFadeInDuration, 0f, true, Callable.From(() => PlayRevealAnimation(turn, sourceCard)));
	}

	public override void PlayDefenseEffect(CombatTurn turn, Card sourceCard, Card targetCard, int damage)
	{
		TweenOverlayTo(0f, OverlayFadeOutDuration, OverlayFadeInDuration + RevealAnimationDuration + OverlayHoldDuration);
	}

	public override void PlayDamageEffect(Card card, int damage)
	{
		damage = Mathf.Max(damage, 0);
		if (damage <= 0)
		{
			return;
		}

		TryPlayCardAnimation(card, DamageAnimationName, "受击动画");
	}

	protected virtual void PlayRevealAnimation(CombatTurn turn, Card sourceCard)
	{
		TryPlayCardAnimation(sourceCard, ResolveRevealAnimationName(turn), "结算展示动画");
	}

	private StringName ResolveRevealAnimationName(CombatTurn turn)
	{
		StringName playerAnimationName = PlayerRevealAnimationName.IsEmpty ? RevealAnimationName : PlayerRevealAnimationName;
		StringName enemyAnimationName = EnemyRevealAnimationName.IsEmpty ? RevealAnimationName : EnemyRevealAnimationName;
		return turn == CombatTurn.Player ? playerAnimationName : enemyAnimationName;
	}

	private void TryPlayCardAnimation(Card card, StringName animationName, string animationLabel)
	{
		if (card == null || animationName.IsEmpty)
		{
			return;
		}

		if (!TryGetAnimationPlayer(card, animationLabel, out AnimationPlayer animationPlayer))
		{
			return;
		}

		if (!TryResolveAnimationName(animationPlayer, card, animationName, out StringName resolvedAnimationName))
		{
			return;
		}

		animationPlayer.Stop();
		PlayAnimationWithReset(animationPlayer, resolvedAnimationName);
	}

	private bool TryGetAnimationPlayer(Card card, string animationLabel, out AnimationPlayer animationPlayer)
	{
		animationPlayer = NodeSearch.FindFirstDescendant<AnimationPlayer>(card);
		if (animationPlayer != null)
		{
			return true;
		}

		GD.PushWarning($"CombatEffectDirector3D: 找不到{animationLabel}播放器, card={card.Name}");
		return false;
	}

	private static bool TryResolveAnimationName(AnimationPlayer animationPlayer, Card card, StringName requestedAnimationName, out StringName resolvedAnimationName)
	{
		resolvedAnimationName = requestedAnimationName;
		if (animationPlayer.HasAnimation(resolvedAnimationName))
		{
			return true;
		}

		string[] availableAnimations = animationPlayer.GetAnimationList();
		if (availableAnimations.Length == 0)
		{
			GD.PushWarning($"CombatEffectDirector3D: {card.Name} 没有可播放的动画，期望动画 '{requestedAnimationName}'");
			return false;
		}

		GD.PushWarning($"CombatEffectDirector3D: {card.Name} 找不到动画 '{requestedAnimationName}'，改为播放首个可用动画");
		resolvedAnimationName = availableAnimations[0];
		return true;
	}

	private void PlayAnimationWithReset(AnimationPlayer animationPlayer, StringName playedAnimationName)
	{
		if (animationPlayer == null)
		{
			return;
		}

		if (ResetAnimationName.IsEmpty || playedAnimationName == ResetAnimationName)
		{
			animationPlayer.Play(playedAnimationName);
			return;
		}

		if (!animationPlayer.HasAnimation(ResetAnimationName))
		{
			animationPlayer.Play(playedAnimationName);
			return;
		}

		animationPlayer.Play(ResetAnimationName);
		animationPlayer.Queue(playedAnimationName);
		animationPlayer.Queue(ResetAnimationName);
	}

	private void SetOverlayAlpha(float alpha, bool visible)
	{
		if (DimOverlay == null)
		{
			return;
		}

		DimOverlay.Visible = visible;
		DimOverlay.Modulate = GetOverlayColor(alpha);
	}

	private void TweenOverlayTo(float alpha, float duration, float delay = 0f, bool hasOnComplete = false, Callable onComplete = default)
	{
		if (DimOverlay == null)
		{
			return;
		}

		Tween tween = CreateTween();
		_overlayTween = tween;
		tween.SetTrans(Tween.TransitionType.Sine);
		tween.SetEase(Tween.EaseType.InOut);
		DimOverlay.Visible = true;
		if (delay > 0f)
		{
			tween.TweenInterval(delay);
		}

		tween.TweenProperty(DimOverlay, "modulate", GetOverlayColor(alpha), duration);

		if (alpha <= 0f)
		{
			tween.TweenCallback(Callable.From(() =>
			{
				if (DimOverlay != null)
				{
					DimOverlay.Visible = false;
				}

				EndIsolatedCapture();
			}));
		}
		else if (hasOnComplete)
		{
			tween.TweenCallback(onComplete);
		}
	}

	private static Color GetOverlayColor(float alpha)
	{
		return new Color(1f, 1f, 1f, Mathf.Clamp(alpha, 0f, 1f));
	}

	private static void SetCardVisible(Card card, bool visible)
	{
		if (card == null)
		{
			return;
		}

		card.Visible = visible;
	}

	private void BeginIsolatedCapture(Card card)
	{
		EndIsolatedCapture();

		if (card == null)
		{
			return;
		}

		EnsureIsolatedCaptureNodes();
		if (_isolatedViewport == null || _isolatedCamera == null || _isolatedPresenter == null)
		{
			return;
		}

		if (!TryGetMainCamera(out Camera3D mainCamera))
		{
			return;
		}

		uint isolatedLayerMask = GetIsolatedLayerMask();
		_capturedMainCamera = mainCamera;
		_capturedMainCameraCullMask = mainCamera.CullMask;
		_capturedCard = card;
		_isolatedCamera.CullMask = isolatedLayerMask;
		_capturedMainCamera.CullMask &= ~isolatedLayerMask;
		CaptureCardLayers(card, isolatedLayerMask);
		CaptureAdditionalIsolatedRoots(isolatedLayerMask);
		_isIsolatedCaptureActive = true;
		_isolatedPresenter.Visible = true;
		SyncIsolatedViewportSize();
		SyncIsolatedCamera();
	}

	private void EndIsolatedCapture()
	{
		RestoreCapturedCardLayers();

		if (GodotObject.IsInstanceValid(_capturedMainCamera))
		{
			_capturedMainCamera.CullMask = _capturedMainCameraCullMask;
		}

		_capturedMainCamera = null;
		_capturedCard = null;
		_capturedMainCameraCullMask = 0;
		_isIsolatedCaptureActive = false;

		if (_isolatedPresenter != null)
		{
			_isolatedPresenter.Visible = false;
		}
	}

	private void EnsureIsolatedCaptureNodes()
	{
		if (_isolatedViewport != null && _isolatedCamera != null && _isolatedPresenter != null)
		{
			return;
		}

		Viewport mainViewport = GetViewport();
		Node overlayParent = DimOverlay?.GetParent();
		if (mainViewport == null || overlayParent == null)
		{
			return;
		}

		_isolatedViewport = new SubViewport
		{
			Name = "ResolutionIsolatedViewport",
			TransparentBg = true,
			HandleInputLocally = false,
			World3D = mainViewport.World3D,
		};
		AddChild(_isolatedViewport);

		_isolatedCamera = new Camera3D
		{
			Name = "ResolutionIsolatedCamera",
			Current = true,
			CullMask = GetIsolatedLayerMask(),
		};
		_isolatedViewport.AddChild(_isolatedCamera);

		_isolatedPresenter = new TextureRect
		{
			Name = "ResolutionIsolatedPresenter",
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Texture = _isolatedViewport.GetTexture(),
			AnchorRight = 1f,
			AnchorBottom = 1f,
		};
		overlayParent.AddChild(_isolatedPresenter);
		overlayParent.MoveChild(_isolatedPresenter, DimOverlay.GetIndex() + 1);
		SyncIsolatedViewportSize();
	}

	private void SyncIsolatedViewportSize()
	{
		if (_isolatedViewport == null)
		{
			return;
		}

		Viewport mainViewport = GetViewport();
		if (mainViewport == null)
		{
			return;
		}

		Vector2 viewportRectSize = mainViewport.GetVisibleRect().Size;
		Vector2I viewportSize = new Vector2I((int)viewportRectSize.X, (int)viewportRectSize.Y);
		if (viewportSize.X <= 0 || viewportSize.Y <= 0)
		{
			return;
		}

		if (_isolatedViewport.Size != viewportSize)
		{
			_isolatedViewport.Size = viewportSize;
		}
	}

	private void SyncIsolatedCamera()
	{
		if (_isolatedCamera == null || !GodotObject.IsInstanceValid(_capturedMainCamera))
		{
			return;
		}

		_isolatedCamera.GlobalTransform = _capturedMainCamera.GlobalTransform;
		_isolatedCamera.Fov = _capturedMainCamera.Fov;
	}

	private void CaptureCardLayers(Card card, uint isolatedLayerMask)
	{
		_capturedInstanceLayers.Clear();
		CollectVisualInstances(card, _capturedInstanceLayers, isolatedLayerMask);
	}

	private void CaptureAdditionalIsolatedRoots(uint isolatedLayerMask)
	{
		if (AdditionalIsolatedRootPaths == null)
		{
			return;
		}

		foreach (NodePath nodePath in AdditionalIsolatedRootPaths)
		{
			if (nodePath.IsEmpty)
			{
				continue;
			}

			Node node = GetNodeOrNull(nodePath);
			if (node == null)
			{
				continue;
			}

			CollectVisualInstances(node, _capturedInstanceLayers, isolatedLayerMask);
		}
	}

	private void RestoreCapturedCardLayers()
	{
		foreach (KeyValuePair<VisualInstance3D, uint> entry in _capturedInstanceLayers)
		{
			if (GodotObject.IsInstanceValid(entry.Key))
			{
				entry.Key.Layers = entry.Value;
			}
		}

		_capturedInstanceLayers.Clear();
	}

	private static void CollectVisualInstances(Node node, Dictionary<VisualInstance3D, uint> capturedLayers, uint isolatedLayerMask)
	{
		if (node == null)
		{
			return;
		}

		if (node is VisualInstance3D visualInstance)
		{
			if (!capturedLayers.ContainsKey(visualInstance))
			{
				capturedLayers[visualInstance] = visualInstance.Layers;
			}

			visualInstance.Layers = isolatedLayerMask;
		}

		foreach (Node child in node.GetChildren())
		{
			CollectVisualInstances(child, capturedLayers, isolatedLayerMask);
		}
	}

	private bool TryGetMainCamera(out Camera3D camera)
	{
		camera = GetViewport()?.GetCamera3D();
		return camera != null;
	}

	private uint GetIsolatedLayerMask()
	{
		int layerIndex = Mathf.Clamp(IsolatedRenderLayer, 1, 20) - 1;
		return (uint)(1 << layerIndex);
	}


	private void KillTweens()
	{
		_overlayTween?.Kill();
		_overlayTween = null;
	}
}

