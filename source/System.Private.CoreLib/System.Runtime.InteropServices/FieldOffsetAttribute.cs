namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Field, Inherited = false)]
public sealed class FieldOffsetAttribute : Attribute
{
	public int Value { get; }

	public FieldOffsetAttribute(int offset)
	{
		Value = offset;
	}
}
