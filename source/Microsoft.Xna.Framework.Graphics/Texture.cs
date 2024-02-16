using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using std;

namespace Microsoft.Xna.Framework.Graphics;

public abstract class Texture : GraphicsResource
{
	private protected SurfaceFormat _format;

	private protected int _levelCount;

	internal bool alreadyRecreated;

	internal bool isActiveRenderTarget;

	internal bool renderTargetContentsDirty;

	internal unsafe StateTrackerTexture* pStateTracker;

	public int LevelCount => _levelCount;

	public SurfaceFormat Format => _format;

	private protected virtual bool MustClamp
	{
		[return: MarshalAs(UnmanagedType.U1)]
		get
		{
			return false;
		}
	}

	internal Texture()
	{
	}

	private protected unsafe void InitializeDescription(SurfaceFormat format)
	{
		_format = format;
		IDirect3DBaseTexture9* comPtr = GetComPtr();
		_levelCount = (int)((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)comPtr + 52)))((nint)comPtr);
		int num = CreateStateWrapper();
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
	}

	private protected unsafe int CreateStateWrapper()
	{
		ProfileCapabilities profileCapabilities = _parent._profileCapabilities;
		bool flag = profileCapabilities.ValidVertexTextureFormats.Contains(_format);
		bool flag2 = profileCapabilities.InvalidFilterFormats.Contains(_format);
		StateTrackerTexture* ptr = (StateTrackerTexture*)_003CModule_003E.@new(28u, (nothrow_t*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E.std_002Enothrow));
		StateTrackerTexture* ptr2;
		try
		{
			ptr2 = ((ptr == null) ? null : _003CModule_003E.Microsoft_002EXna_002EFramework_002EGraphics_002EStateTrackerTexture_002E_007Bctor_007D(ptr, GetComPtr(), (uint)_format, flag, flag2, MustClamp));
		}
		catch
		{
			//try-fault
			_003CModule_003E.delete(ptr, (nothrow_t*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E.std_002Enothrow));
			throw;
		}
		pStateTracker = ptr2;
		return (ptr2 == null) ? (-2147024882) : 0;
	}

	internal static byte GetExpectedByteSizeFromFormat(_D3DFORMAT format)
	{
		if (format <= (_D3DFORMAT)827606349)
		{
			if (format == (_D3DFORMAT)827606349)
			{
				return 4;
			}
			switch (format - 20)
			{
			case (_D3DFORMAT)0:
				return 3;
			case (_D3DFORMAT)1:
				return 4;
			case (_D3DFORMAT)2:
				return 4;
			case (_D3DFORMAT)15:
				return 4;
			case (_D3DFORMAT)12:
				return 4;
			case (_D3DFORMAT)13:
				return 4;
			case (_D3DFORMAT)11:
				return 4;
			case (_D3DFORMAT)14:
				return 4;
			case (_D3DFORMAT)16:
				return 8;
			case (_D3DFORMAT)3:
				return 2;
			case (_D3DFORMAT)5:
				return 2;
			case (_D3DFORMAT)4:
				return 2;
			case (_D3DFORMAT)6:
				return 2;
			case (_D3DFORMAT)10:
				return 2;
			case (_D3DFORMAT)9:
				return 2;
			case (_D3DFORMAT)8:
				return 1;
			case (_D3DFORMAT)7:
				return 1;
			case (_D3DFORMAT)40:
				return 2;
			case (_D3DFORMAT)43:
				return 4;
			case (_D3DFORMAT)44:
				return 4;
			case (_D3DFORMAT)90:
				return 8;
			case (_D3DFORMAT)94:
				return 4;
			case (_D3DFORMAT)95:
				return 8;
			case (_D3DFORMAT)96:
				return 16;
			case (_D3DFORMAT)91:
				return 2;
			case (_D3DFORMAT)92:
				return 4;
			case (_D3DFORMAT)93:
				return 8;
			case (_D3DFORMAT)30:
				return 1;
			case (_D3DFORMAT)61:
				return 2;
			case (_D3DFORMAT)32:
				return 1;
			case (_D3DFORMAT)31:
				return 2;
			case (_D3DFORMAT)21:
				return 1;
			case (_D3DFORMAT)20:
				return 2;
			case (_D3DFORMAT)41:
				return 2;
			case (_D3DFORMAT)42:
				return 4;
			case (_D3DFORMAT)47:
				return 4;
			case (_D3DFORMAT)97:
				return 2;
			case (_D3DFORMAT)55:
				return 4;
			case (_D3DFORMAT)63:
				return 4;
			case (_D3DFORMAT)59:
				return 4;
			case (_D3DFORMAT)57:
				return 4;
			case (_D3DFORMAT)51:
				return 4;
			case (_D3DFORMAT)60:
				return 2;
			case (_D3DFORMAT)53:
				return 2;
			}
		}
		else
		{
			switch (format)
			{
			case (_D3DFORMAT)844388420:
				return 1;
			case (_D3DFORMAT)827611204:
				return 1;
			case (_D3DFORMAT)844715353:
				return 2;
			case (_D3DFORMAT)861165636:
				return 1;
			case (_D3DFORMAT)877942852:
				return 1;
			case (_D3DFORMAT)1111970375:
				return 2;
			case (_D3DFORMAT)894720068:
				return 1;
			case (_D3DFORMAT)1195525970:
				return 2;
			case (_D3DFORMAT)1498831189:
				return 2;
			}
		}
		throw new InvalidOperationException();
	}

	internal unsafe static void GetAndValidateSizes<T>(_D3DSURFACE_DESC* pSurface, uint* pdwFormatSize, uint* pdwElementSize) where T : struct
	{
		if (pSurface == null || pdwFormatSize == null || pdwElementSize == null)
		{
			return;
		}
		*pdwFormatSize = GetExpectedByteSizeFromFormat(*(_D3DFORMAT*)pSurface);
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

	internal unsafe static void GetAndValidateRect(_D3DSURFACE_DESC* __unnamed000, uint* pdwLockWidth, uint* pdwLockHeight, Rectangle? rect)
	{
		if (__unnamed000 == null || pdwLockHeight == null || pdwLockWidth == null)
		{
			return;
		}
		*pdwLockWidth = *(uint*)((byte*)__unnamed000 + 24);
		*pdwLockHeight = *(uint*)((byte*)__unnamed000 + 28);
		if (!rect.HasValue)
		{
			return;
		}
		if (rect.Value.X >= 0 && rect.Value.Width > 0 && rect.Value.Y >= 0 && rect.Value.Height > 0)
		{
			Rectangle value = rect.Value;
			Rectangle value2 = rect.Value;
			if ((uint)(value.Left + value2.Width) <= (uint)(*(int*)((byte*)__unnamed000 + 24)))
			{
				Rectangle value3 = rect.Value;
				Rectangle value4 = rect.Value;
				if ((uint)(value3.Top + value4.Height) <= (uint)(*(int*)((byte*)__unnamed000 + 28)))
				{
					*pdwLockWidth = (uint)rect.Value.Width;
					*pdwLockHeight = (uint)rect.Value.Height;
					return;
				}
			}
			throw new ArgumentException(FrameworkResources.InvalidRectangle, "rect");
		}
		throw new ArgumentException(FrameworkResources.InvalidRectangle, "rect");
	}

	internal unsafe static void ValidateTotalSize(_D3DSURFACE_DESC* __unnamed000, uint dwLockWidth, uint dwLockHeight, uint dwFormatSize, uint dwElementSize, uint elementCount)
	{
		int num = *(int*)__unnamed000;
		_D3DFORMAT d3DFORMAT = (_D3DFORMAT)num;
		int num2 = ((d3DFORMAT == (_D3DFORMAT)827611204 || d3DFORMAT == (_D3DFORMAT)844388420 || d3DFORMAT == (_D3DFORMAT)861165636 || d3DFORMAT == (_D3DFORMAT)877942852 || d3DFORMAT == (_D3DFORMAT)894720068) ? 1 : 0);
		if ((byte)num2 != 0)
		{
			dwLockWidth = dwLockWidth + 3 >> 2;
			dwLockHeight = dwLockHeight + 3 >> 2;
			dwFormatSize = ((num == 827611204) ? 8u : 16u);
		}
		uint num3 = dwLockWidth * dwLockHeight * dwFormatSize;
		if (dwElementSize * elementCount != num3)
		{
			throw new ArgumentException(FrameworkResources.InvalidTotalSize);
		}
	}

	[return: MarshalAs(UnmanagedType.U1)]
	internal static bool CheckCompressedTexture(_D3DFORMAT format)
	{
		int num = ((format == (_D3DFORMAT)827611204 || format == (_D3DFORMAT)844388420 || format == (_D3DFORMAT)861165636 || format == (_D3DFORMAT)877942852 || format == (_D3DFORMAT)894720068) ? 1 : 0);
		return (byte)num != 0;
	}

	internal unsafe static void CopyData<T>(void* pData, int pitch, T[] data, int dataIndex, int elementCount, _D3DSURFACE_DESC* pSurface, uint dwLockWidth, uint dwLockHeight, [MarshalAs(UnmanagedType.U1)] bool isSetting) where T : struct
	{
		if (pSurface == null)
		{
			return;
		}
		fixed (T* ptr2 = &data[dataIndex])
		{
			byte* ptr = (byte*)pData;
			byte* ptr3 = (byte*)ptr2;
			uint num = (uint)System.Runtime.CompilerServices.Unsafe.SizeOf<T>();
			uint num2 = GetExpectedByteSizeFromFormat(*(_D3DFORMAT*)pSurface) / num;
			int num3 = *(int*)pSurface;
			_D3DFORMAT d3DFORMAT = (_D3DFORMAT)num3;
			int num4 = ((d3DFORMAT == (_D3DFORMAT)827611204 || d3DFORMAT == (_D3DFORMAT)844388420 || d3DFORMAT == (_D3DFORMAT)861165636 || d3DFORMAT == (_D3DFORMAT)877942852 || d3DFORMAT == (_D3DFORMAT)894720068) ? 1 : 0);
			if ((byte)num4 != 0)
			{
				dwLockWidth = dwLockWidth + 3 >> 2;
				dwLockHeight = dwLockHeight + 3 >> 2;
				num2 = ((num3 == 827611204) ? 8u : 16u);
			}
			uint num5 = num2 * num * dwLockWidth;
			if (0 >= dwLockHeight)
			{
				return;
			}
			uint num6 = dwLockHeight;
			do
			{
				if (*(int*)pSurface == 21)
				{
					if (isSetting)
					{
						SwapBgr(ptr, ptr3, num5);
					}
					else
					{
						SwapBgr(ptr3, ptr, num5);
					}
				}
				else if (isSetting)
				{
					_003CModule_003E.memcpy_s(ptr, num5, ptr3, num5);
				}
				else
				{
					_003CModule_003E.memcpy_s(ptr3, num5, ptr, num5);
				}
				ptr = pitch + ptr;
				ptr3 = (int)num5 + ptr3;
				num6--;
			}
			while (num6 != 0);
		}
	}

	internal unsafe static void SwapBgr(void* pDest, void* pSrc, uint dwSize)
	{
		uint* ptr = (uint*)pDest;
		if (dwSize >= 4)
		{
			int num = (int)((byte*)pSrc - (nuint)pDest);
			uint num2 = dwSize >> 2;
			do
			{
				uint num3 = *(uint*)(num + (byte*)ptr);
				*ptr = ((num3 >> 16) & 0xFFu) | ((num3 & 0xFF) << 16) | (num3 & 0xFF00FF00u);
				ptr++;
				num2--;
			}
			while (num2 != 0);
		}
	}

	[return: MarshalAs(UnmanagedType.U1)]
	internal static bool IsPowerOfTwo(uint dwNumber)
	{
		if (dwNumber != 0 && ((dwNumber - 1) & dwNumber) == 0)
		{
			return true;
		}
		return false;
	}

	internal unsafe abstract IDirect3DBaseTexture9* GetComPtr();

	internal unsafe int CompareTo(Texture other)
	{
		IDirect3DBaseTexture9* comPtr = GetComPtr();
		IDirect3DBaseTexture9* comPtr2 = other.GetComPtr();
		if (comPtr > comPtr2)
		{
			return -1;
		}
		return (comPtr < comPtr2) ? 1 : 0;
	}
}
