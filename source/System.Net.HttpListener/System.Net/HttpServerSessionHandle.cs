using Microsoft.Win32.SafeHandles;

namespace System.Net;

internal sealed class HttpServerSessionHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	private readonly ulong _serverSessionId;

	internal HttpServerSessionHandle(ulong id)
		: base(ownsHandle: true)
	{
		_serverSessionId = id;
		SetHandle(new IntPtr(1));
	}

	internal ulong DangerousGetServerSessionId()
	{
		return _serverSessionId;
	}

	protected override bool ReleaseHandle()
	{
		return global::Interop.HttpApi.HttpCloseServerSession(_serverSessionId) == 0;
	}
}
