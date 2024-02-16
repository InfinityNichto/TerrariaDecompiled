using System;
using System.Runtime.InteropServices;

namespace Internal.Cryptography;

internal struct PinAndClear : IDisposable
{
	private byte[] _data;

	private GCHandle _gcHandle;

	internal static PinAndClear Track(byte[] data)
	{
		PinAndClear result = default(PinAndClear);
		result._gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
		result._data = data;
		return result;
	}

	public void Dispose()
	{
		Array.Clear(_data);
		_gcHandle.Free();
	}
}
