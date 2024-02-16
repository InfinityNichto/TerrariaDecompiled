using System;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public class BlendState : GraphicsResource
{
	internal Blend cachedColorSourceBlend;

	internal Blend cachedColorDestinationBlend;

	internal BlendFunction cachedColorBlendFunction;

	internal Blend cachedAlphaSourceBlend;

	internal Blend cachedAlphaDestinationBlend;

	internal BlendFunction cachedAlphaBlendFunction;

	internal ColorWriteChannels cachedColorWriteChannels;

	internal ColorWriteChannels cachedColorWriteChannels1;

	internal ColorWriteChannels cachedColorWriteChannels2;

	internal ColorWriteChannels cachedColorWriteChannels3;

	internal Color cachedBlendFactor;

	internal int cachedMultiSampleMask;

	public static readonly BlendState Opaque = new BlendState(Blend.One, Blend.Zero, "BlendState.Opaque");

	public static readonly BlendState AlphaBlend = new BlendState(Blend.One, Blend.InverseSourceAlpha, "BlendState.AlphaBlend");

	public static readonly BlendState Additive = new BlendState(Blend.SourceAlpha, Blend.One, "BlendState.Additive");

	public static readonly BlendState NonPremultiplied = new BlendState(Blend.SourceAlpha, Blend.InverseSourceAlpha, "BlendState.NonPremultiplied");

	internal bool isBound;

	internal bool blendEnable;

	internal bool separateAlphaBlend;

	internal _D3DBLEND d3dColorSourceBlend;

	internal _D3DBLEND d3dColorDestinationBlend;

	internal _D3DBLENDOP d3dColorBlendFunction;

	internal _D3DBLEND d3dAlphaSourceBlend;

	internal _D3DBLEND d3dAlphaDestinationBlend;

	internal _D3DBLENDOP d3dAlphaBlendFunction;

	internal uint d3dBlendFactor;

	internal uint stateTrackerFlags;

	public int MultiSampleMask
	{
		get
		{
			return cachedMultiSampleMask;
		}
		set
		{
			ThrowIfBound();
			cachedMultiSampleMask = value;
		}
	}

	public Color BlendFactor
	{
		get
		{
			return cachedBlendFactor;
		}
		set
		{
			ThrowIfBound();
			cachedBlendFactor = value;
		}
	}

	public ColorWriteChannels ColorWriteChannels3
	{
		get
		{
			return cachedColorWriteChannels3;
		}
		set
		{
			ThrowIfBound();
			cachedColorWriteChannels3 = value;
		}
	}

	public ColorWriteChannels ColorWriteChannels2
	{
		get
		{
			return cachedColorWriteChannels2;
		}
		set
		{
			ThrowIfBound();
			cachedColorWriteChannels2 = value;
		}
	}

	public ColorWriteChannels ColorWriteChannels1
	{
		get
		{
			return cachedColorWriteChannels1;
		}
		set
		{
			ThrowIfBound();
			cachedColorWriteChannels1 = value;
		}
	}

	public ColorWriteChannels ColorWriteChannels
	{
		get
		{
			return cachedColorWriteChannels;
		}
		set
		{
			ThrowIfBound();
			cachedColorWriteChannels = value;
		}
	}

	public BlendFunction AlphaBlendFunction
	{
		get
		{
			return cachedAlphaBlendFunction;
		}
		set
		{
			ThrowIfBound();
			cachedAlphaBlendFunction = value;
		}
	}

	public Blend AlphaDestinationBlend
	{
		get
		{
			return cachedAlphaDestinationBlend;
		}
		set
		{
			ThrowIfBound();
			cachedAlphaDestinationBlend = value;
		}
	}

	public Blend AlphaSourceBlend
	{
		get
		{
			return cachedAlphaSourceBlend;
		}
		set
		{
			ThrowIfBound();
			cachedAlphaSourceBlend = value;
		}
	}

	public BlendFunction ColorBlendFunction
	{
		get
		{
			return cachedColorBlendFunction;
		}
		set
		{
			ThrowIfBound();
			cachedColorBlendFunction = value;
		}
	}

	public Blend ColorDestinationBlend
	{
		get
		{
			return cachedColorDestinationBlend;
		}
		set
		{
			ThrowIfBound();
			cachedColorDestinationBlend = value;
		}
	}

	public Blend ColorSourceBlend
	{
		get
		{
			return cachedColorSourceBlend;
		}
		set
		{
			ThrowIfBound();
			cachedColorSourceBlend = value;
		}
	}

	private void SetDefaults()
	{
		ThrowIfBound();
		cachedColorSourceBlend = Blend.One;
		ThrowIfBound();
		cachedColorDestinationBlend = Blend.Zero;
		ThrowIfBound();
		cachedColorBlendFunction = BlendFunction.Add;
		ThrowIfBound();
		cachedAlphaSourceBlend = Blend.One;
		ThrowIfBound();
		cachedAlphaDestinationBlend = Blend.Zero;
		ThrowIfBound();
		cachedAlphaBlendFunction = BlendFunction.Add;
		ThrowIfBound();
		cachedColorWriteChannels = ColorWriteChannels.All;
		ThrowIfBound();
		cachedColorWriteChannels1 = ColorWriteChannels.All;
		ThrowIfBound();
		cachedColorWriteChannels2 = ColorWriteChannels.All;
		ThrowIfBound();
		cachedColorWriteChannels3 = ColorWriteChannels.All;
		Color white = Color.White;
		ThrowIfBound();
		cachedBlendFactor = white;
		ThrowIfBound();
		cachedMultiSampleMask = -1;
	}

	public BlendState()
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

	private BlendState(Blend sourceBlend, Blend destinationBlend, string name)
	{
		try
		{
			SetDefaults();
			ThrowIfBound();
			cachedColorSourceBlend = sourceBlend;
			ThrowIfBound();
			cachedColorDestinationBlend = destinationBlend;
			ThrowIfBound();
			cachedAlphaSourceBlend = sourceBlend;
			ThrowIfBound();
			cachedAlphaDestinationBlend = destinationBlend;
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

	private void _007EBlendState()
	{
	}

	internal unsafe void Apply(GraphicsDevice device)
	{
		if (isDisposed)
		{
			throw new ObjectDisposedException(typeof(BlendState).Name);
		}
		if (_parent != device)
		{
			ProfileCapabilities profileCapabilities = device._profileCapabilities;
			if (!profileCapabilities.SeparateAlphaBlend)
			{
				Blend alphaBlend = cachedAlphaSourceBlend;
				Blend colorBlend = cachedColorSourceBlend;
				if (GraphicsHelpers.IsSeparateBlend(colorBlend, alphaBlend))
				{
					profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileNoSeparateAlphaBlend, "ColorSourceBlend", "AlphaSourceBlend");
				}
				Blend alphaBlend2 = cachedAlphaDestinationBlend;
				Blend colorBlend2 = cachedColorDestinationBlend;
				if (GraphicsHelpers.IsSeparateBlend(colorBlend2, alphaBlend2))
				{
					profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileNoSeparateAlphaBlend, "ColorDestinationBlend", "AlphaDestinationBlend");
				}
				if (cachedColorBlendFunction != cachedAlphaBlendFunction)
				{
					profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileNoSeparateAlphaBlend, "ColorBlendFunction", "AlphaBlendFunction");
				}
			}
			if (!profileCapabilities.DestBlendSrcAlphaSat)
			{
				if (cachedColorDestinationBlend == Blend.SourceAlphaSaturation)
				{
					profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileFeatureNotSupported, "ColorDestinationBlend = Blend.SourceAlphaSaturation");
				}
				if (cachedAlphaDestinationBlend == Blend.SourceAlphaSaturation)
				{
					profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileFeatureNotSupported, "AlphaDestinationBlend = Blend.SourceAlphaSaturation");
				}
			}
			if (!profileCapabilities.MinMaxSrcDestBlend)
			{
				BlendFunction blendFunction = cachedColorBlendFunction;
				if ((blendFunction == BlendFunction.Min || blendFunction == BlendFunction.Max) && (cachedColorSourceBlend != 0 || cachedColorDestinationBlend != 0))
				{
					profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileNoMinMaxSrcDestBlend, "Color");
				}
				BlendFunction blendFunction2 = cachedAlphaBlendFunction;
				if ((blendFunction2 == BlendFunction.Min || blendFunction2 == BlendFunction.Max) && (cachedAlphaSourceBlend != 0 || cachedAlphaDestinationBlend != 0))
				{
					profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileNoMinMaxSrcDestBlend, "Alpha");
				}
			}
			_parent = device;
			isBound = true;
			Blend blend = cachedColorSourceBlend;
			int num = ((blend != 0 || cachedColorDestinationBlend != Blend.Zero || cachedColorBlendFunction != 0 || cachedAlphaSourceBlend != 0 || cachedAlphaDestinationBlend != Blend.Zero || cachedAlphaBlendFunction != 0) ? 1 : 0);
			blendEnable = (byte)num != 0;
			int num2 = ((GraphicsHelpers.IsSeparateBlend(blend, cachedAlphaSourceBlend) || GraphicsHelpers.IsSeparateBlend(cachedColorDestinationBlend, cachedAlphaDestinationBlend) || cachedColorBlendFunction != cachedAlphaBlendFunction) ? 1 : 0);
			separateAlphaBlend = (byte)num2 != 0;
			d3dColorSourceBlend = _003CModule_003E.ConvertXnaBlendToDx(cachedColorSourceBlend);
			d3dColorDestinationBlend = _003CModule_003E.ConvertXnaBlendToDx(cachedColorDestinationBlend);
			d3dColorBlendFunction = _003CModule_003E.ConvertXnaBlendOpToDx(cachedColorBlendFunction);
			d3dAlphaSourceBlend = _003CModule_003E.ConvertXnaBlendToDx(GraphicsHelpers.AdjustAlphaBlend(cachedAlphaSourceBlend));
			d3dAlphaDestinationBlend = _003CModule_003E.ConvertXnaBlendToDx(GraphicsHelpers.AdjustAlphaBlend(cachedAlphaDestinationBlend));
			d3dAlphaBlendFunction = _003CModule_003E.ConvertXnaBlendOpToDx(cachedAlphaBlendFunction);
			uint num3 = (uint)(cachedBlendFactor.A << 8);
			uint num4 = (cachedBlendFactor.R | num3) << 8;
			uint num5 = (cachedBlendFactor.G | num4) << 8;
			d3dBlendFactor = cachedBlendFactor.B | num5;
			stateTrackerFlags = 0u;
			if (blendEnable)
			{
				stateTrackerFlags = 1u;
			}
			if (cachedColorWriteChannels != ColorWriteChannels.All)
			{
				stateTrackerFlags |= 2u;
			}
			if (cachedColorWriteChannels1 != ColorWriteChannels.All)
			{
				stateTrackerFlags |= 4u;
			}
			if (cachedColorWriteChannels2 != ColorWriteChannels.All)
			{
				stateTrackerFlags |= 8u;
			}
			if (cachedColorWriteChannels3 != ColorWriteChannels.All)
			{
				stateTrackerFlags |= 16u;
			}
		}
		IntPtr pComPtr = (IntPtr)device.pComPtr;
		Helpers.CheckDisposed(device, pComPtr);
		IDirect3DDevice9* pComPtr2 = device.pComPtr;
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)27, blendEnable ? 1u : 0u);
		if (blendEnable)
		{
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)19, (uint)d3dColorSourceBlend);
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)20, (uint)d3dColorDestinationBlend);
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)171, (uint)d3dColorBlendFunction);
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)206, separateAlphaBlend ? 1u : 0u);
			if (separateAlphaBlend)
			{
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)207, (uint)d3dAlphaSourceBlend);
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)208, (uint)d3dAlphaDestinationBlend);
				((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)209, (uint)d3dAlphaBlendFunction);
			}
		}
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)168, (uint)cachedColorWriteChannels);
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)190, (uint)cachedColorWriteChannels1);
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)191, (uint)cachedColorWriteChannels2);
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)192, (uint)cachedColorWriteChannels3);
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)193, d3dBlendFactor);
		((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DRENDERSTATETYPE, uint, int>)(int)(*(uint*)(*(int*)pComPtr2 + 228)))((nint)pComPtr2, (_D3DRENDERSTATETYPE)162, (uint)cachedMultiSampleMask);
		*(uint*)((byte*)device.pStateTracker + 112) = stateTrackerFlags;
	}

	internal void ThrowIfBound()
	{
		if (isBound)
		{
			throw new InvalidOperationException(string.Format(args: new object[1] { typeof(BlendState).Name }, provider: CultureInfo.CurrentCulture, format: FrameworkResources.BoundStateObject));
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
