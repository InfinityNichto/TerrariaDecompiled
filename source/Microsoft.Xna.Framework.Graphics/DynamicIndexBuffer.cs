using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public class DynamicIndexBuffer : IndexBuffer, IGraphicsResource, IDynamicGraphicsResource
{
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

	public unsafe DynamicIndexBuffer(GraphicsDevice graphicsDevice, Type indexType, int indexCount, BufferUsage usage)
	{
		try
		{
			if (indexCount <= 0)
			{
				throw new ArgumentOutOfRangeException("indexCount", FrameworkResources.ResourcesMustBeGreaterThanZeroSize);
			}
			_parent = graphicsDevice;
			uint indexSize = (uint)Marshal.SizeOf(indexType);
			CreateBuffer((uint)indexCount, indexSize, _003CModule_003E.ConvertXnaBufferUsageToDx(usage) | 0x200u, (_D3DPOOL)0);
			graphicsDevice.Resources.AddTrackedObject(this, pComPtr, 0u, _internalHandle, ref _internalHandle);
			return;
		}
		catch
		{
			//try-fault
			Dispose(true);
			throw;
		}
	}

	public unsafe DynamicIndexBuffer(GraphicsDevice graphicsDevice, IndexElementSize indexElementSize, int indexCount, BufferUsage usage)
	{
		try
		{
			if (indexCount <= 0)
			{
				throw new ArgumentOutOfRangeException("indexCount", FrameworkResources.ResourcesMustBeGreaterThanZeroSize);
			}
			_parent = graphicsDevice;
			int indexSize = ((indexElementSize == IndexElementSize.SixteenBits) ? 2 : 4);
			CreateBuffer((uint)indexCount, (uint)indexSize, _003CModule_003E.ConvertXnaBufferUsageToDx(usage) | 0x200u, (_D3DPOOL)0);
			graphicsDevice.Resources.AddTrackedObject(this, pComPtr, 0u, _internalHandle, ref _internalHandle);
			return;
		}
		catch
		{
			//try-fault
			Dispose(true);
			throw;
		}
	}

	public void SetData<T>(T[] data, int startIndex, int elementCount, SetDataOptions options) where T : struct
	{
		SetData(0, data, startIndex, elementCount, options);
	}

	public void SetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, SetDataOptions options) where T : struct
	{
		CopyData(offsetInBytes, data, startIndex, elementCount, _003CModule_003E.ConvertXnaSetDataOptionsToDx(options), isSetting: true);
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
		if (_bufferSize == 0)
		{
			return -2147467259;
		}
		fixed (IDirect3DIndexBuffer9** ptr2 = &pComPtr)
		{
			_D3DFORMAT d3DFORMAT = ((_indexSize == 4) ? ((_D3DFORMAT)102) : ((_D3DFORMAT)101));
			GraphicsDevice parent = _parent;
			IDirect3DDevice9* ptr = parent.pComPtr;
			GraphicsDevice graphicsDevice = parent;
			int num = *(int*)ptr + 108;
			int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, _D3DFORMAT, _D3DPOOL, IDirect3DIndexBuffer9**, void**, int>)(int)(*(uint*)num))((nint)ptr, _bufferSize, graphicsDevice.GetBufferUsage(_usage) | 0x200u, d3DFORMAT, (_D3DPOOL)0, ptr2, null);
			if (num2 >= 0)
			{
				_parent.Resources.AddTrackedObject(this, pComPtr, 0u, _internalHandle, ref _internalHandle);
			}
			return num2;
		}
	}

	int IGraphicsResource.RecreateAndPopulateObject()
	{
		//ILSpy generated this explicit interface implementation from .override directive in RecreateAndPopulateObject
		return this.RecreateAndPopulateObject();
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
}
