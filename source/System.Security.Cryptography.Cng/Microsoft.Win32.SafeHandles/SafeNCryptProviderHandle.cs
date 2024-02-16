using System;

namespace Microsoft.Win32.SafeHandles;

public sealed class SafeNCryptProviderHandle : SafeNCryptHandle
{
	internal SafeNCryptProviderHandle Duplicate()
	{
		return Duplicate<SafeNCryptProviderHandle>();
	}

	internal void SetHandleValue(IntPtr newHandleValue)
	{
		SetHandle(newHandleValue);
	}

	protected override bool ReleaseNativeHandle()
	{
		return ReleaseNativeWithNCryptFreeObject();
	}
}
