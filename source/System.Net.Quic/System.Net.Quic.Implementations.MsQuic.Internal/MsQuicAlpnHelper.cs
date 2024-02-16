using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Security;

namespace System.Net.Quic.Implementations.MsQuic.Internal;

internal static class MsQuicAlpnHelper
{
	public unsafe static void Prepare(List<SslApplicationProtocol> alpnProtocols, [NotNull] out MemoryHandle[] handles, [NotNull] out MsQuicNativeMethods.QuicBuffer[] buffers)
	{
		handles = ArrayPool<MemoryHandle>.Shared.Rent(alpnProtocols.Count);
		buffers = ArrayPool<MsQuicNativeMethods.QuicBuffer>.Shared.Rent(alpnProtocols.Count);
		try
		{
			for (int i = 0; i < alpnProtocols.Count; i++)
			{
				ReadOnlyMemory<byte> protocol = alpnProtocols[i].Protocol;
				MemoryHandle memoryHandle = protocol.Pin();
				handles[i] = memoryHandle;
				buffers[i].Buffer = (byte*)memoryHandle.Pointer;
				buffers[i].Length = (uint)protocol.Length;
			}
		}
		catch
		{
			Return(ref handles, ref buffers);
			throw;
		}
	}

	public static void Return(ref MemoryHandle[] handles, ref MsQuicNativeMethods.QuicBuffer[] buffers)
	{
		MemoryHandle[] array = handles;
		if (array != null)
		{
			MemoryHandle[] array2 = array;
			foreach (MemoryHandle memoryHandle in array2)
			{
				memoryHandle.Dispose();
			}
			handles = null;
			ArrayPool<MemoryHandle>.Shared.Return(array);
		}
		MsQuicNativeMethods.QuicBuffer[] array3 = buffers;
		if (array3 != null)
		{
			buffers = null;
			ArrayPool<MsQuicNativeMethods.QuicBuffer>.Shared.Return(array3);
		}
	}
}
