using Godot;
using DiceCombat.scripts;
using DiceCombat.scripts.card;

namespace DiceCombat.scripts.state_machine;

[GlobalClass]
[Tool]
public partial class CombatEffectDirector3D : CombatEffectDirector
{
	[Export] public CanvasItem DimOverlay { get; set; }

	[ExportGroup("Resolution Effect")]
	[Export] public float OverlayAlpha { get; set; } = 1.0f;
	[Export] public float OverlayFadeInDuration { get; set; } = 0.24f;
	[Export] public float RevealAnimationDuration { get; set; } = 0.40f;
	[Export] public float OverlayHoldDuration { get; set; } = 0.12f;
	[Export] public float OverlayFadeOutDuration { get; set; } = 0.20f;
	[Export] public StringName RevealAnimationName { get; set; } = "reveal";
	[Export] public StringName PlayerRevealAnimationName { get; set; }
	[Export] public StringName EnemyRevealAnimationName { get; set; }
	[Export] public StringName DamageAnimationName { get; set; } = "damage";
	[Export] public StringName ResetAnimationName { get; set; } = "reset";

	private Tween _overlayTween;

	public override void ResetEffects()
	{
		KillTweens();
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


	private void KillTweens()
	{
		_overlayTween?.Kill();
		_overlayTween = null;
	}
}

