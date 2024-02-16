using System;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public class DepthStencilState : GraphicsResource
{
	internal bool cachedDepthBufferEnable;

	internal bool cachedDepthBufferWriteEnable;

	internal CompareFunction cachedDepthBufferFunction;

	internal bool cachedStencilEnable;

	internal CompareFunction cachedStencilFunction;

	internal StencilOperation cachedStencilPass;

	internal StencilOperation cachedStencilFail;

	internal StencilOperation cachedStencilDepthBufferFail;

	internal bool cachedTwoSidedStencilMode;

	internal CompareFunction cachedCounterClockwiseStencilFunction;

	internal StencilOperation cachedCounterClockwiseStencilPass;

	internal StencilOperation cachedCounterClockwiseStencilFail;

	internal StencilOperation cachedCounterClockwiseStencilDepthBufferFail;

	internal int cachedStencilMask;

	internal int cachedStencilWriteMask;

	internal int cachedReferenceStencil;

	public static readonly DepthStencilState None = new DepthStencilState(depthEnable: false, depthWriteEnable: false, "DepthStencilState.None");

	public static readonly DepthStencilState Default = new DepthStencilState(depthEnable: true, depthWriteEnable: true, "DepthStencilState.Default");

	public static readonly DepthStencilState DepthRead = new DepthStencilState(depthEnable: true, depthWriteEnable: false, "DepthStencilState.DepthRead");

	internal bool isBound;

	internal _D3DCMPFUNC d3dDepthBufferFunction;

	internal _D3DCMPFUNC d3dStencilFunction;

	internal _D3DSTENCILOP d3dStencilPass;

	internal _D3DSTENCILOP d3dStencilFail;

	internal _D3DSTENCILOP d3dStencilDepthBufferFail;

	internal _D3DCMPFUNC d3dCounterClockwiseStencilFunction;

	internal _D3DSTENCILOP d3dCounterClockwiseStencilPass;

	internal _D3DSTENCILOP d3dCounterClockwiseStencilFail;

	internal _D3DSTENCILOP d3dCounterClockwiseStencilDepthBufferFail;

	public int ReferenceStencil
	{
		get
		{
			return cachedReferenceStencil;
		}
		set
		{
			ThrowIfBound();
			cachedReferenceStencil = value;
		}
	}

	public int StencilWriteMask
	{
		get
		{
			return cachedStencilWriteMask;
		}
		set
		{
			ThrowIfBound();
			cachedStencilWriteMask = value;
		}
	}

	public int StencilMask
	{
		get
		{
			return cachedStencilMask;
		}
		set
		{
			ThrowIfBound();
			cachedStencilMask = value;
		}
	}

	public StencilOperation CounterClockwiseStencilDepthBufferFail
	{
		get
		{
			return cachedCounterClockwiseStencilDepthBufferFail;
		}
		set
		{
			ThrowIfBound();
			cachedCounterClockwiseStencilDepthBufferFail = value;
		}
	}

	public StencilOperation CounterClockwiseStencilFail
	{
		get
		{
			return cachedCounterClockwiseStencilFail;
		}
		set
		{
			ThrowIfBound();
			cachedCounterClockwiseStencilFail = value;
		}
	}

	public StencilOperation CounterClockwiseStencilPass
	{
		get
		{
			return cachedCounterClockwiseStencilPass;
		}
		set
		{
			ThrowIfBound();
			cachedCounterClockwiseStencilPass = value;
		}
	}

	public CompareFunction CounterClockwiseStencilFunction
	{
		get
		{
			return cachedCounterClockwiseStencilFunction;
		}
		set
		{
			ThrowIfBound();
			cachedCounterClockwiseStencilFunction = value;
		}
	}

	public bool TwoSidedStencilMode
	{
		[return: MarshalAs(UnmanagedType.U1)]
		get
		{
			return cachedTwoSidedStencilMode;
		}
		[param: MarshalAs(UnmanagedType.U1)]
		set
		{
			ThrowIfBound();
			cachedTwoSidedStencilMode = value;
		}
	}

	public StencilOperation StencilDepthBufferFail
	{
		get
		{
			return cachedStencilDepthBufferFail;
		}
		set
		{
			ThrowIfBound();
			cachedStencilDepthBufferFail = value;
		}
	}

	public StencilOperation StencilFail
	{
		get
		{
			return cachedStencilFail;
		}
		set
		{
			ThrowIfBound();
			cachedStencilFail = value;
		}
	}

	public StencilOperation StencilPass
	{
		get
		{
			return cachedStencilPass;
		}
		set
		{
			ThrowIfBound();
			cachedStencilPass = value;
		}
	}

	public CompareFunction StencilFunction
	{
		get
		{
			return cachedStencilFunction;
		}
		set
		{
			ThrowIfBound();
			cachedStencilFunction = value;
		}
	}

	public bool StencilEnable
	{
		[return: MarshalAs(UnmanagedType.U1)]
		get
		{
			return cachedStencilEnable;
		}
		[param: MarshalAs(UnmanagedType.U1)]
		set
		{
			ThrowIfBound();
			cachedStencilEnable = value;
		}
	}

	public CompareFunction DepthBufferFunction
	{
		get
		{
			return cachedDepthBufferFunction;
		}
		set
		{
			ThrowIfBound();
			cachedDepthBufferFunction = value;
		}
	}

	public bool DepthBufferWriteEnable
	{
		[return: MarshalAs(UnmanagedType.U1)]
		get
		{
			return cachedDepthBufferWriteEnable;
		}
		[param: MarshalAs(UnmanagedType.U1)]
		set
		{
			ThrowIfBound();
			cachedDepthBufferWriteEnable = value;
		}
	}

	public bool DepthBufferEnable
	{
		[return: MarshalAs(UnmanagedType.U1)]
		get
		{
			return cachedDepthBufferEnable;
		}
		[param: MarshalAs(UnmanagedType.U1)]
		set
		{
			ThrowIfBound();
			cachedDepthBufferEnable = value;
		}
	}

	private void SetDefaults()
	{
		ThrowIfBound();
		cachedDepthBufferEnable = true;
		ThrowIfBound();
		cachedDepthBufferWriteEnable = true;
		ThrowIfBound();
		cachedDepthBufferFunction = CompareFunction.LessEqual;
		ThrowIfBound();
		cachedStencilEnable = false;
		ThrowIfBound();
		cachedStencilFunction = CompareFunction.Always;
		ThrowIfBound();
		cachedStencilPass = StencilOperation.Keep;
		ThrowIfBound();
		cachedStencilFail = StencilOperation.Keep;
		ThrowIfBound();
		cachedStencilDepthBufferFail = StencilOperation.Keep;
		ThrowIfBound();
		cachedTwoSidedStencilMode = false;
		ThrowIfBound();
		cachedCounterClockwiseStencilFunction = CompareFunction.Always;
		ThrowIfBound();
		cachedCounterClockwiseStencilPass = StencilOperation.Keep;
		ThrowIfBound();
		cachedCounterClockwiseStencilFail = StencilOperation.Keep;
		ThrowIfBound();
		cachedCounterClockwiseStencilDepthBufferFail = StencilOperation.Keep;
		ThrowIfBound();
		cachedStencilMask = -1;
		ThrowIfBound();
		cachedStencilWriteMask = -1;
		ThrowIfBound();
		cachedReferenceStencil = 0;
	}

	public DepthStencilState()
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

	private DepthStencilState([MarshalAs(UnmanagedType.U1)] bool depthEnable, [MarshalAs(UnmanagedType.U1)] bool depthWriteEnable, string name)
	{
		try
		{
			SetDefaults();
			ThrowIfBound();
			cachedDepthBufferEnable = depthEnable;
			ThrowIfBound();
			cachedDepthBufferWriteEnable = depthWriteEnable;
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

	private void _007EDepthStencilState()
	{
	}

	internal unsafe void Apply(GraphicsDevice device)
	{
		if (isDisposed)
		{
			throw new ObjectDisposedException(typeof(DepthStencilState).Name);
		}
		if (_parent != device)
		{
			_parent = device;
			isBound = true;
			d3dDepthBufferFunction = _003CModule_003E.ConvertXnaComparisonToDx(cachedDepthBufferFunction);
			d3dStencilFunction = _003CModule_003E.ConvertXnaComparisonToDx(cachedStencilFunction);
			d3dStencilPass = _003CModule_003E.ConvertXnaStencilToDx(cachedStencilPass);
			d3dStencilFail = _003CModule_003E.ConvertXnaStencilToDx(cachedStencilFail);
			d3dStencilDepthBufferFail = _003CModule_003E.ConvertXnaStencilToDx(cachedStencilDepthBufferFail);
			d3dCounterClockwiseStencilFunction = _003CModule_003E.ConvertXnaComparisonToDx(cachedCounterClockwiseStencilFunction);
			d3dCounterClockwiseStencilPass = _003CModule_003E.ConvertXnaStencilToDx(cachedCounterClockwiseStencilPass);
			d3dCounterClockwiseStencilFail = _003CModule_003E.ConvertXnaStencilToDx(cachedCounterClockwiseStencilFail);
			d3dCounterClockwiseStencilDepthBufferFail = _003CModule_003E.ConvertXnaStencilToDx(cachedCounterClockwiseStencilDepthBufferFail);
		}
		IntPtr pComPtr = (IntPtr)device.pComPtr;
		Helpers.CheckDisposed(device, pComPtr);
		IDirect3DDevice9* pComPtr2 = device.pComPtr;
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)7, cachedDepthBufferEnable ? 1u : 0u);
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)14, cachedDepthBufferWriteEnable ? 1u : 0u);
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)23, (uint)d3dDepthBufferFunction);
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)52, cachedStencilEnable ? 1u : 0u);
		if (cachedStencilEnable)
		{
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)56, (uint)d3dStencilFunction);
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)55, (uint)d3dStencilPass);
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)53, (uint)d3dStencilFail);
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)54, (uint)d3dStencilDepthBufferFail);
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)185, cachedTwoSidedStencilMode ? 1u : 0u);
			if (cachedTwoSidedStencilMode)
			{
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)189, (uint)d3dCounterClockwiseStencilFunction);
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)188, (uint)d3dCounterClockwiseStencilPass);
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)186, (uint)d3dCounterClockwiseStencilFail);
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)187, (uint)d3dCounterClockwiseStencilDepthBufferFail);
			}
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)58, (uint)cachedStencilMask);
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)59, (uint)cachedStencilWriteMask);
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)57, (uint)cachedReferenceStencil);
		}
	}

	internal void ThrowIfBound()
	{
		if (isBound)
		{
			throw new InvalidOperationException(string.Format(args: new object[1] { typeof(DepthStencilState).Name }, provider: CultureInfo.CurrentCulture, format: FrameworkResources.BoundStateObject));
		}
	}

	[HandleProcessCorruptedStateExceptions]
	protected override void Dispose([MarshalAs(UnmanagedType.U1)] bool P_0)
	{
		if (P_0)
		{
			try
			{
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
