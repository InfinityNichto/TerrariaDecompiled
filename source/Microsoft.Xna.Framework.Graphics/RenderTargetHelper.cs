using System;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

internal class RenderTargetHelper : IDisposable
{
	internal IDynamicGraphicsResource pTexture;

	internal unsafe IDirect3DSurface9* pRenderTargetSurface;

	internal unsafe IDirect3DSurface9* pDepthSurface;

	internal int width;

	internal int height;

	internal SurfaceFormat format;

	internal DepthFormat depthFormat;

	internal int multiSampleCount;

	internal RenderTargetUsage usage;

	internal bool isCubemap;

	internal int pixelSize;

	internal bool willItBlend;

	internal unsafe RenderTargetHelper(IDynamicGraphicsResource texture, int width, int height, SurfaceFormat format, DepthFormat depthFormat, int multiSampleCount, RenderTargetUsage usage, ProfileCapabilities profileCapabilities)
	{
		pTexture = texture;
		pRenderTargetSurface = null;
		pDepthSurface = null;
		this.width = width;
		this.height = height;
		this.format = format;
		this.depthFormat = depthFormat;
		this.multiSampleCount = multiSampleCount;
		this.usage = usage;
		base._002Ector();
		isCubemap = texture is TextureCube;
		pixelSize = Texture.GetExpectedByteSizeFromFormat(_003CModule_003E.ConvertXnaFormatToWindows(format));
		int num = ((!profileCapabilities.InvalidBlendFormats.Contains(format)) ? 1 : 0);
		willItBlend = (byte)num != 0;
	}

	private void _0021RenderTargetHelper()
	{
		ReleaseNativeObject();
	}

	private void _007ERenderTargetHelper()
	{
		ReleaseNativeObject();
	}

	internal unsafe void ReleaseNativeObject()
	{
		IDirect3DSurface9* ptr = pRenderTargetSurface;
		if (ptr != null)
		{
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)ptr + 8)))((nint)ptr);
			pRenderTargetSurface = null;
		}
		IDirect3DSurface9* ptr2 = pDepthSurface;
		if (ptr2 != null)
		{
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)ptr2 + 8)))((nint)ptr2);
			pDepthSurface = null;
		}
	}

	internal unsafe void CreateSurfaces(GraphicsDevice graphicsDevice)
	{
		if (multiSampleCount != 0)
		{
			fixed (IDirect3DSurface9** ptr = &pRenderTargetSurface)
			{
				try
				{
					int num = *(int*)graphicsDevice.pComPtr + 112;
					int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, _D3DFORMAT, _D3DMULTISAMPLE_TYPE, uint, int, IDirect3DSurface9**, void**, int>)(int)(*(uint*)num))((nint)graphicsDevice.pComPtr, (uint)width, (uint)height, _003CModule_003E.ConvertXnaFormatToWindows(format), (_D3DMULTISAMPLE_TYPE)multiSampleCount, 0u, 0, ptr, null);
					if (num2 < 0)
					{
						throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
					}
				}
				catch
				{
					//try-fault
					ptr = null;
					throw;
				}
			}
		}
		if (depthFormat != 0)
		{
			IntPtr intPtr = IntPtr.Zero;
			if (usage == RenderTargetUsage.DiscardContents)
			{
				intPtr = graphicsDevice.Resources.FindResourceW(GetSharedDepthSurface);
			}
			if (intPtr != IntPtr.Zero)
			{
				pDepthSurface = (IDirect3DSurface9*)intPtr.ToPointer();
			}
			else
			{
				fixed (IDirect3DSurface9** ptr2 = &pDepthSurface)
				{
					try
					{
						int num3 = *(int*)graphicsDevice.pComPtr + 116;
						int num4 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, _D3DFORMAT, _D3DMULTISAMPLE_TYPE, uint, int, IDirect3DSurface9**, void**, int>)(int)(*(uint*)num3))((nint)graphicsDevice.pComPtr, (uint)width, (uint)height, _003CModule_003E.ConvertXnaFormatToWindows(depthFormat), (_D3DMULTISAMPLE_TYPE)multiSampleCount, 0u, 0, ptr2, null);
						if (num4 < 0)
						{
							throw GraphicsHelpers.GetExceptionFromResult((uint)num4);
						}
					}
					catch
					{
						//try-fault
						ptr2 = null;
						throw;
					}
				}
			}
		}
		try
		{
			return;
		}
		catch
		{
			//try-fault
			throw;
		}
	}

	internal unsafe IntPtr GetSharedDepthSurface(object managedObject)
	{
		RenderTargetHelper helper;
		if (managedObject is RenderTarget2D renderTarget2D)
		{
			helper = renderTarget2D.helper;
		}
		else
		{
			if (!(managedObject is RenderTargetCube renderTargetCube))
			{
				return IntPtr.Zero;
			}
			helper = renderTargetCube.helper;
		}
		if (helper != null && helper.usage == RenderTargetUsage.DiscardContents && helper.width == width && helper.height == height && helper.depthFormat == depthFormat && helper.multiSampleCount == multiSampleCount)
		{
			IDirect3DSurface9* ptr = helper.pDepthSurface;
			if (ptr != null)
			{
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)ptr + 4)))((nint)ptr);
				return new IntPtr(ptr);
			}
		}
		return IntPtr.Zero;
	}

	internal unsafe IDirect3DSurface9* GetRenderTargetSurface(CubeMapFace faceType)
	{
		IDirect3DSurface9* ptr = null;
		IDirect3DSurface9* ptr2 = pRenderTargetSurface;
		if (ptr2 != null)
		{
			ptr = ptr2;
			IDirect3DSurface9* intPtr = ptr;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)intPtr + 4)))((nint)intPtr);
		}
		else if (isCubemap)
		{
			IDirect3DCubeTexture9* pComPtr = ((TextureCube)pTexture).pComPtr;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DCUBEMAP_FACES, uint, IDirect3DSurface9**, int>)(int)(*(uint*)(*(int*)pComPtr + 72)))((nint)pComPtr, (_D3DCUBEMAP_FACES)faceType, 0u, &ptr);
		}
		else
		{
			IDirect3DTexture9* pComPtr2 = ((Texture2D)pTexture).pComPtr;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, IDirect3DSurface9**, int>)(int)(*(uint*)(*(int*)pComPtr2 + 72)))((nint)pComPtr2, 0u, &ptr);
		}
		return ptr;
	}

	internal unsafe IDirect3DSurface9* GetDestinationSurface(CubeMapFace faceType, int mipLevel)
	{
		IDirect3DSurface9* result = null;
		if (isCubemap)
		{
			IDirect3DCubeTexture9* pComPtr = ((TextureCube)pTexture).pComPtr;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DCUBEMAP_FACES, uint, IDirect3DSurface9**, int>)(int)(*(uint*)(*(int*)pComPtr + 72)))((nint)pComPtr, (_D3DCUBEMAP_FACES)faceType, (uint)mipLevel, &result);
		}
		else
		{
			IDirect3DTexture9* pComPtr2 = ((Texture2D)pTexture).pComPtr;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, IDirect3DSurface9**, int>)(int)(*(uint*)(*(int*)pComPtr2 + 72)))((nint)pComPtr2, (uint)mipLevel, &result);
		}
		return result;
	}

	internal static RenderTargetHelper FromRenderTarget(Texture renderTarget)
	{
		if (renderTarget is RenderTarget2D renderTarget2D)
		{
			return renderTarget2D.helper;
		}
		if (renderTarget is RenderTargetCube renderTargetCube)
		{
			return renderTargetCube.helper;
		}
		return null;
	}

	[return: MarshalAs(UnmanagedType.U1)]
	internal static bool IsSameSize(Texture renderTargetA, Texture renderTargetB)
	{
		RenderTargetHelper renderTargetHelper = ((!(renderTargetA is RenderTarget2D renderTarget2D)) ? ((!(renderTargetA is RenderTargetCube renderTargetCube)) ? null : renderTargetCube.helper) : renderTarget2D.helper);
		RenderTargetHelper renderTargetHelper2 = ((!(renderTargetB is RenderTarget2D renderTarget2D2)) ? ((!(renderTargetB is RenderTargetCube renderTargetCube2)) ? null : renderTargetCube2.helper) : renderTarget2D2.helper);
		int num = ((renderTargetHelper.width == renderTargetHelper2.width && renderTargetHelper.height == renderTargetHelper2.height && renderTargetHelper.multiSampleCount == renderTargetHelper2.multiSampleCount && renderTargetHelper.pixelSize == renderTargetHelper2.pixelSize) ? 1 : 0);
		return (byte)num != 0;
	}

	[HandleProcessCorruptedStateExceptions]
	protected virtual void Dispose([MarshalAs(UnmanagedType.U1)] bool P_0)
	{
		if (P_0)
		{
			ReleaseNativeObject();
			return;
		}
		try
		{
			_0021RenderTargetHelper();
		}
		finally
		{
			base.Finalize();
		}
	}

	public virtual sealed void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	~RenderTargetHelper()
	{
		Dispose(false);
	}
}
