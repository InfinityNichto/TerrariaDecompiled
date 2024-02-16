namespace System.Security;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class AllowPartiallyTrustedCallersAttribute : Attribute
{
	public PartialTrustVisibilityLevel PartialTrustVisibilityLevel { get; set; }
}
