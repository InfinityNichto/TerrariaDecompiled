using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net;

internal sealed class HttpResponseStreamAsyncResult : System.Net.LazyAsyncResult
{
	private readonly ThreadPoolBoundHandle _boundHandle;

	internal unsafe NativeOverlapped* _pOverlapped;

	private readonly global::Interop.HttpApi.HTTP_DATA_CHUNK[] _dataChunks;

	internal bool _sentHeaders;

	private unsafe static readonly IOCompletionCallback s_IOCallback = Callback;

	private static readonly byte[] s_CRLFArray = new byte[2] { 13, 10 };

	internal ushort dataChunkCount
	{
		get
		{
			if (_dataChunks == null)
			{
				return 0;
			}
			return (ushort)_dataChunks.Length;
		}
	}

	internal unsafe global::Interop.HttpApi.HTTP_DATA_CHUNK* pDataChunks
	{
		get
		{
			if (_dataChunks == null)
			{
				return null;
			}
			return (global::Interop.HttpApi.HTTP_DATA_CHUNK*)(void*)Marshal.UnsafeAddrOfPinnedArrayElement(_dataChunks, 0);
		}
	}

	internal HttpResponseStreamAsyncResult(object asyncObject, object userState, AsyncCallback callback)
		: base(asyncObject, userState, callback)
	{
	}

	private static byte[] GetChunkHeader(int size, out int offset)
	{
		uint num = 4026531840u;
		byte[] array = new byte[10];
		offset = -1;
		int num2 = 0;
		while (num2 < 8)
		{
			if (offset != -1 || (size & num) != 0L)
			{
				uint num3 = (uint)size >> 28;
				if (num3 < 10)
				{
					array[num2] = (byte)(num3 + 48);
				}
				else
				{
					array[num2] = (byte)(num3 - 10 + 65);
				}
				if (offset == -1)
				{
					offset = num2;
				}
			}
			num2++;
			size <<= 4;
		}
		array[8] = 13;
		array[9] = 10;
		return array;
	}

	internal unsafe HttpResponseStreamAsyncResult(object asyncObject, object userState, AsyncCallback callback, byte[] buffer, int offset, int size, bool chunked, bool sentHeaders, ThreadPoolBoundHandle boundHandle)
		: base(asyncObject, userState, callback)
	{
		_boundHandle = boundHandle;
		_sentHeaders = sentHeaders;
		if (size == 0)
		{
			_dataChunks = null;
			_pOverlapped = boundHandle.AllocateNativeOverlapped(s_IOCallback, this, null);
			return;
		}
		_dataChunks = new global::Interop.HttpApi.HTTP_DATA_CHUNK[(!chunked) ? 1 : 3];
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, "m_pOverlapped:0x" + ((IntPtr)_pOverlapped).ToString("x8"), ".ctor");
		}
		object[] array = new object[1 + _dataChunks.Length];
		array[_dataChunks.Length] = _dataChunks;
		int offset2 = 0;
		byte[] array2 = null;
		if (chunked)
		{
			array2 = GetChunkHeader(size, out offset2);
			_dataChunks[0] = default(global::Interop.HttpApi.HTTP_DATA_CHUNK);
			_dataChunks[0].DataChunkType = global::Interop.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
			_dataChunks[0].BufferLength = (uint)(array2.Length - offset2);
			array[0] = array2;
			_dataChunks[1] = default(global::Interop.HttpApi.HTTP_DATA_CHUNK);
			_dataChunks[1].DataChunkType = global::Interop.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
			_dataChunks[1].BufferLength = (uint)size;
			array[1] = buffer;
			_dataChunks[2] = default(global::Interop.HttpApi.HTTP_DATA_CHUNK);
			_dataChunks[2].DataChunkType = global::Interop.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
			_dataChunks[2].BufferLength = (uint)s_CRLFArray.Length;
			array[2] = s_CRLFArray;
		}
		else
		{
			_dataChunks[0] = default(global::Interop.HttpApi.HTTP_DATA_CHUNK);
			_dataChunks[0].DataChunkType = global::Interop.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
			_dataChunks[0].BufferLength = (uint)size;
			array[0] = buffer;
		}
		_pOverlapped = boundHandle.AllocateNativeOverlapped(s_IOCallback, this, array);
		if (chunked)
		{
			_dataChunks[0].pBuffer = (byte*)(void*)Marshal.UnsafeAddrOfPinnedArrayElement(array2, offset2);
			_dataChunks[1].pBuffer = (byte*)(void*)Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset);
			_dataChunks[2].pBuffer = (byte*)(void*)Marshal.UnsafeAddrOfPinnedArrayElement(s_CRLFArray, 0);
		}
		else
		{
			_dataChunks[0].pBuffer = (byte*)(void*)Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset);
		}
	}

	internal void IOCompleted(uint errorCode, uint numBytes)
	{
		IOCompleted(this, errorCode, numBytes);
	}

	private unsafe static void IOCompleted(HttpResponseStreamAsyncResult asyncResult, uint errorCode, uint numBytes)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(null, $"errorCode:0x{errorCode:x8} numBytes: {numBytes}", "IOCompleted");
		}
		object obj = null;
		try
		{
			if (errorCode != 0 && errorCode != 38)
			{
				asyncResult.ErrorCode = (int)errorCode;
				obj = new HttpListenerException((int)errorCode);
			}
			else if (asyncResult._dataChunks == null)
			{
				obj = 0u;
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.DumpBuffer(null, IntPtr.Zero, 0, "IOCompleted");
				}
			}
			else
			{
				obj = ((asyncResult._dataChunks.Length == 1) ? asyncResult._dataChunks[0].BufferLength : 0u);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					for (int i = 0; i < asyncResult._dataChunks.Length; i++)
					{
						System.Net.NetEventSource.DumpBuffer(null, (IntPtr)asyncResult._dataChunks[0].pBuffer, (int)asyncResult._dataChunks[0].BufferLength, "IOCompleted");
					}
				}
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(null, "Calling Complete()", "IOCompleted");
			}
		}
		catch (Exception ex)
		{
			obj = ex;
		}
		asyncResult.InvokeCallback(obj);
	}

	private unsafe static void Callback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
	{
		object nativeOverlappedState = ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped);
		HttpResponseStreamAsyncResult asyncResult = nativeOverlappedState as HttpResponseStreamAsyncResult;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(null, "errorCode:0x" + errorCode.ToString("x8") + " numBytes:" + numBytes + " nativeOverlapped:0x" + ((IntPtr)nativeOverlapped).ToString("x8"), "Callback");
		}
		IOCompleted(asyncResult, errorCode, numBytes);
	}

	protected unsafe override void Cleanup()
	{
		base.Cleanup();
		if (_pOverlapped != null)
		{
			_boundHandle.FreeNativeOverlapped(_pOverlapped);
		}
	}
}
