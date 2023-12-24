using System;

namespace ReLogic.Text;

public struct GlyphMetrics
{
	public readonly float LeftPadding;

	public readonly float CharacterWidth;

	public readonly float RightPadding;

	public float KernedWidth => LeftPadding + CharacterWidth + RightPadding;

	public float KernedWidthOnNewLine => Math.Max(0f, LeftPadding) + CharacterWidth + RightPadding;

	private GlyphMetrics(float leftPadding, float characterWidth, float rightPadding)
	{
		LeftPadding = leftPadding;
		CharacterWidth = characterWidth;
		RightPadding = rightPadding;
	}

	public static GlyphMetrics FromKerningData(float leftPadding, float characterWidth, float rightPadding)
	{
		return new GlyphMetrics(leftPadding, characterWidth, rightPadding);
	}
}
