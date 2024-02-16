using System;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public class RasterizerState : GraphicsResource
{
	internal CullMode cachedCullMode;

	internal FillMode cachedFillMode;

	internal bool cachedScissorTestEnable;

	internal bool cachedMultiSampleAntiAlias;

	internal float cachedDepthBias;

	internal float cachedSlopeScaleDepthBias;

	public static readonly RasterizerState CullNone = new RasterizerState(CullMode.None, "RasterizerState.CullNone");

	public static readonly RasterizerState CullClockwise = new RasterizerState(CullMode.CullClockwiseFace, "RasterizerState.CullClockwise");

	public static readonly RasterizerState CullCounterClockwise = new RasterizerState(CullMode.CullCounterClockwiseFace, "RasterizerState.CullCounterClockwise");

	internal bool isBound;

	internal _D3DCULL d3dCullMode;

	internal _D3DFILLMODE d3dFillMode;

	public float SlopeScaleDepthBias
	{
		get
		{
			return cachedSlopeScaleDepthBias;
		}
		set
		{
			ThrowIfBound();
			cachedSlopeScaleDepthBias = value;
		}
	}

	public float DepthBias
	{
		get
		{
			return cachedDepthBias;
		}
		set
		{
			ThrowIfBound();
			cachedDepthBias = value;
		}
	}

	public bool MultiSampleAntiAlias
	{
		[return: MarshalAs(UnmanagedType.U1)]
		get
		{
			return cachedMultiSampleAntiAlias;
		}
		[param: MarshalAs(UnmanagedType.U1)]
		set
		{
			ThrowIfBound();
			cachedMultiSampleAntiAlias = value;
		}
	}

	public bool ScissorTestEnable
	{
		[return: MarshalAs(UnmanagedType.U1)]
		get
		{
			return cachedScissorTestEnable;
		}
		[param: MarshalAs(UnmanagedType.U1)]
		set
		{
			ThrowIfBound();
			cachedScissorTestEnable = value;
		}
	}

	public FillMode FillMode
	{
		get
		{
			return cachedFillMode;
		}
		set
		{
			ThrowIfBound();
			cachedFillMode = value;
		}
	}

	public CullMode CullMode
	{
		get
		{
			return cachedCullMode;
		}
		set
		{
			ThrowIfBound();
			cachedCullMode = value;
		}
	}

	private void SetDefaults()
	{
		ThrowIfBound();
		cachedCullMode = CullMode.CullCounterClockwiseFace;
		ThrowIfBound();
		cachedFillMode = FillMode.Solid;
		ThrowIfBound();
		cachedScissorTestEnable = false;
		ThrowIfBound();
		cachedMultiSampleAntiAlias = true;
		ThrowIfBound();
		cachedDepthBias = 0f;
		ThrowIfBound();
		cachedSlopeScaleDepthBias = 0f;
	}

	public RasterizerState()
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

	private RasterizerState(CullMode cullMode, string name)
	{
		try
		{
			SetDefaults();
			ThrowIfBound();
			cachedCullMode = cullMode;
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

	private void _007ERasterizerState()
	{
	}

	internal unsafe void Apply(GraphicsDevice device)
	{
		if (isDisposed)
		{
			throw new ObjectDisposedException(typeof(RasterizerState).Name);
		}
		if (_parent != device)
		{
			_parent = device;
			isBound = true;
			d3dCullMode = _003CModule_003E.ConvertXnaCullModeToDx(cachedCullMode);
			d3dFillMode = _003CModule_003E.ConvertXnaFillModeToDx(cachedFillMode);
		}
		IntPtr pComPtr = (IntPtr)device.pComPtr;
		Helpers.CheckDisposed(device, pComPtr);
		IDirect3DDevice9* pComPtr2 = device.pComPtr;
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)22, (uint)d3dCullMode);
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)8, (uint)d3dFillMode);
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)174, cachedScissorTestEnable ? 1u : 0u);
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)161, cachedMultiSampleAntiAlias ? 1u : 0u);
		float num = cachedDepthBias;
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)195, *(uint*)(&num));
		float num2 = cachedSlopeScaleDepthBias;
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)175, *(uint*)(&num2));
	}

	internal void ThrowIfBound()
	{
		if (isBound)
		{
			throw new InvalidOperationException(string.Format(args: new object[1] { typeof(RasterizerState).Name }, provider: CultureInfo.CurrentCulture, format: FrameworkResources.BoundStateObject));
		}
	}

	[HandleProcessCorruptedStateExceptions]
	protected override void Dispose([MarshalAs(UnmanagedType.U1)] bool P_0)
	{
		if (P_0)
		{
			try
			{
				_007ERasterizerState();
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
