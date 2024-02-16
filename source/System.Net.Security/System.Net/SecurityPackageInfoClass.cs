using System.Runtime.InteropServices;

namespace System.Net;

internal sealed class SecurityPackageInfoClass
{
	internal int Capabilities;

	internal short Version;

	internal short RPCID;

	internal int MaxToken;

	internal string Name;

	internal string Comment;

	internal unsafe SecurityPackageInfoClass(SafeHandle safeHandle, int index)
	{
		if (safeHandle.IsInvalid)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"Invalid handle: {safeHandle}", ".ctor");
			}
			return;
		}
		IntPtr intPtr = safeHandle.DangerousGetHandle() + sizeof(SecurityPackageInfo) * index;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"unmanagedAddress: {intPtr}", ".ctor");
		}
		SecurityPackageInfo* ptr = (SecurityPackageInfo*)(void*)intPtr;
		Capabilities = ptr->Capabilities;
		Version = ptr->Version;
		RPCID = ptr->RPCID;
		MaxToken = ptr->MaxToken;
		IntPtr name = ptr->Name;
		if (name != IntPtr.Zero)
		{
			Name = Marshal.PtrToStringUni(name);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"Name: {Name}", ".ctor");
			}
		}
		name = ptr->Comment;
		if (name != IntPtr.Zero)
		{
			Comment = Marshal.PtrToStringUni(name);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"Comment: {Comment}", ".ctor");
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, ToString(), ".ctor");
		}
	}

	public override string ToString()
	{
		return $"Capabilities:0x{Capabilities:x} Version:{Version} RPCID:{RPCID} MaxToken:{MaxToken} Name:{Name ?? "(null)"} Comment: {Comment ?? "(null)"}";
	}
}
