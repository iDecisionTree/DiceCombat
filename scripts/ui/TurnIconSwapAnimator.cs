using Godot;
using System;

namespace DiceCombat.scripts.ui;

[GlobalClass]
[Tool]
public partial class TurnIconSwapAnimator : Node3D
{
	[Export] public Sprite3D AttackIcon { get; set; }
	[Export] public Sprite3D DefenceIcon { get; set; }
	public Sprite3D DefenseIcon
	{
		get => DefenceIcon;
		set => DefenceIcon = value;
	}
	[Export] public float SwapDuration { get; set; } = 0.35f;
	[Export] public Vector3 SwapRotationDeltaDegrees { get; set; } = new Vector3(0f, 0f, 180f);

	private Vector3 _attackHomePosition;
	private Vector3 _attackHomeRotationDegrees;
	private Vector3 _attackHomeScale;
	private Vector3 _defenceHomePosition;
	private Vector3 _defenceHomeRotationDegrees;
	private Vector3 _defenceHomeScale;
	private bool _isHomeState = true;
	private bool _hasCachedHomeTransforms;
	private bool _isAnimating;

	public override void _Ready()
	{
		CacheHomeTransforms();
		ResetToHome();
	}

	public void ResetToHome()
	{
		CacheHomeTransforms();
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

		Vector3 attackTargetPosition = _isHomeState ? _defenceHomePosition : _attackHomePosition;
		Vector3 defenceTargetPosition = _isHomeState ? _attackHomePosition : _defenceHomePosition;
		Vector3 attackTargetRotation = (_isHomeState ? _defenceHomeRotationDegrees : _attackHomeRotationDegrees) + SwapRotationDeltaDegrees;
		Vector3 defenceTargetRotation = (_isHomeState ? _attackHomeRotationDegrees : _defenceHomeRotationDegrees) + SwapRotationDeltaDegrees;

		Tween tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Sine);
		tween.SetEase(Tween.EaseType.InOut);
		tween.TweenProperty(AttackIcon, "position", attackTargetPosition, SwapDuration);
		tween.Parallel().TweenProperty(AttackIcon, "rotation_degrees", attackTargetRotation, SwapDuration);
		tween.Parallel().TweenProperty(DefenceIcon, "position", defenceTargetPosition, SwapDuration);
		tween.Parallel().TweenProperty(DefenceIcon, "rotation_degrees", defenceTargetRotation, SwapDuration);
		tween.Finished += () =>
		{
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

		_attackHomePosition = AttackIcon.Position;
		_attackHomeRotationDegrees = AttackIcon.RotationDegrees;
		_attackHomeScale = AttackIcon.Scale;
		_defenceHomePosition = DefenceIcon.Position;
		_defenceHomeRotationDegrees = DefenceIcon.RotationDegrees;
		_defenceHomeScale = DefenceIcon.Scale;
		_hasCachedHomeTransforms = true;
	}

	private bool HasValidIcons()
	{
		return AttackIcon != null && DefenceIcon != null;
	}

	private void ApplyHomeTransforms()
	{
		if (!HasValidIcons())
		{
			return;
		}

		AttackIcon.Position = _attackHomePosition;
		AttackIcon.RotationDegrees = _attackHomeRotationDegrees;
		AttackIcon.Scale = _attackHomeScale;
		DefenceIcon.Position = _defenceHomePosition;
		DefenceIcon.RotationDegrees = _defenceHomeRotationDegrees;
		DefenceIcon.Scale = _defenceHomeScale;
	}
}

