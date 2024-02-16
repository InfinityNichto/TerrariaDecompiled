namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, Inherited = false)]
[Obsolete("IDispatchImplAttribute has been deprecated and is not supported.")]
public sealed class IDispatchImplAttribute : Attribute
{
	public IDispatchImplType Value { get; }

	public IDispatchImplAttribute(short implType)
		: this((IDispatchImplType)implType)
	{
	}

	public IDispatchImplAttribute(IDispatchImplType implType)
	{
		Value = implType;
	}
}
