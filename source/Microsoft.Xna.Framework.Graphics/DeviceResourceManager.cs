using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Xna.Framework.Graphics;

internal class DeviceResourceManager
{
	private Dictionary<ulong, ResourceData> pResourceData;

	private GraphicsDevice pParentDevice;

	private object pSyncObject;

	private ulong _currentMaxHandle;

	private void IncrementRefCount(ulong handle)
	{
		if (pSyncObject == null)
		{
			return;
		}
		bool lockTaken = false;
		try
		{
			Monitor.Enter(pSyncObject, ref lockTaken);
			ResourceData value = pResourceData[handle];
			int currentRefCount = value.CurrentRefCount;
			if (currentRefCount == int.MaxValue)
			{
				throw new InvalidOperationException();
			}
			value.CurrentRefCount = currentRefCount + 1;
			pResourceData[handle] = value;
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(pSyncObject);
			}
		}
	}

	[return: MarshalAs(UnmanagedType.U1)]
	private bool Contains(ulong handle)
	{
		bool lockTaken = false;
		try
		{
			Monitor.Enter(pSyncObject, ref lockTaken);
			if (pResourceData.ContainsKey(handle))
			{
				return pResourceData[handle].ManagedObject.IsAlive;
			}
			return false;
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(pSyncObject);
			}
		}
	}

	public DeviceResourceManager(GraphicsDevice parent)
	{
		pParentDevice = parent;
		base._002Ector();
		pResourceData = new Dictionary<ulong, ResourceData>();
		pSyncObject = new object();
		_currentMaxHandle = 0uL;
	}

	public unsafe void AddTrackedObject(object managedObject, void* pComPtr, uint resourceManagementMode, ulong handle, ref ulong updatedHandle)
	{
		bool lockTaken = false;
		try
		{
			Monitor.Enter(pSyncObject, ref lockTaken);
			if (pResourceData.ContainsKey(handle))
			{
				ResourceData value = pResourceData[handle];
				value.pComPtr = pComPtr;
				value.dwResourceManagementMode = resourceManagementMode;
				value.CurrentRefCount = 1;
				value.isDisposed = false;
				pResourceData[handle] = value;
				updatedHandle = handle;
			}
			else
			{
				ResourceData value2 = default(ResourceData);
				value2.ManagedObject = new WeakReference(managedObject);
				value2.pComPtr = pComPtr;
				value2.ResourceName = string.Empty;
				value2.ResourceTag = null;
				value2.dwResourceManagementMode = resourceManagementMode;
				value2.CurrentRefCount = 1;
				value2.isDisposed = false;
				updatedHandle = (value2.objectHandle = ++_currentMaxHandle);
				pResourceData.Add(updatedHandle, value2);
				pParentDevice.FireCreatedEvent(managedObject);
			}
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(pSyncObject);
			}
		}
	}

	public IntPtr FindResourceW(Converter<object, IntPtr> selectionFilter)
	{
		bool lockTaken = false;
		try
		{
			Monitor.Enter(pSyncObject, ref lockTaken);
			Dictionary<ulong, ResourceData>.ValueCollection.Enumerator enumerator = pResourceData.Values.GetEnumerator();
			while (enumerator.MoveNext())
			{
				object target = enumerator.Current.ManagedObject.Target;
				if (target != null)
				{
					IntPtr intPtr = selectionFilter(target);
					IntPtr intPtr2 = intPtr;
					IntPtr intPtr3 = intPtr;
					if (intPtr3 != IntPtr.Zero)
					{
						return intPtr3;
					}
				}
			}
			return IntPtr.Zero;
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(pSyncObject);
			}
		}
	}

	public string GetCachedName(ulong handle)
	{
		bool lockTaken = false;
		try
		{
			Monitor.Enter(pSyncObject, ref lockTaken);
			if (pResourceData.ContainsKey(handle))
			{
				return pResourceData[handle].ResourceName;
			}
			return string.Empty;
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(pSyncObject);
			}
		}
	}

	public object GetCachedTag(ulong handle)
	{
		bool lockTaken = false;
		try
		{
			Monitor.Enter(pSyncObject, ref lockTaken);
			if (pResourceData.ContainsKey(handle))
			{
				return pResourceData[handle].ResourceTag;
			}
			return null;
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(pSyncObject);
			}
		}
	}

	public unsafe object GetCachedObject(void* pComPtr)
	{
		bool lockTaken = false;
		try
		{
			Monitor.Enter(pSyncObject, ref lockTaken);
			Dictionary<ulong, ResourceData>.KeyCollection.Enumerator enumerator = pResourceData.Keys.GetEnumerator();
			while (enumerator.MoveNext())
			{
				ulong current = enumerator.Current;
				if (pResourceData[current].pComPtr == pComPtr && pResourceData[current].ManagedObject.IsAlive)
				{
					return pResourceData[current].ManagedObject.Target;
				}
			}
			return null;
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(pSyncObject);
			}
		}
	}

	public void SetCachedName(ulong handle, string name)
	{
		bool lockTaken = false;
		try
		{
			Monitor.Enter(pSyncObject, ref lockTaken);
			ResourceData value = pResourceData[handle];
			value.ResourceName = name;
			pResourceData[handle] = value;
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(pSyncObject);
			}
		}
	}

	public void SetCachedTag(ulong handle, object tag)
	{
		bool lockTaken = false;
		try
		{
			Monitor.Enter(pSyncObject, ref lockTaken);
			ResourceData value = pResourceData[handle];
			value.ResourceTag = tag;
			pResourceData[handle] = value;
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(pSyncObject);
			}
		}
	}

	public unsafe void ReleaseAllReferences(ulong handle, [MarshalAs(UnmanagedType.U1)] bool dispose)
	{
		if (pSyncObject == null)
		{
			return;
		}
		bool lockTaken = false;
		try
		{
			Monitor.Enter(pSyncObject, ref lockTaken);
			if (!pResourceData.ContainsKey(handle))
			{
				return;
			}
			ResourceData value = pResourceData[handle];
			value.isDisposed = dispose;
			if (dispose)
			{
				pParentDevice.FireDestroyedEvent(GetCachedName(handle), GetCachedTag(handle));
			}
			while (true)
			{
				int currentRefCount = value.CurrentRefCount;
				if (currentRefCount <= 0)
				{
					break;
				}
				void* pComPtr = value.pComPtr;
				uint num = ((delegate* unmanaged[Stdcall, Stdcall]<IntPtr, uint>)(int)(*(uint*)(*(int*)pComPtr + 8)))((nint)pComPtr);
				value.CurrentRefCount = currentRefCount - 1;
			}
			value.pComPtr = null;
			if (dispose)
			{
				pResourceData.Remove(handle);
			}
			else
			{
				pResourceData[handle] = value;
			}
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(pSyncObject);
			}
		}
	}

	public void ReleaseAllDefaultPoolResources()
	{
		if (pSyncObject == null)
		{
			return;
		}
		bool lockTaken = false;
		try
		{
			Monitor.Enter(pSyncObject, ref lockTaken);
			List<ulong> list = new List<ulong>();
			Dictionary<ulong, ResourceData>.KeyCollection.Enumerator enumerator = pResourceData.Keys.GetEnumerator();
			while (enumerator.MoveNext())
			{
				ulong current = enumerator.Current;
				ResourceData resourceData = pResourceData[current];
				if (resourceData.dwResourceManagementMode == 0)
				{
					WeakReference managedObject = resourceData.ManagedObject;
					if (managedObject.IsAlive && managedObject.Target is Effect { isDisposed: false } effect)
					{
						effect.OnLostDevice();
					}
					else
					{
						list.Add(resourceData.objectHandle);
					}
				}
			}
			List<ulong>.Enumerator enumerator2 = list.GetEnumerator();
			while (enumerator2.MoveNext())
			{
				ulong current2 = enumerator2.Current;
				if (pResourceData[current2].CurrentRefCount > 0 && pResourceData[current2].ManagedObject.Target is IGraphicsResource graphicsResource && !pResourceData[current2].isDisposed)
				{
					graphicsResource.ReleaseNativeObject(disposeManagedResource: false);
				}
			}
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(pSyncObject);
			}
		}
	}

	public void ReleaseAllDeviceResources()
	{
		if (pSyncObject == null)
		{
			return;
		}
		bool lockTaken = false;
		try
		{
			Monitor.Enter(pSyncObject, ref lockTaken);
			ulong[] array = new ulong[pResourceData.Keys.Count];
			pResourceData.Keys.CopyTo(array, 0);
			ulong[] array2 = array;
			for (int i = 0; i < (nint)array2.LongLength; i++)
			{
				ulong key = array2[i];
				if (pResourceData[key].CurrentRefCount > 0 && pResourceData[key].ManagedObject.Target is IGraphicsResource graphicsResource && !pResourceData[key].isDisposed)
				{
					graphicsResource.ReleaseNativeObject(disposeManagedResource: false);
				}
			}
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(pSyncObject);
			}
		}
	}

	public void ReleaseAutomaticResources()
	{
		if (pSyncObject == null)
		{
			return;
		}
		int num = 0;
		bool lockTaken = false;
		try
		{
			Monitor.Enter(pSyncObject, ref lockTaken);
			ulong[] array = new ulong[pResourceData.Keys.Count];
			pResourceData.Keys.CopyTo(array, 0);
			for (int i = 0; i < (nint)array.LongLength; i++)
			{
				ulong key = array[i];
				ResourceData resourceData = pResourceData[key];
				switch (resourceData.dwResourceManagementMode)
				{
				case 1u:
				{
					WeakReference managedObject2 = resourceData.ManagedObject;
					if (managedObject2.IsAlive && managedObject2.Target is IGraphicsResource graphicsResource)
					{
						if (!resourceData.isDisposed)
						{
							num = graphicsResource.SaveDataForRecreation();
						}
						if (num < 0)
						{
							throw GraphicsHelpers.GetExceptionFromResult((uint)num);
						}
					}
					break;
				}
				case 0u:
				{
					WeakReference managedObject = resourceData.ManagedObject;
					if (managedObject.IsAlive && managedObject.Target is Effect effect)
					{
						if (!resourceData.isDisposed)
						{
							num = effect.SaveDataForRecreation();
						}
						if (num < 0)
						{
							throw GraphicsHelpers.GetExceptionFromResult((uint)num);
						}
					}
					break;
				}
				}
			}
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(pSyncObject);
			}
		}
	}

	public void RecreateResources(_D3DPOOL pool, [MarshalAs(UnmanagedType.U1)] bool deviceRecreated)
	{
		if (pSyncObject == null)
		{
			return;
		}
		bool lockTaken = false;
		try
		{
			Monitor.Enter(pSyncObject, ref lockTaken);
			ulong[] array = new ulong[pResourceData.Keys.Count];
			pResourceData.Keys.CopyTo(array, 0);
			List<Effect> list = new List<Effect>();
			for (int i = 0; i < (nint)array.LongLength; i++)
			{
				ulong key = array[i];
				ResourceData resourceData = pResourceData[key];
				if (resourceData.dwResourceManagementMode != (uint)pool)
				{
					continue;
				}
				WeakReference managedObject = resourceData.ManagedObject;
				if (!managedObject.IsAlive)
				{
					continue;
				}
				Effect effect = managedObject.Target as Effect;
				if (effect != null && !effect.isDisposed && !deviceRecreated)
				{
					effect.OnResetDevice();
					continue;
				}
				if (managedObject.Target is IGraphicsResource graphicsResource && !resourceData.isDisposed)
				{
					if (effect != null)
					{
						list.Add(effect);
					}
					else
					{
						int num = graphicsResource.RecreateAndPopulateObject();
						if (num < 0)
						{
							throw GraphicsHelpers.GetExceptionFromResult((uint)num);
						}
					}
				}
				if (managedObject.Target is IDynamicGraphicsResource dynamicGraphicsResource)
				{
					dynamicGraphicsResource.SetContentLost(isContentLost: true);
				}
			}
			List<Effect>.Enumerator enumerator = list.GetEnumerator();
			while (enumerator.MoveNext())
			{
				int num2 = enumerator.Current.RecreateAndPopulateObject();
				if (num2 < 0)
				{
					throw GraphicsHelpers.GetExceptionFromResult((uint)num2);
				}
			}
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(pSyncObject);
			}
		}
	}
}
