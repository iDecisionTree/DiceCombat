using Godot;
using DiceCombat.scripts.card;

namespace DiceCombat.scripts.state_machine;

[GlobalClass]
[Tool]
public partial class CombatCameraDirector3D : CombatCameraDirector
{
	[Export] public Camera3D Camera { get; set; }
	[Export] public Node3D PlayerFocusTarget { get; set; }
	[Export] public Node3D EnemyFocusTarget { get; set; }

	[ExportGroup("Shot Settings")]
	[Export] public float MoveDuration { get; set; } = 0.45f;
	[Export] public float ResolutionFov { get; set; } = 48f;
	[Export] public Vector3 ResolutionOffset { get; set; } = new Vector3(0f, 14f, 10f);

	private Vector3 _homePosition;
	private Vector3 _homeRotationDegrees;
	private float _homeFov = 60f;
	private bool _hasCachedHomeTransform;

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


