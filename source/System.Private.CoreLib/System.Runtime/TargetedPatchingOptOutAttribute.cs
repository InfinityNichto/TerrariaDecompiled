namespace System.Runtime;

[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TargetedPatchingOptOutAttribute : Attribute
{
	public string Reason { get; }

	public TargetedPatchingOptOutAttribute(string reason)
	{
		Reason = reason;
	}
}
