namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Module, Inherited = false)]
public sealed class DefaultCharSetAttribute : Attribute
{
	public CharSet CharSet { get; }

	public DefaultCharSetAttribute(CharSet charSet)
	{
		CharSet = charSet;
	}
}
