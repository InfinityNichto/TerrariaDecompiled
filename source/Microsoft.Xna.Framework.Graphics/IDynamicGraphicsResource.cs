using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

internal interface IDynamicGraphicsResource
{
	bool IsContentLost
	{
		[return: MarshalAs(UnmanagedType.U1)]
		get;
	}

	[SpecialName]
	event EventHandler<EventArgs> ContentLost;

	void SetContentLost([MarshalAs(UnmanagedType.U1)] bool isContentLost);
}
