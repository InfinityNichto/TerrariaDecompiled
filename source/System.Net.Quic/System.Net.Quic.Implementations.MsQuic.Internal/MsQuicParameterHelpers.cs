using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.MsQuic.Internal;

internal static class MsQuicParameterHelpers
{
	internal unsafe static MsQuicNativeMethods.SOCKADDR_INET GetINetParam(MsQuicApi api, SafeHandle nativeObject, QUIC_PARAM_LEVEL level, uint param)
	{
		uint bufferLength = (uint)sizeof(MsQuicNativeMethods.SOCKADDR_INET);
		Unsafe.SkipInit(out MsQuicNativeMethods.SOCKADDR_INET result);
		uint status = api.GetParamDelegate(nativeObject, level, param, ref bufferLength, (byte*)(&result));
		QuicExceptionHelpers.ThrowIfFailed(status, "GetINETParam failed.");
		return result;
	}

	internal unsafe static ushort GetUShortParam(MsQuicApi api, SafeHandle nativeObject, QUIC_PARAM_LEVEL level, uint param)
	{
		uint bufferLength = 2u;
		Unsafe.SkipInit(out ushort result);
		uint status = api.GetParamDelegate(nativeObject, level, param, ref bufferLength, (byte*)(&result));
		QuicExceptionHelpers.ThrowIfFailed(status, "GetUShortParam failed.");
		return result;
	}

	internal unsafe static ulong GetULongParam(MsQuicApi api, SafeHandle nativeObject, QUIC_PARAM_LEVEL level, uint param)
	{
		uint bufferLength = 8u;
		Unsafe.SkipInit(out ulong result);
		uint status = api.GetParamDelegate(nativeObject, level, param, ref bufferLength, (byte*)(&result));
		QuicExceptionHelpers.ThrowIfFailed(status, "GetULongParam failed.");
		return result;
	}
}
