using Godot;
using System;

namespace DiceCombat.scripts.ui;

[GlobalClass]
[Tool]
public partial class TurnIconSwapAnimator : Node3D
{
	[Export] public Sprite3D AttackIcon { get; set; }
	[Export] public Sprite3D DefenseIcon { get; set; }
	public Sprite3D DefenceIcon
	{
		get => DefenseIcon;
		set => DefenseIcon = value;
	}
	[Export] public float IntroDuration { get; set; } = 0.8f;
	[Export] public float SwapDuration { get; set; } = 0.35f;
	[Export] public Vector3 SwapRotationDeltaDegrees { get; set; } = new Vector3(0f, 0f, 180f);

	private enum IconLayoutState
	{
		Center,
		Home,
		Swapped,
	}

	private struct IconTransformState
	{
		public Vector3 Position;
		public Vector3 RotationDegrees;
		public Vector3 Scale;
	}

	private IconTransformState _attackHomeState;
	private IconTransformState _defenseHomeState;
	private IconTransformState _attackCenterState;
	private IconTransformState _defenseCenterState;
	private IconLayoutState _layoutState = IconLayoutState.Home;
	private bool _hasCachedHomeTransforms;
	private bool _isAnimating;
	private Tween _swapTween;

	public override void _Ready()
	{
		CacheHomeTransforms();

		if (Engine.IsEditorHint())
		{
			ResetToHome();
			return;
		}

		ResetToCenter();
	}

	public void ResetToHome()
	{
		CacheHomeTransforms();
		StopSwapTween();
		_layoutState = IconLayoutState.Home;
		_isAnimating = false;
		SetIconsVisible(true);
		ApplyHomeTransforms();
	}

	public void ResetToCenter()
	{
		CacheHomeTransforms();
		StopSwapTween();
		_layoutState = IconLayoutState.Center;
		_isAnimating = false;
		ApplyCenterTransforms();
		SetIconsVisible(false);
	}

	public void PlayIntroAnimation(bool useSwappedTargets, Action onFinished = null)
	{
		if (!HasValidIcons())
		{
			onFinished?.Invoke();
			return;
		}

		CacheHomeTransforms();
		if (_isAnimating)
		{
			onFinished?.Invoke();
			return;
		}

		SetIconsVisible(true);
		ApplyCenterTransforms();
		_layoutState = IconLayoutState.Center;
		_isAnimating = true;

		IconLayoutState targetLayoutState = useSwappedTargets ? IconLayoutState.Swapped : IconLayoutState.Home;
		(IconTransformState attackTargetState, IconTransformState defenseTargetState) = GetTargetStates(targetLayoutState);
		Tween tween = CreateTweenToStates(attackTargetState, defenseTargetState, IntroDuration);
		tween.Finished += () =>
		{
			_swapTween = null;
			_layoutState = targetLayoutState;
			_isAnimating = false;
			onFinished?.Invoke();
		};
	}

	public void PlaySwapAnimation(Action onFinished = null)
	{
		if (!HasValidIcons())
		{
			onFinished?.Invoke();
			return;
		}

		CacheHomeTransforms();
		if (_isAnimating)
		{
			onFinished?.Invoke();
			return;
		}

		SetIconsVisible(true);
		_isAnimating = true;

		(IconTransformState attackTargetState, IconTransformState defenseTargetState, IconLayoutState targetLayoutState) = GetSwapTargetStates();
		Tween tween = CreateTweenToStates(attackTargetState, defenseTargetState, SwapDuration);
		tween.Finished += () =>
		{
			_swapTween = null;
			_layoutState = targetLayoutState;
			_isAnimating = false;
			onFinished?.Invoke();
		};
	}

	private void CacheHomeTransforms()
	{
		if (_hasCachedHomeTransforms || !HasValidIcons())
		{
			return;
		}

		_attackHomeState = ReadTransformState(AttackIcon);
		_defenseHomeState = ReadTransformState(DefenseIcon);

		Vector3 centerPosition = (_attackHomeState.Position + _defenseHomeState.Position) * 0.5f;
		_attackCenterState = _attackHomeState;
		_attackCenterState.Position = centerPosition;
		_defenseCenterState = _defenseHomeState;
		_defenseCenterState.Position = centerPosition;
		_hasCachedHomeTransforms = true;
	}

	private bool HasValidIcons()
	{
		return AttackIcon != null && DefenseIcon != null;
	}

	private void ApplyHomeTransforms()
	{
		if (!HasValidIcons())
		{
			return;
		}

		ApplyTransformState(AttackIcon, _attackHomeState);
		ApplyTransformState(DefenseIcon, _defenseHomeState);
	}

	private void ApplyCenterTransforms()
	{
		if (!HasValidIcons())
		{
			return;
		}

		ApplyTransformState(AttackIcon, _attackCenterState);
		ApplyTransformState(DefenseIcon, _defenseCenterState);
	}

	private void SetIconsVisible(bool visible)
	{
		if (!HasValidIcons())
		{
			return;
		}

		AttackIcon.Visible = visible;
		DefenseIcon.Visible = visible;
	}

	private (IconTransformState attackTargetState, IconTransformState defenseTargetState, IconLayoutState targetLayoutState) GetSwapTargetStates()
	{
		bool moveToSwappedState = _layoutState != IconLayoutState.Swapped;
		IconLayoutState targetLayoutState = moveToSwappedState ? IconLayoutState.Swapped : IconLayoutState.Home;
		(IconTransformState attackTargetState, IconTransformState defenseTargetState) = GetTargetStates(targetLayoutState);
		return (attackTargetState, defenseTargetState, targetLayoutState);
	}

	private (IconTransformState attackTargetState, IconTransformState defenseTargetState) GetTargetStates(IconLayoutState targetLayoutState)
	{
		IconTransformState attackTargetState = targetLayoutState == IconLayoutState.Swapped ? _defenseHomeState : _attackHomeState;
		IconTransformState defenseTargetState = targetLayoutState == IconLayoutState.Swapped ? _attackHomeState : _defenseHomeState;

		if (targetLayoutState == IconLayoutState.Swapped)
		{
			attackTargetState.RotationDegrees += SwapRotationDeltaDegrees;
			defenseTargetState.RotationDegrees += SwapRotationDeltaDegrees;
		}

		return (attackTargetState, defenseTargetState);
	}

	private Tween CreateTweenToStates(IconTransformState attackTargetState, IconTransformState defenseTargetState, float duration)
	{
		StopSwapTween();
		Tween tween = CreateTween();
		_swapTween = tween;
		tween.SetTrans(Tween.TransitionType.Sine);
		tween.SetEase(Tween.EaseType.InOut);
		TweenIconTo(AttackIcon, attackTargetState, tween, duration);
		TweenIconTo(DefenseIcon, defenseTargetState, tween.Parallel(), duration);
		return tween;
	}

	private static IconTransformState ReadTransformState(Node3D node)
	{
		return new IconTransformState
		{
			Position = node.Position,
			RotationDegrees = node.RotationDegrees,
			Scale = node.Scale
		};
	}

	private static void ApplyTransformState(Node3D node, IconTransformState state)
	{
		node.Position = state.Position;
		node.RotationDegrees = state.RotationDegrees;
		node.Scale = state.Scale;
	}

	private void TweenIconTo(Node3D icon, IconTransformState targetState, Tween tween, float duration)
	{
		float tweenDuration = Mathf.Max(duration, 0f);
		tween.TweenProperty(icon, "position", targetState.Position, tweenDuration);
		tween.Parallel().TweenProperty(icon, "rotation_degrees", targetState.RotationDegrees, tweenDuration);
		tween.Parallel().TweenProperty(icon, "scale", targetState.Scale, tweenDuration);
	}

	private void StopSwapTween()
	{
		_swapTween?.Kill();
		_swapTween = null;
	}
}

