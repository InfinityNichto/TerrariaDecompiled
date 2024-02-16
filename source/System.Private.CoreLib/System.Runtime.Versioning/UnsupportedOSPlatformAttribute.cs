namespace System.Runtime.Versioning;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
public sealed class UnsupportedOSPlatformAttribute : OSPlatformAttribute
{
	public UnsupportedOSPlatformAttribute(string platformName)
		: base(platformName)
	{
	}
}
