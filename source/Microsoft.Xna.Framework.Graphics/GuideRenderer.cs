using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.GamerServices;
using std;

namespace Microsoft.Xna.Framework.Graphics;

internal class GuideRenderer : IDisposable
{
	private IGuideRendererProxySource proxySource;

	private IGraphicsDeviceService graphicsDeviceService;

	private GraphicsDevice graphicsDevice;

	private int deviceResetCount;

	private Dictionary<int, IntPtr> resourceTable;

	private unsafe IDirect3DStateBlock9* pStateBlock;

	public GuideRenderer(IGuideRendererProxySource proxySource)
	{
		this.proxySource = proxySource;
		resourceTable = new Dictionary<int, IntPtr>();
		deviceResetCount = -1;
		if ((graphicsDeviceService = proxySource.ServiceProvider.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService) == null)
		{
			throw new InvalidOperationException(FrameworkResources.NoGraphicsDevice);
		}
		graphicsDeviceService.DeviceCreated += GraphicsDeviceCreated;
		graphicsDeviceService.DeviceResetting += GraphicsDeviceResetting;
		graphicsDeviceService.DeviceReset += GraphicsDeviceReset;
		graphicsDeviceService.DeviceDisposing += GraphicsDeviceDisposing;
		if (graphicsDeviceService.GraphicsDevice != null)
		{
			GraphicsDeviceCreated(graphicsDeviceService, EventArgs.Empty);
		}
	}

	private void _007EGuideRenderer()
	{
		if (graphicsDeviceService != null)
		{
			graphicsDeviceService.DeviceCreated -= GraphicsDeviceCreated;
			graphicsDeviceService.DeviceResetting -= GraphicsDeviceResetting;
			graphicsDeviceService.DeviceReset -= GraphicsDeviceReset;
			graphicsDeviceService.DeviceDisposing -= GraphicsDeviceDisposing;
		}
		_ = EventArgs.Empty;
		if (graphicsDevice != null)
		{
			graphicsDevice.DrawGuide -= DrawGuide;
			graphicsDevice = null;
		}
		DestroyResources();
	}

	private void _0021GuideRenderer()
	{
		DestroyResources();
	}

	private unsafe void DestroyResources()
	{
		IDirect3DStateBlock9* ptr = pStateBlock;
		if (ptr != null)
		{
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)ptr + 8)))((nint)ptr);
			pStateBlock = null;
		}
		Dictionary<int, IntPtr> dictionary = resourceTable;
		if (dictionary == null)
		{
			return;
		}
		Dictionary<int, IntPtr>.ValueCollection.Enumerator enumerator = dictionary.Values.GetEnumerator();
		if (enumerator.MoveNext())
		{
			do
			{
				IUnknown* ptr2 = (IUnknown*)enumerator.Current.ToPointer();
				if (ptr2 != null)
				{
					((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)ptr2 + 8)))((nint)ptr2);
				}
			}
			while (enumerator.MoveNext());
		}
		resourceTable.Clear();
	}

	private void GraphicsDeviceCreated(object sender, EventArgs e)
	{
		if ((graphicsDevice = graphicsDeviceService.GraphicsDevice) == null)
		{
			throw new InvalidOperationException(FrameworkResources.NoGraphicsDevice);
		}
		GraphicsDeviceReset(sender, e);
		graphicsDevice.DrawGuide += DrawGuide;
	}

	private void GraphicsDeviceResetting(object sender, EventArgs e)
	{
		DestroyResources();
	}

	private unsafe void GraphicsDeviceReset(object sender, EventArgs e)
	{
		deviceResetCount++;
		IDirect3DDevice9* pComPtr = graphicsDevice.pComPtr;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out IDirect3DStateBlock9* ptr);
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DSTATEBLOCKTYPE, IDirect3DStateBlock9**, int>)(int)(*(uint*)(*(int*)pComPtr + 236)))((nint)pComPtr, (_D3DSTATEBLOCKTYPE)1, &ptr);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
		pStateBlock = ptr;
	}

	private void GraphicsDeviceDisposing(object sender, EventArgs e)
	{
		if (graphicsDevice != null)
		{
			graphicsDevice.DrawGuide -= DrawGuide;
			graphicsDevice = null;
		}
		DestroyResources();
	}

	private unsafe void DrawGuide(object sender, EventArgs e)
	{
		if (!object.ReferenceEquals(sender, graphicsDevice))
		{
			return;
		}
		IntPtr commandData = default(IntPtr);
		PresentationParameters presentationParameters = graphicsDevice.PresentationParameters;
		if (proxySource.GetDrawingCommandsFromProxy(deviceResetCount, presentationParameters.BackBufferWidth, presentationParameters.BackBufferHeight, out commandData, out var commandDataSize))
		{
			IDirect3DStateBlock9* intPtr = pStateBlock;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr + 16)))((nint)intPtr);
			try
			{
				SetStandardRenderStates();
				ReplayDrawingCommands(commandData, commandDataSize);
			}
			finally
			{
				IDirect3DStateBlock9* intPtr2 = pStateBlock;
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr2 + 20)))((nint)intPtr2);
			}
		}
	}

	private void SetStandardRenderStates()
	{
		PresentationParameters presentationParameters = graphicsDevice.PresentationParameters;
		Viewport viewport = default(Viewport);
		viewport.X = 0;
		viewport.Y = 0;
		viewport.Width = presentationParameters.BackBufferWidth;
		viewport.Height = presentationParameters.BackBufferHeight;
		viewport.MinDepth = 0f;
		viewport.MaxDepth = 1f;
		graphicsDevice.Viewport = viewport;
	}

	private unsafe void ReplayDrawingCommands(IntPtr commandData, int commandDataSize)
	{
		IDirect3DDevice9* pComPtr = graphicsDevice.pComPtr;
		byte* ptr = (byte*)commandData.ToPointer();
		byte* ptr2 = ptr + commandDataSize;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DVERTEXELEMENT9 d3DVERTEXELEMENT);
		System.Runtime.CompilerServices.Unsafe.SkipInit(out IDirect3DVertexDeclaration9* value3);
		System.Runtime.CompilerServices.Unsafe.SkipInit(out IDirect3DVertexShader9* value7);
		System.Runtime.CompilerServices.Unsafe.SkipInit(out IDirect3DPixelShader9* value5);
		System.Runtime.CompilerServices.Unsafe.SkipInit(out IDirect3DTexture9* value);
		System.Runtime.CompilerServices.Unsafe.SkipInit(out tagRECT tagRECT);
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DSURFACE_DESC d3DSURFACE_DESC);
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DLOCKED_RECT d3DLOCKED_RECT);
		while (ptr < ptr2)
		{
			int num = *(int*)ptr;
			ptr += 4;
			switch (num)
			{
			case 1:
			{
				int num38 = *(int*)ptr;
				ptr += 4;
				IUnknown* resource_003CIUnknown_003E = GetResource_003CIUnknown_003E(num38);
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)resource_003CIUnknown_003E + 8)))((nint)resource_003CIUnknown_003E);
				resourceTable.Remove(num38);
				break;
			}
			case 2:
			{
				int key2 = *(int*)ptr;
				ptr += 4;
				int num10 = *(int*)ptr;
				ptr += 4;
				_D3DVERTEXELEMENT9* ptr5 = (_D3DVERTEXELEMENT9*)ptr;
				ptr = ((num10 * 8 + 3) & -4) + ptr;
				_D3DVERTEXELEMENT9* ptr6 = (_D3DVERTEXELEMENT9*)_003CModule_003E.new_005B_005D(((uint)(num10 + 1) > 536870911u) ? uint.MaxValue : ((uint)(num10 + 1 << 3)), (nothrow_t*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E.std_002Enothrow));
				if (ptr6 == null)
				{
					throw new OutOfMemoryException();
				}
				try
				{
					for (int i = 0; i < num10; i++)
					{
						// IL cpblk instruction
						System.Runtime.CompilerServices.Unsafe.CopyBlockUnaligned(i * 8 + (byte*)ptr6, i * 8 + (byte*)ptr5, 8);
					}
					*(short*)(&d3DVERTEXELEMENT) = 255;
					System.Runtime.CompilerServices.Unsafe.As<_D3DVERTEXELEMENT9, short>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DVERTEXELEMENT, 2)) = 0;
					System.Runtime.CompilerServices.Unsafe.As<_D3DVERTEXELEMENT9, sbyte>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DVERTEXELEMENT, 4)) = 17;
					System.Runtime.CompilerServices.Unsafe.As<_D3DVERTEXELEMENT9, sbyte>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DVERTEXELEMENT, 5)) = 0;
					System.Runtime.CompilerServices.Unsafe.As<_D3DVERTEXELEMENT9, sbyte>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DVERTEXELEMENT, 6)) = 0;
					System.Runtime.CompilerServices.Unsafe.As<_D3DVERTEXELEMENT9, sbyte>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DVERTEXELEMENT, 7)) = 0;
					// IL cpblk instruction
					System.Runtime.CompilerServices.Unsafe.CopyBlockUnaligned(num10 * 8 + (byte*)ptr6, ref d3DVERTEXELEMENT, 8);
					int num11 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DVERTEXELEMENT9*, IDirect3DVertexDeclaration9**, int>)(int)(*(uint*)(*(int*)pComPtr + 344)))((nint)pComPtr, ptr6, &value3);
					if (num11 < 0)
					{
						throw GraphicsHelpers.GetExceptionFromResult((uint)num11);
					}
					IntPtr value4 = new IntPtr(value3);
					resourceTable.Add(key2, value4);
				}
				finally
				{
					_003CModule_003E.delete_005B_005D(ptr6);
				}
				break;
			}
			case 3:
			{
				int key4 = *(int*)ptr;
				ptr += 4;
				int num16 = *(int*)ptr;
				ptr += 4;
				byte* ptr9 = ptr;
				ptr = ((num16 + 3) & -4) + ptr;
				int num17 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint*, IDirect3DVertexShader9**, int>)(int)(*(uint*)(*(int*)pComPtr + 364)))((nint)pComPtr, (uint*)ptr9, &value7);
				if (num17 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num17);
				}
				IntPtr value8 = new IntPtr(value7);
				resourceTable.Add(key4, value8);
				break;
			}
			case 4:
			{
				int key3 = *(int*)ptr;
				ptr += 4;
				int num14 = *(int*)ptr;
				ptr += 4;
				byte* ptr8 = ptr;
				ptr = ((num14 + 3) & -4) + ptr;
				int num15 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint*, IDirect3DPixelShader9**, int>)(int)(*(uint*)(*(int*)pComPtr + 424)))((nint)pComPtr, (uint*)ptr8, &value5);
				if (num15 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num15);
				}
				IntPtr value6 = new IntPtr(value5);
				resourceTable.Add(key3, value6);
				break;
			}
			case 5:
			{
				int key = *(int*)ptr;
				ptr += 4;
				int num6 = *(int*)ptr;
				ptr += 4;
				int num7 = *(int*)ptr;
				ptr += 4;
				int num8 = *(int*)ptr;
				ptr += 4;
				_D3DFORMAT d3DFORMAT2 = *(_D3DFORMAT*)ptr;
				ptr += 4;
				int num9 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, uint, uint, _D3DFORMAT, _D3DPOOL, IDirect3DTexture9**, void**, int>)(int)(*(uint*)(*(int*)pComPtr + 92)))((nint)pComPtr, (uint)num6, (uint)num7, (uint)num8, 0u, d3DFORMAT2, (_D3DPOOL)1, &value, null);
				if (num9 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num9);
				}
				IntPtr value2 = new IntPtr(value);
				resourceTable.Add(key, value2);
				break;
			}
			case 6:
			{
				int handle5 = *(int*)ptr;
				ptr += 4;
				int num30 = *(int*)ptr;
				ptr += 4;
				// IL cpblk instruction
				System.Runtime.CompilerServices.Unsafe.CopyBlock(ref tagRECT, ptr, 16);
				ptr += 16;
				int num31 = *(int*)ptr;
				ptr += 4;
				byte* ptr12 = ptr;
				ptr = ((num31 + 3) & -4) + ptr;
				IDirect3DTexture9* resource_003CIDirect3DTexture9_003E = GetResource_003CIDirect3DTexture9_003E(handle5);
				int num32 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DSURFACE_DESC*, int>)(int)(*(uint*)(*(int*)resource_003CIDirect3DTexture9_003E + 68)))((nint)resource_003CIDirect3DTexture9_003E, (uint)num30, &d3DSURFACE_DESC);
				if (num32 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num32);
				}
				int num33 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DLOCKED_RECT*, tagRECT*, uint, int>)(int)(*(uint*)(*(int*)resource_003CIDirect3DTexture9_003E + 76)))((nint)resource_003CIDirect3DTexture9_003E, (uint)num30, &d3DLOCKED_RECT, &tagRECT, 0u);
				if (num33 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num33);
				}
				int num34 = System.Runtime.CompilerServices.Unsafe.As<tagRECT, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref tagRECT, 12)) - System.Runtime.CompilerServices.Unsafe.As<tagRECT, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref tagRECT, 4));
				if (*(int*)(&d3DSURFACE_DESC) == 827611204 || *(int*)(&d3DSURFACE_DESC) == 844388420 || *(int*)(&d3DSURFACE_DESC) == 861165636 || *(int*)(&d3DSURFACE_DESC) == 877942852 || *(int*)(&d3DSURFACE_DESC) == 894720068)
				{
					num34 /= 4;
				}
				int num35 = num31 / num34;
				if (0 < num34)
				{
					int num36 = num34;
					do
					{
						_003CModule_003E.memcpy_s((void*)(int)System.Runtime.CompilerServices.Unsafe.As<_D3DLOCKED_RECT, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DLOCKED_RECT, 4)), (uint)num35, ptr12, (uint)num35);
						System.Runtime.CompilerServices.Unsafe.As<_D3DLOCKED_RECT, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DLOCKED_RECT, 4)) = *(int*)(&d3DLOCKED_RECT) + System.Runtime.CompilerServices.Unsafe.As<_D3DLOCKED_RECT, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DLOCKED_RECT, 4));
						ptr12 = num35 + ptr12;
						num36--;
					}
					while (num36 != 0);
				}
				int num37 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, int>)(int)(*(uint*)(*(int*)resource_003CIDirect3DTexture9_003E + 80)))((nint)resource_003CIDirect3DTexture9_003E, (uint)num30);
				if (num37 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num37);
				}
				break;
			}
			case 7:
			{
				int handle4 = *(int*)ptr;
				ptr += 4;
				int num29 = *(int*)pComPtr + 348;
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, IDirect3DVertexDeclaration9*, int>)(int)(*(uint*)num29))((nint)pComPtr, GetResource_003CIDirect3DVertexDeclaration9_003E(handle4));
				break;
			}
			case 8:
			{
				int handle3 = *(int*)ptr;
				ptr += 4;
				int num28 = *(int*)pComPtr + 368;
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, IDirect3DVertexShader9*, int>)(int)(*(uint*)num28))((nint)pComPtr, GetResource_003CIDirect3DVertexShader9_003E(handle3));
				break;
			}
			case 9:
			{
				int handle2 = *(int*)ptr;
				ptr += 4;
				int num27 = *(int*)pComPtr + 428;
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, IDirect3DPixelShader9*, int>)(int)(*(uint*)num27))((nint)pComPtr, GetResource_003CIDirect3DPixelShader9_003E(handle2));
				break;
			}
			case 10:
			{
				int num25 = *(int*)ptr;
				ptr += 4;
				int handle = *(int*)ptr;
				ptr += 4;
				int num26 = *(int*)pComPtr + 260;
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, IDirect3DBaseTexture9*, int>)(int)(*(uint*)num26))((nint)pComPtr, (uint)num25, (IDirect3DBaseTexture9*)GetResource_003CIDirect3DTexture9_003E(handle));
				break;
			}
			case 11:
			{
				_D3DRENDERSTATETYPE d3DRENDERSTATETYPE = *(_D3DRENDERSTATETYPE*)ptr;
				ptr += 4;
				uint num24 = *(uint*)ptr;
				ptr += 4;
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr + 228)))((nint)pComPtr, d3DRENDERSTATETYPE, num24);
				break;
			}
			case 12:
			{
				uint num22 = *(uint*)ptr;
				ptr += 4;
				_D3DSAMPLERSTATETYPE d3DSAMPLERSTATETYPE = *(_D3DSAMPLERSTATETYPE*)ptr;
				ptr += 4;
				uint num23 = *(uint*)ptr;
				ptr += 4;
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DSAMPLERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr + 276)))((nint)pComPtr, num22, d3DSAMPLERSTATETYPE, num23);
				break;
			}
			case 13:
			{
				uint num20 = *(uint*)ptr;
				ptr += 4;
				uint num21 = *(uint*)ptr;
				ptr += 4;
				float* ptr11 = (float*)ptr;
				ptr = ((int)(num21 * 16 + 3) & -4) + ptr;
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, float*, uint, int>)(int)(*(uint*)(*(int*)pComPtr + 376)))((nint)pComPtr, num20, ptr11, num21);
				break;
			}
			case 14:
			{
				uint num18 = *(uint*)ptr;
				ptr += 4;
				uint num19 = *(uint*)ptr;
				ptr += 4;
				float* ptr10 = (float*)ptr;
				ptr = ((int)(num19 * 16 + 3) & -4) + ptr;
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, float*, uint, int>)(int)(*(uint*)(*(int*)pComPtr + 436)))((nint)pComPtr, num18, ptr10, num19);
				break;
			}
			case 15:
			{
				_D3DPRIMITIVETYPE d3DPRIMITIVETYPE2 = *(_D3DPRIMITIVETYPE*)ptr;
				ptr += 4;
				int num12 = *(int*)ptr;
				ptr += 4;
				int num13 = *(int*)ptr;
				ptr += 4;
				byte* ptr7 = ptr;
				ptr = ((GetVertexCount(d3DPRIMITIVETYPE2, num12) * num13 + 3) & -4) + ptr;
				if (!graphicsDevice._insideScene)
				{
					((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)pComPtr + 164)))((nint)pComPtr);
					graphicsDevice._insideScene = true;
				}
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DPRIMITIVETYPE, uint, void*, uint, int>)(int)(*(uint*)(*(int*)pComPtr + 332)))((nint)pComPtr, d3DPRIMITIVETYPE2, (uint)num12, ptr7, (uint)num13);
				break;
			}
			case 16:
			{
				_D3DPRIMITIVETYPE d3DPRIMITIVETYPE = *(_D3DPRIMITIVETYPE*)ptr;
				ptr += 4;
				int num2 = *(int*)ptr;
				ptr += 4;
				int num3 = *(int*)ptr;
				ptr += 4;
				int num4 = *(int*)ptr;
				ptr += 4;
				int num5 = *(int*)ptr;
				ptr += 4;
				_D3DFORMAT d3DFORMAT = *(_D3DFORMAT*)ptr;
				ptr += 4;
				byte* ptr3 = ptr;
				ptr = ((GetVertexCount(d3DPRIMITIVETYPE, num2) * num4 + 3) & -4) + ptr;
				byte* ptr4 = ptr;
				ptr = ((num5 * num3 + 3) & -4) + ptr;
				if (!graphicsDevice._insideScene)
				{
					((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)pComPtr + 164)))((nint)pComPtr);
					graphicsDevice._insideScene = true;
				}
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DPRIMITIVETYPE, uint, uint, uint, void*, _D3DFORMAT, void*, uint, int>)(int)(*(uint*)(*(int*)pComPtr + 336)))((nint)pComPtr, d3DPRIMITIVETYPE, 0u, (uint)num3, (uint)num2, ptr3, d3DFORMAT, ptr4, (uint)num5);
				break;
			}
			default:
				throw new InvalidOperationException();
			}
		}
		if (ptr > ptr2)
		{
			throw new ArgumentOutOfRangeException("commandDataSize");
		}
	}

	private static int GetVertexCount(_D3DPRIMITIVETYPE primitiveType, int primitiveCount)
	{
		if (primitiveCount == 0)
		{
			return 0;
		}
		return primitiveType switch
		{
			(_D3DPRIMITIVETYPE)1 => primitiveCount, 
			(_D3DPRIMITIVETYPE)2 => primitiveCount << 1, 
			(_D3DPRIMITIVETYPE)3 => primitiveCount + 1, 
			(_D3DPRIMITIVETYPE)4 => primitiveCount * 3, 
			(_D3DPRIMITIVETYPE)5 => primitiveCount + 2, 
			(_D3DPRIMITIVETYPE)6 => primitiveCount + 2, 
			_ => 0, 
		};
	}

	[HandleProcessCorruptedStateExceptions]
	protected virtual void Dispose([MarshalAs(UnmanagedType.U1)] bool P_0)
	{
		if (P_0)
		{
			_007EGuideRenderer();
			return;
		}
		try
		{
			_0021GuideRenderer();
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

	~GuideRenderer()
	{
		Dispose(false);
	}

	private unsafe IUnknown* GetResource_003CIUnknown_003E(int handle)
	{
		if (handle == 0)
		{
			return null;
		}
		return (IUnknown*)resourceTable[handle].ToPointer();
	}

	private unsafe IDirect3DTexture9* GetResource_003CIDirect3DTexture9_003E(int handle)
	{
		if (handle == 0)
		{
			return null;
		}
		return (IDirect3DTexture9*)resourceTable[handle].ToPointer();
	}

	private unsafe IDirect3DVertexDeclaration9* GetResource_003CIDirect3DVertexDeclaration9_003E(int handle)
	{
		if (handle == 0)
		{
			return null;
		}
		return (IDirect3DVertexDeclaration9*)resourceTable[handle].ToPointer();
	}

	private unsafe IDirect3DVertexShader9* GetResource_003CIDirect3DVertexShader9_003E(int handle)
	{
		if (handle == 0)
		{
			return null;
		}
		return (IDirect3DVertexShader9*)resourceTable[handle].ToPointer();
	}

	private unsafe IDirect3DPixelShader9* GetResource_003CIDirect3DPixelShader9_003E(int handle)
	{
		if (handle == 0)
		{
			return null;
		}
		return (IDirect3DPixelShader9*)resourceTable[handle].ToPointer();
	}
}
