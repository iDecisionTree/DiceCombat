using Godot;
using DiceCombat.scripts.ui;

namespace DiceCombat.scripts.eff;

[GlobalClass]
public partial class PlayerKOEffect : Node3D
{
	[Export] public RadialBlurController BlurController { get; set; }

	private AnimationPlayer _animationPlayer;

	public override void _Ready()
	{
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_animationPlayer.AnimationStarted += OnAnimationStarted;
	}

	private void OnAnimationStarted(StringName name)
	{
		if (name == "ko")
		{
			BlurController?.PlayImpactBlur();
		}
	}
}
