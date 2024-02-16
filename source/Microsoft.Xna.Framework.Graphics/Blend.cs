using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Xna.Framework.Graphics;

[SuppressMessage("Microsoft.Naming", "CA1712:DoNotPrefixEnumValuesWithTypeName")]
public enum Blend
{
	One,
	Zero,
	SourceColor,
	InverseSourceColor,
	SourceAlpha,
	InverseSourceAlpha,
	DestinationColor,
	InverseDestinationColor,
	DestinationAlpha,
	InverseDestinationAlpha,
	BlendFactor,
	InverseBlendFactor,
	SourceAlphaSaturation
}
