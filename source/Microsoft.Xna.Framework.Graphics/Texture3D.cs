using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public class Texture3D : Texture, IGraphicsResource
{
	private int _width;

	private int _height;

	private int _depth;

	private IntPtr[] pFaceData;

	internal unsafe IDirect3DVolumeTexture9* pComPtr;

	public int Depth => _depth;

	public int Height => _height;

	public int Width => _width;

	private unsafe void InitializeDescription(SurfaceFormat? format)
	{
		IntPtr intPtr = (IntPtr)pComPtr;
		Helpers.CheckDisposed(this, intPtr);
		IDirect3DVolumeTexture9* ptr = pComPtr;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DVOLUME_DESC d3DVOLUME_DESC);
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DVOLUME_DESC*, int>)(int)(*(uint*)(*(int*)ptr + 68)))((nint)ptr, 0u, &d3DVOLUME_DESC);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
		if (!format.HasValue)
		{
			format = _003CModule_003E.ConvertWindowsFormatToXna(*(_D3DFORMAT*)(&d3DVOLUME_DESC));
		}
		_width = System.Runtime.CompilerServices.Unsafe.As<_D3DVOLUME_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DVOLUME_DESC, 16));
		_height = System.Runtime.CompilerServices.Unsafe.As<_D3DVOLUME_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DVOLUME_DESC, 20));
		_depth = System.Runtime.CompilerServices.Unsafe.As<_D3DVOLUME_DESC, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DVOLUME_DESC, 24));
		base.InitializeDescription(format.Value);
	}

	private unsafe Texture3D(IDirect3DVolumeTexture9* pInterface, GraphicsDevice pDevice)
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

	public Texture3D(GraphicsDevice graphicsDevice, int width, int height, int depth, [MarshalAs(UnmanagedType.U1)] bool mipMap, SurfaceFormat format)
	{
		try
		{
			CreateTexture(graphicsDevice, width, height, depth, mipMap, format);
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
		SetData<T>(elementCount: (data != null) ? data.Length : 0, level: 0, left: 0, top: 0, right: _width, bottom: _height, front: 0, back: _depth, data: data, startIndex: 0);
	}

	public void SetData<T>(T[] data, int startIndex, int elementCount) where T : struct
	{
		SetData(0, 0, 0, _width, _height, 0, _depth, data, startIndex, elementCount);
	}

	public void SetData<T>(int level, int left, int top, int right, int bottom, int front, int back, T[] data, int startIndex, int elementCount) where T : struct
	{
		CopyData(level, left, top, right, bottom, front, back, data, startIndex, elementCount, 0u, isSetting: true);
	}

	public void GetData<T>(T[] data) where T : struct
	{
		GetData<T>(elementCount: (data != null) ? data.Length : 0, level: 0, left: 0, top: 0, right: _width, bottom: _height, front: 0, back: _depth, data: data, startIndex: 0);
	}

	public void GetData<T>(T[] data, int startIndex, int elementCount) where T : struct
	{
		GetData(0, 0, 0, _width, _height, 0, _depth, data, startIndex, elementCount);
	}

	public void GetData<T>(int level, int left, int top, int right, int bottom, int front, int back, T[] data, int startIndex, int elementCount) where T : struct
	{
		CopyData(level, left, top, right, bottom, front, back, data, startIndex, elementCount, 16u, isSetting: false);
	}

	internal unsafe static void CopyData<T>(void* pData, int rowPitch, int slicePitch, T[] data, int dataIndex, int elementCount, _D3DVOLUME_DESC* pVolume, uint dwLockWidth, uint dwLockHeight, uint dwLockDepth, [MarshalAs(UnmanagedType.U1)] bool isSetting) where T : struct
	{
		if (pVolume == null)
		{
			return;
		}
		fixed (T* ptr2 = &data[dataIndex])
		{
			byte* ptr = (byte*)pData;
			byte* ptr3 = (byte*)ptr2;
			uint num = (uint)System.Runtime.CompilerServices.Unsafe.SizeOf<T>();
			uint num2 = Texture.GetExpectedByteSizeFromFormat(*(_D3DFORMAT*)pVolume) / num * num * dwLockWidth;
			if (0 >= dwLockDepth)
			{
				return;
			}
			uint num3 = dwLockDepth;
			do
			{
				byte* ptr4 = ptr;
				if (0 < dwLockHeight)
				{
					uint num4 = dwLockHeight;
					do
					{
						if (*(int*)pVolume == 21)
						{
							if (isSetting)
							{
								Texture.SwapBgr(ptr4, ptr3, num2);
							}
							else
							{
								Texture.SwapBgr(ptr3, ptr4, num2);
							}
						}
						else if (isSetting)
						{
							_003CModule_003E.memcpy_s(ptr4, num2, ptr3, num2);
						}
						else
						{
							_003CModule_003E.memcpy_s(ptr3, num2, ptr4, num2);
						}
						ptr4 = rowPitch + ptr4;
						ptr3 = (int)num2 + ptr3;
						num4--;
					}
					while (num4 != 0);
				}
				ptr = slicePitch + ptr;
				num3--;
			}
			while (num3 != 0);
		}
	}

	private unsafe void CopyData<T>(int level, int left, int top, int right, int bottom, int front, int back, T[] data, int startIndex, int elementCount, uint options, [MarshalAs(UnmanagedType.U1)] bool isSetting) where T : struct
	{
		IntPtr intPtr = (IntPtr)pComPtr;
		Helpers.CheckDisposed(this, intPtr);
		if (data != null && data.Length != 0)
		{
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
			IDirect3DVolumeTexture9* ptr = pComPtr;
			System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DVOLUME_DESC d3DVOLUME_DESC);
			int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DVOLUME_DESC*, int>)(int)(*(uint*)(*(int*)ptr + 68)))((nint)ptr, (uint)level, &d3DVOLUME_DESC);
			if (num3 < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
			}
			Helpers.ValidateCopyParameters(data.Length, startIndex, elementCount);
			System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DBOX d3DBOX);
			*(int*)(&d3DBOX) = left;
			System.Runtime.CompilerServices.Unsafe.As<_D3DBOX, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DBOX, 8)) = right;
			System.Runtime.CompilerServices.Unsafe.As<_D3DBOX, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DBOX, 4)) = top;
			System.Runtime.CompilerServices.Unsafe.As<_D3DBOX, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DBOX, 12)) = bottom;
			System.Runtime.CompilerServices.Unsafe.As<_D3DBOX, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DBOX, 16)) = front;
			System.Runtime.CompilerServices.Unsafe.As<_D3DBOX, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DBOX, 20)) = back;
			System.Runtime.CompilerServices.Unsafe.SkipInit(out uint num4);
			System.Runtime.CompilerServices.Unsafe.SkipInit(out uint num5);
			GetAndValidateSizes<T>(&d3DVOLUME_DESC, &num4, &num5);
			System.Runtime.CompilerServices.Unsafe.SkipInit(out uint num6);
			System.Runtime.CompilerServices.Unsafe.SkipInit(out uint num7);
			System.Runtime.CompilerServices.Unsafe.SkipInit(out uint num8);
			GetAndValidateBox(&d3DVOLUME_DESC, &num6, &num7, &num8, &d3DBOX);
			uint num9 = num6 * num7 * num8 * num4;
			if ((int)num5 * elementCount != (int)num9)
			{
				throw new ArgumentException(FrameworkResources.InvalidTotalSize);
			}
			IDirect3DVolumeTexture9* ptr2 = pComPtr;
			System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DLOCKED_BOX d3DLOCKED_BOX);
			num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DLOCKED_BOX*, _D3DBOX*, uint, int>)(int)(*(uint*)(*(int*)ptr2 + 76)))((nint)ptr2, (uint)level, &d3DLOCKED_BOX, &d3DBOX, options);
			if (num3 >= 0)
			{
				try
				{
					CopyData((void*)(int)System.Runtime.CompilerServices.Unsafe.As<_D3DLOCKED_BOX, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DLOCKED_BOX, 8)), *(int*)(&d3DLOCKED_BOX), System.Runtime.CompilerServices.Unsafe.As<_D3DLOCKED_BOX, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DLOCKED_BOX, 4)), data, startIndex, elementCount, &d3DVOLUME_DESC, num6, num7, num8, isSetting);
				}
				finally
				{
					if (num3 >= 0)
					{
						ptr2 = pComPtr;
						num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, int>)(int)(*(uint*)(*(int*)ptr2 + 80)))((nint)ptr2, (uint)level);
					}
				}
				if (num3 >= 0)
				{
					return;
				}
			}
			throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
		}
		throw new ArgumentNullException("data", FrameworkResources.NullNotAllowed);
	}

	private unsafe void CreateTexture(GraphicsDevice graphicsDevice, int width, int height, int depth, [MarshalAs(UnmanagedType.U1)] bool mipMap, SurfaceFormat format)
	{
		if (graphicsDevice == null)
		{
			throw new ArgumentNullException("graphicsDevice", FrameworkResources.DeviceCannotBeNullOnResourceCreate);
		}
		if (width <= 0)
		{
			throw new ArgumentOutOfRangeException("width", FrameworkResources.ResourcesMustBeGreaterThanZeroSize);
		}
		if (height <= 0)
		{
			throw new ArgumentOutOfRangeException("height", FrameworkResources.ResourcesMustBeGreaterThanZeroSize);
		}
		if (depth <= 0)
		{
			throw new ArgumentOutOfRangeException("depth", FrameworkResources.ResourcesMustBeGreaterThanZeroSize);
		}
		ProfileCapabilities profileCapabilities = graphicsDevice._profileCapabilities;
		if (profileCapabilities.MaxVolumeExtent == 0)
		{
			profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileFeatureNotSupported, typeof(Texture3D).Name);
		}
		if (!profileCapabilities.ValidVolumeFormats.Contains(format))
		{
			profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileFormatNotSupported, typeof(Texture3D).Name, format);
		}
		int maxVolumeExtent = profileCapabilities.MaxVolumeExtent;
		if (width > maxVolumeExtent || height > maxVolumeExtent || depth > maxVolumeExtent)
		{
			profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileTooBig, typeof(Texture3D).Name, maxVolumeExtent);
		}
		int num = Math.Max(Math.Max(width, height), depth);
		int num2 = Math.Min(Math.Min(width, height), depth);
		int num3 = (num + num2 - 1) / num2;
		int maxTextureAspectRatio = profileCapabilities.MaxTextureAspectRatio;
		if (num3 > maxTextureAspectRatio)
		{
			profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileAspectRatio, typeof(Texture3D).Name, maxTextureAspectRatio);
		}
		if (!profileCapabilities.NonPow2Volume && (!Texture.IsPowerOfTwo((uint)width) || !Texture.IsPowerOfTwo((uint)height) || !Texture.IsPowerOfTwo((uint)depth)))
		{
			profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileNotPowerOfTwo, typeof(Texture3D).Name);
		}
		int num4 = ((!mipMap) ? 1 : 0);
		fixed (IDirect3DVolumeTexture9** ptr = &pComPtr)
		{
			int num5 = *(int*)graphicsDevice.pComPtr + 96;
			int num6 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, uint, uint, uint, _D3DFORMAT, _D3DPOOL, IDirect3DVolumeTexture9**, void**, int>)(int)(*(uint*)num5))((nint)graphicsDevice.pComPtr, (uint)width, (uint)height, (uint)depth, (uint)num4, 0u, _003CModule_003E.ConvertXnaFormatToWindows(format), (_D3DPOOL)1, ptr, null);
			if (num6 < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num6);
			}
			isDisposed = false;
			_parent = graphicsDevice;
			SurfaceFormat? format2 = format;
			InitializeDescription(format2);
			graphicsDevice.Resources.AddTrackedObject(this, pComPtr, 1u, _internalHandle, ref _internalHandle);
		}
	}

	internal unsafe static void GetAndValidateSizes<T>(_D3DVOLUME_DESC* pVolume, uint* pdwFormatSize, uint* pdwElementSize) where T : struct
	{
		if (pVolume == null || pdwFormatSize == null || pdwElementSize == null)
		{
			return;
		}
		*pdwFormatSize = Texture.GetExpectedByteSizeFromFormat(*(_D3DFORMAT*)pVolume);
		uint num = (*pdwElementSize = (uint)System.Runtime.CompilerServices.Unsafe.SizeOf<T>());
		uint num2 = *pdwFormatSize;
		if (num != num2)
		{
			if (num2 <= num)
			{
				throw new ArgumentException(FrameworkResources.InvalidDataSize);
			}
			if (num2 % num != 0)
			{
				throw new ArgumentException(FrameworkResources.InvalidDataSize);
			}
		}
	}

	internal unsafe static void GetAndValidateBox(_D3DVOLUME_DESC* pVolume, uint* pdwLockWidth, uint* pdwLockHeight, uint* pdwLockDepth, _D3DBOX* pBox)
	{
		if (pVolume == null || pdwLockHeight == null || pdwLockWidth == null || pdwLockDepth == null)
		{
			return;
		}
		*pdwLockWidth = *(uint*)((byte*)pVolume + 16);
		*pdwLockHeight = *(uint*)((byte*)pVolume + 20);
		*pdwLockDepth = *(uint*)((byte*)pVolume + 24);
		if (pBox == null)
		{
			return;
		}
		uint num = *(uint*)((byte*)pBox + 8);
		if (num <= (uint)(*(int*)((byte*)pVolume + 16)))
		{
			uint num2 = *(uint*)pBox;
			if (num2 < num)
			{
				uint num3 = *(uint*)((byte*)pBox + 12);
				if (num3 <= (uint)(*(int*)((byte*)pVolume + 20)) && (uint)(*(int*)((byte*)pBox + 4)) < num3)
				{
					uint num4 = *(uint*)((byte*)pBox + 20);
					if (num4 <= (uint)(*(int*)((byte*)pVolume + 24)) && (uint)(*(int*)((byte*)pBox + 16)) < num4)
					{
						*pdwLockWidth = num - num2;
						*pdwLockHeight = (uint)(*(int*)((byte*)pBox + 12) - *(int*)((byte*)pBox + 4));
						*pdwLockDepth = (uint)(*(int*)((byte*)pBox + 20) - *(int*)((byte*)pBox + 16));
						return;
					}
				}
			}
		}
		throw new ArgumentException(FrameworkResources.InvalidRectangle, "box");
	}

	internal unsafe static void ValidateTotalSize(_D3DVOLUME_DESC* pVolume, uint dwLockWidth, uint dwLockHeight, uint dwLockDepth, uint dwFormatSize, uint dwElementSize, uint elementCount)
	{
		if (pVolume != null)
		{
			uint num = dwLockWidth * dwLockHeight * dwLockDepth * dwFormatSize;
			if (dwElementSize * elementCount != num)
			{
				throw new ArgumentException(FrameworkResources.InvalidTotalSize);
			}
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
		if (alreadyRecreated)
		{
			return 0;
		}
		if (pComPtr != null)
		{
			return -2147467259;
		}
		fixed (IDirect3DVolumeTexture9** ptr2 = &pComPtr)
		{
			IDirect3DDevice9* ptr = _parent.pComPtr;
			int num = *(int*)ptr + 96;
			int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, uint, uint, uint, _D3DFORMAT, _D3DPOOL, IDirect3DVolumeTexture9**, void**, int>)(int)(*(uint*)num))((nint)ptr, (uint)_width, (uint)_height, (uint)_depth, (uint)_levelCount, 0u, _003CModule_003E.ConvertXnaFormatToWindows(_format), (_D3DPOOL)1, ptr2, null);
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
		IDirect3DVolumeTexture9* ptr = pComPtr;
		if (ptr == null)
		{
			return 0;
		}
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DVOLUME_DESC d3DVOLUME_DESC);
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DVOLUME_DESC*, int>)(int)(*(uint*)(*(int*)ptr + 68)))((nint)ptr, 0u, &d3DVOLUME_DESC);
		if (num >= 0)
		{
			if (isStoring)
			{
				pFaceData = new IntPtr[_levelCount];
			}
			uint num2 = System.Runtime.CompilerServices.Unsafe.As<_D3DVOLUME_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DVOLUME_DESC, 16));
			uint num3 = System.Runtime.CompilerServices.Unsafe.As<_D3DVOLUME_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DVOLUME_DESC, 20));
			uint num4 = System.Runtime.CompilerServices.Unsafe.As<_D3DVOLUME_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DVOLUME_DESC, 24));
			byte expectedByteSizeFromFormat = Texture.GetExpectedByteSizeFromFormat(*(_D3DFORMAT*)(&d3DVOLUME_DESC));
			int num5 = 0;
			int num6 = 0;
			if (0 < _levelCount)
			{
				System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DLOCKED_BOX d3DLOCKED_BOX);
				do
				{
					byte* ptr2 = null;
					int num7;
					if (!isStoring)
					{
						ref IntPtr reference = ref pFaceData[num5];
						num5++;
						ptr2 = (byte*)reference.ToPointer();
						num7 = 0;
					}
					else
					{
						num7 = 16;
					}
					ptr = pComPtr;
					num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DLOCKED_BOX*, _D3DBOX*, uint, int>)(int)(*(uint*)(*(int*)ptr + 76)))((nint)ptr, (uint)num6, &d3DLOCKED_BOX, null, (uint)num7);
					if (num < 0)
					{
						break;
					}
					uint num8 = expectedByteSizeFromFormat * num2;
					if (isStoring)
					{
						ptr2 = (byte*)_003CModule_003E.new_005B_005D(num8 * num4 * num3);
					}
					byte* ptr3 = ptr2;
					byte* ptr4 = (byte*)(int)System.Runtime.CompilerServices.Unsafe.As<_D3DLOCKED_BOX, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DLOCKED_BOX, 8));
					if (0 < num4)
					{
						uint num9 = num4;
						do
						{
							byte* ptr5 = ptr4;
							if (0 < num3)
							{
								uint num10 = num3;
								do
								{
									if (isStoring)
									{
										_003CModule_003E.memcpy_s(ptr3, num8 * num3, ptr5, num8);
									}
									else
									{
										_003CModule_003E.memcpy_s(ptr5, *(uint*)(&d3DLOCKED_BOX), ptr3, num8);
									}
									ptr5 = *(int*)(&d3DLOCKED_BOX) + ptr5;
									ptr3 = (int)num8 + ptr3;
									num10--;
								}
								while (num10 != 0);
							}
							ptr4 = System.Runtime.CompilerServices.Unsafe.As<_D3DLOCKED_BOX, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DLOCKED_BOX, 4)) + ptr4;
							num9--;
						}
						while (num9 != 0);
					}
					ptr = pComPtr;
					num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, int>)(int)(*(uint*)(*(int*)ptr + 80)))((nint)ptr, (uint)num6);
					if (num < 0)
					{
						break;
					}
					if (isStoring)
					{
						IntPtr intPtr = (IntPtr)ptr2;
						ref IntPtr reference2 = ref pFaceData[num5];
						num5++;
						reference2 = intPtr;
					}
					num2 = ((num2 <= 1) ? 1u : (num2 >> 1));
					num3 = ((num3 <= 1) ? 1u : (num3 >> 1));
					num4 = ((num4 <= 1) ? 1u : (num4 >> 1));
					num6++;
				}
				while (num6 < _levelCount);
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

	internal unsafe static Texture3D GetManagedObject(IDirect3DVolumeTexture9* pInterface, GraphicsDevice pDevice, uint pool)
	{
		Texture3D texture3D = pDevice.Resources.GetCachedObject(pInterface) as Texture3D;
		if (texture3D != null)
		{
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)pInterface + 8)))((nint)pInterface);
			texture3D.isDisposed = false;
			GC.ReRegisterForFinalize(texture3D);
		}
		else
		{
			texture3D = new Texture3D(pInterface, pDevice);
			pDevice.Resources.AddTrackedObject(texture3D, pInterface, pool, 0uL, ref texture3D._internalHandle);
		}
		return texture3D;
	}

	private void OnObjectCreation()
	{
		InitializeDescription(null);
	}

	private void _0021Texture3D()
	{
		if (!isDisposed)
		{
			ReleaseNativeObject(disposeManagedResource: true);
			CleanupSavedData();
		}
	}

	private void _007ETexture3D()
	{
		_0021Texture3D();
	}

	[HandleProcessCorruptedStateExceptions]
	protected override void Dispose([MarshalAs(UnmanagedType.U1)] bool P_0)
	{
		if (P_0)
		{
			try
			{
				_007ETexture3D();
				return;
			}
			finally
			{
				base.Dispose(true);
			}
		}
		try
		{
			_0021Texture3D();
		}
		finally
		{
			base.Dispose(false);
		}
	}
}
