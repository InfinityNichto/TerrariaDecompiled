using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public class IndexBuffer : GraphicsResource, IGraphicsResource
{
	private protected uint _usage;

	private protected uint _indexCount;

	private protected uint _indexSize;

	private protected uint _bufferSize;

	private protected uint _pool;

	private unsafe void* pBufferData;

	internal unsafe IDirect3DIndexBuffer9* pComPtr;

	internal bool IsWriteOnly
	{
		[return: MarshalAs(UnmanagedType.U1)]
		get
		{
			return (_usage & 8) == 8;
		}
	}

	public int IndexCount => (int)_indexCount;

	public IndexElementSize IndexElementSize => (_indexSize != 2) ? IndexElementSize.ThirtyTwoBits : IndexElementSize.SixteenBits;

	public BufferUsage BufferUsage => _003CModule_003E.ConvertDxBufferUsageToXna(_usage);

	private unsafe IndexBuffer(IDirect3DIndexBuffer9* pInterface, GraphicsDevice pDevice)
	{
		pComPtr = pInterface;
		((object)this)._002Ector();
		try
		{
			_parent = pDevice;
			return;
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
	}

	public unsafe IndexBuffer(GraphicsDevice graphicsDevice, Type indexType, int indexCount, BufferUsage usage)
	{
		try
		{
			if (indexCount <= 0)
			{
				throw new ArgumentOutOfRangeException("indexCount", FrameworkResources.ResourcesMustBeGreaterThanZeroSize);
			}
			_parent = graphicsDevice;
			uint indexSize = (uint)Marshal.SizeOf(indexType);
			CreateBuffer((uint)indexCount, indexSize, _003CModule_003E.ConvertXnaBufferUsageToDx(usage), (_D3DPOOL)1);
			graphicsDevice.Resources.AddTrackedObject(this, pComPtr, 1u, _internalHandle, ref _internalHandle);
			return;
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
	}

	public unsafe IndexBuffer(GraphicsDevice graphicsDevice, IndexElementSize indexElementSize, int indexCount, BufferUsage usage)
	{
		try
		{
			if (indexCount <= 0)
			{
				throw new ArgumentOutOfRangeException("indexCount", FrameworkResources.ResourcesMustBeGreaterThanZeroSize);
			}
			_parent = graphicsDevice;
			int indexSize = ((indexElementSize == IndexElementSize.SixteenBits) ? 2 : 4);
			CreateBuffer((uint)indexCount, (uint)indexSize, _003CModule_003E.ConvertXnaBufferUsageToDx(usage), (_D3DPOOL)1);
			graphicsDevice.Resources.AddTrackedObject(this, pComPtr, 1u, _internalHandle, ref _internalHandle);
			return;
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
	}

	private protected IndexBuffer()
	{
	}

	public void SetData<T>(T[] data) where T : struct
	{
		int elementCount = ((data != null) ? data.Length : 0);
		SetData(0, data, 0, elementCount);
	}

	public void SetData<T>(T[] data, int startIndex, int elementCount) where T : struct
	{
		SetData(0, data, startIndex, elementCount);
	}

	public void SetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount) where T : struct
	{
		CopyData(offsetInBytes, data, startIndex, elementCount, 0u, isSetting: true);
	}

	public void GetData<T>(T[] data) where T : struct
	{
		int elementCount = ((data != null) ? data.Length : 0);
		GetData(0, data, 0, elementCount);
	}

	public void GetData<T>(T[] data, int startIndex, int elementCount) where T : struct
	{
		GetData(0, data, startIndex, elementCount);
	}

	public void GetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount) where T : struct
	{
		CopyData(offsetInBytes, data, startIndex, elementCount, 16u, isSetting: false);
	}

	private protected unsafe void CopyData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, uint options, [MarshalAs(UnmanagedType.U1)] bool isSetting) where T : struct
	{
		IntPtr intPtr = (IntPtr)pComPtr;
		Helpers.CheckDisposed(this, intPtr);
		if (data != null && data.Length != 0)
		{
			int num = -2147467259;
			if ((options & 0x3010) == 0 && _parent.Indices == this)
			{
				throw GraphicsHelpers.GetExceptionFromResult(2147500036u);
			}
			if (!isSetting && (_usage & 8) == 8)
			{
				throw new NotSupportedException(FrameworkResources.WriteOnlyGetNotSupported);
			}
			Helpers.ValidateCopyParameters(data.Length, startIndex, elementCount);
			try
			{
				uint num2 = (uint)(System.Runtime.CompilerServices.Unsafe.SizeOf<T>() * elementCount);
				if ((uint)((int)num2 + offsetInBytes) > _bufferSize)
				{
					throw new InvalidOperationException(FrameworkResources.ResourceDataMustBeCorrectSize);
				}
				if (_parent.IsDeviceLost)
				{
					if (isSetting)
					{
						return;
					}
					fixed (T* ptr = &data[startIndex])
					{
						try
						{
							// IL initblk instruction
							System.Runtime.CompilerServices.Unsafe.InitBlock(ptr, 0, num2);
						}
						catch
						{
							//try-fault
							ptr = null;
							throw;
						}
					}
					return;
				}
				void* ptr2 = null;
				byte* ptr3 = (byte*)pBufferData;
				if (IsWriteOnly && _pool == 1)
				{
					ptr3 = offsetInBytes + ptr3;
				}
				IDirect3DIndexBuffer9* ptr4 = pComPtr;
				num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, void**, uint, int>)(int)(*(uint*)(*(int*)ptr4 + 44)))((nint)ptr4, (uint)offsetInBytes, num2, &ptr2, options);
				if (num >= 0)
				{
					fixed (T* ptr5 = &data[startIndex])
					{
						try
						{
							if (isSetting)
							{
								_003CModule_003E.memcpy_s(ptr2, _bufferSize - (uint)offsetInBytes, ptr5, num2);
								if (IsWriteOnly && _pool == 1)
								{
									_003CModule_003E.memcpy_s(ptr3, _bufferSize - (uint)offsetInBytes, ptr5, num2);
								}
							}
							else
							{
								_003CModule_003E.memcpy_s(ptr5, num2, ptr2, num2);
							}
						}
						catch
						{
							//try-fault
							ptr5 = null;
							throw;
						}
					}
				}
			}
			finally
			{
				if (num >= 0)
				{
					IDirect3DIndexBuffer9* intPtr2 = pComPtr;
					num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr2 + 48)))((nint)intPtr2);
				}
			}
			if (num < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num);
			}
			if (isSetting && this is IDynamicGraphicsResource dynamicGraphicsResource)
			{
				dynamicGraphicsResource.SetContentLost(isContentLost: false);
			}
			return;
		}
		throw new ArgumentNullException("data", FrameworkResources.NullNotAllowed);
	}

	private protected unsafe void CreateBuffer(uint indexCount, uint indexSize, uint usage, _D3DPOOL pool)
	{
		isDisposed = false;
		_D3DFORMAT d3DFORMAT;
		if (indexSize == 4)
		{
			d3DFORMAT = (_D3DFORMAT)102;
			if (!_parent._profileCapabilities.IndexElementSize32)
			{
				_parent._profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileNoIndexElementSize32);
			}
		}
		else
		{
			if (indexSize != 2)
			{
				throw new ArgumentException(FrameworkResources.IndexBuffersMustBeSizedCorrectly);
			}
			d3DFORMAT = (_D3DFORMAT)101;
		}
		uint num = (_bufferSize = indexCount * indexSize);
		int maxIndexBufferSize = _parent._profileCapabilities.MaxIndexBufferSize;
		if (num > (uint)maxIndexBufferSize)
		{
			_parent._profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileTooBig, typeof(IndexBuffer).Name, maxIndexBufferSize);
		}
		fixed (IDirect3DIndexBuffer9** ptr = &pComPtr)
		{
			int num2 = *(int*)_parent.pComPtr + 108;
			int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, _D3DFORMAT, _D3DPOOL, IDirect3DIndexBuffer9**, void**, int>)(int)(*(uint*)num2))((nint)_parent.pComPtr, _bufferSize, _parent.GetBufferUsage(usage), d3DFORMAT, pool, ptr, null);
			if (num3 < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
			}
			_usage = usage;
			_indexCount = indexCount;
			_indexSize = indexSize;
			_pool = (uint)pool;
			if ((usage & 8) == 8 && pool == (_D3DPOOL)1 && (pBufferData = _003CModule_003E.new_005B_005D(_bufferSize)) == null)
			{
				throw new InsufficientMemoryException();
			}
		}
	}

	internal unsafe virtual int SaveDataForRecreation()
	{
		if (pComPtr == null)
		{
			return 0;
		}
		int num = 0;
		if ((_usage & 8) == 8)
		{
			goto IL_0088;
		}
		void* ptr = null;
		if ((pBufferData = _003CModule_003E.new_005B_005D(_bufferSize)) == null)
		{
			return -2147024882;
		}
		IDirect3DIndexBuffer9* ptr2 = pComPtr;
		num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, void**, uint, int>)(int)(*(uint*)(*(int*)ptr2 + 44)))((nint)ptr2, 0u, 0u, &ptr, 0u);
		if (num >= 0)
		{
			uint bufferSize = _bufferSize;
			_003CModule_003E.memcpy_s(pBufferData, bufferSize, ptr, bufferSize);
			ptr2 = pComPtr;
			IDirect3DIndexBuffer9* intPtr = ptr2;
			num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr + 48)))((nint)intPtr);
			if (num >= 0)
			{
				goto IL_0088;
			}
		}
		void* ptr3 = pBufferData;
		if (ptr3 != null)
		{
			_003CModule_003E.delete_005B_005D(ptr3);
			pBufferData = null;
		}
		goto IL_00a8;
		IL_0088:
		ReleaseNativeObject(disposeManagedResource: false);
		goto IL_00a8;
		IL_00a8:
		return num;
	}

	int IGraphicsResource.SaveDataForRecreation()
	{
		//ILSpy generated this explicit interface implementation from .override directive in SaveDataForRecreation
		return this.SaveDataForRecreation();
	}

	internal unsafe virtual int RecreateAndPopulateObject()
	{
		if (pComPtr != null)
		{
			return -2147467259;
		}
		if (pBufferData == null && _bufferSize != 0)
		{
			return -2147467259;
		}
		fixed (IDirect3DIndexBuffer9** ptr2 = &pComPtr)
		{
			_D3DFORMAT d3DFORMAT = ((_indexSize == 4) ? ((_D3DFORMAT)102) : ((_D3DFORMAT)101));
			GraphicsDevice parent = _parent;
			IDirect3DDevice9* ptr = parent.pComPtr;
			GraphicsDevice graphicsDevice = parent;
			int num = *(int*)ptr + 108;
			int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, _D3DFORMAT, _D3DPOOL, IDirect3DIndexBuffer9**, void**, int>)(int)(*(uint*)num))((nint)ptr, _bufferSize, graphicsDevice.GetBufferUsage(_usage), d3DFORMAT, (_D3DPOOL)1, ptr2, null);
			if (num2 >= 0)
			{
				void* ptr3 = null;
				IDirect3DIndexBuffer9* ptr4 = pComPtr;
				num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, void**, uint, int>)(int)(*(uint*)(*(int*)ptr4 + 44)))((nint)ptr4, 0u, 0u, &ptr3, 0u);
				if (num2 >= 0)
				{
					uint bufferSize = _bufferSize;
					_003CModule_003E.memcpy_s(ptr3, bufferSize, pBufferData, bufferSize);
					ptr4 = pComPtr;
					IDirect3DIndexBuffer9* intPtr = ptr4;
					num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr + 48)))((nint)intPtr);
				}
				if (!((_usage & 8) == 8))
				{
					void* ptr5 = pBufferData;
					if (ptr5 != null)
					{
						_003CModule_003E.delete_005B_005D(ptr5);
						pBufferData = null;
					}
				}
			}
			_parent.Resources.AddTrackedObject(this, pComPtr, 1u, _internalHandle, ref _internalHandle);
			return num2;
		}
	}

	int IGraphicsResource.RecreateAndPopulateObject()
	{
		//ILSpy generated this explicit interface implementation from .override directive in RecreateAndPopulateObject
		return this.RecreateAndPopulateObject();
	}

	internal unsafe virtual void ReleaseNativeObject([MarshalAs(UnmanagedType.U1)] bool disposeManagedResource)
	{
		GraphicsDevice parent = _parent;
		if (parent != null && pComPtr != null)
		{
			parent.Resources.ReleaseAllReferences(_internalHandle, disposeManagedResource);
		}
		pComPtr = null;
	}

	void IGraphicsResource.ReleaseNativeObject([MarshalAs(UnmanagedType.U1)] bool disposeManagedResource)
	{
		//ILSpy generated this explicit interface implementation from .override directive in ReleaseNativeObject
		this.ReleaseNativeObject(disposeManagedResource);
	}

	internal unsafe static IndexBuffer GetManagedObject(IDirect3DIndexBuffer9* pInterface, GraphicsDevice pDevice, uint pool)
	{
		IndexBuffer indexBuffer = pDevice.Resources.GetCachedObject(pInterface) as IndexBuffer;
		if (indexBuffer != null)
		{
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)pInterface + 8)))((nint)pInterface);
			indexBuffer.isDisposed = false;
			GC.ReRegisterForFinalize(indexBuffer);
		}
		else
		{
			indexBuffer = new IndexBuffer(pInterface, pDevice);
			pDevice.Resources.AddTrackedObject(indexBuffer, pInterface, pool, 0uL, ref indexBuffer._internalHandle);
		}
		return indexBuffer;
	}

	private void OnObjectCreation()
	{
	}

	private unsafe void _0021IndexBuffer()
	{
		if (!isDisposed)
		{
			ReleaseNativeObject(disposeManagedResource: true);
			void* ptr = pBufferData;
			if (ptr != null)
			{
				_003CModule_003E.delete_005B_005D(ptr);
				pBufferData = null;
			}
		}
	}

	private void _007EIndexBuffer()
	{
		_0021IndexBuffer();
	}

	[HandleProcessCorruptedStateExceptions]
	protected override void Dispose([MarshalAs(UnmanagedType.U1)] bool P_0)
	{
		if (P_0)
		{
			try
			{
				_007EIndexBuffer();
				return;
			}
			finally
			{
				base.Dispose(true);
			}
		}
		try
		{
			_0021IndexBuffer();
		}
		finally
		{
			base.Dispose(false);
		}
	}
}
