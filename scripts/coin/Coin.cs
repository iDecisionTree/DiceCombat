using Godot;
using System;

namespace DiceCombat.scripts.coin;

public enum CoinSide
{
	Front,
	Back,
}

[GlobalClass]
public partial class Coin : RigidBody3D
{
	[ExportGroup("Face Marker")]
	[Export] public Node3D FrontMarker { get; set; }
	[Export] public Node3D BackMarker { get; set; }
	[Export] public Node3D FrontResultText { get; set; }
	[Export] public Node3D BackResultText { get; set; }

	[ExportGroup("Physics")]
	[Export] public float SpawnHeight { get; set; } = 2.25f;
	[Export] public Vector2 SpawnAreaHalfExtents { get; set; } = new Vector2(0.4f, 0.4f);
	[Export] public float TossUpImpulse { get; set; } = 2.4f;
	[Export] public float TossHorizontalImpulse { get; set; } = 0.8f;
	[Export] public float TorqueImpulseStrength { get; set; } = 5f;
	[Export] public float StableTimeRequired { get; set; } = 0.2f;
	[Export] public float MaxTossTime { get; set; } = 10f;
	[Export] public float LinearSleepThreshold { get; set; } = 0.05f;
	[Export] public float AngularSleepThreshold { get; set; } = 0.2f;

	[ExportGroup("Result Presentation")]
	[Export] public float ResultMoveDuration { get; set; } = 0.75f;
	[Export] public float ResultHoldDuration { get; set; } = 2.0f;
	[Export] public float ResultDistanceFromCamera { get; set; } = 4f;
	[Export] public float ResultHeightOffset { get; set; } = -0.2f;

	public CoinSide CurrentSide { get; private set; } = CoinSide.Front;

	private Transform3D _homeTransform;
	private bool _hasHomeTransform;
	private bool _isTossing;
	private float _tossElapsed;
	private float _stableElapsed;
	private Action<CoinSide> _onFinished;
	private Tween _resultTween;

	public override void _Ready()
	{
		CacheHomeTransform();
		CacheResultTextNodes();

		if (!Engine.IsEditorHint())
		{
			ResetToIdle();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_isTossing)
		{
			return;
		}

		_tossElapsed += (float)delta;
		bool isSettled = LinearVelocity.LengthSquared() <= LinearSleepThreshold * LinearSleepThreshold && AngularVelocity.LengthSquared() <= AngularSleepThreshold * AngularSleepThreshold;

		if (isSettled || Sleeping)
		{
			_stableElapsed += (float)delta;
		}
		else
		{
			_stableElapsed = 0f;
		}

		if (_stableElapsed >= StableTimeRequired || _tossElapsed >= MaxTossTime)
		{
			FinishToss(ResolveFaceUp());
		}
	}

	public void PlayToss(Action<CoinSide> onFinished = null)
	{
		CacheHomeTransform();
		CacheResultTextNodes();
		_resultTween?.Kill();
		_onFinished = onFinished;
		CurrentSide = CoinSide.Front;
		_isTossing = true;
		_tossElapsed = 0f;
		_stableElapsed = 0f;
		SetResultTextVisible(null);

		Visible = true;
		Freeze = true;
		Sleeping = false;
		LinearVelocity = Vector3.Zero;
		AngularVelocity = Vector3.Zero;

		Vector3 spawnPosition = _homeTransform.Origin + new Vector3(
			(float)GD.RandRange(-SpawnAreaHalfExtents.X, SpawnAreaHalfExtents.X),
			SpawnHeight,
			(float)GD.RandRange(-SpawnAreaHalfExtents.Y, SpawnAreaHalfExtents.Y)
		);

		Transform = new Transform3D(_homeTransform.Basis, spawnPosition);
		RotationDegrees = new Vector3(
			GD.Randf() * 360f,
			GD.Randf() * 360f,
			GD.Randf() * 360f
		);

		Freeze = false;

		Vector3 launchImpulse = new Vector3(
			(float)GD.RandRange(-TossHorizontalImpulse, TossHorizontalImpulse),
			TossUpImpulse,
			(float)GD.RandRange(-TossHorizontalImpulse, TossHorizontalImpulse)
		);
		Vector3 spinImpulse = new Vector3(
			(float)GD.RandRange(-1f, 1f),
			(float)GD.RandRange(-1f, 1f),
			(float)GD.RandRange(-1f, 1f)
		) * TorqueImpulseStrength;

		ApplyCentralImpulse(launchImpulse);
		ApplyTorqueImpulse(spinImpulse);
	}

	public void ResetToIdle()
	{
		CacheHomeTransform();
		CacheResultTextNodes();
		_resultTween?.Kill();
		_resultTween = null;
		_onFinished = null;
		_isTossing = false;
		_tossElapsed = 0f;
		_stableElapsed = 0f;
		SetResultTextVisible(null);
		Freeze = true;
		Sleeping = false;
		LinearVelocity = Vector3.Zero;
		AngularVelocity = Vector3.Zero;
		Transform = _homeTransform;
		Visible = false;
	}

	private void CacheHomeTransform()
	{
		if (_hasHomeTransform)
		{
			return;
		}

		_homeTransform = Transform;
		_hasHomeTransform = true;
	}

	private void CacheResultTextNodes()
	{
		FrontResultText ??= GetNodeOrNull<Node3D>("RichText3D_Front");
		BackResultText ??= GetNodeOrNull<Node3D>("RichText3D_Back");
	}

	private CoinSide ResolveFaceUp()
	{
		if (FrontMarker == null || BackMarker == null)
		{
			GD.PrintErr("Coin: FrontMarker 或 BackMarker 未绑定, 默认判定为正面。");
			return CoinSide.Front;
		}

		return FrontMarker.GlobalPosition.Y >= BackMarker.GlobalPosition.Y ? CoinSide.Front : CoinSide.Back;
	}

	private void FinishToss(CoinSide side)
	{
		if (!_isTossing)
		{
			return;
		}

		_isTossing = false;
		CurrentSide = side;
		Freeze = true;
		LinearVelocity = Vector3.Zero;
		AngularVelocity = Vector3.Zero;
		SetResultTextVisible(null);

		PlayResultPresentation(side);
	}

	private void PlayResultPresentation(CoinSide side)
	{
		_resultTween?.Kill();

		if (!TryGetPresentationCamera(out Camera3D camera))
		{
			_resultTween = CreateTween();
			_resultTween.TweenCallback(Callable.From(() => SetResultTextVisible(side)));
			_resultTween.TweenInterval(Mathf.Max(ResultHoldDuration, 0f));
			_resultTween.TweenCallback(Callable.From(() => CompleteResultPresentation(side)));
			return;
		}

		Transform3D targetTransform = BuildResultPresentationTransform(camera, side);
		_resultTween = CreateTween();
		_resultTween.TweenProperty(this, "global_transform", targetTransform, Mathf.Max(ResultMoveDuration, 0f))
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
		_resultTween.TweenCallback(Callable.From(() => SetResultTextVisible(side)));
		_resultTween.TweenInterval(Mathf.Max(ResultHoldDuration, 0f));
		_resultTween.TweenCallback(Callable.From(() => CompleteResultPresentation(side)));
	}

	private Transform3D BuildResultPresentationTransform(Camera3D camera, CoinSide side)
	{
		Vector3 cameraForward = (-camera.GlobalBasis.Z).Normalized();
		Vector3 cameraUp = camera.GlobalBasis.Y.Normalized();
		Vector3 cameraRight = camera.GlobalBasis.X.Normalized();
		Vector3 targetPosition = camera.GlobalPosition + cameraForward * ResultDistanceFromCamera + cameraUp * ResultHeightOffset;
		Vector3 toCameraDirection = (camera.GlobalPosition - targetPosition).Normalized();
		Vector3 targetYAxis = side == CoinSide.Front ? toCameraDirection : -toCameraDirection;
		Vector3 screenDown = -cameraUp;
		Vector3 targetZAxis = screenDown - targetYAxis * screenDown.Dot(targetYAxis);

		if (targetZAxis.LengthSquared() <= 0.0001f)
		{
			targetZAxis = cameraForward - targetYAxis * cameraForward.Dot(targetYAxis);
		}

		if (targetZAxis.LengthSquared() <= 0.0001f)
		{
			targetZAxis = cameraRight - targetYAxis * cameraRight.Dot(targetYAxis);
		}

		targetZAxis = targetZAxis.Normalized();
		Vector3 targetXAxis = targetYAxis.Cross(targetZAxis);

		if (targetXAxis.LengthSquared() <= 0.0001f)
		{
			targetXAxis = cameraRight - targetYAxis * cameraRight.Dot(targetYAxis);
		}

		targetXAxis = targetXAxis.Normalized();
		targetZAxis = targetXAxis.Cross(targetYAxis).Normalized();
		Basis targetBasis = new Basis(targetXAxis, targetYAxis, targetZAxis).Orthonormalized();
		return new Transform3D(targetBasis, targetPosition);
	}

	private bool TryGetPresentationCamera(out Camera3D camera)
	{
		camera = GetViewport()?.GetCamera3D();
		return camera != null;
	}

	private void SetResultTextVisible(CoinSide? side)
	{
		if (FrontResultText != null)
		{
			FrontResultText.Visible = side == CoinSide.Front;
		}

		if (BackResultText != null)
		{
			BackResultText.Visible = side == CoinSide.Back;
		}
	}

	private void CompleteResultPresentation(CoinSide side)
	{
		Action<CoinSide> callback = _onFinished;
		ResetToIdle();
		callback?.Invoke(side);
	}
}