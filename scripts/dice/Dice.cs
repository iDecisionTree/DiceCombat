using Godot;

namespace DiceCombat.scripts.dice;

[GlobalClass]
[Tool]
public partial class Dice : RigidBody3D
{
    [Signal] public delegate void RollFinishedEventHandler(int num);
    [Signal] public delegate void SelectedEventHandler(Dice dice);
    
    [Export] public DiceType DiceType { get; set; }

    [ExportGroup("Face Marker")]
    [Export] public Node3D Face1 { get; set; }
    [Export] public Node3D Face2 { get; set; }
    [Export] public Node3D Face3 { get; set; }
    [Export] public Node3D Face4 { get; set; }
    [Export] public Node3D Face5 { get; set; }
    [Export] public Node3D Face6 { get; set; }
    [Export] public Node3D Face7 { get; set; }
    [Export] public Node3D Face8 { get; set; }
    [Export] public Node3D Face9 { get; set; }
    [Export] public Node3D Face10 { get; set; }
    [Export] public Node3D Face11 { get; set; }
    [Export] public Node3D Face12 { get; set; }
    
    [ExportGroup("Outline Setting")]
    [Export] public MeshInstance3D Outline { get; set; }
    [Export] public float OutlineWidth { get; set; }
    [Export] public Color OutlineColor { get; set; }

    public DiceData DiceData { get; set; }
    
    public bool CanBeSelected { get; set; }
    public bool IsSelected { get; private set; }

    public int CurrentFace => _currentFace;

    private int _currentFace = 1;
    private bool _isRolling = false;
    private float _rollElapsed;
    private float _stableElapsed;
    private float _stableTimeRequired = 0.2f;
    private float _maxRollTime = 6.0f;
    private float _linearSleepThreshold = 0.05f;
    private float _angularSleepThreshold = 0.2f;

	public override void _Ready()
	{
        EnsureVisualMesh();
        EnsureCollisionShape();
        SetSelected(false);
    }

    public override void _PhysicsProcess(double delta)
	{
	    if (!_isRolling)
	    {
	        return;
	    }

	    _rollElapsed += (float)delta;

        bool isSettled = LinearVelocity.LengthSquared() <= _linearSleepThreshold * _linearSleepThreshold && AngularVelocity.LengthSquared() <= _angularSleepThreshold * _angularSleepThreshold;

	    if (isSettled || Sleeping)
	    {
	        _stableElapsed += (float)delta;
	    }
	    else
	    {
	        _stableElapsed = 0f;
	    }

        if (_stableElapsed >= _stableTimeRequired || _rollElapsed >= _maxRollTime)
	    {
	        FinishRoll(ResolveFaceFromWorldUp());
	    }
	}

    public override void _InputEvent(Camera3D camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, int shapeIdx)
    {
        if (!CanBeSelected)
        {
            return;
        }
        
        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
        {
            EmitSignal(SignalName.Selected, this);
        }
    }

    public void ConfigureRollTiming(float stableTimeRequired, float maxRollTime, float linearSleepThreshold, float angularSleepThreshold)
    {
        _stableTimeRequired = Mathf.Max(stableTimeRequired, 0f);
        _maxRollTime = Mathf.Max(maxRollTime, 0.01f);
        _linearSleepThreshold = Mathf.Max(linearSleepThreshold, 0f);
        _angularSleepThreshold = Mathf.Max(angularSleepThreshold, 0f);
    }

    public void BeginRollTracking()
    {
        _rollElapsed = 0f;
        _stableElapsed = 0f;
        _isRolling = true;
    }

    public void SetSelected(bool selected, bool animate = false)
    {
		if (IsSelected == selected && !animate)
		{
			return;
		}

		IsSelected = selected;
        GD.Print($"骰子选择状态: {selected}");

        if (Outline == null)
        {
            return;
        }

        ApplyOutlineMaterial(selected);
        Vector3 targetScale = selected ? new Vector3(1f + OutlineWidth, 1f + OutlineWidth, 1f + OutlineWidth) : Vector3.One;

        if (!animate)
        {
            Outline.Scale = targetScale;
            return;
        }

        Tween tween = CreateTween();
        tween.TweenProperty(Outline, "scale", targetScale, 0.12f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
    }

  private void ApplyOutlineMaterial(bool selected)
  {
    if (!selected)
    {
      return;
    }

    ShaderMaterial material = Outline.GetMaterialOverride() as ShaderMaterial;
    if (material != null)
    {
      material.SetShaderParameter("color", OutlineColor);
    }
  }

    public Transform3D GetPresentationTransform(Vector3 targetPosition)
    {
        int face = DiceData != null && DiceData.Num > 0 ? DiceData.Num : CurrentFace;
        Vector3 currentFaceDirection = GetWorldFaceDirection(face);
        Basis targetBasis = GlobalBasis;

        if (currentFaceDirection.LengthSquared() > 0.0001f)
        {
            targetBasis = GetAlignmentBasis(currentFaceDirection.Normalized(), Vector3.Up) * GlobalBasis;
        }

        return new Transform3D(targetBasis, targetPosition);
    }

    private Vector3 GetWorldFaceDirection(int face)
    {
        Node3D marker = GetFaceMarker(face);
        if (marker == null)
        {
            return Vector3.Up;
        }

        Vector3 direction = marker.GlobalPosition - GlobalPosition;
        return direction.LengthSquared() > 0.0001f ? direction : Vector3.Up;
    }

    private Basis GetAlignmentBasis(Vector3 fromDirection, Vector3 toDirection)
    {
        Vector3 from = fromDirection.Normalized();
        Vector3 to = toDirection.Normalized();
        float dot = Mathf.Clamp(from.Dot(to), -1f, 1f);

        if (dot > 0.9999f)
        {
            return Basis.Identity;
        }

        if (dot < -0.9999f)
        {
            Vector3 axis = from.Cross(Vector3.Right);
            if (axis.LengthSquared() < 0.0001f)
            {
                axis = from.Cross(Vector3.Forward);
            }

            return new Basis(axis.Normalized(), Mathf.Pi);
        }

        Vector3 rotationAxis = from.Cross(to).Normalized();
        float angle = Mathf.Acos(dot);
        return new Basis(rotationAxis, angle);
    }

    private void FinishRoll(int num)
    {
        if (!_isRolling)
        {
            return;
        }

        _isRolling = false;
        Freeze = true;
        LinearVelocity = Vector3.Zero;
        AngularVelocity = Vector3.Zero;
        _currentFace = ClampFace(num);
        EmitSignal(SignalName.RollFinished, _currentFace);
    }

    private int ClampFace(int num)
    {
        int maxFace = GetMaxFaceCount();

        if (num < 1)
        {
            return 1;
        }

        if (num > maxFace)
        {
            return maxFace;
        }

        return num;
    }

    private int ResolveFaceFromWorldUp()
    {
        int maxFace = GetMaxFaceCount();
        int bestFace = ClampFace(_currentFace);
        float bestHeight = DiceType == DiceType.Dice4 ? float.PositiveInfinity : float.NegativeInfinity;

        for (int face = 1; face <= maxFace; face++)
        {
            Node3D marker = GetFaceMarker(face);
            if (marker == null)
            {
                continue;
            }

            float height = marker.GlobalPosition.Y;
            bool isBetter = DiceType == DiceType.Dice4 ? height < bestHeight : height > bestHeight;

            if (isBetter)
            {
                bestHeight = height;
                bestFace = face;
            }
        }

        return bestHeight > float.NegativeInfinity ? bestFace : ClampFace(_currentFace);
    }

    private int GetMaxFaceCount()
    {
        return DiceType switch
        {
            DiceType.Dice4 => 4,
            DiceType.Dice6 => 6,
            DiceType.Dice8 => 8,
            DiceType.Dice12 => 12,
            _ => 6
        };
    }

    private Node3D GetFaceMarker(int face)
    {
        return face switch
        {
            1 => Face1,
            2 => Face2,
            3 => Face3,
            4 => Face4,
            5 => Face5,
            6 => Face6,
            7 => Face7,
            8 => Face8,
            9 => Face9,
            10 => Face10,
            11 => Face11,
            12 => Face12,
            _ => null
        };
    }


    private void EnsureVisualMesh()
    {
        MeshInstance3D mesh = FindAnyMeshInstance(this);
        if (mesh != null)
        {
            return;
        }

        mesh = new MeshInstance3D
        {
            Name = "VisualMesh",
            Mesh = new BoxMesh
            {
                Size = Vector3.One
            }
        };
        AddChild(mesh);
    }

    private void EnsureCollisionShape()
    {
        if (FindAnyCollisionShape(this) != null)
        {
            return;
        }

        MeshInstance3D meshInstance = FindAnyMeshInstance(this);
        Shape3D shape = null;

        if (meshInstance?.Mesh != null)
        {
            shape = meshInstance.Mesh.CreateConvexShape();
        }

        if (shape == null)
        {
            shape = new BoxShape3D
            {
                Size = Vector3.One
            };
        }

        CollisionShape3D collisionShape = new CollisionShape3D
        {
            Name = "CollisionShape3D",
            Shape = shape
        };
        AddChild(collisionShape);
    }

    private MeshInstance3D FindAnyMeshInstance(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is MeshInstance3D meshInstance)
            {
                return meshInstance;
            }

            MeshInstance3D nested = FindAnyMeshInstance(child);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private CollisionShape3D FindAnyCollisionShape(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is CollisionShape3D collisionShape)
            {
                return collisionShape;
            }

            CollisionShape3D nested = FindAnyCollisionShape(child);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
}

