using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using _003CCppImplementationDetails_003E;
using Microsoft.Xna.Framework.GamerServices;
using std;

namespace Microsoft.Xna.Framework.Graphics;

public class GraphicsDevice : IDisposable
{
	private SamplerStateCollection pSamplerState;

	private SamplerStateCollection pVertexSamplerState;

	private DeviceResourceManager pResourceManager;

	private TextureCollection pTextureCollection;

	private TextureCollection pVertexTextureCollection;

	private GraphicsAdapter pCurrentAdapter;

	private PresentationParameters pInternalCachedParams;

	private PresentationParameters pPublicCachedParams;

	internal ResourceCreatedEventArgs pCreatedEventArgs;

	internal ResourceDestroyedEventArgs pDestroyedEventArgs;

	private EventHandler<EventArgs> _003Cbacking_store_003EDeviceResetting;

	private EventHandler<EventArgs> _003Cbacking_store_003EDeviceReset;

	private EventHandler<EventArgs> _003Cbacking_store_003EDeviceLost;

	private EventHandler<ResourceCreatedEventArgs> _003Cbacking_store_003EResourceCreated;

	private EventHandler<ResourceDestroyedEventArgs> _003Cbacking_store_003EResourceDestroyed;

	internal BlendState cachedBlendState;

	internal Color cachedBlendFactor;

	internal int cachedMultiSampleMask;

	internal DepthStencilState cachedDepthStencilState;

	internal int cachedReferenceStencil;

	internal RasterizerState cachedRasterizerState;

	private bool blendStateDirty;

	private bool depthStencilStateDirty;

	private int lazyClearFlags;

	private int savedBackBufferClearFlags;

	private Viewport currentViewport;

	internal VertexBufferBinding[] currentVertexBuffers;

	internal int currentVertexBufferCount;

	internal int instanceStreamMask;

	internal DeclarationManager vertexDeclarationManager;

	internal IndexBuffer _currentIB;

	internal unsafe StateTrackerDevice* pStateTracker;

	internal EffectPass activePass;

	internal bool _insideScene;

	internal unsafe IDirect3DSurface9* pImplicitDepthSurface;

	internal uint _creationFlags;

	internal readonly EmbeddedNativeType_003C_D3DCAPS9_003E d3dCaps;

	internal _D3DDEVTYPE _deviceType;

	internal GraphicsProfile _graphicsProfile;

	internal ProfileCapabilities _profileCapabilities;

	internal DisplayMode _displayMode;

	private GraphicsDevice _parent;

	private RenderTargetHelper[] currentRenderTargets;

	private RenderTargetBinding[] currentRenderTargetBindings;

	private int currentRenderTargetCount;

	private bool willItBlend;

	private SurfaceFormat whyWontItBlend;

	internal bool isDisposed;

	private EventHandler<EventArgs> _003Cbacking_store_003EDisposing;

	internal unsafe IDirect3DDevice9* pComPtr;

	internal ulong _internalHandle;

	internal ushort spriteBeginCount;

	internal ushort spriteImmediateBeginCount;

	internal EventHandler<EventArgs> _DrawGuideHandler;

	public bool IsDisposed
	{
		[return: MarshalAs(UnmanagedType.U1)]
		get
		{
			return isDisposed;
		}
	}

	private ClearOptions DefaultClearOptions
	{
		get
		{
			ClearOptions result = ClearOptions.Target;
			DepthFormat depthFormat = ((currentRenderTargetCount <= 0) ? pInternalCachedParams.DepthStencilFormat : currentRenderTargets[0].depthFormat);
			if (depthFormat != 0)
			{
				result = ClearOptions.Target | ClearOptions.DepthBuffer;
				if (depthFormat == DepthFormat.Depth24Stencil8)
				{
					result = ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil;
				}
			}
			return result;
		}
	}

	internal unsafe bool IsDeviceLost
	{
		[return: MarshalAs(UnmanagedType.U1)]
		get
		{
			IDirect3DDevice9* ptr = pComPtr;
			if (ptr == null)
			{
				return true;
			}
			int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)ptr + 12)))((nint)ptr);
			int num2 = ((num == -2005530520 || num == -2005530519) ? 1 : 0);
			return (byte)num2 != 0;
		}
	}

	public unsafe Rectangle ScissorRectangle
	{
		get
		{
			IntPtr intPtr = (IntPtr)pComPtr;
			Helpers.CheckDisposed(this, intPtr);
			IDirect3DDevice9* ptr = pComPtr;
			System.Runtime.CompilerServices.Unsafe.SkipInit(out tagRECT result);
			int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, tagRECT*, int>)(int)(*(uint*)(*(int*)ptr + 304)))((nint)ptr, &result);
			if (num < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num);
			}
			System.Runtime.CompilerServices.Unsafe.As<tagRECT, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref result, 8)) -= *(int*)(&result);
			System.Runtime.CompilerServices.Unsafe.As<tagRECT, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref result, 12)) -= System.Runtime.CompilerServices.Unsafe.As<tagRECT, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref result, 4));
			return (Rectangle)result;
		}
		set
		{
			IntPtr intPtr = (IntPtr)pComPtr;
			Helpers.CheckDisposed(this, intPtr);
			if (*(int*)(&value) >= 0 && System.Runtime.CompilerServices.Unsafe.As<Rectangle, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref value, 8)) >= 0 && System.Runtime.CompilerServices.Unsafe.As<Rectangle, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref value, 4)) >= 0 && System.Runtime.CompilerServices.Unsafe.As<Rectangle, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref value, 12)) >= 0)
			{
				System.Runtime.CompilerServices.Unsafe.As<Rectangle, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref value, 8)) = *(int*)(&value) + System.Runtime.CompilerServices.Unsafe.As<Rectangle, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref value, 8));
				System.Runtime.CompilerServices.Unsafe.As<Rectangle, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref value, 12)) = System.Runtime.CompilerServices.Unsafe.As<Rectangle, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref value, 4)) + System.Runtime.CompilerServices.Unsafe.As<Rectangle, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref value, 12));
				int num;
				int num2;
				if (currentRenderTargetCount > 0)
				{
					RenderTargetHelper renderTargetHelper = currentRenderTargets[0];
					num = renderTargetHelper.width;
					num2 = renderTargetHelper.height;
				}
				else
				{
					num = pInternalCachedParams.BackBufferWidth;
					num2 = pInternalCachedParams.BackBufferHeight;
				}
				if (*(int*)(&value) <= num && System.Runtime.CompilerServices.Unsafe.As<Rectangle, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref value, 8)) <= num && System.Runtime.CompilerServices.Unsafe.As<Rectangle, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref value, 4)) <= num2 && System.Runtime.CompilerServices.Unsafe.As<Rectangle, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref value, 12)) <= num2)
				{
					if (System.Runtime.CompilerServices.Unsafe.As<Rectangle, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref value, 8)) - *(int*)(&value) <= num && System.Runtime.CompilerServices.Unsafe.As<Rectangle, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref value, 12)) - System.Runtime.CompilerServices.Unsafe.As<Rectangle, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref value, 4)) <= num2)
					{
						IDirect3DDevice9* ptr = pComPtr;
						int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, tagRECT*, int>)(int)(*(uint*)(*(int*)ptr + 300)))((nint)ptr, (tagRECT*)(&value));
						if (num3 < 0)
						{
							throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
						}
						return;
					}
					throw new ArgumentException(FrameworkResources.ScissorInvalid, "value");
				}
				throw new ArgumentException(FrameworkResources.ScissorInvalid, "value");
			}
			throw new ArgumentException(FrameworkResources.ScissorInvalid, "value");
		}
	}

	public unsafe IndexBuffer Indices
	{
		get
		{
			return _currentIB;
		}
		set
		{
			IntPtr intPtr = (IntPtr)pComPtr;
			Helpers.CheckDisposed(this, intPtr);
			if (value != null)
			{
				IntPtr intPtr2 = (IntPtr)value.pComPtr;
				Helpers.CheckDisposed(value, intPtr2);
			}
			if (value == _currentIB)
			{
				return;
			}
			IDirect3DIndexBuffer9* ptr = ((value == null) ? null : value.pComPtr);
			IDirect3DDevice9* ptr2 = pComPtr;
			int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, IDirect3DIndexBuffer9*, int>)(int)(*(uint*)(*(int*)ptr2 + 416)))((nint)ptr2, ptr);
			if (num < 0)
			{
				if (value != null && value.GraphicsDevice != this)
				{
					throw new InvalidOperationException(FrameworkResources.InvalidDevice);
				}
				throw GraphicsHelpers.GetExceptionFromResult((uint)num);
			}
			_currentIB = value;
		}
	}

	public unsafe Viewport Viewport
	{
		get
		{
			IntPtr intPtr = (IntPtr)pComPtr;
			Helpers.CheckDisposed(this, intPtr);
			return currentViewport;
		}
		set
		{
			IntPtr intPtr = (IntPtr)pComPtr;
			Helpers.CheckDisposed(this, intPtr);
			if (value.X >= 0 && value.Y >= 0 && value.Width > 0 && value.Height > 0)
			{
				int num;
				int num2;
				if (currentRenderTargetCount > 0)
				{
					RenderTargetHelper renderTargetHelper = currentRenderTargets[0];
					num = renderTargetHelper.width;
					num2 = renderTargetHelper.height;
				}
				else
				{
					num = pInternalCachedParams.BackBufferWidth;
					num2 = pInternalCachedParams.BackBufferHeight;
				}
				if (value.X + value.Width <= num && value.Y + value.Height <= num2)
				{
					if (!(value.MinDepth < 0f) && !(value.MinDepth > 1f))
					{
						if (!(value.MaxDepth < 0f) && !(value.MaxDepth > 1f))
						{
							double num3 = value.MaxDepth;
							if (num3 < (double)value.MinDepth)
							{
								throw new ArgumentException(FrameworkResources.ViewportInvalid, "value");
							}
							IDirect3DDevice9* ptr = pComPtr;
							int num4 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DVIEWPORT9*, int>)(int)(*(uint*)(*(int*)ptr + 188)))((nint)ptr, (_D3DVIEWPORT9*)(int)(ref value));
							if (num4 < 0)
							{
								throw GraphicsHelpers.GetExceptionFromResult((uint)num4);
							}
							currentViewport = value;
							return;
						}
						throw new ArgumentException(FrameworkResources.ViewportInvalid, "value");
					}
					throw new ArgumentException(FrameworkResources.ViewportInvalid, "value");
				}
				throw new ArgumentException(FrameworkResources.ViewportInvalid, "value");
			}
			throw new ArgumentException(FrameworkResources.ViewportInvalid, "value");
		}
	}

	public unsafe DisplayMode DisplayMode
	{
		get
		{
			IntPtr intPtr = (IntPtr)pComPtr;
			Helpers.CheckDisposed(this, intPtr);
			IDirect3DDevice9* ptr = pComPtr;
			System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DDISPLAYMODE d3DDISPLAYMODE);
			int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DDISPLAYMODE*, int>)(int)(*(uint*)(*(int*)ptr + 32)))((nint)ptr, 0u, &d3DDISPLAYMODE);
			if (num < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num);
			}
			DisplayMode displayMode = _displayMode;
			if (displayMode == null)
			{
				_displayMode = new DisplayMode(*(int*)(&d3DDISPLAYMODE), System.Runtime.CompilerServices.Unsafe.As<_D3DDISPLAYMODE, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DDISPLAYMODE, 4)), _003CModule_003E.ConvertWindowsFormatToXna(System.Runtime.CompilerServices.Unsafe.As<_D3DDISPLAYMODE, _D3DFORMAT>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DDISPLAYMODE, 12))));
			}
			else
			{
				displayMode._width = *(int*)(&d3DDISPLAYMODE);
				_displayMode._height = System.Runtime.CompilerServices.Unsafe.As<_D3DDISPLAYMODE, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DDISPLAYMODE, 4));
				_displayMode._format = _003CModule_003E.ConvertWindowsFormatToXna(System.Runtime.CompilerServices.Unsafe.As<_D3DDISPLAYMODE, _D3DFORMAT>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DDISPLAYMODE, 12)));
			}
			return _displayMode;
		}
	}

	public unsafe GraphicsDeviceStatus GraphicsDeviceStatus
	{
		get
		{
			IntPtr intPtr = (IntPtr)pComPtr;
			Helpers.CheckDisposed(this, intPtr);
			IDirect3DDevice9* intPtr2 = pComPtr;
			int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr2 + 12)))((nint)intPtr2);
			if (num < 0)
			{
				return num switch
				{
					-2005530519 => GraphicsDeviceStatus.NotReset, 
					-2005530520 => GraphicsDeviceStatus.Lost, 
					_ => throw GraphicsHelpers.GetExceptionFromResult((uint)num), 
				};
			}
			return GraphicsDeviceStatus.Normal;
		}
	}

	public GraphicsProfile GraphicsProfile => _graphicsProfile;

	public GraphicsAdapter Adapter => pCurrentAdapter;

	public PresentationParameters PresentationParameters => pPublicCachedParams;

	public RasterizerState RasterizerState
	{
		get
		{
			return cachedRasterizerState;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value", FrameworkResources.NullNotAllowed);
			}
			if (value != cachedRasterizerState)
			{
				EffectPass effectPass = activePass;
				if (effectPass != null && (effectPass._stateFlags & EffectStateFlags.Rasterizer) != 0)
				{
					effectPass.EndPass();
					activePass = null;
				}
				value.Apply(this);
				cachedRasterizerState = value;
			}
		}
	}

	public unsafe int ReferenceStencil
	{
		get
		{
			return cachedReferenceStencil;
		}
		set
		{
			IntPtr intPtr = (IntPtr)pComPtr;
			Helpers.CheckDisposed(this, intPtr);
			EffectPass effectPass = activePass;
			if (effectPass != null && (effectPass._stateFlags & EffectStateFlags.DepthStencil) != 0)
			{
				effectPass.EndPass();
				activePass = null;
			}
			IDirect3DDevice9* ptr = pComPtr;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)ptr + 228)))((nint)ptr, (_D3DRENDERSTATETYPE)57, (uint)value);
			cachedReferenceStencil = value;
			depthStencilStateDirty = true;
		}
	}

	public DepthStencilState DepthStencilState
	{
		get
		{
			return cachedDepthStencilState;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value", FrameworkResources.NullNotAllowed);
			}
			if (value != cachedDepthStencilState || depthStencilStateDirty)
			{
				EffectPass effectPass = activePass;
				if (effectPass != null && (effectPass._stateFlags & EffectStateFlags.DepthStencil) != 0)
				{
					effectPass.EndPass();
					activePass = null;
				}
				value.Apply(this);
				cachedDepthStencilState = value;
				cachedReferenceStencil = value.cachedReferenceStencil;
				depthStencilStateDirty = false;
			}
		}
	}

	public unsafe int MultiSampleMask
	{
		get
		{
			return cachedMultiSampleMask;
		}
		set
		{
			IntPtr intPtr = (IntPtr)pComPtr;
			Helpers.CheckDisposed(this, intPtr);
			EffectPass effectPass = activePass;
			if (effectPass != null && (effectPass._stateFlags & EffectStateFlags.Blend) != 0)
			{
				effectPass.EndPass();
				activePass = null;
			}
			IDirect3DDevice9* ptr = pComPtr;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)ptr + 228)))((nint)ptr, (_D3DRENDERSTATETYPE)162, (uint)value);
			cachedMultiSampleMask = value;
			blendStateDirty = true;
		}
	}

	public unsafe Color BlendFactor
	{
		get
		{
			return cachedBlendFactor;
		}
		set
		{
			IntPtr intPtr = (IntPtr)pComPtr;
			Helpers.CheckDisposed(this, intPtr);
			EffectPass effectPass = activePass;
			if (effectPass != null && (effectPass._stateFlags & EffectStateFlags.Blend) != 0)
			{
				effectPass.EndPass();
				activePass = null;
			}
			int num = *(int*)pComPtr + 228;
			uint num2 = (uint)(value.A << 8);
			uint num3 = (value.R | num2) << 8;
			uint num4 = (value.G | num3) << 8;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)num))((nint)pComPtr, (_D3DRENDERSTATETYPE)193, value.B | num4);
			cachedBlendFactor = value;
			blendStateDirty = true;
		}
	}

	public BlendState BlendState
	{
		get
		{
			return cachedBlendState;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value", FrameworkResources.NullNotAllowed);
			}
			if (value != cachedBlendState || blendStateDirty)
			{
				EffectPass effectPass = activePass;
				if (effectPass != null && (effectPass._stateFlags & EffectStateFlags.Blend) != 0)
				{
					effectPass.EndPass();
					activePass = null;
				}
				value.Apply(this);
				cachedBlendState = value;
				ref Color reference = ref cachedBlendFactor;
				reference = value.cachedBlendFactor;
				cachedMultiSampleMask = value.cachedMultiSampleMask;
				blendStateDirty = false;
			}
		}
	}

	public TextureCollection VertexTextures => pVertexTextureCollection;

	public TextureCollection Textures => pTextureCollection;

	public SamplerStateCollection VertexSamplerStates => pVertexSamplerState;

	public SamplerStateCollection SamplerStates => pSamplerState;

	internal DeviceResourceManager Resources => pResourceManager;

	[SpecialName]
	internal event EventHandler<EventArgs> DrawGuide
	{
		add
		{
			_DrawGuideHandler = (EventHandler<EventArgs>)Delegate.Combine(_DrawGuideHandler, value);
		}
		remove
		{
			_DrawGuideHandler = (EventHandler<EventArgs>)Delegate.Remove(_DrawGuideHandler, value);
		}
	}

	[SpecialName]
	public event EventHandler<EventArgs> Disposing
	{
		[MethodImpl(MethodImplOptions.Synchronized)]
		add
		{
			_003Cbacking_store_003EDisposing = (EventHandler<EventArgs>)Delegate.Combine(_003Cbacking_store_003EDisposing, value);
		}
		[MethodImpl(MethodImplOptions.Synchronized)]
		remove
		{
			_003Cbacking_store_003EDisposing = (EventHandler<EventArgs>)Delegate.Remove(_003Cbacking_store_003EDisposing, value);
		}
	}

	[SpecialName]
	public event EventHandler<ResourceDestroyedEventArgs> ResourceDestroyed
	{
		[MethodImpl(MethodImplOptions.Synchronized)]
		add
		{
			_003Cbacking_store_003EResourceDestroyed = (EventHandler<ResourceDestroyedEventArgs>)Delegate.Combine(_003Cbacking_store_003EResourceDestroyed, value);
		}
		[MethodImpl(MethodImplOptions.Synchronized)]
		remove
		{
			_003Cbacking_store_003EResourceDestroyed = (EventHandler<ResourceDestroyedEventArgs>)Delegate.Remove(_003Cbacking_store_003EResourceDestroyed, value);
		}
	}

	[SpecialName]
	public event EventHandler<ResourceCreatedEventArgs> ResourceCreated
	{
		[MethodImpl(MethodImplOptions.Synchronized)]
		add
		{
			_003Cbacking_store_003EResourceCreated = (EventHandler<ResourceCreatedEventArgs>)Delegate.Combine(_003Cbacking_store_003EResourceCreated, value);
		}
		[MethodImpl(MethodImplOptions.Synchronized)]
		remove
		{
			_003Cbacking_store_003EResourceCreated = (EventHandler<ResourceCreatedEventArgs>)Delegate.Remove(_003Cbacking_store_003EResourceCreated, value);
		}
	}

	[SpecialName]
	public event EventHandler<EventArgs> DeviceLost
	{
		[MethodImpl(MethodImplOptions.Synchronized)]
		add
		{
			_003Cbacking_store_003EDeviceLost = (EventHandler<EventArgs>)Delegate.Combine(_003Cbacking_store_003EDeviceLost, value);
		}
		[MethodImpl(MethodImplOptions.Synchronized)]
		remove
		{
			_003Cbacking_store_003EDeviceLost = (EventHandler<EventArgs>)Delegate.Remove(_003Cbacking_store_003EDeviceLost, value);
		}
	}

	[SpecialName]
	public event EventHandler<EventArgs> DeviceReset
	{
		[MethodImpl(MethodImplOptions.Synchronized)]
		add
		{
			_003Cbacking_store_003EDeviceReset = (EventHandler<EventArgs>)Delegate.Combine(_003Cbacking_store_003EDeviceReset, value);
		}
		[MethodImpl(MethodImplOptions.Synchronized)]
		remove
		{
			_003Cbacking_store_003EDeviceReset = (EventHandler<EventArgs>)Delegate.Remove(_003Cbacking_store_003EDeviceReset, value);
		}
	}

	[SpecialName]
	public event EventHandler<EventArgs> DeviceResetting
	{
		[MethodImpl(MethodImplOptions.Synchronized)]
		add
		{
			_003Cbacking_store_003EDeviceResetting = (EventHandler<EventArgs>)Delegate.Combine(_003Cbacking_store_003EDeviceResetting, value);
		}
		[MethodImpl(MethodImplOptions.Synchronized)]
		remove
		{
			_003Cbacking_store_003EDeviceResetting = (EventHandler<EventArgs>)Delegate.Remove(_003Cbacking_store_003EDeviceResetting, value);
		}
	}

	private unsafe void CreateDevice(GraphicsAdapter adapter, PresentationParameters presentationParameters)
	{
		_creationFlags = 38u;
		_D3DCAPS9* t = d3dCaps.t;
		int num = *(int*)GraphicsAdapter.pComPtr + 56;
		int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DDEVTYPE, _D3DCAPS9*, int>)(int)(*(uint*)num))((nint)GraphicsAdapter.pComPtr, adapter.adapter, _deviceType, t);
		if (num2 < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
		}
		if (_deviceType == (_D3DDEVTYPE)1 && ((uint)(*(int*)((byte*)d3dCaps.t + 28)) & 0x10000u) != 0)
		{
			_creationFlags = (_creationFlags & 0xFFFFFFDFu) | 0x40u;
		}
		System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DPRESENT_PARAMETERS_ d3DPRESENT_PARAMETERS_);
		*(int*)(&d3DPRESENT_PARAMETERS_) = 0;
		// IL initblk instruction
		System.Runtime.CompilerServices.Unsafe.InitBlock(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DPRESENT_PARAMETERS_, 4), 0, 52);
		ConvertPresentationParametersToNative(adapter, presentationParameters, &d3DPRESENT_PARAMETERS_);
		fixed (IDirect3DDevice9** ptr = &pComPtr)
		{
			try
			{
				int num3 = *(int*)GraphicsAdapter.pComPtr + 64;
				int num4 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DDEVTYPE, HWND__*, uint, _D3DPRESENT_PARAMETERS_*, IDirect3DDevice9**, int>)(int)(*(uint*)num3))((nint)GraphicsAdapter.pComPtr, adapter.adapter, _deviceType, (HWND__*)(int)System.Runtime.CompilerServices.Unsafe.As<_D3DPRESENT_PARAMETERS_, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DPRESENT_PARAMETERS_, 28)), _creationFlags, &d3DPRESENT_PARAMETERS_, ptr);
				if (num4 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num4);
				}
			}
			catch
			{
				//try-fault
				ptr = null;
				throw;
			}
		}
		StateTrackerDevice* ptr2 = (StateTrackerDevice*)_003CModule_003E.@new(144u, (nothrow_t*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E.std_002Enothrow));
		StateTrackerDevice* ptr3;
		try
		{
			if (ptr2 != null)
			{
				ProfileCapabilities profileCapabilities = _profileCapabilities;
				ptr3 = _003CModule_003E.Microsoft_002EXna_002EFramework_002EGraphics_002EStateTrackerDevice_002E_007Bctor_007D(ptr2, pComPtr, profileCapabilities.VertexShaderVersion, profileCapabilities.PixelShaderVersion);
			}
			else
			{
				ptr3 = null;
			}
		}
		catch
		{
			//try-fault
			_003CModule_003E.delete(ptr2, (nothrow_t*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _003CModule_003E.std_002Enothrow));
			throw;
		}
		pStateTracker = ptr3;
		if (ptr3 == null)
		{
			throw new OutOfMemoryException();
		}
		ConvertPresentationParametersToManaged(&d3DPRESENT_PARAMETERS_, presentationParameters);
		if (_profileCapabilities.OcclusionQuery)
		{
			IDirect3DQuery9* ptr4 = null;
			IDirect3DDevice9* ptr5 = pComPtr;
			int num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DQUERYTYPE, IDirect3DQuery9**, int>)(int)(*(uint*)(*(int*)ptr5 + 472)))((nint)ptr5, (_D3DQUERYTYPE)9, &ptr4);
			if (ptr4 != null)
			{
				IDirect3DQuery9* intPtr = ptr4;
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)intPtr + 8)))((nint)intPtr);
				ptr4 = null;
			}
			if (num5 < 0)
			{
				StateTrackerDevice* ptr6 = pStateTracker;
				if (ptr6 != null)
				{
					((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)ptr6 + 8)))((nint)ptr6);
					pStateTracker = null;
				}
				IDirect3DDevice9* ptr7 = pComPtr;
				if (ptr7 != null)
				{
					((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)ptr7 + 8)))((nint)ptr7);
					pComPtr = null;
				}
				_profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileInvalidDevice);
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

	private unsafe void CreateHelperClasses()
	{
		pImplicitDepthSurface = null;
		pSamplerState = new SamplerStateCollection(this, 0, _profileCapabilities.MaxSamplers);
		pVertexSamplerState = new SamplerStateCollection(this, 257, _profileCapabilities.MaxVertexSamplers);
		pTextureCollection = new TextureCollection(this, 0, _profileCapabilities.MaxSamplers);
		pVertexTextureCollection = new TextureCollection(this, 257, _profileCapabilities.MaxVertexSamplers);
		currentVertexBuffers = new VertexBufferBinding[_profileCapabilities.MaxVertexStreams];
		vertexDeclarationManager = new DeclarationManager(this);
		pResourceManager = new DeviceResourceManager(this);
		ProfileCapabilities profileCapabilities = _profileCapabilities;
		currentRenderTargets = new RenderTargetHelper[profileCapabilities.MaxRenderTargets];
		currentRenderTargetBindings = new RenderTargetBinding[profileCapabilities.MaxRenderTargets];
		willItBlend = true;
		fixed (IDirect3DSurface9** ptr = &pImplicitDepthSurface)
		{
			int num = *(int*)pComPtr + 160;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, IDirect3DSurface9**, int>)(int)(*(uint*)num))((nint)pComPtr, ptr);
		}
	}

	public unsafe void Present(Rectangle? sourceRectangle, Rectangle? destinationRectangle, IntPtr overrideWindowHandle)
	{
		tagRECT* ptr = null;
		tagRECT* ptr2 = null;
		Rectangle rectangle = default(Rectangle);
		Rectangle rectangle2 = default(Rectangle);
		if (sourceRectangle.HasValue)
		{
			rectangle = sourceRectangle.Value;
			ptr = (tagRECT*)(int)(ref rectangle);
			if (ptr != null)
			{
				*(int*)((byte*)ptr + 8) += *(int*)ptr;
				*(int*)((byte*)ptr + 12) += *(int*)((byte*)ptr + 4);
			}
		}
		if (destinationRectangle.HasValue)
		{
			rectangle2 = destinationRectangle.Value;
			ptr2 = (tagRECT*)(int)(ref rectangle2);
			if (ptr2 != null)
			{
				*(int*)((byte*)ptr2 + 8) += *(int*)ptr2;
				*(int*)((byte*)ptr2 + 12) += *(int*)((byte*)ptr2 + 4);
			}
		}
		Present(ptr, ptr2, (HWND__*)overrideWindowHandle.ToPointer());
	}

	public unsafe void Present()
	{
		Present(null, null, null);
	}

	[CLSCompliant(false)]
	private unsafe void Present(tagRECT* pSource, tagRECT* pDest, HWND__* hOverride)
	{
		IntPtr intPtr = (IntPtr)pComPtr;
		Helpers.CheckDisposed(this, intPtr);
		if (currentRenderTargetCount > 0)
		{
			throw new InvalidOperationException(FrameworkResources.CannotPresentActiveRenderTargets);
		}
		EffectPass effectPass = activePass;
		if (effectPass != null)
		{
			effectPass.EndPass();
			activePass = null;
		}
		if (lazyClearFlags != 0)
		{
			ClearDirtyBuffers();
		}
		if (pSource == null && pDest == null && hOverride == null)
		{
			raise_DrawGuide(this, EventArgs.Empty);
		}
		if (_insideScene)
		{
			IDirect3DDevice9* intPtr2 = pComPtr;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr2 + 168)))((nint)intPtr2);
			_insideScene = false;
		}
		try
		{
			IDirect3DDevice9* ptr = pComPtr;
			int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, tagRECT*, tagRECT*, HWND__*, _RGNDATA*, int>)(int)(*(uint*)(*(int*)ptr + 68)))((nint)ptr, pSource, pDest, hOverride, null);
			lazyClearFlags = (int)DefaultClearOptions;
			if (num < 0)
			{
				if (num == -2005530520)
				{
					raise_DeviceLost(this, EventArgs.Empty);
				}
				throw GraphicsHelpers.GetExceptionFromResult((uint)num);
			}
		}
		catch (InvalidOperationException)
		{
		}
	}

	internal void FireCreatedEvent(object resource)
	{
		ResourceCreatedEventArgs resourceCreatedEventArgs = pCreatedEventArgs;
		if (resourceCreatedEventArgs == null)
		{
			pCreatedEventArgs = new ResourceCreatedEventArgs(resource);
		}
		else
		{
			resourceCreatedEventArgs._resource = resource;
		}
		EventHandler<ResourceCreatedEventArgs> eventHandler = _003Cbacking_store_003EResourceCreated;
		if (eventHandler != null)
		{
			eventHandler(this, pCreatedEventArgs);
		}
		pCreatedEventArgs._resource = null;
	}

	internal void FireDestroyedEvent(string name, object tag)
	{
		ResourceDestroyedEventArgs resourceDestroyedEventArgs = pDestroyedEventArgs;
		if (resourceDestroyedEventArgs == null)
		{
			pDestroyedEventArgs = new ResourceDestroyedEventArgs(name, tag);
		}
		else
		{
			resourceDestroyedEventArgs._name = name;
			pDestroyedEventArgs._tag = tag;
		}
		EventHandler<ResourceDestroyedEventArgs> eventHandler = _003Cbacking_store_003EResourceDestroyed;
		if (eventHandler != null)
		{
			eventHandler(this, pDestroyedEventArgs);
		}
	}

	internal unsafe void VerifyCanDraw([MarshalAs(UnmanagedType.U1)] bool bUserPrimitives, [MarshalAs(UnmanagedType.U1)] bool bIndexedPrimitives)
	{
		if (lazyClearFlags != 0)
		{
			ClearDirtyBuffers();
		}
		int num = 0;
		if (0 < currentRenderTargetCount)
		{
			do
			{
				currentRenderTargets[num].pTexture.SetContentLost(isContentLost: false);
				num++;
			}
			while (num < currentRenderTargetCount);
		}
		StateTrackerDevice* ptr = pStateTracker;
		uint num2 = *(uint*)((byte*)ptr + 20);
		uint num3 = *(uint*)((byte*)ptr + 24);
		Texture texture;
		if (num2 != 0 && num3 != 0)
		{
			bool num4 = num2 < 768;
			int num5 = ((num3 < 768) ? 1 : 0);
			if ((num4 ? 1 : 0) != num5)
			{
				throw new InvalidOperationException(FrameworkResources.CannotMixShader2and3);
			}
			if (!_003CModule_003E.Microsoft_002EXna_002EFramework_002EGraphics_002EVertexShaderInputSemantics_002EIsCompatibleWithDecl((VertexShaderInputSemantics*)((byte*)ptr + 28), (VertexShaderInputSemantics*)((byte*)ptr + 60)))
			{
				string text = FrameworkResources.MissingVertexShaderInput;
				ptr = pStateTracker;
				System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DDECLUSAGE usage);
				System.Runtime.CompilerServices.Unsafe.SkipInit(out int num6);
				if (_003CModule_003E.Microsoft_002EXna_002EFramework_002EGraphics_002EVertexShaderInputSemantics_002EGetMissingSemantic((VertexShaderInputSemantics*)((byte*)ptr + 28), (VertexShaderInputSemantics*)((byte*)ptr + 60), &usage, &num6))
				{
					text = string.Format(args: new object[3]
					{
						text,
						_003CModule_003E.ConvertDxVertexElementUsageToXna(usage),
						num6
					}, provider: CultureInfo.CurrentCulture, format: FrameworkResources.MissingVertexShaderInputDetails);
				}
				throw new InvalidOperationException(text);
			}
			if (!willItBlend && *(int*)((byte*)pStateTracker + 112) != 0)
			{
				_profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileInvalidBlendFormat, whyWontItBlend);
			}
			StateTrackerDevice* ptr2 = pStateTracker;
			uint num7 = (*(uint*)((byte*)ptr2 + 124) | *(uint*)((byte*)ptr2 + 120) | *(uint*)((byte*)ptr2 + 116)) & *(uint*)((byte*)ptr2 + 136);
			if (num7 != 0)
			{
				int num8 = 0;
				if ((num7 & 1) == 0)
				{
					do
					{
						num8++;
					}
					while (((uint)(1 << num8) & num7) == 0);
					if (num8 >= 16)
					{
						texture = pVertexTextureCollection[num8 - 16];
						goto IL_0183;
					}
				}
				texture = pTextureCollection[num8];
				goto IL_0183;
			}
			goto IL_01ad;
		}
		throw new InvalidOperationException(FrameworkResources.CannotDrawNoShader);
		IL_0183:
		SurfaceFormat surfaceFormat = texture?.Format ?? ((SurfaceFormat)(-1));
		_profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileInvalidFilterFormat, surfaceFormat);
		goto IL_01ad;
		IL_01ad:
		ptr = pStateTracker;
		if (((*(int*)((byte*)ptr + 132) | *(int*)((byte*)ptr + 128)) & *(int*)((byte*)ptr + 140)) != 0)
		{
			_profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileNoWrapNonPow2);
		}
		if (!bUserPrimitives)
		{
			if (bIndexedPrimitives && _currentIB == null)
			{
				throw new InvalidOperationException(FrameworkResources.CannotDrawNoData);
			}
			if (currentVertexBufferCount <= 0)
			{
				throw new InvalidOperationException(FrameworkResources.CannotDrawNoData);
			}
		}
	}

	[SpecialName]
	protected void raise_DeviceResetting(object value0, EventArgs value1)
	{
		_003Cbacking_store_003EDeviceResetting?.Invoke(value0, value1);
	}

	[SpecialName]
	protected void raise_DeviceReset(object value0, EventArgs value1)
	{
		_003Cbacking_store_003EDeviceReset?.Invoke(value0, value1);
	}

	[SpecialName]
	protected void raise_DeviceLost(object value0, EventArgs value1)
	{
		_003Cbacking_store_003EDeviceLost?.Invoke(value0, value1);
	}

	[SpecialName]
	protected void raise_ResourceCreated(object value0, ResourceCreatedEventArgs value1)
	{
		_003Cbacking_store_003EResourceCreated?.Invoke(value0, value1);
	}

	[SpecialName]
	protected void raise_ResourceDestroyed(object value0, ResourceDestroyedEventArgs value1)
	{
		_003Cbacking_store_003EResourceDestroyed?.Invoke(value0, value1);
	}

	private unsafe void ConvertPresentationParametersToNative(GraphicsAdapter adapter, PresentationParameters presentationParameters, _D3DPRESENT_PARAMETERS_* pp)
	{
		if (presentationParameters.DeviceWindowHandle == IntPtr.Zero)
		{
			throw new ArgumentException(FrameworkResources.NullWindowHandleNotAllowed);
		}
		Helpers.ValidateOrientation(presentationParameters.DisplayOrientation);
		if (presentationParameters.BackBufferWidth <= 0 || presentationParameters.BackBufferHeight <= 0)
		{
			System.Runtime.CompilerServices.Unsafe.SkipInit(out tagRECT tagRECT);
			if (!presentationParameters.IsFullScreen && _003CModule_003E.GetClientRect((HWND__*)presentationParameters.DeviceWindowHandle.ToPointer(), &tagRECT) != 0)
			{
				presentationParameters.BackBufferWidth = System.Runtime.CompilerServices.Unsafe.As<tagRECT, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref tagRECT, 8)) - *(int*)(&tagRECT);
				presentationParameters.BackBufferHeight = System.Runtime.CompilerServices.Unsafe.As<tagRECT, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref tagRECT, 12)) - System.Runtime.CompilerServices.Unsafe.As<tagRECT, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref tagRECT, 4));
			}
			else
			{
				presentationParameters.BackBufferWidth = adapter.CurrentDisplayMode.Width;
				presentationParameters.BackBufferHeight = adapter.CurrentDisplayMode.Height;
			}
		}
		SurfaceFormat selectedFormat = presentationParameters.BackBufferFormat;
		DepthFormat selectedDepthFormat = presentationParameters.DepthStencilFormat;
		int selectedMultiSampleCount = presentationParameters.MultiSampleCount;
		adapter.QueryFormat(isBackBuffer: true, _deviceType, _graphicsProfile, selectedFormat, selectedDepthFormat, selectedMultiSampleCount, out selectedFormat, out selectedDepthFormat, out selectedMultiSampleCount);
		*(int*)pp = presentationParameters.BackBufferWidth;
		*(int*)((byte*)pp + 4) = presentationParameters.BackBufferHeight;
		*(_D3DFORMAT*)((byte*)pp + 8) = _003CModule_003E.ConvertXnaFormatToWindows(selectedFormat);
		*(int*)((byte*)pp + 12) = 1;
		int num = (((*(_D3DFORMAT*)((byte*)pp + 40) = _003CModule_003E.ConvertXnaFormatToWindows(selectedDepthFormat)) != 0) ? 1 : 0);
		*(int*)((byte*)pp + 36) = num;
		*(int*)((byte*)pp + 16) = selectedMultiSampleCount;
		*(int*)((byte*)pp + 28) = (int)presentationParameters.DeviceWindowHandle.ToPointer();
		int num2 = ((!presentationParameters.IsFullScreen) ? 1 : 0);
		*(int*)((byte*)pp + 32) = num2;
		*(int*)((byte*)pp + 24) = 1;
		switch (presentationParameters.PresentationInterval)
		{
		default:
			*(int*)((byte*)pp + 52) = 0;
			break;
		case PresentInterval.Immediate:
			*(int*)((byte*)pp + 52) = int.MinValue;
			break;
		case PresentInterval.Two:
			if (presentationParameters.IsFullScreen && ((uint)(*(int*)((byte*)d3dCaps.t + 20)) & 2u) != 0)
			{
				*(int*)((byte*)pp + 52) = 2;
			}
			else
			{
				*(int*)((byte*)pp + 52) = 1;
			}
			break;
		case PresentInterval.One:
			*(int*)((byte*)pp + 52) = 1;
			break;
		}
	}

	private unsafe void ConvertPresentationParametersToManaged(_D3DPRESENT_PARAMETERS_* pp, PresentationParameters presentationParameters)
	{
		presentationParameters.BackBufferWidth = *(int*)pp;
		presentationParameters.BackBufferHeight = *(int*)((byte*)pp + 4);
		presentationParameters.BackBufferFormat = _003CModule_003E.ConvertWindowsFormatToXna(*(_D3DFORMAT*)((byte*)pp + 8));
		DepthFormat depthStencilFormat = ((*(int*)((byte*)pp + 36) != 0) ? _003CModule_003E.ConvertWindowsDepthFormatToXna(*(_D3DFORMAT*)((byte*)pp + 40)) : DepthFormat.None);
		presentationParameters.DepthStencilFormat = depthStencilFormat;
		presentationParameters.MultiSampleCount = *(int*)((byte*)pp + 16);
		switch ((uint)(*(int*)((byte*)pp + 52)))
		{
		default:
			presentationParameters.PresentationInterval = PresentInterval.Default;
			break;
		case 2147483648u:
			presentationParameters.PresentationInterval = PresentInterval.Immediate;
			break;
		case 2u:
			presentationParameters.PresentationInterval = PresentInterval.Two;
			break;
		case 1u:
			presentationParameters.PresentationInterval = PresentInterval.One;
			break;
		}
	}

	internal void ClearBlendState()
	{
		cachedBlendState = null;
		ref Color reference = ref cachedBlendFactor;
		reference = BlendState.Opaque.cachedBlendFactor;
		cachedMultiSampleMask = BlendState.Opaque.cachedMultiSampleMask;
		blendStateDirty = true;
	}

	internal void ClearDepthStencilState()
	{
		cachedDepthStencilState = null;
		cachedReferenceStencil = DepthStencilState.Default.cachedReferenceStencil;
		depthStencilStateDirty = true;
	}

	internal void ClearRasterizerState()
	{
		cachedRasterizerState = null;
	}

	private unsafe GraphicsDevice(IDirect3DDevice9* pInterface, GraphicsDevice pDevice)
	{
		EmbeddedNativeType_003C_D3DCAPS9_003E embeddedNativeType_003C_D3DCAPS9_003E = new EmbeddedNativeType_003C_D3DCAPS9_003E();
		try
		{
			d3dCaps = embeddedNativeType_003C_D3DCAPS9_003E;
			_parent = pDevice;
			pComPtr = pInterface;
			base._002Ector();
			CreateHelperClasses();
			return;
		}
		catch
		{
			//try-fault
			((IDisposable)d3dCaps).Dispose();
			throw;
		}
	}

	public GraphicsDevice(GraphicsAdapter adapter, GraphicsProfile graphicsProfile, PresentationParameters presentationParameters)
	{
		EmbeddedNativeType_003C_D3DCAPS9_003E embeddedNativeType_003C_D3DCAPS9_003E = new EmbeddedNativeType_003C_D3DCAPS9_003E();
		try
		{
			d3dCaps = embeddedNativeType_003C_D3DCAPS9_003E;
			base._002Ector();
			if (presentationParameters == null)
			{
				throw new ArgumentNullException("presentationParameters", FrameworkResources.NullNotAllowed);
			}
			if (adapter == null)
			{
				throw new ArgumentNullException("adapter", FrameworkResources.NullNotAllowed);
			}
			_deviceType = GraphicsAdapter.CurrentDeviceType;
			_graphicsProfile = graphicsProfile;
			_profileCapabilities = ProfileCapabilities.GetInstance(graphicsProfile);
			if (!adapter.IsProfileSupported(_deviceType, _graphicsProfile))
			{
				_profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileInvalidDevice);
			}
			CreateDevice(adapter, presentationParameters);
			pCurrentAdapter = adapter;
			_DrawGuideHandler = null;
			pInternalCachedParams = presentationParameters.Clone();
			pPublicCachedParams = presentationParameters.Clone();
			CreateHelperClasses();
			InitializeDeviceState();
			GuideRendererConnector.GuideRendererType = typeof(GuideRenderer);
			return;
		}
		catch
		{
			//try-fault
			((IDisposable)d3dCaps).Dispose();
			throw;
		}
	}

	public void Reset()
	{
		Reset(pInternalCachedParams, pCurrentAdapter);
	}

	public void Reset(PresentationParameters presentationParameters)
	{
		Reset(presentationParameters, pCurrentAdapter);
	}

	public unsafe void Reset(PresentationParameters presentationParameters, GraphicsAdapter graphicsAdapter)
	{
		IntPtr intPtr = (IntPtr)pComPtr;
		Helpers.CheckDisposed(this, intPtr);
		bool flag = false;
		if (presentationParameters == null)
		{
			throw new ArgumentNullException("presentationParameters", FrameworkResources.NullNotAllowed);
		}
		if (graphicsAdapter == null)
		{
			throw new ArgumentNullException("graphicsAdapter", FrameworkResources.NullNotAllowed);
		}
		EventArgs empty = EventArgs.Empty;
		_003Cbacking_store_003EDeviceResetting?.Invoke(this, empty);
		int num = ((currentRenderTargetCount == 0 && presentationParameters.BackBufferWidth == pInternalCachedParams.BackBufferWidth && presentationParameters.BackBufferHeight == pInternalCachedParams.BackBufferHeight) ? 1 : 0);
		bool saveViewport = (byte)num != 0;
		SavedDeviceState savedDeviceState = new SavedDeviceState(this, saveViewport);
		if (graphicsAdapter != pCurrentAdapter && _deviceType == (_D3DDEVTYPE)1 && graphicsAdapter.IsProfileSupported((_D3DDEVTYPE)1, _graphicsProfile))
		{
			flag = true;
		}
		EffectPass effectPass = activePass;
		if (effectPass != null)
		{
			effectPass.EndPass();
			activePass = null;
		}
		pResourceManager.ReleaseAllDefaultPoolResources();
		lazyClearFlags = 0;
		savedBackBufferClearFlags = 0;
		Indices = null;
		SetVertexBuffers(null, 0);
		SetRenderTargets(null, 0);
		pTextureCollection.ResetState();
		pVertexTextureCollection.ResetState();
		StateTrackerDevice* ptr = pStateTracker;
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, IDirect3DVertexDeclaration9*, int>)(int)(*(uint*)(*(int*)ptr + 348)))((nint)ptr, null);
		ptr = pStateTracker;
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, IDirect3DVertexShader9*, int>)(int)(*(uint*)(*(int*)ptr + 368)))((nint)ptr, null);
		ptr = pStateTracker;
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, IDirect3DPixelShader9*, int>)(int)(*(uint*)(*(int*)ptr + 428)))((nint)ptr, null);
		vertexDeclarationManager.ReleaseAllDeclarations();
		IDirect3DSurface9* ptr2 = pImplicitDepthSurface;
		if (ptr2 != null)
		{
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)ptr2 + 8)))((nint)ptr2);
			pImplicitDepthSurface = null;
		}
		if (flag)
		{
			pResourceManager.ReleaseAutomaticResources();
			ptr = pStateTracker;
			if (ptr != null)
			{
				StateTrackerDevice* intPtr2 = ptr;
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)intPtr2 + 8)))((nint)intPtr2);
				pStateTracker = null;
			}
			IDirect3DDevice9* ptr3 = pComPtr;
			IDirect3DDevice9* intPtr3 = ptr3;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)intPtr3 + 8)))((nint)intPtr3);
			pComPtr = null;
			CreateDevice(graphicsAdapter, presentationParameters);
			pResourceManager.RecreateResources((_D3DPOOL)1, flag);
			pCurrentAdapter = graphicsAdapter;
		}
		else
		{
			System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DPRESENT_PARAMETERS_ d3DPRESENT_PARAMETERS_);
			*(int*)(&d3DPRESENT_PARAMETERS_) = 0;
			// IL initblk instruction
			System.Runtime.CompilerServices.Unsafe.InitBlock(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DPRESENT_PARAMETERS_, 4), 0, 52);
			ConvertPresentationParametersToNative(pCurrentAdapter, presentationParameters, &d3DPRESENT_PARAMETERS_);
			IDirect3DDevice9* ptr3 = pComPtr;
			int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DPRESENT_PARAMETERS_*, int>)(int)(*(uint*)(*(int*)ptr3 + 64)))((nint)ptr3, &d3DPRESENT_PARAMETERS_);
			ConvertPresentationParametersToManaged(&d3DPRESENT_PARAMETERS_, presentationParameters);
			if (num2 < 0)
			{
				throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
			}
		}
		fixed (IDirect3DSurface9** ptr4 = &pImplicitDepthSurface)
		{
			int num3 = *(int*)pComPtr + 160;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, IDirect3DSurface9**, int>)(int)(*(uint*)num3))((nint)pComPtr, ptr4);
			pResourceManager.RecreateResources((_D3DPOOL)0, flag);
			pInternalCachedParams = presentationParameters.Clone();
			pPublicCachedParams = presentationParameters.Clone();
			InitializeDeviceState();
			savedDeviceState.Restore();
			EventArgs empty2 = EventArgs.Empty;
			_003Cbacking_store_003EDeviceReset?.Invoke(this, empty2);
			_insideScene = false;
		}
	}

	public unsafe void DrawPrimitives(PrimitiveType primitiveType, int startVertex, int primitiveCount)
	{
		IntPtr intPtr = (IntPtr)pComPtr;
		Helpers.CheckDisposed(this, intPtr);
		if (primitiveCount <= 0)
		{
			throw new ArgumentOutOfRangeException("primitiveCount", FrameworkResources.MustDrawSomething);
		}
		int maxPrimitiveCount = _profileCapabilities.MaxPrimitiveCount;
		if (primitiveCount > maxPrimitiveCount)
		{
			_profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileMaxPrimitiveCount, maxPrimitiveCount);
		}
		VerifyCanDraw(bUserPrimitives: false, bIndexedPrimitives: false);
		if (instanceStreamMask != 0)
		{
			throw new InvalidOperationException(FrameworkResources.NonZeroInstanceFrequency);
		}
		if (!_insideScene)
		{
			IDirect3DDevice9* intPtr2 = pComPtr;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr2 + 164)))((nint)intPtr2);
			_insideScene = true;
		}
		int num = *(int*)pComPtr + 324;
		int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DPRIMITIVETYPE, uint, uint, int>)(int)(*(uint*)num))((nint)pComPtr, _003CModule_003E.ConvertXnaPrimitiveTypeToDx(primitiveType), (uint)startVertex, (uint)primitiveCount);
		if (num2 < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
		}
	}

	public unsafe void DrawIndexedPrimitives(PrimitiveType primitiveType, int baseVertex, int minVertexIndex, int numVertices, int startIndex, int primitiveCount)
	{
		IntPtr intPtr = (IntPtr)pComPtr;
		Helpers.CheckDisposed(this, intPtr);
		if (numVertices <= 0)
		{
			throw new ArgumentOutOfRangeException("numVertices", FrameworkResources.NumberVerticesMustBeGreaterZero);
		}
		if (primitiveCount <= 0)
		{
			throw new ArgumentOutOfRangeException("primitiveCount", FrameworkResources.MustDrawSomething);
		}
		int maxPrimitiveCount = _profileCapabilities.MaxPrimitiveCount;
		if (primitiveCount > maxPrimitiveCount)
		{
			_profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileMaxPrimitiveCount, maxPrimitiveCount);
		}
		VerifyCanDraw(bUserPrimitives: false, bIndexedPrimitives: true);
		if (instanceStreamMask != 0)
		{
			throw new InvalidOperationException(FrameworkResources.NonZeroInstanceFrequency);
		}
		if (!_insideScene)
		{
			IDirect3DDevice9* intPtr2 = pComPtr;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr2 + 164)))((nint)intPtr2);
			_insideScene = true;
		}
		int num = *(int*)pComPtr + 328;
		int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DPRIMITIVETYPE, int, uint, uint, uint, uint, int>)(int)(*(uint*)num))((nint)pComPtr, _003CModule_003E.ConvertXnaPrimitiveTypeToDx(primitiveType), baseVertex, (uint)minVertexIndex, (uint)numVertices, (uint)startIndex, (uint)primitiveCount);
		if (num2 < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
		}
	}

	public unsafe void DrawInstancedPrimitives(PrimitiveType primitiveType, int baseVertex, int minVertexIndex, int numVertices, int startIndex, int primitiveCount, int instanceCount)
	{
		IntPtr intPtr = (IntPtr)pComPtr;
		Helpers.CheckDisposed(this, intPtr);
		if (numVertices <= 0)
		{
			throw new ArgumentOutOfRangeException("numVertices", FrameworkResources.NumberVerticesMustBeGreaterZero);
		}
		if (primitiveCount <= 0)
		{
			throw new ArgumentOutOfRangeException("primitiveCount", FrameworkResources.MustDrawSomething);
		}
		int maxPrimitiveCount = _profileCapabilities.MaxPrimitiveCount;
		if (primitiveCount > maxPrimitiveCount)
		{
			_profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileMaxPrimitiveCount, maxPrimitiveCount);
		}
		if (instanceCount <= 0)
		{
			throw new ArgumentOutOfRangeException("instanceCount", FrameworkResources.MustDrawSomething);
		}
		maxPrimitiveCount = _profileCapabilities.MaxPrimitiveCount;
		if (instanceCount > maxPrimitiveCount)
		{
			_profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileMaxPrimitiveCount, maxPrimitiveCount);
		}
		VerifyCanDraw(bUserPrimitives: false, bIndexedPrimitives: true);
		int num = instanceStreamMask;
		bool flag = num == 0;
		bool flag2 = num == (1 << currentVertexBufferCount) - 1;
		if (!flag && !flag2)
		{
			_D3DPRIMITIVETYPE d3DPRIMITIVETYPE = _003CModule_003E.ConvertXnaPrimitiveTypeToDx(primitiveType);
			if (!_insideScene)
			{
				IDirect3DDevice9* intPtr2 = pComPtr;
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr2 + 164)))((nint)intPtr2);
				_insideScene = true;
			}
			if ((uint)(*(int*)((byte*)pStateTracker + 20)) >= 768u)
			{
				try
				{
					for (int i = 0; i < currentVertexBufferCount; i++)
					{
						uint num2 = 1073741823u;
						uint instanceFrequency = (uint)currentVertexBuffers[i]._instanceFrequency;
						uint num3 = ((instanceFrequency == 0) ? ((uint)instanceCount | 0x40000000u) : (((instanceFrequency >= 1073741823) ? 1073741823u : instanceFrequency) | 0x80000000u));
						IDirect3DDevice9* ptr = pComPtr;
						int num4 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, int>)(int)(*(uint*)(*(int*)ptr + 408)))((nint)ptr, (uint)i, num3);
						if (num4 < 0)
						{
							throw GraphicsHelpers.GetExceptionFromResult((uint)num4);
						}
					}
					IDirect3DDevice9* ptr2 = pComPtr;
					int num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DPRIMITIVETYPE, int, uint, uint, uint, uint, int>)(int)(*(uint*)(*(int*)ptr2 + 328)))((nint)ptr2, d3DPRIMITIVETYPE, baseVertex, (uint)minVertexIndex, (uint)numVertices, (uint)startIndex, (uint)primitiveCount);
					if (num5 < 0)
					{
						throw GraphicsHelpers.GetExceptionFromResult((uint)num5);
					}
					return;
				}
				finally
				{
					int num6 = 0;
					if (0 < currentVertexBufferCount)
					{
						do
						{
							IDirect3DDevice9* ptr3 = pComPtr;
							((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, int>)(int)(*(uint*)(*(int*)ptr3 + 408)))((nint)ptr3, (uint)num6, 1u);
							num6++;
						}
						while (num6 < currentVertexBufferCount);
					}
				}
			}
			int num7 = 0;
			int num8 = 0;
			System.Runtime.CompilerServices.Unsafe.SkipInit(out _0024ArrayType_0024_0024_0024BY0BA_0040H _0024ArrayType_0024_0024_0024BY0BA_0040H);
			System.Runtime.CompilerServices.Unsafe.SkipInit(out _0024ArrayType_0024_0024_0024BY0BA_0040I _0024ArrayType_0024_0024_0024BY0BA_0040I);
			System.Runtime.CompilerServices.Unsafe.SkipInit(out _0024ArrayType_0024_0024_0024BY0BA_0040I _0024ArrayType_0024_0024_0024BY0BA_0040I2);
			System.Runtime.CompilerServices.Unsafe.SkipInit(out _0024ArrayType_0024_0024_0024BY0BA_0040I _0024ArrayType_0024_0024_0024BY0BA_0040I3);
			System.Runtime.CompilerServices.Unsafe.SkipInit(out _0024ArrayType_0024_0024_0024BY0BA_0040PAUIDirect3DVertexBuffer9_0040_0040 _0024ArrayType_0024_0024_0024BY0BA_0040PAUIDirect3DVertexBuffer9_0040_0040);
			if (0 < currentVertexBufferCount)
			{
				do
				{
					if (num8 < 16)
					{
						if ((instanceStreamMask & (1 << num8)) != 0)
						{
							*(int*)((ref *(_003F*)(num7 * 4)) + (ref *(_003F*)(&_0024ArrayType_0024_0024_0024BY0BA_0040H))) = num8;
							VertexBufferBinding[] array = currentVertexBuffers;
							*(int*)((ref *(_003F*)(num7 * 4)) + (ref *(_003F*)(&_0024ArrayType_0024_0024_0024BY0BA_0040I))) = array[num8]._vertexOffset;
							*(int*)((ref *(_003F*)(num7 * 4)) + (ref *(_003F*)(&_0024ArrayType_0024_0024_0024BY0BA_0040I2))) = array[num8]._instanceFrequency;
							VertexBuffer vertexBuffer = array[num8]._vertexBuffer;
							*(int*)((ref *(_003F*)(num7 * 4)) + (ref *(_003F*)(&_0024ArrayType_0024_0024_0024BY0BA_0040I3))) = vertexBuffer._vertexDeclaration._vertexStride;
							*(int*)((ref *(_003F*)(num7 * 4)) + (ref *(_003F*)(&_0024ArrayType_0024_0024_0024BY0BA_0040PAUIDirect3DVertexBuffer9_0040_0040))) = (int)vertexBuffer.pComPtr;
							num7++;
						}
						num8++;
						continue;
					}
					throw new InvalidOperationException();
				}
				while (num8 < currentVertexBufferCount);
			}
			int num9 = 0;
			if (0 >= instanceCount)
			{
				return;
			}
			int num13;
			while (true)
			{
				int num10 = 0;
				if (0 < num7)
				{
					do
					{
						uint num11 = (uint)(((int)((uint)num9 / (uint)(*(int*)((ref *(_003F*)(num10 * 4)) + (ref *(_003F*)(&_0024ArrayType_0024_0024_0024BY0BA_0040I2))))) + *(int*)((ref *(_003F*)(num10 * 4)) + (ref *(_003F*)(&_0024ArrayType_0024_0024_0024BY0BA_0040I)))) * *(int*)((ref *(_003F*)(num10 * 4)) + (ref *(_003F*)(&_0024ArrayType_0024_0024_0024BY0BA_0040I3))));
						IDirect3DDevice9* ptr4 = pComPtr;
						int num12 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, IDirect3DVertexBuffer9*, uint, uint, int>)(int)(*(uint*)(*(int*)ptr4 + 400)))((nint)ptr4, *(uint*)((ref *(_003F*)(num10 * 4)) + (ref *(_003F*)(&_0024ArrayType_0024_0024_0024BY0BA_0040H))), (IDirect3DVertexBuffer9*)(int)(*(uint*)((ref *(_003F*)(num10 * 4)) + (ref *(_003F*)(&_0024ArrayType_0024_0024_0024BY0BA_0040PAUIDirect3DVertexBuffer9_0040_0040)))), num11, 0u);
						if (num12 >= 0)
						{
							num10++;
							continue;
						}
						throw GraphicsHelpers.GetExceptionFromResult((uint)num12);
					}
					while (num10 < num7);
				}
				IDirect3DDevice9* ptr5 = pComPtr;
				num13 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DPRIMITIVETYPE, int, uint, uint, uint, uint, int>)(int)(*(uint*)(*(int*)ptr5 + 328)))((nint)ptr5, d3DPRIMITIVETYPE, baseVertex, (uint)minVertexIndex, (uint)numVertices, (uint)startIndex, (uint)primitiveCount);
				if (num13 < 0)
				{
					break;
				}
				num9++;
				if (num9 >= instanceCount)
				{
					return;
				}
			}
			throw GraphicsHelpers.GetExceptionFromResult((uint)num13);
		}
		throw new InvalidOperationException(FrameworkResources.InvalidInstanceStreams);
	}

	private uint GetElementCountFromPrimitiveType(PrimitiveType primitiveType, int primitiveCount)
	{
		uint result = uint.MaxValue;
		switch (primitiveType)
		{
		case PrimitiveType.LineList:
			result = (uint)(primitiveCount << 1);
			break;
		case PrimitiveType.TriangleList:
			result = (uint)(primitiveCount * 3);
			break;
		case PrimitiveType.LineStrip:
			result = (uint)(primitiveCount + 1);
			break;
		case PrimitiveType.TriangleStrip:
			result = (uint)(primitiveCount + 2);
			break;
		}
		return result;
	}

	private unsafe void BeginUserPrimitives(VertexDeclaration vertexDeclaration)
	{
		int num = 0;
		if (0 < currentVertexBufferCount)
		{
			do
			{
				if (num > 0)
				{
					IDirect3DDevice9* ptr = pComPtr;
					((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, IDirect3DVertexBuffer9*, uint, uint, int>)(int)(*(uint*)(*(int*)ptr + 400)))((nint)ptr, (uint)num, null, 0u, 0u);
				}
				currentVertexBuffers[num] = default(VertexBufferBinding);
				num++;
			}
			while (num < currentVertexBufferCount);
		}
		currentVertexBufferCount = 0;
		instanceStreamMask = 0;
		vertexDeclarationManager.SetVertexDeclaration(vertexDeclaration);
		if (!_insideScene)
		{
			IDirect3DDevice9* intPtr = pComPtr;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr + 164)))((nint)intPtr);
			_insideScene = true;
		}
	}

	public void DrawUserIndexedPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int numVertices, int[] indexData, int indexOffset, int primitiveCount) where T : struct, IVertexType
	{
		if (!_profileCapabilities.IndexElementSize32)
		{
			_profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileNoIndexElementSize32);
		}
		T[] vertexData2 = vertexData;
		DrawUserIndexedPrimitives(primitiveType, vertexData2, vertexOffset, numVertices, indexData, indexOffset, primitiveCount, VertexDeclarationFactory<T>.VertexDeclaration, (_D3DFORMAT)102);
	}

	public void DrawUserIndexedPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int numVertices, short[] indexData, int indexOffset, int primitiveCount) where T : struct, IVertexType
	{
		T[] vertexData2 = vertexData;
		DrawUserIndexedPrimitives(primitiveType, vertexData2, vertexOffset, numVertices, indexData, indexOffset, primitiveCount, VertexDeclarationFactory<T>.VertexDeclaration, (_D3DFORMAT)101);
	}

	public void DrawUserIndexedPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int numVertices, int[] indexData, int indexOffset, int primitiveCount, VertexDeclaration vertexDeclaration) where T : struct
	{
		if (!_profileCapabilities.IndexElementSize32)
		{
			_profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileNoIndexElementSize32);
		}
		DrawUserIndexedPrimitives(primitiveType, vertexData, vertexOffset, numVertices, indexData, indexOffset, primitiveCount, vertexDeclaration, (_D3DFORMAT)102);
	}

	public void DrawUserIndexedPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int numVertices, short[] indexData, int indexOffset, int primitiveCount, VertexDeclaration vertexDeclaration) where T : struct
	{
		DrawUserIndexedPrimitives(primitiveType, vertexData, vertexOffset, numVertices, indexData, indexOffset, primitiveCount, vertexDeclaration, (_D3DFORMAT)101);
	}

	private unsafe void DrawUserIndexedPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int numVertices, Array indexData, int indexOffset, int primitiveCount, VertexDeclaration vertexDeclaration, _D3DFORMAT indexFormat) where T : struct
	{
		IntPtr intPtr = (IntPtr)pComPtr;
		Helpers.CheckDisposed(this, intPtr);
		if (vertexData != null && vertexData.Length != 0)
		{
			if (indexData != null && indexData.Length != 0)
			{
				if (vertexDeclaration == null)
				{
					throw new ArgumentNullException("vertexDeclaration", FrameworkResources.NullNotAllowed);
				}
				if (numVertices <= 0)
				{
					throw new ArgumentOutOfRangeException("numVertices", FrameworkResources.NumberVerticesMustBeGreaterZero);
				}
				if (primitiveCount <= 0)
				{
					throw new ArgumentOutOfRangeException("primitiveCount", FrameworkResources.MustDrawSomething);
				}
				int maxPrimitiveCount = _profileCapabilities.MaxPrimitiveCount;
				if (primitiveCount > maxPrimitiveCount)
				{
					_profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileMaxPrimitiveCount, maxPrimitiveCount);
				}
				if (vertexOffset >= 0 && vertexOffset < (nint)vertexData.LongLength)
				{
					if (indexOffset >= 0 && indexOffset < indexData.Length)
					{
						if ((uint)((int)GetElementCountFromPrimitiveType(primitiveType, primitiveCount) + indexOffset) > (uint)indexData.Length)
						{
							throw new ArgumentOutOfRangeException("primitiveCount", FrameworkResources.MustBeValidIndex);
						}
						if (vertexOffset + numVertices > (nint)vertexData.LongLength)
						{
							throw new ArgumentOutOfRangeException("vertexData", FrameworkResources.MustBeValidIndex);
						}
						BeginUserPrimitives(vertexDeclaration);
						VerifyCanDraw(bUserPrimitives: true, bIndexedPrimitives: true);
						GCHandle gCHandle = GCHandle.Alloc(indexData, GCHandleType.Pinned);
						try
						{
							fixed (T* ptr2 = &vertexData[vertexOffset])
							{
								try
								{
									void* ptr = Marshal.UnsafeAddrOfPinnedArrayElement(indexData, indexOffset).ToPointer();
									int num = *(int*)pComPtr + 336;
									int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DPRIMITIVETYPE, uint, uint, uint, void*, _D3DFORMAT, void*, uint, int>)(int)(*(uint*)num))((nint)pComPtr, _003CModule_003E.ConvertXnaPrimitiveTypeToDx(primitiveType), 0u, (uint)numVertices, (uint)primitiveCount, ptr, indexFormat, ptr2, (uint)System.Runtime.CompilerServices.Unsafe.SizeOf<T>());
									if (num2 >= 0)
									{
										return;
									}
									throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
								}
								catch
								{
									//try-fault
									ptr2 = null;
									throw;
								}
							}
						}
						finally
						{
							if (gCHandle.IsAllocated)
							{
								gCHandle.Free();
							}
							_currentIB = null;
						}
					}
					throw new ArgumentOutOfRangeException("indexOffset", FrameworkResources.OffsetNotValid);
				}
				throw new ArgumentOutOfRangeException("vertexOffset", FrameworkResources.OffsetNotValid);
			}
			throw new ArgumentNullException("indexData", FrameworkResources.NullNotAllowed);
		}
		throw new ArgumentNullException("vertexData", FrameworkResources.NullNotAllowed);
	}

	public void DrawUserPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int primitiveCount) where T : struct, IVertexType
	{
		DrawUserPrimitives(primitiveType, vertexData, vertexOffset, primitiveCount, VertexDeclarationFactory<T>.VertexDeclaration);
	}

	public unsafe void DrawUserPrimitives<T>(PrimitiveType primitiveType, T[] vertexData, int vertexOffset, int primitiveCount, VertexDeclaration vertexDeclaration) where T : struct
	{
		IntPtr intPtr = (IntPtr)pComPtr;
		Helpers.CheckDisposed(this, intPtr);
		if (vertexData == null)
		{
			throw new ArgumentNullException("vertexData", FrameworkResources.NullNotAllowed);
		}
		if (vertexDeclaration == null)
		{
			throw new ArgumentNullException("vertexDeclaration", FrameworkResources.NullNotAllowed);
		}
		if (primitiveCount <= 0)
		{
			throw new ArgumentOutOfRangeException("primitiveCount", FrameworkResources.MustDrawSomething);
		}
		int maxPrimitiveCount = _profileCapabilities.MaxPrimitiveCount;
		if (primitiveCount > maxPrimitiveCount)
		{
			_profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileMaxPrimitiveCount, maxPrimitiveCount);
		}
		if (vertexOffset >= 0)
		{
			int num = vertexData.Length;
			if (vertexOffset < num)
			{
				if ((uint)((int)GetElementCountFromPrimitiveType(primitiveType, primitiveCount) + vertexOffset) > (uint)num)
				{
					throw new ArgumentOutOfRangeException("primitiveCount", FrameworkResources.MustBeValidIndex);
				}
				BeginUserPrimitives(vertexDeclaration);
				VerifyCanDraw(bUserPrimitives: true, bIndexedPrimitives: false);
				fixed (T* ptr = &vertexData[vertexOffset])
				{
					int num2 = *(int*)pComPtr + 332;
					int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DPRIMITIVETYPE, uint, void*, uint, int>)(int)(*(uint*)num2))((nint)pComPtr, _003CModule_003E.ConvertXnaPrimitiveTypeToDx(primitiveType), (uint)primitiveCount, ptr, (uint)System.Runtime.CompilerServices.Unsafe.SizeOf<T>());
					if (num3 < 0)
					{
						throw GraphicsHelpers.GetExceptionFromResult((uint)num3);
					}
					return;
				}
			}
		}
		throw new ArgumentOutOfRangeException("vertexOffset", FrameworkResources.OffsetNotValid);
	}

	public void Clear(ClearOptions options, Vector4 color, float depth, int stencil)
	{
		Color color2 = new Color(color);
		Clear(options, color2, depth, stencil);
	}

	public unsafe void Clear(ClearOptions options, Color color, float depth, int stencil)
	{
		IntPtr intPtr = (IntPtr)pComPtr;
		Helpers.CheckDisposed(this, intPtr);
		uint num = 0u;
		IDirect3DDevice9* ptr = pComPtr;
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint*, int>)(int)(*(uint*)(*(int*)ptr + 232)))((nint)ptr, (_D3DRENDERSTATETYPE)174, &num);
		if (num != 0)
		{
			ptr = pComPtr;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)ptr + 228)))((nint)ptr, (_D3DRENDERSTATETYPE)174, 0u);
		}
		int num2;
		int num3;
		if (currentRenderTargetCount > 0)
		{
			RenderTargetHelper renderTargetHelper = currentRenderTargets[0];
			num2 = renderTargetHelper.width;
			num3 = renderTargetHelper.height;
		}
		else
		{
			num2 = pInternalCachedParams.BackBufferWidth;
			num3 = pInternalCachedParams.BackBufferHeight;
		}
		bool flag = false;
		if (currentViewport.X != 0 || currentViewport.Y != 0 || currentViewport.Width != num2 || currentViewport.Height != num3)
		{
			System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DVIEWPORT9 d3DVIEWPORT);
			*(int*)(&d3DVIEWPORT) = 0;
			System.Runtime.CompilerServices.Unsafe.As<_D3DVIEWPORT9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DVIEWPORT, 4)) = 0;
			System.Runtime.CompilerServices.Unsafe.As<_D3DVIEWPORT9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DVIEWPORT, 8)) = num2;
			System.Runtime.CompilerServices.Unsafe.As<_D3DVIEWPORT9, int>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DVIEWPORT, 12)) = num3;
			System.Runtime.CompilerServices.Unsafe.As<_D3DVIEWPORT9, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DVIEWPORT, 16)) = 0f;
			System.Runtime.CompilerServices.Unsafe.As<_D3DVIEWPORT9, float>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DVIEWPORT, 20)) = 1f;
			ptr = pComPtr;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DVIEWPORT9*, int>)(int)(*(uint*)(*(int*)ptr + 188)))((nint)ptr, &d3DVIEWPORT);
			flag = true;
		}
		uint num4 = (uint)(color.A << 8);
		uint num5 = (color.R | num4) << 8;
		uint num6 = (color.G | num5) << 8;
		uint num7 = color.B | num6;
		IDirect3DDevice9* ptr2 = pComPtr;
		int num8 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, _D3DRECT*, uint, uint, float, uint, int>)(int)(*(uint*)(*(int*)ptr2 + 172)))((nint)ptr2, 0u, null, (uint)options, num7, depth, (uint)stencil);
		if (num != 0)
		{
			ptr2 = pComPtr;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)ptr2 + 228)))((nint)ptr2, (_D3DRENDERSTATETYPE)174, num);
		}
		if (flag)
		{
			Viewport viewport = currentViewport;
			ptr2 = pComPtr;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DVIEWPORT9*, int>)(int)(*(uint*)(*(int*)ptr2 + 188)))((nint)ptr2, (_D3DVIEWPORT9*)(int)(ref viewport));
		}
		if (num8 < 0)
		{
			ClearOptions clearOptions = options & (ClearOptions.DepthBuffer | ClearOptions.Stencil);
			if ((DefaultClearOptions & clearOptions) != clearOptions)
			{
				throw new InvalidOperationException(FrameworkResources.CannotClearNullDepth);
			}
			throw GraphicsHelpers.GetExceptionFromResult((uint)num8);
		}
		int num9 = 0;
		if (0 < currentRenderTargetCount)
		{
			do
			{
				currentRenderTargets[num9].pTexture.SetContentLost(isContentLost: false);
				num9++;
			}
			while (num9 < currentRenderTargetCount);
		}
		lazyClearFlags &= (int)(~options);
	}

	public void Clear(Color color)
	{
		Clear(DefaultClearOptions, color, 1f, 0);
	}

	public unsafe void SetRenderTargets(params RenderTargetBinding[] renderTargets)
	{
		if (renderTargets != null && (nint)renderTargets.LongLength > 0)
		{
			fixed (RenderTargetBinding* pBindings = &renderTargets[0])
			{
				try
				{
					SetRenderTargets(pBindings, renderTargets.Length);
				}
				catch
				{
					//try-fault
					pBindings = null;
					throw;
				}
			}
		}
		else
		{
			SetRenderTargets(null, 0);
		}
	}

	private unsafe void SetRenderTargets(RenderTargetBinding* pBindings, int renderTargetCount)
	{
		IntPtr intPtr = (IntPtr)pComPtr;
		Helpers.CheckDisposed(this, intPtr);
		if (renderTargetCount == currentRenderTargetCount)
		{
			int num = 0;
			if (0 >= renderTargetCount)
			{
				return;
			}
			RenderTargetBinding[] array = currentRenderTargetBindings;
			int num2 = System.Runtime.CompilerServices.Unsafe.SizeOf<RenderTargetBinding>();
			RenderTargetBinding* ptr = pBindings;
			while (ptr->_renderTarget == array[num]._renderTarget && ptr->_cubeMapFace == array[num]._cubeMapFace)
			{
				num++;
				ptr = (RenderTargetBinding*)(num2 + (byte*)ptr);
				if (num >= renderTargetCount)
				{
					return;
				}
			}
			if (num >= renderTargetCount)
			{
				return;
			}
		}
		int maxRenderTargets = _profileCapabilities.MaxRenderTargets;
		if (renderTargetCount > maxRenderTargets)
		{
			_profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileMaxRenderTargets, maxRenderTargets);
		}
		int num3 = 0;
		if (0 < renderTargetCount)
		{
			do
			{
				Texture renderTarget = ((RenderTargetBinding*)(System.Runtime.CompilerServices.Unsafe.SizeOf<RenderTargetBinding>() * num3 + (byte*)pBindings))->_renderTarget;
				if (renderTarget != null)
				{
					IntPtr intPtr2 = (IntPtr)renderTarget.GetComPtr();
					Helpers.CheckDisposed(renderTarget, intPtr2);
					if (renderTarget.GraphicsDevice == this)
					{
						if (num3 > 0)
						{
							int num4 = 0;
							if (0 < num3)
							{
								int num5 = System.Runtime.CompilerServices.Unsafe.SizeOf<RenderTargetBinding>();
								RenderTargetBinding* ptr2 = pBindings;
								do
								{
									if (renderTarget != ptr2->_renderTarget)
									{
										num4++;
										ptr2 = (RenderTargetBinding*)(num5 + (byte*)ptr2);
										continue;
									}
									throw new ArgumentException(FrameworkResources.CannotSetAlreadyUsedRenderTarget);
								}
								while (num4 < num3);
							}
							if (!RenderTargetHelper.IsSameSize(renderTarget, pBindings->_renderTarget))
							{
								throw new ArgumentException(FrameworkResources.RenderTargetsMustMatch);
							}
						}
						num3++;
						continue;
					}
					throw new InvalidOperationException(FrameworkResources.InvalidDevice);
				}
				throw new ArgumentException(FrameworkResources.NullNotAllowed);
			}
			while (num3 < renderTargetCount);
		}
		if (currentRenderTargetCount <= 0)
		{
			savedBackBufferClearFlags = lazyClearFlags;
		}
		else
		{
			if (lazyClearFlags != 0)
			{
				ClearDirtyBuffers();
			}
			int num6 = 0;
			if (0 < currentRenderTargetCount)
			{
				do
				{
					ResolveRenderTarget(num6);
					num6++;
				}
				while (num6 < currentRenderTargetCount);
			}
		}
		int num7 = 0;
		willItBlend = true;
		currentViewport = default(Viewport);
		currentViewport.MaxDepth = 1f;
		if (renderTargetCount <= 0)
		{
			IDirect3DSurface9* ptr3 = null;
			IDirect3DDevice9* ptr4 = pComPtr;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, _D3DBACKBUFFER_TYPE, IDirect3DSurface9**, int>)(int)(*(uint*)(*(int*)ptr4 + 72)))((nint)ptr4, 0u, 0u, (_D3DBACKBUFFER_TYPE)0, &ptr3);
			IDirect3DDevice9* ptr5 = pComPtr;
			num7 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, IDirect3DSurface9*, int>)(int)(*(uint*)(*(int*)ptr5 + 148)))((nint)ptr5, 0u, ptr3);
			if (ptr3 != null)
			{
				IDirect3DSurface9* intPtr3 = ptr3;
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)intPtr3 + 8)))((nint)intPtr3);
				ptr3 = null;
			}
			if (num7 >= 0)
			{
				ptr5 = pComPtr;
				num7 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, IDirect3DSurface9*, int>)(int)(*(uint*)(*(int*)ptr5 + 156)))((nint)ptr5, pImplicitDepthSurface);
			}
			if (pInternalCachedParams.RenderTargetUsage == RenderTargetUsage.DiscardContents)
			{
				lazyClearFlags = 7;
			}
			else
			{
				lazyClearFlags = savedBackBufferClearFlags;
			}
			currentViewport.Width = pInternalCachedParams.BackBufferWidth;
			currentViewport.Height = pInternalCachedParams.BackBufferHeight;
		}
		else
		{
			lazyClearFlags = 0;
			int num8 = 0;
			if (0 < renderTargetCount)
			{
				do
				{
					Texture renderTarget2 = ((RenderTargetBinding*)(System.Runtime.CompilerServices.Unsafe.SizeOf<RenderTargetBinding>() * num8 + (byte*)pBindings))->_renderTarget;
					RenderTargetHelper renderTargetHelper = RenderTargetHelper.FromRenderTarget(renderTarget2);
					currentRenderTargets[num8] = renderTargetHelper;
					ref RenderTargetBinding reference = ref currentRenderTargetBindings[num8];
					reference = System.Runtime.CompilerServices.Unsafe.Read<RenderTargetBinding>(System.Runtime.CompilerServices.Unsafe.SizeOf<RenderTargetBinding>() * num8 + (byte*)pBindings);
					renderTarget2.isActiveRenderTarget = true;
					if (!renderTargetHelper.willItBlend)
					{
						willItBlend = false;
						whyWontItBlend = renderTargetHelper.format;
					}
					if (num7 >= 0)
					{
						IDirect3DSurface9* renderTargetSurface = renderTargetHelper.GetRenderTargetSurface(((RenderTargetBinding*)(System.Runtime.CompilerServices.Unsafe.SizeOf<RenderTargetBinding>() * num8 + (byte*)pBindings))->_cubeMapFace);
						IDirect3DDevice9* ptr6 = pComPtr;
						num7 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, IDirect3DSurface9*, int>)(int)(*(uint*)(*(int*)ptr6 + 148)))((nint)ptr6, (uint)num8, renderTargetSurface);
						if (renderTargetSurface != null)
						{
							((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)renderTargetSurface + 8)))((nint)renderTargetSurface);
						}
						if (num7 >= 0 && num8 == 0)
						{
							IDirect3DDevice9* ptr7 = pComPtr;
							num7 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, IDirect3DSurface9*, int>)(int)(*(uint*)(*(int*)ptr7 + 156)))((nint)ptr7, renderTargetHelper.pDepthSurface);
						}
					}
					if (renderTargetHelper.usage == RenderTargetUsage.DiscardContents)
					{
						int num9 = (lazyClearFlags |= 1);
						if (num8 == 0)
						{
							lazyClearFlags = num9 | 6;
						}
					}
					else if (renderTargetHelper.pRenderTargetSurface != null && (renderTargetHelper.isCubemap || renderTarget2.renderTargetContentsDirty) && num7 >= 0)
					{
						if (_insideScene)
						{
							IDirect3DDevice9* intPtr4 = pComPtr;
							((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr4 + 168)))((nint)intPtr4);
						}
						IDirect3DSurface9* destinationSurface = renderTargetHelper.GetDestinationSurface(((RenderTargetBinding*)(System.Runtime.CompilerServices.Unsafe.SizeOf<RenderTargetBinding>() * num8 + (byte*)pBindings))->_cubeMapFace, 0);
						IDirect3DSurface9* pRenderTargetSurface = renderTargetHelper.pRenderTargetSurface;
						IDirect3DDevice9* ptr8 = pComPtr;
						int num10 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, IDirect3DSurface9*, tagRECT*, IDirect3DSurface9*, tagRECT*, _D3DTEXTUREFILTERTYPE, int>)(int)(*(uint*)(*(int*)ptr8 + 136)))((nint)ptr8, destinationSurface, null, pRenderTargetSurface, null, (_D3DTEXTUREFILTERTYPE)0);
						num7 = ((num10 < 0) ? _003CModule_003E.D3DXLoadSurfaceFromSurface(pRenderTargetSurface, null, null, destinationSurface, null, null, uint.MaxValue, 0u) : num10);
						if (destinationSurface != null)
						{
							((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)destinationSurface + 8)))((nint)destinationSurface);
						}
						if (num7 >= 0)
						{
							renderTarget2.renderTargetContentsDirty = false;
						}
						if (_insideScene)
						{
							IDirect3DDevice9* intPtr5 = pComPtr;
							((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr5 + 164)))((nint)intPtr5);
						}
					}
					num8++;
				}
				while (num8 < renderTargetCount);
			}
			currentViewport.Width = currentRenderTargets[0].width;
			currentViewport.Height = currentRenderTargets[0].height;
		}
		int num11 = renderTargetCount;
		if (renderTargetCount < currentRenderTargetCount)
		{
			do
			{
				currentRenderTargets[num11] = null;
				currentRenderTargetBindings[num11] = default(RenderTargetBinding);
				if (num11 > 0)
				{
					IDirect3DDevice9* ptr9 = pComPtr;
					((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, IDirect3DSurface9*, int>)(int)(*(uint*)(*(int*)ptr9 + 148)))((nint)ptr9, (uint)num11, null);
				}
				num11++;
			}
			while (num11 < currentRenderTargetCount);
		}
		currentRenderTargetCount = renderTargetCount;
		lazyClearFlags = (int)DefaultClearOptions & lazyClearFlags;
		if (num7 < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num7);
		}
	}

	private unsafe void ResolveRenderTarget(int index)
	{
		int num = 0;
		RenderTargetHelper renderTargetHelper = currentRenderTargets[index];
		RenderTargetBinding[] array = currentRenderTargetBindings;
		Texture renderTarget = array[index]._renderTarget;
		CubeMapFace cubeMapFace = array[index]._cubeMapFace;
		renderTarget.isActiveRenderTarget = false;
		if (renderTarget.GetComPtr() == null)
		{
			return;
		}
		if (_insideScene)
		{
			IDirect3DDevice9* intPtr = pComPtr;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr + 168)))((nint)intPtr);
		}
		IDirect3DSurface9* ptr = renderTargetHelper.GetDestinationSurface(cubeMapFace, 0);
		IDirect3DSurface9* pRenderTargetSurface = renderTargetHelper.pRenderTargetSurface;
		if (pRenderTargetSurface != null)
		{
			IDirect3DSurface9* ptr2 = pRenderTargetSurface;
			IDirect3DDevice9* ptr3 = pComPtr;
			int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, IDirect3DSurface9*, tagRECT*, IDirect3DSurface9*, tagRECT*, _D3DTEXTUREFILTERTYPE, int>)(int)(*(uint*)(*(int*)ptr3 + 136)))((nint)ptr3, ptr2, null, ptr, null, (_D3DTEXTUREFILTERTYPE)2);
			if (num2 >= 0)
			{
				num = num2;
			}
			else
			{
				IDirect3DDevice9* ptr4 = pComPtr;
				int num3 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, IDirect3DSurface9*, tagRECT*, IDirect3DSurface9*, tagRECT*, _D3DTEXTUREFILTERTYPE, int>)(int)(*(uint*)(*(int*)ptr4 + 136)))((nint)ptr4, ptr2, null, ptr, null, (_D3DTEXTUREFILTERTYPE)0);
				num = ((num3 < 0) ? _003CModule_003E.D3DXLoadSurfaceFromSurface(ptr, null, null, ptr2, null, null, uint.MaxValue, 0u) : num3);
			}
		}
		uint levelCount = (uint)renderTarget.LevelCount;
		uint num4 = 1u;
		if (1 < levelCount)
		{
			do
			{
				if (num >= 0)
				{
					IDirect3DSurface9* destinationSurface = renderTargetHelper.GetDestinationSurface(cubeMapFace, (int)num4);
					IDirect3DDevice9* ptr5 = pComPtr;
					int num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, IDirect3DSurface9*, tagRECT*, IDirect3DSurface9*, tagRECT*, _D3DTEXTUREFILTERTYPE, int>)(int)(*(uint*)(*(int*)ptr5 + 136)))((nint)ptr5, ptr, null, destinationSurface, null, (_D3DTEXTUREFILTERTYPE)2);
					if (num5 >= 0)
					{
						num = num5;
					}
					else
					{
						ptr5 = pComPtr;
						num5 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, IDirect3DSurface9*, tagRECT*, IDirect3DSurface9*, tagRECT*, _D3DTEXTUREFILTERTYPE, int>)(int)(*(uint*)(*(int*)ptr5 + 136)))((nint)ptr5, ptr, null, destinationSurface, null, (_D3DTEXTUREFILTERTYPE)0);
						num = ((num5 < 0) ? _003CModule_003E.D3DXLoadSurfaceFromSurface(destinationSurface, null, null, ptr, null, null, uint.MaxValue, 0u) : num5);
					}
					if (ptr != null)
					{
						IDirect3DSurface9* intPtr2 = ptr;
						((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)intPtr2 + 8)))((nint)intPtr2);
					}
					ptr = destinationSurface;
				}
				num4++;
			}
			while (num4 < levelCount);
		}
		if (ptr != null)
		{
			IDirect3DSurface9* intPtr3 = ptr;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)intPtr3 + 8)))((nint)intPtr3);
		}
		if (_insideScene)
		{
			IDirect3DDevice9* intPtr4 = pComPtr;
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr4 + 164)))((nint)intPtr4);
		}
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
	}

	private unsafe void ClearDirtyBuffers()
	{
		Color color = new Color(68, 34, 136, 255);
		int num = currentRenderTargetCount;
		if (num <= 0)
		{
			Clear((ClearOptions)lazyClearFlags, color, 1f, 0);
		}
		else
		{
			if (((uint)lazyClearFlags & (true ? 1u : 0u)) != 0)
			{
				int num2 = 0;
				if (0 < num)
				{
					do
					{
						RenderTargetHelper renderTargetHelper = currentRenderTargets[num2];
						if (renderTargetHelper.usage == RenderTargetUsage.DiscardContents)
						{
							IDirect3DSurface9* renderTargetSurface = renderTargetHelper.GetRenderTargetSurface(currentRenderTargetBindings[num2]._cubeMapFace);
							uint num3 = (uint)(color.A << 8);
							uint num4 = (color.R | num3) << 8;
							uint num5 = (color.G | num4) << 8;
							uint num6 = color.B | num5;
							IDirect3DDevice9* ptr = pComPtr;
							int num7 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, IDirect3DSurface9*, tagRECT*, uint, int>)(int)(*(uint*)(*(int*)ptr + 140)))((nint)ptr, renderTargetSurface, null, num6);
							if (renderTargetSurface != null)
							{
								((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)renderTargetSurface + 8)))((nint)renderTargetSurface);
							}
							if (num7 < 0)
							{
								throw GraphicsHelpers.GetExceptionFromResult((uint)num7);
							}
						}
						num2++;
					}
					while (num2 < currentRenderTargetCount);
				}
			}
			int num8 = lazyClearFlags & 6;
			if (num8 != 0)
			{
				Clear((ClearOptions)num8, color, 1f, 0);
			}
		}
		lazyClearFlags = 0;
	}

	public unsafe void SetRenderTarget(RenderTargetCube renderTarget, CubeMapFace cubeMapFace)
	{
		if (renderTarget != null)
		{
			RenderTargetBinding renderTargetBinding = new RenderTargetBinding(renderTarget, cubeMapFace);
			SetRenderTargets(&renderTargetBinding, 1);
		}
		else
		{
			SetRenderTargets(null, 0);
		}
	}

	public unsafe void SetRenderTarget(RenderTarget2D renderTarget)
	{
		if (renderTarget != null)
		{
			RenderTargetBinding renderTargetBinding = new RenderTargetBinding(renderTarget);
			SetRenderTargets(&renderTargetBinding, 1);
		}
		else
		{
			SetRenderTargets(null, 0);
		}
	}

	public RenderTargetBinding[] GetRenderTargets()
	{
		int num = currentRenderTargetCount;
		RenderTargetBinding[] array = new RenderTargetBinding[num];
		Array.Copy(currentRenderTargetBindings, array, num);
		return array;
	}

	public unsafe void GetBackBufferData<T>(Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct
	{
		IntPtr intPtr = (IntPtr)pComPtr;
		Helpers.CheckDisposed(this, intPtr);
		if (!_profileCapabilities.GetBackBufferData)
		{
			_profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileFeatureNotSupported, "GetBackBufferData");
		}
		if (data != null)
		{
			int num = data.Length;
			if (num != 0)
			{
				Helpers.ValidateCopyParameters(num, startIndex, elementCount);
				if (currentRenderTargetCount > 0)
				{
					throw new InvalidOperationException(FrameworkResources.CannotGetBackBufferActiveRenderTargets);
				}
				if (lazyClearFlags != 0)
				{
					ClearDirtyBuffers();
				}
				IDirect3DSurface9* ptr = null;
				IDirect3DTexture9* ptr2 = null;
				IDirect3DSurface9* ptr3 = null;
				bool flag = false;
				try
				{
					IDirect3DDevice9* ptr4 = pComPtr;
					int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, _D3DBACKBUFFER_TYPE, IDirect3DSurface9**, int>)(int)(*(uint*)(*(int*)ptr4 + 72)))((nint)ptr4, 0u, 0u, (_D3DBACKBUFFER_TYPE)0, &ptr);
					if (num2 >= 0)
					{
						System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DSURFACE_DESC d3DSURFACE_DESC);
						num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DSURFACE_DESC*, int>)(int)(*(uint*)(*(int*)ptr + 48)))((nint)ptr, &d3DSURFACE_DESC);
						if (num2 >= 0)
						{
							System.Runtime.CompilerServices.Unsafe.SkipInit(out uint dwFormatSize);
							System.Runtime.CompilerServices.Unsafe.SkipInit(out uint dwElementSize);
							Texture.GetAndValidateSizes<T>(&d3DSURFACE_DESC, &dwFormatSize, &dwElementSize);
							System.Runtime.CompilerServices.Unsafe.SkipInit(out uint dwLockWidth);
							System.Runtime.CompilerServices.Unsafe.SkipInit(out uint dwLockHeight);
							Texture.GetAndValidateRect(&d3DSURFACE_DESC, &dwLockWidth, &dwLockHeight, rect);
							Texture.ValidateTotalSize(&d3DSURFACE_DESC, dwLockWidth, dwLockHeight, dwFormatSize, dwElementSize, (uint)elementCount);
							if (_insideScene)
							{
								ptr4 = pComPtr;
								IDirect3DDevice9* intPtr2 = ptr4;
								((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr2 + 168)))((nint)intPtr2);
								flag = true;
							}
							ptr4 = pComPtr;
							num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, uint, uint, _D3DFORMAT, _D3DPOOL, IDirect3DTexture9**, void**, int>)(int)(*(uint*)(*(int*)ptr4 + 92)))((nint)ptr4, System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 24)), System.Runtime.CompilerServices.Unsafe.As<_D3DSURFACE_DESC, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DSURFACE_DESC, 28)), 1u, 0u, *(_D3DFORMAT*)(&d3DSURFACE_DESC), (_D3DPOOL)2, &ptr2, null);
							if (num2 >= 0)
							{
								num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, IDirect3DSurface9**, int>)(int)(*(uint*)(*(int*)ptr2 + 72)))((nint)ptr2, 0u, &ptr3);
								if (num2 >= 0)
								{
									num2 = _003CModule_003E.D3DXLoadSurfaceFromSurface(ptr3, null, null, ptr, null, null, uint.MaxValue, 0u);
									if (num2 >= 0)
									{
										tagRECT* ptr5 = null;
										Rectangle rectangle = default(Rectangle);
										if (rect.HasValue)
										{
											rectangle = rect.Value;
											ptr5 = (tagRECT*)(int)(ref rectangle);
											if (ptr5 != null)
											{
												*(int*)((byte*)ptr5 + 8) += *(int*)ptr5;
												*(int*)((byte*)ptr5 + 12) += *(int*)((byte*)ptr5 + 4);
											}
										}
										System.Runtime.CompilerServices.Unsafe.SkipInit(out _D3DLOCKED_RECT d3DLOCKED_RECT);
										num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DLOCKED_RECT*, tagRECT*, uint, int>)(int)(*(uint*)(*(int*)ptr3 + 52)))((nint)ptr3, &d3DLOCKED_RECT, ptr5, 0u);
										if (num2 >= 0)
										{
											try
											{
												Texture.CopyData((void*)(int)System.Runtime.CompilerServices.Unsafe.As<_D3DLOCKED_RECT, uint>(ref System.Runtime.CompilerServices.Unsafe.AddByteOffset(ref d3DLOCKED_RECT, 4)), *(int*)(&d3DLOCKED_RECT), data, startIndex, elementCount, &d3DSURFACE_DESC, dwLockWidth, dwLockHeight, isSetting: false);
											}
											finally
											{
												IDirect3DSurface9* intPtr3 = ptr3;
												((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr3 + 56)))((nint)intPtr3);
											}
											if (pInternalCachedParams.RenderTargetUsage == RenderTargetUsage.DiscardContents)
											{
												lazyClearFlags = (int)DefaultClearOptions;
											}
											if (num2 >= 0)
											{
												return;
											}
										}
									}
								}
							}
						}
					}
					throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
				}
				finally
				{
					if (ptr3 != null)
					{
						IDirect3DSurface9* intPtr4 = ptr3;
						((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)intPtr4 + 8)))((nint)intPtr4);
						ptr3 = null;
					}
					if (ptr2 != null)
					{
						IDirect3DTexture9* intPtr5 = ptr2;
						((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)intPtr5 + 8)))((nint)intPtr5);
						ptr2 = null;
					}
					if (ptr != null)
					{
						IDirect3DSurface9* intPtr6 = ptr;
						((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)intPtr6 + 8)))((nint)intPtr6);
						ptr = null;
					}
					if (flag)
					{
						IDirect3DDevice9* intPtr7 = pComPtr;
						((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, int>)(int)(*(uint*)(*(int*)intPtr7 + 164)))((nint)intPtr7);
					}
				}
			}
		}
		throw new ArgumentNullException("data", FrameworkResources.NullNotAllowed);
	}

	public void GetBackBufferData<T>(T[] data, int startIndex, int elementCount) where T : struct
	{
		GetBackBufferData(null, data, startIndex, elementCount);
	}

	public void GetBackBufferData<T>(T[] data) where T : struct
	{
		int elementCount = ((data != null) ? data.Length : 0);
		GetBackBufferData(null, data, 0, elementCount);
	}

	public VertexBufferBinding[] GetVertexBuffers()
	{
		int num = currentVertexBufferCount;
		VertexBufferBinding[] array = new VertexBufferBinding[num];
		Array.Copy(currentVertexBuffers, array, num);
		return array;
	}

	public unsafe void SetVertexBuffer(VertexBuffer vertexBuffer, int vertexOffset)
	{
		if (vertexBuffer != null)
		{
			VertexBufferBinding vertexBufferBinding = new VertexBufferBinding(vertexBuffer, vertexOffset);
			SetVertexBuffers(&vertexBufferBinding, 1);
		}
		else
		{
			SetVertexBuffers(null, 0);
		}
	}

	public unsafe void SetVertexBuffer(VertexBuffer vertexBuffer)
	{
		if (vertexBuffer != null)
		{
			VertexBufferBinding vertexBufferBinding = new VertexBufferBinding(vertexBuffer);
			SetVertexBuffers(&vertexBufferBinding, 1);
		}
		else
		{
			SetVertexBuffers(null, 0);
		}
	}

	internal unsafe void SetVertexBuffers(VertexBufferBinding* pBindings, int vertexBufferCount)
	{
		IntPtr intPtr = (IntPtr)pComPtr;
		Helpers.CheckDisposed(this, intPtr);
		int maxVertexStreams = _profileCapabilities.MaxVertexStreams;
		if (vertexBufferCount > maxVertexStreams)
		{
			_profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileMaxVertexStreams, maxVertexStreams);
		}
		int i = 0;
		try
		{
			for (i = 0; i < vertexBufferCount; i++)
			{
				VertexBuffer vertexBuffer = ((VertexBufferBinding*)(System.Runtime.CompilerServices.Unsafe.SizeOf<VertexBufferBinding>() * i + (byte*)pBindings))->_vertexBuffer;
				if (vertexBuffer == null)
				{
					throw new ArgumentException(FrameworkResources.NullNotAllowed);
				}
				IntPtr intPtr2 = (IntPtr)vertexBuffer.pComPtr;
				IntPtr intPtr3 = intPtr2;
				Helpers.CheckDisposed(vertexBuffer, intPtr2);
				if (vertexBuffer.GraphicsDevice != this)
				{
					throw new InvalidOperationException(FrameworkResources.InvalidDevice);
				}
				VertexBufferBinding[] array = currentVertexBuffers;
				VertexBuffer vertexBuffer2 = array[i]._vertexBuffer;
				VertexBufferBinding* ptr = (VertexBufferBinding*)((byte*)pBindings + System.Runtime.CompilerServices.Unsafe.SizeOf<VertexBufferBinding>() * i);
				int vertexOffset = ptr->_vertexOffset;
				int instanceFrequency = ptr->_instanceFrequency;
				if (vertexBuffer != vertexBuffer2 || vertexOffset != array[i]._vertexOffset || instanceFrequency != array[i]._instanceFrequency)
				{
					int vertexStride = vertexBuffer._vertexDeclaration._vertexStride;
					IDirect3DDevice9* ptr2 = pComPtr;
					int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, IDirect3DVertexBuffer9*, uint, uint, int>)(int)(*(uint*)(*(int*)ptr2 + 400)))((nint)ptr2, (uint)i, vertexBuffer.pComPtr, (uint)(vertexStride * vertexOffset), (uint)vertexStride);
					if (num < 0)
					{
						throw GraphicsHelpers.GetExceptionFromResult((uint)num);
					}
					ref VertexBufferBinding reference = ref currentVertexBuffers[i];
					reference = System.Runtime.CompilerServices.Unsafe.Read<VertexBufferBinding>(System.Runtime.CompilerServices.Unsafe.SizeOf<VertexBufferBinding>() * i + (byte*)pBindings);
					if (instanceFrequency != 0)
					{
						instanceStreamMask |= 1 << i;
					}
					else
					{
						instanceStreamMask &= ~(1 << i);
					}
				}
			}
		}
		finally
		{
			int num2 = i;
			if (i < currentVertexBufferCount)
			{
				do
				{
					IDirect3DDevice9* ptr2 = pComPtr;
					((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, IDirect3DVertexBuffer9*, uint, uint, int>)(int)(*(uint*)(*(int*)ptr2 + 400)))((nint)ptr2, (uint)num2, null, 0u, 0u);
					currentVertexBuffers[num2] = default(VertexBufferBinding);
					instanceStreamMask &= ~(1 << num2);
					num2++;
				}
				while (num2 < currentVertexBufferCount);
			}
			currentVertexBufferCount = i;
		}
		if (vertexBufferCount > 0)
		{
			vertexDeclarationManager.SetVertexDeclaration(pBindings, vertexBufferCount);
		}
	}

	public unsafe void SetVertexBuffers(params VertexBufferBinding[] vertexBuffers)
	{
		if (vertexBuffers != null && (nint)vertexBuffers.LongLength > 0)
		{
			fixed (VertexBufferBinding* pBindings = &vertexBuffers[0])
			{
				try
				{
					SetVertexBuffers(pBindings, vertexBuffers.Length);
				}
				catch
				{
					//try-fault
					pBindings = null;
					throw;
				}
			}
		}
		else
		{
			SetVertexBuffers(null, 0);
		}
	}

	internal uint GetBufferUsage(uint usage)
	{
		usage = (((_creationFlags & 0x20) == 0) ? (usage & 0xFFFFFFEFu) : (usage | 0x10u));
		return usage;
	}

	private unsafe int CopySurface(IDirect3DSurface9* pSourceSurface, IDirect3DSurface9* pDestinationSurface, [MarshalAs(UnmanagedType.U1)] bool useFilter)
	{
		IDirect3DDevice9* ptr;
		int num;
		if (useFilter)
		{
			ptr = pComPtr;
			num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, IDirect3DSurface9*, tagRECT*, IDirect3DSurface9*, tagRECT*, _D3DTEXTUREFILTERTYPE, int>)(int)(*(uint*)(*(int*)ptr + 136)))((nint)ptr, pSourceSurface, null, pDestinationSurface, null, (_D3DTEXTUREFILTERTYPE)2);
			if (num >= 0)
			{
				return num;
			}
		}
		ptr = pComPtr;
		num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, IDirect3DSurface9*, tagRECT*, IDirect3DSurface9*, tagRECT*, _D3DTEXTUREFILTERTYPE, int>)(int)(*(uint*)(*(int*)ptr + 136)))((nint)ptr, pSourceSurface, null, pDestinationSurface, null, (_D3DTEXTUREFILTERTYPE)0);
		if (num >= 0)
		{
			return num;
		}
		return _003CModule_003E.D3DXLoadSurfaceFromSurface(pDestinationSurface, null, null, pSourceSurface, null, null, uint.MaxValue, 0u);
	}

	private void InitializeDeviceState()
	{
		cachedBlendState = null;
		cachedDepthStencilState = null;
		cachedRasterizerState = null;
		BlendState = BlendState.Opaque;
		DepthStencilState = DepthStencilState.Default;
		RasterizerState = RasterizerState.CullCounterClockwise;
		pSamplerState.InitializeDeviceState();
		pVertexSamplerState.InitializeDeviceState();
		currentViewport = default(Viewport);
		currentViewport.Width = pInternalCachedParams.BackBufferWidth;
		currentViewport.Height = pInternalCachedParams.BackBufferHeight;
		currentViewport.MaxDepth = 1f;
		lazyClearFlags = (int)DefaultClearOptions;
	}

	[SpecialName]
	protected void raise_Disposing(object value0, EventArgs value1)
	{
		_003Cbacking_store_003EDisposing?.Invoke(value0, value1);
	}

	private unsafe void _0021GraphicsDevice()
	{
		if (!isDisposed)
		{
			isDisposed = true;
			pResourceManager?.ReleaseAllDeviceResources();
			vertexDeclarationManager?.ReleaseAllDeclarations();
			IDirect3DSurface9* ptr = pImplicitDepthSurface;
			if (ptr != null)
			{
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)ptr + 8)))((nint)ptr);
				pImplicitDepthSurface = null;
			}
			StateTrackerDevice* ptr2 = pStateTracker;
			if (ptr2 != null)
			{
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)ptr2 + 8)))((nint)ptr2);
				pStateTracker = null;
			}
			IDirect3DDevice9* ptr3 = pComPtr;
			if (ptr3 != null)
			{
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)ptr3 + 8)))((nint)ptr3);
				pComPtr = null;
			}
		}
	}

	private void _007EGraphicsDevice()
	{
		if (!isDisposed)
		{
			_0021GraphicsDevice();
			EventArgs empty = EventArgs.Empty;
			_003Cbacking_store_003EDisposing?.Invoke(this, empty);
		}
	}

	internal unsafe static GraphicsDevice GetManagedObject(IDirect3DDevice9* pInterface, GraphicsDevice pDevice, uint pool)
	{
		GraphicsDevice graphicsDevice = pDevice.pResourceManager.GetCachedObject(pInterface) as GraphicsDevice;
		if (graphicsDevice != null)
		{
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)pInterface + 8)))((nint)pInterface);
			graphicsDevice.isDisposed = false;
			GC.ReRegisterForFinalize(graphicsDevice);
		}
		else
		{
			graphicsDevice = new GraphicsDevice(pInterface, pDevice);
			pDevice.pResourceManager.AddTrackedObject(graphicsDevice, pInterface, pool, 0uL, ref graphicsDevice._internalHandle);
		}
		return graphicsDevice;
	}

	private void OnObjectCreation()
	{
		CreateHelperClasses();
	}

	[SpecialName]
	internal void raise_DrawGuide(object sender, EventArgs e)
	{
		_DrawGuideHandler?.Invoke(sender, e);
	}

	[HandleProcessCorruptedStateExceptions]
	protected virtual void Dispose([MarshalAs(UnmanagedType.U1)] bool P_0)
	{
		if (P_0)
		{
			try
			{
				_007EGraphicsDevice();
				return;
			}
			finally
			{
				((IDisposable)d3dCaps).Dispose();
			}
		}
		try
		{
			_0021GraphicsDevice();
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

	~GraphicsDevice()
	{
		Dispose(false);
	}
}
