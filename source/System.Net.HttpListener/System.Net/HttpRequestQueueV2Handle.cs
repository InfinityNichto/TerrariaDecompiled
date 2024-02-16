using Microsoft.Win32.SafeHandles;

namespace System.Net;

internal sealed class HttpRequestQueueV2Handle : SafeHandleZeroOrMinusOneIsInvalid
{
	public HttpRequestQueueV2Handle()
		: base(ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		return global::Interop.HttpApi.HttpCloseRequestQueue(handle) == 0;
	}
}
