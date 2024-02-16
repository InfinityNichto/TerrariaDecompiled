namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
public sealed class MarshalAsAttribute : Attribute
{
	public VarEnum SafeArraySubType;

	public Type? SafeArrayUserDefinedSubType;

	public int IidParameterIndex;

	public UnmanagedType ArraySubType;

	public short SizeParamIndex;

	public int SizeConst;

	public string? MarshalType;

	public Type? MarshalTypeRef;

	public string? MarshalCookie;

	public UnmanagedType Value { get; }

	public MarshalAsAttribute(UnmanagedType unmanagedType)
	{
		Value = unmanagedType;
	}

	public MarshalAsAttribute(short unmanagedType)
	{
		Value = (UnmanagedType)unmanagedType;
	}
}
