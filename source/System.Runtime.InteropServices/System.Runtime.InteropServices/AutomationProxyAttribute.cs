namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
public sealed class AutomationProxyAttribute : Attribute
{
	public bool Value { get; }

	public AutomationProxyAttribute(bool val)
	{
		Value = val;
	}
}
