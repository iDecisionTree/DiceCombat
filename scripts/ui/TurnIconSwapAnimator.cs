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
	[Export] public float SwapDuration { get; set; } = 0.35f;
	[Export] public Vector3 SwapRotationDeltaDegrees { get; set; } = new Vector3(0f, 0f, 180f);

	private struct IconTransformState
	{
		public Vector3 Position;
		public Vector3 RotationDegrees;
		public Vector3 Scale;
	}

	private IconTransformState _attackHomeState;
	private IconTransformState _defenseHomeState;
	private bool _isHomeState = true;
	private bool _hasCachedHomeTransforms;
	private bool _isAnimating;
	private Tween _swapTween;

	public override void _Ready()
	{
		CacheHomeTransforms();
		ResetToHome();
	}

	public void ResetToHome()
	{
		CacheHomeTransforms();
		StopSwapTween();
		_isHomeState = true;
		_isAnimating = false;
		ApplyHomeTransforms();
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

		_isAnimating = true;

		(IconTransformState attackTargetState, IconTransformState defenseTargetState) = GetSwapTargetStates();
		Tween tween = CreateSwapTween(attackTargetState, defenseTargetState);
		tween.Finished += () =>
		{
			_swapTween = null;
			_isHomeState = !_isHomeState;
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

	private (IconTransformState attackTargetState, IconTransformState defenseTargetState) GetSwapTargetStates()
	{
		IconTransformState attackTargetState = _isHomeState ? _defenseHomeState : _attackHomeState;
		IconTransformState defenseTargetState = _isHomeState ? _attackHomeState : _defenseHomeState;

		attackTargetState.RotationDegrees += SwapRotationDeltaDegrees;
		defenseTargetState.RotationDegrees += SwapRotationDeltaDegrees;
		return (attackTargetState, defenseTargetState);
	}

	private Tween CreateSwapTween(IconTransformState attackTargetState, IconTransformState defenseTargetState)
	{
		StopSwapTween();
		Tween tween = CreateTween();
		_swapTween = tween;
		tween.SetTrans(Tween.TransitionType.Sine);
		tween.SetEase(Tween.EaseType.InOut);
		TweenIconTo(AttackIcon, attackTargetState, tween);
		TweenIconTo(DefenseIcon, defenseTargetState, tween.Parallel());
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

	private void TweenIconTo(Node3D icon, IconTransformState targetState, Tween tween)
	{
		tween.TweenProperty(icon, "position", targetState.Position, SwapDuration);
		tween.Parallel().TweenProperty(icon, "rotation_degrees", targetState.RotationDegrees, SwapDuration);
		tween.Parallel().TweenProperty(icon, "scale", targetState.Scale, SwapDuration);
	}

	private void StopSwapTween()
	{
		_swapTween?.Kill();
		_swapTween = null;
	}
}

