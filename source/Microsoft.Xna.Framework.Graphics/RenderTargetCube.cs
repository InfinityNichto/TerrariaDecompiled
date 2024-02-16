using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public class RenderTargetCube : TextureCube, IDynamicGraphicsResource
{
	internal RenderTargetHelper helper;

	internal bool _contentLost;

	private EventHandler<EventArgs> _003Cbacking_store_003EContentLost;

	public bool IsContentLost
	{
		[return: MarshalAs(UnmanagedType.U1)]
		get
		{
			if (!_contentLost)
			{
				_contentLost = _parent.IsDeviceLost;
			}
			return _contentLost;
		}
	}

	public RenderTargetUsage RenderTargetUsage => helper.usage;

	public int MultiSampleCount => helper.multiSampleCount;

	public DepthFormat DepthStencilFormat => helper.depthFormat;

	[SpecialName]
	public virtual event EventHandler<EventArgs> ContentLost
	{
		[MethodImpl(MethodImplOptions.Synchronized)]
		add
		{
			_003Cbacking_store_003EContentLost = (EventHandler<EventArgs>)Delegate.Combine(_003Cbacking_store_003EContentLost, value);
		}
		[MethodImpl(MethodImplOptions.Synchronized)]
		remove
		{
			_003Cbacking_store_003EContentLost = (EventHandler<EventArgs>)Delegate.Remove(_003Cbacking_store_003EContentLost, value);
		}
	}

	public RenderTargetCube(GraphicsDevice graphicsDevice, int size, [MarshalAs(UnmanagedType.U1)] bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage)
	{
		try
		{
			CreateRenderTarget(graphicsDevice, size, mipMap, preferredFormat, preferredDepthFormat, preferredMultiSampleCount, usage);
			return;
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
	}

	public RenderTargetCube(GraphicsDevice graphicsDevice, int size, [MarshalAs(UnmanagedType.U1)] bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat)
	{
		try
		{
			CreateRenderTarget(graphicsDevice, size, mipMap, preferredFormat, preferredDepthFormat, 0, RenderTargetUsage.DiscardContents);
			return;
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
	}

	private void _007ERenderTargetCube()
	{
	}

	internal void CreateRenderTarget(GraphicsDevice graphicsDevice, int size, [MarshalAs(UnmanagedType.U1)] bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage)
	{
		if (graphicsDevice == null)
		{
			throw new ArgumentNullException("graphicsDevice", FrameworkResources.DeviceCannotBeNullOnResourceCreate);
		}
		graphicsDevice.Adapter.QueryFormat(isBackBuffer: false, graphicsDevice._deviceType, graphicsDevice._graphicsProfile, preferredFormat, preferredDepthFormat, preferredMultiSampleCount, out var selectedFormat, out var selectedDepthFormat, out var selectedMultiSampleCount);
		TextureCube.ValidateCreationParameters(graphicsDevice._profileCapabilities, size, selectedFormat);
		(helper = new RenderTargetHelper(this, size, size, selectedFormat, selectedDepthFormat, selectedMultiSampleCount, usage, graphicsDevice._profileCapabilities)).CreateSurfaces(graphicsDevice);
		CreateTexture(graphicsDevice, size, mipMap, 1u, (_D3DPOOL)0, selectedFormat);
	}

	internal override int SaveDataForRecreation()
	{
		return 0;
	}

	int IGraphicsResource.SaveDataForRecreation()
	{
		//ILSpy generated this explicit interface implementation from .override directive in SaveDataForRecreation
		return this.SaveDataForRecreation();
	}

	internal unsafe override int RecreateAndPopulateObject()
	{
		if (pComPtr != null)
		{
			return -2147467259;
		}
		int num = ((_levelCount > 1) ? 1 : 0);
		CreateTexture(_parent, _size, (byte)num != 0, 1u, (_D3DPOOL)0, _format);
		helper.CreateSurfaces(_parent);
		return 0;
	}

	int IGraphicsResource.RecreateAndPopulateObject()
	{
		//ILSpy generated this explicit interface implementation from .override directive in RecreateAndPopulateObject
		return this.RecreateAndPopulateObject();
	}

	internal override void ReleaseNativeObject([MarshalAs(UnmanagedType.U1)] bool disposeManagedResource)
	{
		base.ReleaseNativeObject(disposeManagedResource);
		helper?.ReleaseNativeObject();
	}

	void IGraphicsResource.ReleaseNativeObject([MarshalAs(UnmanagedType.U1)] bool disposeManagedResource)
	{
		//ILSpy generated this explicit interface implementation from .override directive in ReleaseNativeObject
		this.ReleaseNativeObject(disposeManagedResource);
	}

	internal virtual void SetContentLost([MarshalAs(UnmanagedType.U1)] bool isContentLost)
	{
		_contentLost = isContentLost;
		if (isContentLost)
		{
			raise_ContentLost(this, EventArgs.Empty);
		}
	}

	void IDynamicGraphicsResource.SetContentLost([MarshalAs(UnmanagedType.U1)] bool isContentLost)
	{
		//ILSpy generated this explicit interface implementation from .override directive in SetContentLost
		this.SetContentLost(isContentLost);
	}

	[SpecialName]
	protected virtual void raise_ContentLost(object value0, EventArgs value1)
	{
		_003Cbacking_store_003EContentLost?.Invoke(value0, value1);
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
