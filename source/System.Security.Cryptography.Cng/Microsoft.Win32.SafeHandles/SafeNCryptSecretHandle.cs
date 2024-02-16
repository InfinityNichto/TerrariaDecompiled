namespace Microsoft.Win32.SafeHandles;

public sealed class SafeNCryptSecretHandle : SafeNCryptHandle
{
	protected override bool ReleaseNativeHandle()
	{
		return ReleaseNativeWithNCryptFreeObject();
	}
}
