namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class TypeForwardedToAttribute : Attribute
{
	public Type Destination { get; }

	public TypeForwardedToAttribute(Type destination)
	{
		Destination = destination;
	}
}
