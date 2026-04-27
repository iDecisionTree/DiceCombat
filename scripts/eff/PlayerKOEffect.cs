using Godot;
using DiceCombat.scripts.ui;

namespace DiceCombat.scripts.eff;

[GlobalClass]
public partial class PlayerKOEffect : Node3D
{
	[Export] public RadialBlurController BlurController { get; set; }
	[Export] public Node3D FocusTarget { get; set; }

	private AnimationPlayer _animationPlayer;

	public PlayerKOEffect()
	{
	}

	public override void _Ready()
	{
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
	}

	/// <summary>
	/// 由动画轨道 call_method 触发，K 砸落地面的瞬间调用。
	/// </summary>
	private void TriggerImpactBlur()
	{
		BlurController?.PlayImpactBlur();
	}

	public Vector3 GetFocusPoint()
	{
		if (FocusTarget != null)
		{
			return FocusTarget.GlobalPosition;
		}

		return GlobalPosition;
	}
}
