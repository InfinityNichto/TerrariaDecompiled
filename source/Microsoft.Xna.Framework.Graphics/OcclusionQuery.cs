using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics;

public class OcclusionQuery : GraphicsResource, IGraphicsResource
{
	private int _pixelCount;

	private bool _isAvailable;

	private bool _isInBeginEndPair;

	private bool _hasCalledBegin;

	private bool _hasIsCompleteBeenQueried;

	internal unsafe IDirect3DQuery9* pComPtr;

	public int PixelCount
	{
		get
		{
			if (!IsComplete)
			{
				throw new InvalidOperationException(FrameworkResources.DataNotAvailable);
			}
			return _pixelCount;
		}
	}

	public unsafe bool IsComplete
	{
		[return: MarshalAs(UnmanagedType.U1)]
		get
		{
			_hasIsCompleteBeenQueried = true;
			IDirect3DQuery9* ptr = pComPtr;
			if (ptr == null)
			{
				return false;
			}
			if (!_hasCalledBegin)
			{
				return false;
			}
			System.Runtime.CompilerServices.Unsafe.SkipInit(out uint pixelCount);
			byte b = ((((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, void*, uint, uint, int>)(int)(*(uint*)(*(int*)ptr + 28)))((nint)ptr, &pixelCount, 4u, 1u) == 0) ? ((byte)1) : ((byte)0));
			_isAvailable = b != 0;
			if (b != 0)
			{
				_pixelCount = (int)pixelCount;
			}
			return b != 0;
		}
	}

	private unsafe OcclusionQuery(IDirect3DQuery9* pInterface, GraphicsDevice pDevice)
	{
		pComPtr = pInterface;
		((object)this)._002Ector();
		try
		{
			_parent = pDevice;
			return;
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
	}

	public unsafe OcclusionQuery(GraphicsDevice graphicsDevice)
	{
		try
		{
			if (graphicsDevice == null)
			{
				throw new ArgumentNullException("graphicsDevice", FrameworkResources.DeviceCannotBeNullOnResourceCreate);
			}
			if (!graphicsDevice._profileCapabilities.OcclusionQuery)
			{
				graphicsDevice._profileCapabilities.ThrowNotSupportedException(FrameworkResources.ProfileFeatureNotSupported, typeof(OcclusionQuery).Name);
			}
			_hasIsCompleteBeenQueried = true;
			fixed (IDirect3DQuery9** ptr = &pComPtr)
			{
				int num = *(int*)graphicsDevice.pComPtr + 472;
				int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DQUERYTYPE, IDirect3DQuery9**, int>)(int)(*(uint*)num))((nint)graphicsDevice.pComPtr, (_D3DQUERYTYPE)9, ptr);
				if (num2 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
				}
				_parent = graphicsDevice;
				graphicsDevice.Resources.AddTrackedObject(this, pComPtr, 0u, _internalHandle, ref _internalHandle);
				return;
			}
		}
		catch
		{
			//try-fault
			base.Dispose(true);
			throw;
		}
	}

	public unsafe void Begin()
	{
		if (_isInBeginEndPair)
		{
			throw new InvalidOperationException(FrameworkResources.EndMustBeCalledBeforeBegin);
		}
		if (!_hasIsCompleteBeenQueried)
		{
			throw new InvalidOperationException(FrameworkResources.IsCompleteMustBeCalled);
		}
		IDirect3DQuery9* ptr = pComPtr;
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, int>)(int)(*(uint*)(*(int*)ptr + 24)))((nint)ptr, 2u);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
		_isAvailable = false;
		_isInBeginEndPair = true;
		_hasCalledBegin = true;
		_hasIsCompleteBeenQueried = false;
	}

	public unsafe void End()
	{
		if (!_isInBeginEndPair)
		{
			throw new InvalidOperationException(FrameworkResources.BeginMustBeCalledBeforeEnd);
		}
		IDirect3DQuery9* ptr = pComPtr;
		int num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint, int>)(int)(*(uint*)(*(int*)ptr + 24)))((nint)ptr, 1u);
		if (num < 0)
		{
			throw GraphicsHelpers.GetExceptionFromResult((uint)num);
		}
		_isInBeginEndPair = false;
	}

	internal virtual int SaveDataForRecreation()
	{
		return 0;
	}

	int IGraphicsResource.SaveDataForRecreation()
	{
		//ILSpy generated this explicit interface implementation from .override directive in SaveDataForRecreation
		return this.SaveDataForRecreation();
	}

	internal unsafe virtual int RecreateAndPopulateObject()
	{
		if (pComPtr != null)
		{
			return -2147467259;
		}
		fixed (IDirect3DQuery9** ptr = &pComPtr)
		{
			int num = *(int*)_parent.pComPtr + 472;
			int num2 = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, _D3DQUERYTYPE, IDirect3DQuery9**, int>)(int)(*(uint*)num))((nint)_parent.pComPtr, (_D3DQUERYTYPE)9, ptr);
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

	internal unsafe virtual void ReleaseNativeObject([MarshalAs(UnmanagedType.U1)] bool disposeManagedResource)
	{
		GraphicsDevice parent = _parent;
		if (parent != null && pComPtr != null)
		{
			parent.Resources.ReleaseAllReferences(_internalHandle, disposeManagedResource);
		}
		_isAvailable = false;
		pComPtr = null;
	}

	void IGraphicsResource.ReleaseNativeObject([MarshalAs(UnmanagedType.U1)] bool disposeManagedResource)
	{
		//ILSpy generated this explicit interface implementation from .override directive in ReleaseNativeObject
		this.ReleaseNativeObject(disposeManagedResource);
	}

	internal unsafe static OcclusionQuery GetManagedObject(IDirect3DQuery9* pInterface, GraphicsDevice pDevice, uint pool)
	{
		OcclusionQuery occlusionQuery = pDevice.Resources.GetCachedObject(pInterface) as OcclusionQuery;
		if (occlusionQuery != null)
		{
			((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)pInterface + 8)))((nint)pInterface);
			occlusionQuery.isDisposed = false;
			GC.ReRegisterForFinalize(occlusionQuery);
		}
		else
		{
			occlusionQuery = new OcclusionQuery(pInterface, pDevice);
			pDevice.Resources.AddTrackedObject(occlusionQuery, pInterface, pool, 0uL, ref occlusionQuery._internalHandle);
		}
		return occlusionQuery;
	}

	private void OnObjectCreation()
	{
	}

	private void _0021OcclusionQuery()
	{
		if (!isDisposed)
		{
			isDisposed = true;
			ReleaseNativeObject(disposeManagedResource: true);
		}
	}

	private void _007EOcclusionQuery()
	{
		if (!isDisposed)
		{
			isDisposed = true;
			ReleaseNativeObject(disposeManagedResource: true);
		}
	}

	[HandleProcessCorruptedStateExceptions]
	protected override void Dispose([MarshalAs(UnmanagedType.U1)] bool P_0)
	{
		if (P_0)
		{
			try
			{
				_007EOcclusionQuery();
				return;
			}
			finally
			{
				base.Dispose(true);
			}
		}
		try
		{
			_0021OcclusionQuery();
		}
		finally
		{
			base.Dispose(false);
		}
	}
}
