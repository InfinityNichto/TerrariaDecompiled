using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public class TextureCube : Texture, IGraphicsResource
{
	private protected int _size;

	private protected bool _shouldNotRecreate;

	private IntPtr[] pFaceData;

	internal unsafe IDirect3DCubeTexture9* pComPtr;

	public int Size => _size;

	private unsafe TextureCube(IDirect3DCubeTexture9* pInterface, GraphicsDevice pDevice)
	{
		pComPtr = pInterface;
		((object)this)._002Ector();
		try
		{
			_parent = pDevice;
			InitializeDescription(null);
			return;
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
	}

	private protected TextureCube()
	{
	}

	public TextureCube(GraphicsDevice graphicsDevice, int size, [MarshalAs(UnmanagedType.U1)] bool mipMap, SurfaceFormat format)
	{
		try
		{
			CreateTexture(graphicsDevice, size, mipMap, 0u, (_D3DPOOL)1, format);
			return;
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
	}

	public void SetData<T>(CubeMapFace cubeMapFace, T[] data) where T : struct
	{
		Rectangle? rect = null;
		int elementCount = ((data != null) ? data.Length : 0);
		SetData(cubeMapFace, 0, rect, data, 0, elementCount);
	}

	public void SetData<T>(CubeMapFace cubeMapFace, T[] data, int startIndex, int elementCount) where T : struct
	{
		SetData(cubeMapFace, 0, null, data, startIndex, elementCount);
	}

	public void SetData<T>(CubeMapFace cubeMapFace, int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct
	{
		CopyData(cubeMapFace, level, rect, data, startIndex, elementCount, 0u, isSetting: true);
	}

	public void GetData<T>(CubeMapFace cubeMapFace, T[] data) where T : struct
	{
		Rectangle? rect = null;
		int elementCount = ((data != null) ? data.Length : 0);
		GetData(cubeMapFace, 0, rect, data, 0, elementCount);
	}

	public void GetData<T>(CubeMapFace cubeMapFace, T[] data, int startIndex, int elementCount) where T : struct
	{
		GetData(cubeMapFace, 0, null, data, startIndex, elementCount);
	}

	public void GetData<T>(CubeMapFace cubeMapFace, int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct
	{
		CopyData(cubeMapFace, level, rect, data, startIndex, elementCount, 16u, isSetting: false);
	}

	private unsafe void CopyData<T>(CubeMapFace cubeMapFace, int level, Rectangle? rect, T[] data, int startIndex, int elementCount, uint options, [MarshalAs(UnmanagedType.U1)] bool isSetting) where T : struct
	{
		IntPtr intPtr = (IntPtr)pComPtr;
		Helpers.CheckDisposed(this, intPtr);
		if (data != null && data.Length != 0)
		{
			if (isActiveRenderTarget)
			{
				throw new InvalidOperationException(FrameworkResources.MustResolveRenderTarget);
			}
			if (isSetting)
			{
				int num = 0;
				GraphicsDevice parent = _parent;
				if (0 < parent.Textures._maxTextures)
				{
					do
					{
						if (parent.Textures[num] != this)
						{
							num++;
							parent = _parent;
							continue;
						}
						throw GraphicsHelpers.GetExceptionFromResult(2147500036u);
					}
					while (num < parent.Textures._maxTextures);
				}
				int num2 = 0;
				GraphicsDevice parent2 = _parent;
				if (0 < parent2.VertexTextures._maxTextures)
				{
					do
					{
						if (parent2.VertexTextures[num2] != this)
						{
							num2++;
							parent2 = _parent;
							continue;
						}
						throw GraphicsHelpers.GetExceptionFromResult(2147500036u);
					}
					while (num2 < parent2.VertexTextures._maxTextures);
				}
			}
			IDirect3DCubeTexture9* ptr = pComPtr;
			System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DSURFACE_DESC d3DSURFACE_DESC);
			int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DSURFACE_DESC*, int>)(int)(*(uint*)(*(int*)ptr + 68)))((nint)ptr, (uint)level, &d3DSURFACE_DESC);
			if (num3 < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
			}
			Helpers.ValidateCopyParameters(data.Length, startIndex, elementCount);
			System.Runtime.CompilerServices.Unsafe.SkipInit(out uint dwFormatSize);
			System.Runtime.CompilerServices.Unsafe.SkipInit(out uint dwElementSize);
			Texture.GetAndValidateSizes<T>(&d3DSURFACE_DESC, &dwFormatSize, &dwElementSize);
			System.Runtime.CompilerServices.Unsafe.SkipInit(out uint dwLockWidth);
			System.Runtime.CompilerServices.Unsafe.SkipInit(out uint dwLockHeight);
			Texture.GetAndValidateRect(&d3DSURFACE_DESC, &dwLockWidth, &dwLockHeight, rect);
			Texture.ValidateTotalSize(&d3DSURFACE_DESC, dwLockWidth, dwLockHeight, dwFormatSize, dwElementSize, (uint)elementCount);
			tagRECT* ptr2 = null;
			Rectangle rectangle = default(Rectangle);
			if (rect.HasValue)
			{
				rectangle = rect.Value;
				ptr2 = (tagRECT*)(int)(ref rectangle);
				if (ptr2 != null)
				{
					*(int*)((byte*)ptr2 + 8) += *(int*)ptr2;
					*(int*)((byte*)ptr2 + 12) += *(int*)((byte*)ptr2 + 4);
				}
			}
			int num5;
			System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DLOCKED_RECT d3DLOCKED_RECT);
			if (System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 12)) == 0)
			{
				if (!isSetting)
				{
					GraphicsDevice parent3 = _parent;
					GraphicsDevice graphicsDevice = parent3;
					GraphicsDevice graphicsDevice2 = parent3;
					GraphicsDevice graphicsDevice3 = parent3;
					int num4 = *(int*)GraphicsAdapter.pComPtr + 40;
					if (((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DDEVTYPE, _D3DFORMAT, uint, _D3DRESOURCETYPE, _D3DFORMAT, int>)(int)(*(uint*)num4))((nint)GraphicsAdapter.pComPtr, graphicsDevice3.Adapter.adapter, graphicsDevice2._deviceType, _003CModule_003E.ConvertXnaFormatToWindowsAdapterFormat(graphicsDevice.Adapter.CurrentDisplayMode.Format), 1u, (_D3DRESOURCETYPE)3, *(_D3DFORMAT*)(&d3DSURFACE_DESC)) < 0)
					{
						throw new InvalidOperationException(FrameworkResources.CannotUseFormatTypeAsManualWhenLocking);
					}
				}
				IDirect3DSurface9* ptr3 = null;
				IDirect3DTexture9* ptr4 = null;
				IDirect3DSurface9* ptr5 = null;
				IDirect3DDevice9* ptr6 = _parent.pComPtr;
				num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, uint, uint, _D3DFORMAT, _D3DPOOL, IDirect3DTexture9**, void**, int>)(int)(*(uint*)(*(int*)ptr6 + 92)))((nint)ptr6, System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 24)), System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 28)), 1u, 0u, *(_D3DFORMAT*)(&d3DSURFACE_DESC), (_D3DPOOL)2, &ptr4, null);
				if (num5 >= 0)
				{
					num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, IDirect3DSurface9**, int>)(int)(*(uint*)(*(int*)ptr4 + 72)))((nint)ptr4, 0u, &ptr3);
					if (num5 >= 0)
					{
						ptr = pComPtr;
						num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DCUBEMAP_FACES, uint, IDirect3DSurface9**, int>)(int)(*(uint*)(*(int*)ptr + 72)))((nint)ptr, (_D3DCUBEMAP_FACES)cubeMapFace, (uint)level, &ptr5);
						if (num5 >= 0)
						{
							if (isSetting)
							{
								if (ptr2 != null && (*(int*)ptr2 != 0 || *(int*)((byte*)ptr2 + 4) != 0 || *(int*)((byte*)ptr2 + 8) != System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 24)) || *(int*)((byte*)ptr2 + 12) != System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 28))))
								{
									num5 = _003CModule_003E.D3DXLoadSurfaceFromSurface(ptr3, null, null, ptr5, null, null, 1u, 0u);
								}
								if (num5 >= 0)
								{
									num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DLOCKED_RECT*, tagRECT*, uint, int>)(int)(*(uint*)(*(int*)ptr3 + 52)))((nint)ptr3, &d3DLOCKED_RECT, ptr2, 0u);
									if (num5 >= 0)
									{
										try
										{
											Texture.CopyData((void*)(int)System.Runtime.CompilerServices.Unsafe.As<_D3DLOCKED_RECT, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DLOCKED_RECT, 4)), *(int*)(&d3DLOCKED_RECT), data, startIndex, elementCount, &d3DSURFACE_DESC, dwLockWidth, dwLockHeight, isSetting);
										}
										finally
										{
											IDirect3DSurface9* intPtr2 = ptr3;
											num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr2 + 56)))((nint)intPtr2);
										}
										if (num5 >= 0)
										{
											num5 = _003CModule_003E.D3DXLoadSurfaceFromSurface(ptr5, null, ptr2, ptr3, null, ptr2, 1u, 0u);
										}
									}
								}
							}
							else
							{
								num5 = _003CModule_003E.D3DXLoadSurfaceFromSurface(ptr3, null, ptr2, ptr5, null, ptr2, 1u, 0u);
								if (num5 >= 0)
								{
									num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DLOCKED_RECT*, tagRECT*, uint, int>)(int)(*(uint*)(*(int*)ptr3 + 52)))((nint)ptr3, &d3DLOCKED_RECT, ptr2, 0u);
									if (num5 >= 0)
									{
										try
										{
											Texture.CopyData((void*)(int)System.Runtime.CompilerServices.Unsafe.As<_D3DLOCKED_RECT, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DLOCKED_RECT, 4)), *(int*)(&d3DLOCKED_RECT), data, startIndex, elementCount, &d3DSURFACE_DESC, dwLockWidth, dwLockHeight, isSetting: false);
										}
										finally
										{
											IDirect3DSurface9* intPtr3 = ptr3;
											num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr3 + 56)))((nint)intPtr3);
										}
									}
								}
							}
						}
					}
				}
				if (ptr5 != null)
				{
					IDirect3DSurface9* intPtr4 = ptr5;
					((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)intPtr4 + 8)))((nint)intPtr4);
					ptr5 = null;
				}
				if (ptr3 != null)
				{
					IDirect3DSurface9* intPtr5 = ptr3;
					((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)intPtr5 + 8)))((nint)intPtr5);
					ptr3 = null;
				}
				if (ptr4 != null)
				{
					IDirect3DTexture9* intPtr6 = ptr4;
					((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)intPtr6 + 8)))((nint)intPtr6);
					ptr4 = null;
				}
			}
			else
			{
				ptr = pComPtr;
				num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DCUBEMAP_FACES, uint, _D3DLOCKED_RECT*, tagRECT*, uint, int>)(int)(*(uint*)(*(int*)ptr + 76)))((nint)ptr, (_D3DCUBEMAP_FACES)cubeMapFace, (uint)level, &d3DLOCKED_RECT, ptr2, options);
				if (num5 >= 0)
				{
					try
					{
						Texture.CopyData((void*)(int)System.Runtime.CompilerServices.Unsafe.As<_D3DLOCKED_RECT, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DLOCKED_RECT, 4)), *(int*)(&d3DLOCKED_RECT), data, startIndex, elementCount, &d3DSURFACE_DESC, dwLockWidth, dwLockHeight, isSetting);
					}
					finally
					{
						ptr = pComPtr;
						num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DCUBEMAP_FACES, uint, int>)(int)(*(uint*)(*(int*)ptr + 80)))((nint)ptr, (_D3DCUBEMAP_FACES)cubeMapFace, (uint)level);
					}
				}
			}
			if (_parent.IsDeviceLost)
			{
				if (isSetting)
				{
					return;
				}
				fixed (T* ptr7 = &data[startIndex])
				{
					try
					{
						uint num6 = (uint)(System.Runtime.CompilerServices.Unsafe.SizeOf<T>() * elementCount);
						// IL initblk instruction
						System.Runtime.CompilerServices.Unsafe.InitBlock(ptr7, 0, num6);
					}
					catch
					{
						//try-fault
						ptr7 = null;
						throw;
					}
				}
			}
			else
			{
				if (num5 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num5);
				}
				if (isSetting && this is IDynamicGraphicsResource dynamicGraphicsResource)
				{
					dynamicGraphicsResource.SetContentLost(isContentLost: false);
				}
			}
			return;
		}
		throw new ArgumentNullException("data", FrameworkResources.NullNotAllowed);
	}

	private protected unsafe void InitializeDescription(SurfaceFormat? format)
	{
		IDirect3DCubeTexture9* ptr = pComPtr;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DSURFACE_DESC d3DSURFACE_DESC);
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DSURFACE_DESC*, int>)(int)(*(uint*)(*(int*)ptr + 68)))((nint)ptr, 0u, &d3DSURFACE_DESC);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
		if (!format.HasValue)
		{
			format = _003CModule_003E.ConvertWindowsFormatToXna(*(_D3DFORMAT*)(&d3DSURFACE_DESC));
		}
		_size = System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 24));
		int shouldNotRecreate = ((System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 12)) == 0) ? 1 : 0);
		_shouldNotRecreate = (byte)shouldNotRecreate != 0;
		base.InitializeDescription(format.Value);
	}

	private protected unsafe void CreateTexture(GraphicsDevice graphicsDevice, int size, [MarshalAs(UnmanagedType.U1)] bool mipMap, uint usage, _D3DPOOL pool, SurfaceFormat format)
	{
		if (graphicsDevice == null)
		{
			throw new ArgumentNullException("graphicsDevice", FrameworkResources.DeviceCannotBeNullOnResourceCreate);
		}
		ValidateCreationParameters(graphicsDevice._profileCapabilities, size, format);
		int num = ((!mipMap) ? 1 : 0);
		fixed (IDirect3DCubeTexture9** ptr = &pComPtr)
		{
			int num2 = *(int*)graphicsDevice.pComPtr + 100;
			int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, uint, _D3DFORMAT, _D3DPOOL, IDirect3DCubeTexture9**, void**, int>)(int)(*(uint*)num2))((nint)graphicsDevice.pComPtr, (uint)size, (uint)num, usage, _003CModule_003E.ConvertXnaFormatToWindows(format), pool, ptr, null);
			if (num3 < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
			}
			isDisposed = false;
			_parent = graphicsDevice;
			SurfaceFormat? format2 = format;
			InitializeDescription(format2);
			graphicsDevice.Resources.AddTrackedObject(this, pComPtr, (uint)pool, _internalHandle, ref _internalHandle);
		}
	}

	private protected static void ValidateCreationParameters(ProfileCapabilities profile, int size, SurfaceFormat format)
	{
		if (size <= 0)
		{
			throw new ArgumentOutOfRangeException("size", FrameworkResources.ResourcesMustBeGreaterThanZeroSize);
		}
		bool flag = Texture.CheckCompressedTexture(_003CModule_003E.ConvertXnaFormatToWindows(format));
		if (!profile.ValidCubeFormats.Contains(format))
		{
			profile.ThrowNotSupportedException(FrameworkResources.ProfileFormatNotSupported, typeof(TextureCube).Name, format);
		}
		int maxCubeSize = profile.MaxCubeSize;
		if (size > maxCubeSize)
		{
			profile.ThrowNotSupportedException(FrameworkResources.ProfileTooBig, typeof(TextureCube).Name, maxCubeSize);
		}
		if (!profile.NonPow2Cube && !Texture.IsPowerOfTwo((uint)size))
		{
			profile.ThrowNotSupportedException(FrameworkResources.ProfileNotPowerOfTwo, typeof(TextureCube).Name);
		}
		if (flag && ((uint)size & 3u) != 0)
		{
			throw new ArgumentException(FrameworkResources.DxtNotMultipleOfFour);
		}
	}

	internal unsafe virtual int SaveDataForRecreation()
	{
		if (pComPtr == null)
		{
			return 0;
		}
		CleanupSavedData();
		int num = CopyOrRestoreData(isStoring: true);
		if (num >= 0)
		{
			ReleaseNativeObject(disposeManagedResource: false);
		}
		else
		{
			CleanupSavedData();
		}
		alreadyRecreated = false;
		return num;
	}

	int IGraphicsResource.SaveDataForRecreation()
	{
		//ILSpy generated this explicit interface implementation from .override directive in SaveDataForRecreation
		return this.SaveDataForRecreation();
	}

	internal unsafe virtual int RecreateAndPopulateObject()
	{
		if (_shouldNotRecreate)
		{
			return 0;
		}
		if (alreadyRecreated)
		{
			return 0;
		}
		if (pComPtr != null)
		{
			return -2147467259;
		}
		fixed (IDirect3DCubeTexture9** ptr2 = &pComPtr)
		{
			IDirect3DDevice9* ptr = _parent.pComPtr;
			int num = *(int*)ptr + 100;
			int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, uint, _D3DFORMAT, _D3DPOOL, IDirect3DCubeTexture9**, void**, int>)(int)(*(uint*)num))((nint)ptr, (uint)_size, (uint)_levelCount, 0u, _003CModule_003E.ConvertXnaFormatToWindows(_format), (_D3DPOOL)1, ptr2, null);
			if (num2 >= 0)
			{
				num2 = CreateStateWrapper();
				if (num2 >= 0)
				{
					num2 = CopyOrRestoreData(isStoring: false);
					if (num2 >= 0)
					{
						_parent.Resources.AddTrackedObject(this, pComPtr, 1u, _internalHandle, ref _internalHandle);
					}
				}
				CleanupSavedData();
			}
			alreadyRecreated = true;
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
		StateTrackerTexture* ptr = pStateTracker;
		if (ptr != null)
		{
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)ptr + 8)))((nint)ptr);
			pStateTracker = null;
		}
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

	internal unsafe int CopyOrRestoreData([MarshalAs(UnmanagedType.U1)] bool isStoring)
	{
		IDirect3DCubeTexture9* ptr = pComPtr;
		if (ptr == null)
		{
			return 0;
		}
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DSURFACE_DESC d3DSURFACE_DESC);
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DSURFACE_DESC*, int>)(int)(*(uint*)(*(int*)ptr + 68)))((nint)ptr, 0u, &d3DSURFACE_DESC);
		if (num >= 0)
		{
			if (isStoring)
			{
				pFaceData = new IntPtr[_levelCount * 6];
			}
			uint num2 = System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 24));
			uint num3 = System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 28));
			bool flag = Texture.CheckCompressedTexture(*(_D3DFORMAT*)(&d3DSURFACE_DESC));
			byte b = ((!flag) ? Texture.GetExpectedByteSizeFromFormat(*(_D3DFORMAT*)(&d3DSURFACE_DESC)) : ((byte)((*(int*)(&d3DSURFACE_DESC) == 827611204) ? 8u : 16u)));
			int num4 = 0;
			int num5 = 0;
			if (0 < _levelCount)
			{
				System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DLOCKED_RECT d3DLOCKED_RECT);
				do
				{
					int num6 = 0;
					do
					{
						byte* ptr2 = null;
						int num7;
						if (!isStoring)
						{
							ref IntPtr reference = ref pFaceData[num4];
							num4++;
							ptr2 = (byte*)reference.ToPointer();
							num7 = 0;
						}
						else
						{
							num7 = 16;
						}
						ptr = pComPtr;
						num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DCUBEMAP_FACES, uint, _D3DLOCKED_RECT*, tagRECT*, uint, int>)(int)(*(uint*)(*(int*)ptr + 76)))((nint)ptr, (_D3DCUBEMAP_FACES)num6, (uint)num5, &d3DLOCKED_RECT, null, (uint)num7);
						if (num < 0)
						{
							break;
						}
						uint num8 = num2;
						uint num9 = num3;
						if (flag)
						{
							num8 = num2 + 3 >> 2;
							num9 = num3 + 3 >> 2;
						}
						uint num10 = b * num8;
						byte* ptr3 = (byte*)(int)System.Runtime.CompilerServices.Unsafe.As<_D3DLOCKED_RECT, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DLOCKED_RECT, 4));
						if (isStoring)
						{
							ptr2 = (byte*)_003CModule_003E.new_005B_005D(num10 * num9);
						}
						byte* ptr4 = ptr2;
						if (0 < num9)
						{
							uint num11 = num9;
							do
							{
								if (isStoring)
								{
									_003CModule_003E.memcpy_s(ptr4, num10 * num9, ptr3, num10);
								}
								else
								{
									_003CModule_003E.memcpy_s(ptr3, *(uint*)(&d3DLOCKED_RECT), ptr4, num10);
								}
								ptr3 = *(int*)(&d3DLOCKED_RECT) + ptr3;
								ptr4 = (int)num10 + ptr4;
								num11--;
							}
							while (num11 != 0);
						}
						ptr = pComPtr;
						num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DCUBEMAP_FACES, uint, int>)(int)(*(uint*)(*(int*)ptr + 80)))((nint)ptr, (_D3DCUBEMAP_FACES)num6, (uint)num5);
						if (num < 0)
						{
							break;
						}
						if (isStoring)
						{
							IntPtr intPtr = (IntPtr)ptr2;
							ref IntPtr reference2 = ref pFaceData[num4];
							num4++;
							reference2 = intPtr;
						}
						num6++;
					}
					while (num6 < 6);
					num2 = ((num2 <= 1) ? 1u : (num2 >> 1));
					num3 = ((num3 <= 1) ? 1u : (num3 >> 1));
					num5++;
				}
				while (num5 < _levelCount);
			}
		}
		return num;
	}

	internal unsafe void CleanupSavedData()
	{
		IntPtr[] array = pFaceData;
		if (array == null)
		{
			return;
		}
		int num = 0;
		if (0 < (nint)array.LongLength)
		{
			do
			{
				void* ptr = pFaceData[num].ToPointer();
				if (ptr != null)
				{
					_003CModule_003E.delete_005B_005D(ptr);
				}
				ref IntPtr reference = ref pFaceData[num];
				reference = IntPtr.Zero;
				num++;
			}
			while (num < (nint)pFaceData.LongLength);
		}
		pFaceData = null;
	}

	internal unsafe override IDirect3DBaseTexture9* GetComPtr()
	{
		return (IDirect3DBaseTexture9*)pComPtr;
	}

	internal unsafe static TextureCube GetManagedObject(IDirect3DCubeTexture9* pInterface, GraphicsDevice pDevice, uint pool)
	{
		TextureCube textureCube = pDevice.Resources.GetCachedObject(pInterface) as TextureCube;
		if (textureCube != null)
		{
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)pInterface + 8)))((nint)pInterface);
			textureCube.isDisposed = false;
			GC.ReRegisterForFinalize(textureCube);
		}
		else
		{
			textureCube = new TextureCube(pInterface, pDevice);
			pDevice.Resources.AddTrackedObject(textureCube, pInterface, pool, 0uL, ref textureCube._internalHandle);
		}
		return textureCube;
	}

	private void OnObjectCreation()
	{
		InitializeDescription(null);
	}

	private void _0021TextureCube()
	{
		if (!isDisposed)
		{
			ReleaseNativeObject(disposeManagedResource: true);
			CleanupSavedData();
		}
	}

	private void _007ETextureCube()
	{
		_0021TextureCube();
	}

	[HandleProcessCorruptedStateExceptions]
	protected override void Dispose([MarshalAs(UnmanagedType.U1)] bool P_0)
	{
		if (P_0)
		{
			try
			{
				_007ETextureCube();
				return;
			}
			finally
			{
				base.Dispose(true);
			}
		}
		try
		{
			_0021TextureCube();
		}
		finally
		{
			base.Dispose(false);
		}
	}
}
