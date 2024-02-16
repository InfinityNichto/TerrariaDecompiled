using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public class VertexBuffer : GraphicsResource, IGraphicsResource
{
	private protected uint _usage;

	private protected uint _size;

	private protected uint _pool;

	internal uint _vertexCount;

	internal VertexDeclaration _vertexDeclaration;

	private unsafe void* pBufferData;

	internal unsafe IDirect3DVertexBuffer9* pComPtr;

	internal bool IsWriteOnly
	{
		[return: MarshalAs(UnmanagedType.U1)]
		get
		{
			return (_usage & 8) == 8;
		}
	}

	public VertexDeclaration VertexDeclaration => _vertexDeclaration;

	public int VertexCount => (int)_vertexCount;

	public BufferUsage BufferUsage => _003CModule_003E.ConvertDxBufferUsageToXna(_usage);

	private unsafe VertexBuffer(IDirect3DVertexBuffer9* pInterface, GraphicsDevice pDevice)
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

	private protected VertexBuffer()
	{
	}

	public unsafe VertexBuffer(GraphicsDevice graphicsDevice, Type vertexType, int vertexCount, BufferUsage usage)
	{
		try
		{
			VertexDeclaration vertexDeclaration = VertexDeclaration.FromType(vertexType);
			if (vertexCount <= 0)
			{
				throw new ArgumentOutOfRangeException("vertexCount", FrameworkResources.ResourcesMustBeGreaterThanZeroSize);
			}
			_parent = graphicsDevice;
			CreateBuffer(vertexDeclaration, (uint)vertexCount, _003CModule_003E.ConvertXnaBufferUsageToDx(usage), (_D3DPOOL)1);
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

	public unsafe VertexBuffer(GraphicsDevice graphicsDevice, VertexDeclaration vertexDeclaration, int vertexCount, BufferUsage usage)
	{
		try
		{
			if (vertexDeclaration == null)
			{
				throw new ArgumentNullException("vertexDeclaration", FrameworkResources.NullNotAllowed);
			}
			if (vertexCount <= 0)
			{
				throw new ArgumentOutOfRangeException("vertexCount", FrameworkResources.ResourcesMustBeGreaterThanZeroSize);
			}
			_parent = graphicsDevice;
			CreateBuffer(vertexDeclaration, (uint)vertexCount, _003CModule_003E.ConvertXnaBufferUsageToDx(usage), (_D3DPOOL)1);
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

	public void SetData<T>(T[] data) where T : struct
	{
		int elementCount = ((data != null) ? data.Length : 0);
		SetData(0, data, 0, elementCount, 0);
	}

	public void SetData<T>(T[] data, int startIndex, int elementCount) where T : struct
	{
		SetData(0, data, startIndex, elementCount, 0);
	}

	public void SetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride) where T : struct
	{
		CopyData(offsetInBytes, data, startIndex, elementCount, vertexStride, 0u, isSetting: true);
	}

	public void GetData<T>(T[] data) where T : struct
	{
		int elementCount = ((data != null) ? data.Length : 0);
		GetData(0, data, 0, elementCount, 0);
	}

	public void GetData<T>(T[] data, int startIndex, int elementCount) where T : struct
	{
		GetData(0, data, startIndex, elementCount, 0);
	}

	public void GetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride) where T : struct
	{
		CopyData(offsetInBytes, data, startIndex, elementCount, vertexStride, 16u, isSetting: false);
	}

	internal unsafe void CopyData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride, uint options, [MarshalAs(UnmanagedType.U1)] bool isSetting) where T : struct
	{
		IntPtr intPtr = (IntPtr)pComPtr;
		Helpers.CheckDisposed(this, intPtr);
		if (data != null)
		{
			int num = data.Length;
			if (num != 0)
			{
				int num2 = -2147467259;
				if ((options & 0x3010) == 0)
				{
					int num3 = 0;
					GraphicsDevice parent = _parent;
					int currentVertexBufferCount = parent.currentVertexBufferCount;
					if (0 < currentVertexBufferCount)
					{
						VertexBufferBinding[] currentVertexBuffers = parent.currentVertexBuffers;
						do
						{
							if (currentVertexBuffers[num3]._vertexBuffer != this)
							{
								num3++;
								continue;
							}
							throw GraphicsHelpers.GetExceptionFromResult(2147500036u);
						}
						while (num3 < currentVertexBufferCount);
					}
				}
				if (!isSetting && (_usage & 8) == 8)
				{
					throw new NotSupportedException(FrameworkResources.WriteOnlyGetNotSupported);
				}
				Helpers.ValidateCopyParameters(num, startIndex, elementCount);
				try
				{
					uint num4 = (uint)System.Runtime.CompilerServices.Unsafe.SizeOf<T>();
					uint num5 = num4 * (uint)elementCount;
					int num6;
					if (vertexStride != 0)
					{
						num6 = vertexStride - (int)num4;
						if (num6 < 0)
						{
							throw new ArgumentOutOfRangeException("vertexStride", FrameworkResources.VertexStrideTooSmall);
						}
						if (elementCount > 1)
						{
							num5 = (uint)((elementCount - 1) * num6) + num5;
						}
					}
					else
					{
						num6 = 0;
					}
					if ((uint)((int)num5 + offsetInBytes) > _size)
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
								System.Runtime.CompilerServices.Unsafe.InitBlock(ptr, 0, num5);
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
					IDirect3DVertexBuffer9* ptr4 = pComPtr;
					num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, void**, uint, int>)(int)(*(uint*)(*(int*)ptr4 + 44)))((nint)ptr4, (uint)offsetInBytes, num5, &ptr2, options);
					if (num2 >= 0)
					{
						if (num6 == 0)
						{
							fixed (T* ptr5 = &data[startIndex])
							{
								try
								{
									if (isSetting)
									{
										_003CModule_003E.memcpy_s(ptr2, _size - (uint)offsetInBytes, ptr5, num5);
										if (IsWriteOnly && _pool == 1)
										{
											_003CModule_003E.memcpy_s(ptr3, _size - (uint)offsetInBytes, ptr5, num5);
										}
									}
									else
									{
										_003CModule_003E.memcpy_s(ptr5, num5, ptr2, num5);
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
						else
						{
							byte* ptr6 = (byte*)ptr2;
							byte* ptr7 = ptr3;
							for (int i = 0; i < elementCount; i++)
							{
								fixed (T* ptr8 = &data[i + startIndex])
								{
									try
									{
										if (isSetting)
										{
											_003CModule_003E.memcpy_s(ptr6, _size - (uint)offsetInBytes, ptr8, (uint)System.Runtime.CompilerServices.Unsafe.SizeOf<T>());
											if (IsWriteOnly && _pool == 1)
											{
												_003CModule_003E.memcpy_s(ptr7, _size - (uint)offsetInBytes, ptr8, (uint)System.Runtime.CompilerServices.Unsafe.SizeOf<T>());
											}
										}
										else
										{
											_003CModule_003E.memcpy_s(ptr8, num5, ptr6, (uint)System.Runtime.CompilerServices.Unsafe.SizeOf<T>());
										}
										ptr6 = vertexStride + ptr6;
										if (IsWriteOnly && _pool == 1)
										{
											ptr7 = vertexStride + ptr7;
										}
									}
									catch
									{
										//try-fault
										ptr8 = null;
										throw;
									}
								}
							}
						}
					}
				}
				finally
				{
					if (num2 >= 0)
					{
						IDirect3DVertexBuffer9* intPtr2 = pComPtr;
						num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr2 + 48)))((nint)intPtr2);
					}
				}
				if (num2 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
				}
				if (isSetting && this is IDynamicGraphicsResource dynamicGraphicsResource)
				{
					dynamicGraphicsResource.SetContentLost(isContentLost: false);
				}
				return;
			}
		}
		throw new ArgumentNullException("data", FrameworkResources.NullNotAllowed);
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
		if ((pBufferData = _003CModule_003E.new_005B_005D(_size)) == null)
		{
			return -2147024882;
		}
		IDirect3DVertexBuffer9* ptr2 = pComPtr;
		num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, void**, uint, int>)(int)(*(uint*)(*(int*)ptr2 + 44)))((nint)ptr2, 0u, 0u, &ptr, 0u);
		if (num >= 0)
		{
			uint size = _size;
			_003CModule_003E.memcpy_s(pBufferData, size, ptr, size);
			ptr2 = pComPtr;
			IDirect3DVertexBuffer9* intPtr = ptr2;
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
		if (pBufferData == null && _size != 0)
		{
			return -2147467259;
		}
		fixed (IDirect3DVertexBuffer9** ptr2 = &pComPtr)
		{
			GraphicsDevice parent = _parent;
			IDirect3DDevice9* ptr = parent.pComPtr;
			GraphicsDevice graphicsDevice = parent;
			int num = *(int*)ptr + 104;
			int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, uint, _D3DPOOL, IDirect3DVertexBuffer9**, void**, int>)(int)(*(uint*)num))((nint)ptr, _size, graphicsDevice.GetBufferUsage(_usage), 0u, (_D3DPOOL)1, ptr2, null);
			if (num2 >= 0)
			{
				void* ptr3 = null;
				IDirect3DVertexBuffer9* ptr4 = pComPtr;
				num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, void**, uint, int>)(int)(*(uint*)(*(int*)ptr4 + 44)))((nint)ptr4, 0u, 0u, &ptr3, 0u);
				if (num2 >= 0)
				{
					uint size = _size;
					_003CModule_003E.memcpy_s(ptr3, size, pBufferData, size);
					ptr4 = pComPtr;
					IDirect3DVertexBuffer9* intPtr = ptr4;
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

	private protected unsafe void CreateBuffer(VertexDeclaration vertexDeclaration, uint dwVertexCount, uint usage, _D3DPOOL pool)
	{
		isDisposed = false;
		if (vertexDeclaration.isDisposed)
		{
			throw new ObjectDisposedException(typeof(VertexDeclaration).Name);
		}
		GraphicsDevice parent = ((GraphicsResource)vertexDeclaration)._parent;
		GraphicsDevice parent2 = _parent;
		if (parent != parent2)
		{
			vertexDeclaration.Bind(parent2);
		}
		uint num = (uint)vertexDeclaration._vertexStride * dwVertexCount;
		int maxVertexBufferSize = _parent._profileCapabilities.MaxVertexBufferSize;
		if (num > (uint)maxVertexBufferSize)
		{
			_parent._profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileTooBig, typeof(VertexBuffer).Name, maxVertexBufferSize);
		}
		fixed (IDirect3DVertexBuffer9** ptr = &pComPtr)
		{
			int num2 = *(int*)_parent.pComPtr + 104;
			int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, uint, _D3DPOOL, IDirect3DVertexBuffer9**, void**, int>)(int)(*(uint*)num2))((nint)_parent.pComPtr, num, _parent.GetBufferUsage(usage), 0u, pool, ptr, null);
			if (num3 < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
			}
			_usage = usage;
			_size = num;
			_pool = (uint)pool;
			_vertexCount = dwVertexCount;
			_vertexDeclaration = vertexDeclaration;
			if ((usage & 8) == 8 && pool == (_D3DPOOL)1 && (pBufferData = _003CModule_003E.new_005B_005D(num)) == null)
			{
				throw new InsufficientMemoryException();
			}
		}
	}

	internal unsafe static VertexBuffer GetManagedObject(IDirect3DVertexBuffer9* pInterface, GraphicsDevice pDevice, uint pool)
	{
		VertexBuffer vertexBuffer = pDevice.Resources.GetCachedObject(pInterface) as VertexBuffer;
		if (vertexBuffer != null)
		{
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)pInterface + 8)))((nint)pInterface);
			vertexBuffer.isDisposed = false;
			GC.ReRegisterForFinalize(vertexBuffer);
		}
		else
		{
			vertexBuffer = new VertexBuffer(pInterface, pDevice);
			pDevice.Resources.AddTrackedObject(vertexBuffer, pInterface, pool, 0uL, ref vertexBuffer._internalHandle);
		}
		return vertexBuffer;
	}

	private void OnObjectCreation()
	{
	}

	private unsafe void _0021VertexBuffer()
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

	private void _007EVertexBuffer()
	{
		_0021VertexBuffer();
	}

	[HandleProcessCorruptedStateExceptions]
	protected override void Dispose([MarshalAs(UnmanagedType.U1)] bool P_0)
	{
		if (P_0)
		{
			try
			{
				_007EVertexBuffer();
				return;
			}
			finally
			{
				base.Dispose(true);
			}
		}
		try
		{
			_0021VertexBuffer();
		}
		finally
		{
			base.Dispose(false);
		}
	}
}
