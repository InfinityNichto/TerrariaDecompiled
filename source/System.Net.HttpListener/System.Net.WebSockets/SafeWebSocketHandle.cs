using Microsoft.Win32.SafeHandles;

namespace System.Net.WebSockets;

internal sealed class SafeWebSocketHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	public SafeWebSocketHandle()
		: base(ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		if (IsInvalid)
		{
			return true;
		}
		WebSocketProtocolComponent.WebSocketDeleteHandle(handle);
		return true;
	}
}
