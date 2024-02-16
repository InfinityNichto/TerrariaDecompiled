using System.Reflection.Emit;

namespace System.Xml.Serialization;

internal sealed class MethodBuilderInfo
{
	public readonly MethodBuilder MethodBuilder;

	public readonly Type[] ParameterTypes;

	public MethodBuilderInfo(MethodBuilder methodBuilder, Type[] parameterTypes)
	{
		MethodBuilder = methodBuilder;
		ParameterTypes = parameterTypes;
	}
}
