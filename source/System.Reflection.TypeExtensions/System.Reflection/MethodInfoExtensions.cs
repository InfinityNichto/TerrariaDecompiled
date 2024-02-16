namespace System.Reflection;

public static class MethodInfoExtensions
{
	public static MethodInfo GetBaseDefinition(this MethodInfo method)
	{
		ArgumentNullException.ThrowIfNull(method, "method");
		return method.GetBaseDefinition();
	}
}
