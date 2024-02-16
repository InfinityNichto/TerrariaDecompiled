using System;

namespace Microsoft.Xna.Framework.Graphics;

[Flags]
public enum ClearOptions
{
	Target = 1,
	DepthBuffer = 2,
	Stencil = 4
}
