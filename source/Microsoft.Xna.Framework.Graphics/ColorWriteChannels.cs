using System;

namespace Microsoft.Xna.Framework.Graphics;

[Flags]
public enum ColorWriteChannels
{
	None = 0,
	Red = 1,
	Green = 2,
	Blue = 4,
	Alpha = 8,
	All = 0xF
}
