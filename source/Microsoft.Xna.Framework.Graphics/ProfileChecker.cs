using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

internal static class ProfileChecker
{
	public static _D3DFORMAT IRRELEVANT_ADAPTER_FORMAT = (_D3DFORMAT)22;

	[return: MarshalAs(UnmanagedType.U1)]
	public unsafe static bool IsProfileSupported(IDirect3D9* pD3D, uint adapter, _D3DDEVTYPE deviceType, GraphicsProfile graphicsProfile)
	{
		ProfileCapabilities instance = ProfileCapabilities.GetInstance(graphicsProfile);
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DCAPS9 d3DCAPS);
		if (((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DDEVTYPE, _D3DCAPS9*, int>)(int)(*(uint*)(*(int*)pD3D + 56)))((nint)pD3D, adapter, deviceType, &d3DCAPS) < 0)
		{
			return false;
		}
		if (deviceType == (_D3DDEVTYPE)1 && (System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 28)) & 0x10000) == 0)
		{
			System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 196)) = -130560;
			System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 236)) = 15;
		}
		if ((uint)(System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 196)) & 0xFFFF) < instance.VertexShaderVersion)
		{
			return false;
		}
		if ((uint)(System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 204)) & 0xFFFF) < instance.PixelShaderVersion)
		{
			return false;
		}
		if ((uint)System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 180)) < (uint)instance.MaxPrimitiveCount)
		{
			return false;
		}
		if ((uint)System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 188)) < (uint)instance.MaxVertexStreams)
		{
			return false;
		}
		if ((uint)System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 192)) < (uint)instance.MaxStreamStride)
		{
			return false;
		}
		int num = (instance.IndexElementSize32 ? 16777214 : 65534);
		if ((uint)System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 184)) < (uint)num)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 212)) & 0x10) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 212)) & 1) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 36)) & 0x4000000) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 36)) & 0x2000) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 36)) & 0x1000000) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 36)) & 0x2000000) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 56)) & 8) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 56)) & 0x4000) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 32)) & 2) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 32)) & 0x10) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 32)) & 0x20) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 32)) & 0x40) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 32)) & 0x80) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 32)) & 0x800) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 84)) & 4) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 84)) & 1) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 84)) & 2) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 40)) & 0x80) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 40)) & 4) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 40)) & 0x10) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 40)) & 0x40) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 40)) & 2) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 40)) & 8) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 40)) & 1) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 40)) & 0x20) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 136)) & 1) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 136)) & 2) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 136)) & 4) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 136)) & 8) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 136)) & 0x10) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 136)) & 0x20) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 136)) & 0x40) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 136)) & 0x80) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 136)) & 0x100) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 44)) & 0x2000) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 44)) & 0x40) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 44)) & 0x100) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 44)) & 0x80) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 44)) & 0x200) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 44)) & 0x20) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 44)) & 8) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 44)) & 2) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 44)) & 0x10) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 44)) & 0x400) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 44)) & 4) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 44)) & 1) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 48)) & 0x2000) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 48)) & 0x40) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 48)) & 0x100) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 48)) & 0x80) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 48)) & 0x200) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 48)) & 0x20) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 48)) & 8) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 48)) & 2) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 48)) & 0x10) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 48)) & 4) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 48)) & 1) == 0)
		{
			return false;
		}
		if (instance.DestBlendSrcAlphaSat && (System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 48)) & 0x400) == 0)
		{
			return false;
		}
		if (instance.SeparateAlphaBlend && (System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 32)) & 0x20000) == 0)
		{
			return false;
		}
		int maxRenderTargets = instance.MaxRenderTargets;
		if ((uint)System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 240)) < (uint)maxRenderTargets)
		{
			return false;
		}
		if (maxRenderTargets > 1)
		{
			if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 32)) & 0x4000) == 0)
			{
				return false;
			}
			if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 32)) & 0x80000) == 0)
			{
				return false;
			}
		}
		int maxTextureSize = instance.MaxTextureSize;
		if ((uint)System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 88)) < (uint)maxTextureSize)
		{
			return false;
		}
		if ((uint)System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 92)) < (uint)maxTextureSize)
		{
			return false;
		}
		if (System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 104)) != 0 && (uint)System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 104)) < (uint)instance.MaxTextureAspectRatio)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 60)) & 4) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 60)) & 0x4000) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 60)) & 0x800) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 60)) & 0x10000) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 60)) & 1) == 0)
		{
			return false;
		}
		if (((uint)System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 60)) & 0x20u) != 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 76)) & 4) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 76)) & 1) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 76)) & 2) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 76)) & 0x10) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 64)) & 0x1000000) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 64)) & 0x2000000) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 64)) & 0x100) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 64)) & 0x200) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 64)) & 0x10000) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 64)) & 0x20000) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 68)) & 0x1000000) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 68)) & 0x2000000) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 68)) & 0x100) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 68)) & 0x200) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 68)) & 0x10000) == 0)
		{
			return false;
		}
		if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 68)) & 0x20000) == 0)
		{
			return false;
		}
		int maxVolumeExtent = instance.MaxVolumeExtent;
		if (maxVolumeExtent > 0)
		{
			if ((uint)System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 96)) < (uint)maxVolumeExtent)
			{
				return false;
			}
			if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 60)) & 0x2000) == 0)
			{
				return false;
			}
			if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 60)) & 0x8000) == 0)
			{
				return false;
			}
			if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 80)) & 4) == 0)
			{
				return false;
			}
			if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 80)) & 1) == 0)
			{
				return false;
			}
			if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 80)) & 2) == 0)
			{
				return false;
			}
			if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 80)) & 0x10) == 0)
			{
				return false;
			}
			if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 72)) & 0x1000000) == 0)
			{
				return false;
			}
			if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 72)) & 0x2000000) == 0)
			{
				return false;
			}
			if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 72)) & 0x100) == 0)
			{
				return false;
			}
			if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 72)) & 0x200) == 0)
			{
				return false;
			}
			if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 72)) & 0x10000) == 0)
			{
				return false;
			}
			if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 72)) & 0x20000) == 0)
			{
				return false;
			}
		}
		if (instance.NonPow2Unconditional)
		{
			if (((uint)System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 60)) & 2u) != 0)
			{
				return false;
			}
		}
		else if (((uint)System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 60)) & 2u) != 0 && (System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 60)) & 0x100) == 0)
		{
			return false;
		}
		if (instance.NonPow2Cube && ((uint)System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 60)) & 0x20000u) != 0)
		{
			return false;
		}
		if (instance.NonPow2Volume && ((uint)System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 60)) & 0x40000u) != 0)
		{
			return false;
		}
		if (instance.MaxVertexSamplers > 0)
		{
			if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 284)) & 0x1000000) == 0)
			{
				return false;
			}
			if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 284)) & 0x100) == 0)
			{
				return false;
			}
		}
		List<VertexElementFormat>.Enumerator enumerator = instance.ValidVertexFormats.GetEnumerator();
		if (enumerator.MoveNext())
		{
			do
			{
				switch (enumerator.Current)
				{
				case VertexElementFormat.Color:
					if (((uint)System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 236)) & 2u) != 0)
					{
						break;
					}
					return false;
				case VertexElementFormat.Byte4:
					if (((uint)System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 236)) & (true ? 1u : 0u)) != 0)
					{
						break;
					}
					return false;
				case VertexElementFormat.NormalizedShort2:
					if (((uint)System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 236)) & 4u) != 0)
					{
						break;
					}
					return false;
				case VertexElementFormat.NormalizedShort4:
					if (((uint)System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 236)) & 8u) != 0)
					{
						break;
					}
					return false;
				case VertexElementFormat.HalfVector2:
					if (((uint)System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 236)) & 0x100u) != 0)
					{
						break;
					}
					return false;
				case VertexElementFormat.HalfVector4:
					if ((System.Runtime.CompilerServices.Unsafe.As<_D3DCAPS9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DCAPS, 236)) & 0x200) == 0)
					{
						return false;
					}
					break;
				}
			}
			while (enumerator.MoveNext());
		}
		List<SurfaceFormat>.Enumerator enumerator2 = instance.ValidTextureFormats.GetEnumerator();
		if (enumerator2.MoveNext())
		{
			do
			{
				SurfaceFormat current = enumerator2.Current;
				if (!CheckTextureFormat(instance, pD3D, adapter, deviceType, (_D3DRESOURCETYPE)3, current))
				{
					return false;
				}
			}
			while (enumerator2.MoveNext());
		}
		List<SurfaceFormat>.Enumerator enumerator3 = instance.ValidCubeFormats.GetEnumerator();
		if (enumerator3.MoveNext())
		{
			do
			{
				SurfaceFormat current2 = enumerator3.Current;
				if (!CheckTextureFormat(instance, pD3D, adapter, deviceType, (_D3DRESOURCETYPE)5, current2))
				{
					return false;
				}
			}
			while (enumerator3.MoveNext());
		}
		List<SurfaceFormat>.Enumerator enumerator4 = instance.ValidVolumeFormats.GetEnumerator();
		if (enumerator4.MoveNext())
		{
			do
			{
				SurfaceFormat current3 = enumerator4.Current;
				if (!CheckTextureFormat(instance, pD3D, adapter, deviceType, (_D3DRESOURCETYPE)4, current3))
				{
					return false;
				}
			}
			while (enumerator4.MoveNext());
		}
		List<SurfaceFormat>.Enumerator enumerator5 = instance.ValidVertexTextureFormats.GetEnumerator();
		if (enumerator5.MoveNext())
		{
			do
			{
				SurfaceFormat current4 = enumerator5.Current;
				if (!CheckVertexTextureFormat(instance, pD3D, adapter, deviceType, current4))
				{
					return false;
				}
			}
			while (enumerator5.MoveNext());
		}
		if (!CheckRenderTargetFormat(instance, pD3D, adapter, deviceType, SurfaceFormat.Color))
		{
			return false;
		}
		if (instance.ValidTextureFormats.Contains(SurfaceFormat.HdrBlendable) && !CheckRenderTargetFormat(instance, pD3D, adapter, deviceType, SurfaceFormat.HdrBlendable))
		{
			return false;
		}
		return true;
	}

	[return: MarshalAs(UnmanagedType.U1)]
	private unsafe static bool CheckTextureFormat(ProfileCapabilities profileCapabilities, IDirect3D9* pD3D, uint adapter, _D3DDEVTYPE deviceType, _D3DRESOURCETYPE resourceType, SurfaceFormat format)
	{
		_D3DFORMAT d3DFORMAT = _003CModule_003E.ConvertXnaFormatToWindows(format);
		int num = *(int*)pD3D + 40;
		if (((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DDEVTYPE, _D3DFORMAT, uint, _D3DRESOURCETYPE, _D3DFORMAT, int>)(int)(*(uint*)num))((nint)pD3D, adapter, deviceType, IRRELEVANT_ADAPTER_FORMAT, 0u, resourceType, d3DFORMAT) < 0)
		{
			return false;
		}
		int num2 = *(int*)pD3D + 40;
		if (((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DDEVTYPE, _D3DFORMAT, uint, _D3DRESOURCETYPE, _D3DFORMAT, int>)(int)(*(uint*)num2))((nint)pD3D, adapter, deviceType, IRRELEVANT_ADAPTER_FORMAT, 2097152u, resourceType, d3DFORMAT) < 0)
		{
			return false;
		}
		if (!profileCapabilities.InvalidFilterFormats.Contains(format))
		{
			int num3 = *(int*)pD3D + 40;
			if (((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DDEVTYPE, _D3DFORMAT, uint, _D3DRESOURCETYPE, _D3DFORMAT, int>)(int)(*(uint*)num3))((nint)pD3D, adapter, deviceType, IRRELEVANT_ADAPTER_FORMAT, 131072u, resourceType, d3DFORMAT) < 0)
			{
				return false;
			}
		}
		return true;
	}

	[return: MarshalAs(UnmanagedType.U1)]
	private unsafe static bool CheckVertexTextureFormat(ProfileCapabilities profileCapabilities, IDirect3D9* pD3D, uint adapter, _D3DDEVTYPE deviceType, SurfaceFormat format)
	{
		_D3DFORMAT d3DFORMAT = _003CModule_003E.ConvertXnaFormatToWindows(format);
		uint num = 3145728u;
		if (!profileCapabilities.InvalidFilterFormats.Contains(format))
		{
			num = 3276800u;
		}
		int num2 = *(int*)pD3D + 40;
		if (((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DDEVTYPE, _D3DFORMAT, uint, _D3DRESOURCETYPE, _D3DFORMAT, int>)(int)(*(uint*)num2))((nint)pD3D, adapter, deviceType, IRRELEVANT_ADAPTER_FORMAT, num, (_D3DRESOURCETYPE)3, d3DFORMAT) < 0)
		{
			return false;
		}
		int num3 = *(int*)pD3D + 40;
		if (((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DDEVTYPE, _D3DFORMAT, uint, _D3DRESOURCETYPE, _D3DFORMAT, int>)(int)(*(uint*)num3))((nint)pD3D, adapter, deviceType, IRRELEVANT_ADAPTER_FORMAT, num, (_D3DRESOURCETYPE)5, d3DFORMAT) < 0)
		{
			return false;
		}
		int num4 = *(int*)pD3D + 40;
		return ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DDEVTYPE, _D3DFORMAT, uint, _D3DRESOURCETYPE, _D3DFORMAT, int>)(int)(*(uint*)num4))((nint)pD3D, adapter, deviceType, IRRELEVANT_ADAPTER_FORMAT, num, (_D3DRESOURCETYPE)4, d3DFORMAT) >= 0;
	}

	[return: MarshalAs(UnmanagedType.U1)]
	private unsafe static bool CheckRenderTargetFormat(ProfileCapabilities profileCapabilities, IDirect3D9* pD3D, uint adapter, _D3DDEVTYPE deviceType, SurfaceFormat format)
	{
		uint num = 1u;
		if (!profileCapabilities.InvalidBlendFormats.Contains(format))
		{
			num = 524289u;
		}
		int num2 = *(int*)pD3D + 40;
		return ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DDEVTYPE, _D3DFORMAT, uint, _D3DRESOURCETYPE, _D3DFORMAT, int>)(int)(*(uint*)num2))((nint)pD3D, adapter, deviceType, IRRELEVANT_ADAPTER_FORMAT, num, (_D3DRESOURCETYPE)1, _003CModule_003E.ConvertXnaFormatToWindows(format)) >= 0;
	}
}
