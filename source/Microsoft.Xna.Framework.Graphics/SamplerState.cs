using System;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public class SamplerState : GraphicsResource
{
	internal TextureFilter cachedFilter;

	internal TextureAddressMode cachedAddressU;

	internal TextureAddressMode cachedAddressV;

	internal TextureAddressMode cachedAddressW;

	internal int cachedMaxAnisotropy;

	internal int cachedMaxMipLevel;

	internal float cachedMipMapLevelOfDetailBias;

	public static readonly SamplerState PointWrap = new SamplerState(TextureFilter.Point, TextureAddressMode.Wrap, "SamplerState.PointWrap");

	public static readonly SamplerState PointClamp = new SamplerState(TextureFilter.Point, TextureAddressMode.Clamp, "SamplerState.PointClamp");

	public static readonly SamplerState LinearWrap = new SamplerState(TextureFilter.Linear, TextureAddressMode.Wrap, "SamplerState.LinearWrap");

	public static readonly SamplerState LinearClamp = new SamplerState(TextureFilter.Linear, TextureAddressMode.Clamp, "SamplerState.LinearClamp");

	public static readonly SamplerState AnisotropicWrap = new SamplerState(TextureFilter.Anisotropic, TextureAddressMode.Wrap, "SamplerState.AnisotropicWrap");

	public static readonly SamplerState AnisotropicClamp = new SamplerState(TextureFilter.Anisotropic, TextureAddressMode.Clamp, "SamplerState.AnisotropicClamp");

	internal bool isBound;

	internal _D3DTEXTUREFILTERTYPE minFilter;

	internal _D3DTEXTUREFILTERTYPE magFilter;

	internal _D3DTEXTUREFILTERTYPE mipFilter;

	internal _D3DTEXTUREADDRESS d3dAddressU;

	internal _D3DTEXTUREADDRESS d3dAddressV;

	internal _D3DTEXTUREADDRESS d3dAddressW;

	internal uint filterMinFlag;

	internal uint filterMagFlag;

	internal uint filterMipFlag;

	internal uint nonClampAddressUFlag;

	internal uint nonClampAddressVFlag;

	public float MipMapLevelOfDetailBias
	{
		get
		{
			return cachedMipMapLevelOfDetailBias;
		}
		set
		{
			ThrowIfBound();
			cachedMipMapLevelOfDetailBias = value;
		}
	}

	public int MaxMipLevel
	{
		get
		{
			return cachedMaxMipLevel;
		}
		set
		{
			ThrowIfBound();
			cachedMaxMipLevel = value;
		}
	}

	public int MaxAnisotropy
	{
		get
		{
			return cachedMaxAnisotropy;
		}
		set
		{
			ThrowIfBound();
			cachedMaxAnisotropy = value;
		}
	}

	public TextureAddressMode AddressW
	{
		get
		{
			return cachedAddressW;
		}
		set
		{
			ThrowIfBound();
			cachedAddressW = value;
		}
	}

	public TextureAddressMode AddressV
	{
		get
		{
			return cachedAddressV;
		}
		set
		{
			ThrowIfBound();
			cachedAddressV = value;
		}
	}

	public TextureAddressMode AddressU
	{
		get
		{
			return cachedAddressU;
		}
		set
		{
			ThrowIfBound();
			cachedAddressU = value;
		}
	}

	public TextureFilter Filter
	{
		get
		{
			return cachedFilter;
		}
		set
		{
			ThrowIfBound();
			cachedFilter = value;
		}
	}

	private void SetDefaults()
	{
		ThrowIfBound();
		cachedFilter = TextureFilter.Linear;
		ThrowIfBound();
		cachedAddressU = TextureAddressMode.Wrap;
		ThrowIfBound();
		cachedAddressV = TextureAddressMode.Wrap;
		ThrowIfBound();
		cachedAddressW = TextureAddressMode.Wrap;
		ThrowIfBound();
		cachedMaxAnisotropy = 4;
		ThrowIfBound();
		cachedMaxMipLevel = 0;
		ThrowIfBound();
		cachedMipMapLevelOfDetailBias = 0f;
	}

	public SamplerState()
	{
		try
		{
			SetDefaults();
			isBound = false;
			return;
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
	}

	private SamplerState(TextureFilter filter, TextureAddressMode address, string name)
	{
		try
		{
			SetDefaults();
			ThrowIfBound();
			cachedFilter = filter;
			ThrowIfBound();
			cachedAddressU = address;
			ThrowIfBound();
			cachedAddressV = address;
			ThrowIfBound();
			cachedAddressW = address;
			base.Name = name;
			isBound = true;
			return;
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
	}

	private void _007ESamplerState()
	{
	}

	internal unsafe void Apply(GraphicsDevice device, int samplerIndex)
	{
		if (isDisposed)
		{
			throw new ObjectDisposedException(typeof(SamplerState).Name);
		}
		if (_parent != device)
		{
			_parent = device;
			isBound = true;
			uint num = *(uint*)((byte*)device.d3dCaps.t + 64);
			switch (cachedFilter)
			{
			default:
				minFilter = (_D3DTEXTUREFILTERTYPE)2;
				magFilter = (_D3DTEXTUREFILTERTYPE)2;
				mipFilter = (_D3DTEXTUREFILTERTYPE)2;
				break;
			case TextureFilter.Point:
				minFilter = (_D3DTEXTUREFILTERTYPE)1;
				magFilter = (_D3DTEXTUREFILTERTYPE)1;
				mipFilter = (_D3DTEXTUREFILTERTYPE)1;
				break;
			case TextureFilter.Anisotropic:
			{
				_D3DTEXTUREFILTERTYPE d3DTEXTUREFILTERTYPE = (_D3DTEXTUREFILTERTYPE)(((num & 0x400) | 0x800) >> 10);
				minFilter = d3DTEXTUREFILTERTYPE;
				_D3DTEXTUREFILTERTYPE d3DTEXTUREFILTERTYPE2 = (_D3DTEXTUREFILTERTYPE)(((num & 0x4000000) | 0x8000000) >> 26);
				magFilter = d3DTEXTUREFILTERTYPE2;
				mipFilter = (_D3DTEXTUREFILTERTYPE)2;
				break;
			}
			case TextureFilter.LinearMipPoint:
				minFilter = (_D3DTEXTUREFILTERTYPE)2;
				magFilter = (_D3DTEXTUREFILTERTYPE)2;
				mipFilter = (_D3DTEXTUREFILTERTYPE)1;
				break;
			case TextureFilter.PointMipLinear:
				minFilter = (_D3DTEXTUREFILTERTYPE)1;
				magFilter = (_D3DTEXTUREFILTERTYPE)1;
				mipFilter = (_D3DTEXTUREFILTERTYPE)2;
				break;
			case TextureFilter.MinLinearMagPointMipLinear:
				minFilter = (_D3DTEXTUREFILTERTYPE)2;
				magFilter = (_D3DTEXTUREFILTERTYPE)1;
				mipFilter = (_D3DTEXTUREFILTERTYPE)2;
				break;
			case TextureFilter.MinLinearMagPointMipPoint:
				minFilter = (_D3DTEXTUREFILTERTYPE)2;
				magFilter = (_D3DTEXTUREFILTERTYPE)1;
				mipFilter = (_D3DTEXTUREFILTERTYPE)1;
				break;
			case TextureFilter.MinPointMagLinearMipLinear:
				minFilter = (_D3DTEXTUREFILTERTYPE)1;
				magFilter = (_D3DTEXTUREFILTERTYPE)2;
				mipFilter = (_D3DTEXTUREFILTERTYPE)2;
				break;
			case TextureFilter.MinPointMagLinearMipPoint:
				minFilter = (_D3DTEXTUREFILTERTYPE)1;
				magFilter = (_D3DTEXTUREFILTERTYPE)2;
				mipFilter = (_D3DTEXTUREFILTERTYPE)1;
				break;
			}
			d3dAddressU = _003CModule_003E.ConvertXnaAddressModeToDx(cachedAddressU);
			d3dAddressV = _003CModule_003E.ConvertXnaAddressModeToDx(cachedAddressV);
			d3dAddressW = _003CModule_003E.ConvertXnaAddressModeToDx(cachedAddressW);
			int num2 = ((minFilter != (_D3DTEXTUREFILTERTYPE)1) ? 1 : 0);
			filterMinFlag = (uint)num2;
			int num3 = ((magFilter != (_D3DTEXTUREFILTERTYPE)1) ? 1 : 0);
			filterMagFlag = (uint)num3;
			int num4 = ((mipFilter != (_D3DTEXTUREFILTERTYPE)1) ? 1 : 0);
			filterMipFlag = (uint)num4;
			int num5 = ((d3dAddressU != (_D3DTEXTUREADDRESS)3) ? 1 : 0);
			nonClampAddressUFlag = (uint)num5;
			int num6 = ((d3dAddressV != (_D3DTEXTUREADDRESS)3) ? 1 : 0);
			nonClampAddressVFlag = (uint)num6;
		}
		IntPtr pComPtr = (IntPtr)device.pComPtr;
		Helpers.CheckDisposed(device, pComPtr);
		int num7 = ((samplerIndex < 257) ? samplerIndex : (samplerIndex - 241));
		EffectPass activePass = device.activePass;
		if (activePass != null && ((uint)activePass._stateFlags & (uint)(8 << num7)) != 0)
		{
			activePass.EndPass();
			device.activePass = null;
		}
		IDirect3DDevice9* pComPtr2 = device.pComPtr;
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DSAMPLERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 276)))((nint)pComPtr2, (uint)samplerIndex, (_D3DSAMPLERSTATETYPE)6, (uint)minFilter);
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DSAMPLERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 276)))((nint)pComPtr2, (uint)samplerIndex, (_D3DSAMPLERSTATETYPE)5, (uint)magFilter);
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DSAMPLERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 276)))((nint)pComPtr2, (uint)samplerIndex, (_D3DSAMPLERSTATETYPE)7, (uint)mipFilter);
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DSAMPLERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 276)))((nint)pComPtr2, (uint)samplerIndex, (_D3DSAMPLERSTATETYPE)1, (uint)d3dAddressU);
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DSAMPLERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 276)))((nint)pComPtr2, (uint)samplerIndex, (_D3DSAMPLERSTATETYPE)2, (uint)d3dAddressV);
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DSAMPLERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 276)))((nint)pComPtr2, (uint)samplerIndex, (_D3DSAMPLERSTATETYPE)3, (uint)d3dAddressW);
		uint num8 = *(uint*)((byte*)device.d3dCaps.t + 108);
		int num9 = cachedMaxAnisotropy;
		uint num10 = (((uint)num9 >= num8) ? num8 : ((uint)num9));
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DSAMPLERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 276)))((nint)pComPtr2, (uint)samplerIndex, (_D3DSAMPLERSTATETYPE)10, num10);
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DSAMPLERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 276)))((nint)pComPtr2, (uint)samplerIndex, (_D3DSAMPLERSTATETYPE)9, (uint)cachedMaxMipLevel);
		float num11 = cachedMipMapLevelOfDetailBias;
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DSAMPLERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 276)))((nint)pComPtr2, (uint)samplerIndex, (_D3DSAMPLERSTATETYPE)8, *(uint*)(&num11));
		StateTrackerDevice* pStateTracker = device.pStateTracker;
		uint num12 = (uint)(~(1 << num7));
		*(int*)((byte*)pStateTracker + 116) = (int)(filterMinFlag << num7) | (*(int*)((byte*)pStateTracker + 116) & (int)num12);
		*(int*)((byte*)pStateTracker + 120) = (int)(filterMagFlag << num7) | ((int)num12 & *(int*)((byte*)pStateTracker + 120));
		*(int*)((byte*)pStateTracker + 124) = (int)(filterMipFlag << num7) | (*(int*)((byte*)pStateTracker + 124) & (int)num12);
		*(int*)((byte*)pStateTracker + 128) = (int)(nonClampAddressUFlag << num7) | (*(int*)((byte*)pStateTracker + 128) & (int)num12);
		*(int*)((byte*)pStateTracker + 132) = (int)(nonClampAddressVFlag << num7) | (*(int*)((byte*)pStateTracker + 132) & (int)num12);
	}

	internal void ThrowIfBound()
	{
		if (isBound)
		{
			throw new InvalidOperationException(string.Format(args: new object[1] { typeof(SamplerState).Name }, provider: CultureInfo.CurrentCulture, format: FrameworkResources.BoundStateObject));
		}
	}

	[HandleProcessCorruptedStateExceptions]
	protected override void Dispose([MarshalAs(UnmanagedType.U1)] bool P_0)
	{
		if (P_0)
		{
			try
			{
				_007ESamplerState();
				return;
			}
			finally
			{
				base.Dispose(true);
			}
		}
		base.Dispose(false);
	}
}
