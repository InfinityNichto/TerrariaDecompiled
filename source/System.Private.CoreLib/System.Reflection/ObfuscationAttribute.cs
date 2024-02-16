namespace System.Reflection;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false)]
public sealed class ObfuscationAttribute : Attribute
{
	public bool StripAfterObfuscation { get; set; } = true;


	public bool Exclude { get; set; } = true;


	public bool ApplyToMembers { get; set; } = true;


	public string? Feature { get; set; } = "all";

}
