using System;

namespace Microsoft.Xna.Framework;

[Flags]
internal enum XnaImageOperation
{
	Nothing = 0,
	Scale = 1,
	Crop = 2
}
