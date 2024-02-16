using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Xml.Serialization;

internal static class TypeExtensions
{
	public static bool TryConvertTo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] this Type targetType, object data, out object returnValue)
	{
		if (targetType == null)
		{
			throw new ArgumentNullException("targetType");
		}
		returnValue = null;
		if (data == null)
		{
			return !targetType.IsValueType;
		}
		Type type = data.GetType();
		if (targetType == type || targetType.IsAssignableFrom(type))
		{
			returnValue = data;
			return true;
		}
		MethodInfo[] methods = targetType.GetMethods(BindingFlags.Static | BindingFlags.Public);
		MethodInfo[] array = methods;
		foreach (MethodInfo methodInfo in array)
		{
			if (methodInfo.Name == "op_Implicit" && methodInfo.ReturnType != null && targetType.IsAssignableFrom(methodInfo.ReturnType))
			{
				ParameterInfo[] parameters = methodInfo.GetParameters();
				if (parameters != null && parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(type))
				{
					returnValue = methodInfo.Invoke(null, new object[1] { data });
					return true;
				}
			}
		}
		return false;
	}
}
