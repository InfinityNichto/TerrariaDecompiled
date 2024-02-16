using System.Diagnostics.CodeAnalysis;

namespace System.Reflection;

public abstract class DispatchProxy
{
	protected abstract object? Invoke(MethodInfo? targetMethod, object?[]? args);

	public static T Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TProxy>() where TProxy : DispatchProxy
	{
		return (T)DispatchProxyGenerator.CreateProxyInstance(typeof(TProxy), typeof(T));
	}
}
