using Godot;

namespace DiceCombat.scripts.ui;

[GlobalClass]
public partial class RadialBlurController : ColorRect
{
	[Export] public float PeakStrength { get; set; } = 0.25f;
	[Export] public float FadeInDuration { get; set; } = 0.1f;
	[Export] public float HoldDuration { get; set; } = 0.2f;
	[Export] public float FadeOutDuration { get; set; } = 0.4f;

	private ShaderMaterial _material;
	private Tween _tween;

	public override void _Ready()
	{
		_material = Material as ShaderMaterial;
		Visible = false;
	}

	public void PlayImpactBlur()
	{
		_tween?.Kill();
		Visible = true;
		SetBlur(0f);

		_tween = CreateTween();
		_tween.SetParallel(false);
		_tween.TweenMethod(Callable.From<float>(SetBlur), 0f, PeakStrength, FadeInDuration);
		_tween.TweenInterval(HoldDuration);
		_tween.TweenMethod(Callable.From<float>(SetBlur), PeakStrength, 0f, FadeOutDuration);
		_tween.TweenCallback(Callable.From(() => Visible = false));
	}

	private void SetBlur(float value)
	{
		if (_material != null)
		{
			_material.SetShaderParameter("blur_strength", value);
		}
	}
}
