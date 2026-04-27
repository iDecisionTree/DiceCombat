using Godot;

namespace DiceCombat.scripts.ui;

[GlobalClass]
public partial class SkyboxRotator : Camera3D
{
	[Export]
	public float RotationSpeed { get; set; } = 0.8f;

	public override void _Ready()
	{
		Rotation = new Vector3(Rotation.X, Mathf.DegToRad((float)GD.RandRange(0.0, 360.0)), Rotation.Z);
	}

	public override void _Process(double delta)
	{
		RotateY(Mathf.DegToRad(RotationSpeed) * (float)delta);
	}
}
