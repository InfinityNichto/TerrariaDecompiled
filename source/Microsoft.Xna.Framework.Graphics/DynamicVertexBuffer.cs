using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public class DynamicVertexBuffer : VertexBuffer, IGraphicsResource, IDynamicGraphicsResource
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

	public unsafe DynamicVertexBuffer(GraphicsDevice graphicsDevice, Type vertexType, int vertexCount, BufferUsage usage)
	{
		try
		{
			VertexDeclaration vertexDeclaration = VertexDeclaration.FromType(vertexType);
			if (vertexCount <= 0)
			{
				throw new ArgumentOutOfRangeException("vertexCount", FrameworkResources.ResourcesMustBeGreaterThanZeroSize);
			}
			_parent = graphicsDevice;
			CreateBuffer(vertexDeclaration, (uint)vertexCount, _003CModule_003E.ConvertXnaBufferUsageToDx(usage) | 0x200u, (_D3DPOOL)0);
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

	public unsafe DynamicVertexBuffer(GraphicsDevice graphicsDevice, VertexDeclaration vertexDeclaration, int vertexCount, BufferUsage usage)
	{
		try
		{
			if (vertexDeclaration == null)
			{
				throw new ArgumentNullException("vertexDeclaration", FrameworkResources.NullNotAllowed);
			}
			if (vertexCount <= 0)
			{
				throw new ArgumentOutOfRangeException("vertexCount", FrameworkResources.ResourcesMustBeGreaterThanZeroSize);
			}
			_parent = graphicsDevice;
			CreateBuffer(vertexDeclaration, (uint)vertexCount, _003CModule_003E.ConvertXnaBufferUsageToDx(usage) | 0x200u, (_D3DPOOL)0);
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
		SetData(0, data, startIndex, elementCount, 0, options);
	}

	public void SetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride, SetDataOptions options) where T : struct
	{
		CopyData(offsetInBytes, data, startIndex, elementCount, vertexStride, _003CModule_003E.ConvertXnaSetDataOptionsToDx(options), isSetting: true);
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
		if (_size == 0)
		{
			return -2147467259;
		}
		fixed (IDirect3DVertexBuffer9** ptr2 = &pComPtr)
		{
			GraphicsDevice parent = _parent;
			IDirect3DDevice9* ptr = parent.pComPtr;
			GraphicsDevice graphicsDevice = parent;
			int num = *(int*)ptr + 104;
			int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, uint, uint, _D3DPOOL, IDirect3DVertexBuffer9**, void**, int>)(int)(*(uint*)num))((nint)ptr, _size, graphicsDevice.GetBufferUsage(_usage) | 0x200u, 0u, (_D3DPOOL)0, ptr2, null);
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
