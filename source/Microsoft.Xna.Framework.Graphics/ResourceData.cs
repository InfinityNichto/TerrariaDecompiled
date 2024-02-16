using System;

namespace Microsoft.Xna.Framework.Graphics;

internal struct ResourceData
{
	public string ResourceName;

	public unsafe void* pComPtr;

	public object ResourceTag;

	public WeakReference ManagedObject;

	public uint dwResourceManagementMode;

	public int CurrentRefCount;

	public ulong objectHandle;

	public bool isDisposed;
}
