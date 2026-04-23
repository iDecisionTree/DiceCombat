using Godot;

namespace DiceCombat.scripts.ui;

[GlobalClass]
[Tool]
public partial class ScaledThemeButton : Button
{
    [Export] public bool AdjustCornerRadius { get; set; }
	[Export] public Vector4 CornerRadiusFactor { get; set; }
	[Export] public bool AdjustBorder { get; set; }
	[Export] public Vector4 BorderFactor { get; set; }
	[Export] public string[] StyleBoxNames { get; set; }
	[Export] public bool AdjustFontSize { get; set; }
	[Export] public float FontSizeFactor { get; set; }
	[Export] public string[] FontSizeNames { get; set; }

	public override void _Ready()
	{
		Resized += OnResized;
		OnResized();
	}
	
	private void OnResized()
	{
		if (AdjustCornerRadius || AdjustBorder)
		{
			foreach (string name in StyleBoxNames)
			{
				StyleBox sb = GetTargetStyleBox(name);
				if (sb is StyleBoxFlat flat)
				{
					StyleBoxFlat newFlat = flat.Duplicate() as StyleBoxFlat;
					if (newFlat == null)
					{
						continue;
					}
					
					float baseSize = Mathf.Min(Size.X, Size.Y);

					if (AdjustCornerRadius)
					{
						newFlat.CornerRadiusTopLeft = (int)(baseSize * CornerRadiusFactor.X);
						newFlat.CornerRadiusTopRight = (int)(baseSize * CornerRadiusFactor.Y);
						newFlat.CornerRadiusBottomLeft = (int)(baseSize * CornerRadiusFactor.Z);
						newFlat.CornerRadiusBottomRight = (int)(baseSize * CornerRadiusFactor.W);
					}

					if (AdjustBorder)
					{
						newFlat.BorderWidthLeft = (int)(baseSize * BorderFactor.X);
						newFlat.BorderWidthTop = (int)(baseSize * BorderFactor.Y);
						newFlat.BorderWidthRight = (int)(baseSize * BorderFactor.Z);
						newFlat.BorderWidthBottom = (int)(baseSize * BorderFactor.W);
					}

					AddThemeStyleboxOverride(name, newFlat);
				}
			}
		}

		if (AdjustFontSize)
		{
			int fontSize = (int)(Size.Y * FontSizeFactor);
			foreach (string name in FontSizeNames)
			{
				AddThemeFontSizeOverride(name, fontSize);
			}
		}
	}
	
	private StyleBox GetTargetStyleBox(string styleBoxName)
	{
		if (!string.IsNullOrEmpty(styleBoxName))
		{
			StyleBox sb = GetThemeStylebox(styleBoxName);
			if (sb != null && (sb is StyleBoxFlat || sb is StyleBoxTexture))
			{
				return sb;
			}
		}

		return null;
	}
}
