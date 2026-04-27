using System;
using Godot;
using DiceCombat.scripts.card;

namespace DiceCombat.scripts.state_machine;

[GlobalClass]
public partial class CombatCameraDirector3D : CombatCameraDirector
{
	[Export] public Camera3D Camera { get; set; }
	[Export] public Node3D PlayerFocusTarget { get; set; }
	[Export] public Node3D EnemyFocusTarget { get; set; }

	[ExportGroup("Shot Settings")]
	[Export] public float MoveDuration { get; set; } = 0.25f;
	[Export] public float ResolutionFov { get; set; } = 48f;
	[Export] public Vector3 ResolutionOffset { get; set; } = new Vector3(-8f, 2f, 12f);

	private Vector3 _homePosition;
	private Vector3 _homeRotationDegrees;
	private float _homeFov = 60f;
	private bool _hasCachedHomeTransform;
	private Tween _focusTween;

	public override void _Ready()
	{
		CacheHomeTransform();
		ResetCamera();
	}

	public override void ResetCamera()
	{
		if (!TryGetCamera(out Camera3D camera))
		{
			return;
		}

		CacheHomeTransform();
		ApplyCameraTransform(camera, _homePosition, _homeRotationDegrees, _homeFov);
	}

	public override void OnBattleStarted()
	{
		ResetCamera();
	}

	public override void OnTurnChanged(CombatTurn turn, int roundCount)
	{
	}

	public override void OnDamageResolved(CombatTurn turn, Card sourceCard, Card targetCard, int damage)
	{
	}

	public override void OnBattleEnded(CombatState finalState)
	{
		ResetCamera();
	}

	public override void FocusOnNode(Node3D target, float duration, Vector3? customOffset = null, Action onFinished = null)
	{
		if (!TryGetCamera(out Camera3D camera) || target == null)
		{
			onFinished?.Invoke();
			return;
		}

		CacheHomeTransform();
		KillFocusTween();

		Vector3 offset = customOffset ?? ResolutionOffset;
		Vector3 lookTarget = target.GlobalPosition;
		Vector3 targetPosition = lookTarget + offset;
		Transform3D lookTransform = new Transform3D(Basis.Identity, targetPosition).LookingAt(lookTarget, Vector3.Up);
		Vector3 eulerRad = lookTransform.Basis.GetEuler();
		Vector3 targetRotationDegrees = new Vector3(
			Mathf.RadToDeg(eulerRad.X),
			Mathf.RadToDeg(eulerRad.Y),
			Mathf.RadToDeg(eulerRad.Z));
		float targetFov = Mathf.Max(ResolutionFov, 1f);
		float tweenDuration = Mathf.Max(duration, 0f);

		if (tweenDuration <= 0f)
		{
			ApplyCameraTransform(camera, targetPosition, targetRotationDegrees, targetFov);
			onFinished?.Invoke();
			return;
		}

		_focusTween = CreateTween();
		_focusTween.SetTrans(Tween.TransitionType.Sine);
		_focusTween.SetEase(Tween.EaseType.InOut);
		_focusTween.TweenProperty(camera, "position", targetPosition, tweenDuration);
		_focusTween.Parallel().TweenProperty(camera, "rotation_degrees", targetRotationDegrees, tweenDuration);
		_focusTween.Parallel().TweenProperty(camera, "fov", targetFov, tweenDuration);
		_focusTween.Finished += () =>
		{
			_focusTween = null;
			onFinished?.Invoke();
		};
	}

	private void KillFocusTween()
	{
		_focusTween?.Kill();
		_focusTween = null;
	}

	private void CacheHomeTransform()
	{
		if (_hasCachedHomeTransform || Camera == null)
		{
			return;
		}

		_homePosition = Camera.Position;
		_homeRotationDegrees = Camera.RotationDegrees;
		_homeFov = Camera.Fov;
		_hasCachedHomeTransform = true;
	}


	private static void ApplyCameraTransform(Camera3D camera, Vector3 targetPosition, Vector3 targetRotationDegrees, float targetFov)
	{
		if (camera == null)
		{
			return;
		}

		camera.Position = targetPosition;
		camera.RotationDegrees = targetRotationDegrees;
		camera.Fov = targetFov;
	}

	private bool TryGetCamera(out Camera3D camera)
	{
		camera = Camera;
		return camera != null;
	}
}


