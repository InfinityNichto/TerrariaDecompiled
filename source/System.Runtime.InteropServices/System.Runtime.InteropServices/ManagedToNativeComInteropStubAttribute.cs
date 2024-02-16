namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class ManagedToNativeComInteropStubAttribute : Attribute
{
	public Type ClassType { get; }

	public string MethodName { get; }

	public ManagedToNativeComInteropStubAttribute(Type classType, string methodName)
	{
		ClassType = classType;
		MethodName = methodName;
	}
}
