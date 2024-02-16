using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.MsQuic.Internal;

internal static class MsQuicTraceHelper
{
	internal static string GetTraceId(SafeMsQuicStreamHandle handle)
	{
		return "[strm][0x" + GetIntPtrHex(handle) + "]";
	}

	internal static string GetTraceId(SafeMsQuicConnectionHandle handle)
	{
		return "[conn][0x" + GetIntPtrHex(handle) + "]";
	}

	internal static string GetTraceId(SafeMsQuicListenerHandle handle)
	{
		return "[list][0x" + GetIntPtrHex(handle) + "]";
	}

	private static string GetIntPtrHex(SafeHandle handle)
	{
		return handle.DangerousGetHandle().ToString("X11");
	}
}
