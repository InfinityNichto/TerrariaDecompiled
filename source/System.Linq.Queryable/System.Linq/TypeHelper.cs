using System.Diagnostics.CodeAnalysis;

namespace System.Linq;

internal static class TypeHelper
{
	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:RequiresUnreferencedCode", Justification = "GetInterfaces is only called if 'definition' is interface type. In that case though the interface must be present (otherwise the Type of it could not exist) which also means that the trimmer kept the interface and thus kept it on all types which implement it. It doesn't matter if the GetInterfaces call below returns fewer typesas long as it returns the 'definition' as well.")]
	internal static Type FindGenericType(Type definition, Type type)
	{
		bool? flag = null;
		while (type != null && type != typeof(object))
		{
			if (type.IsGenericType && type.GetGenericTypeDefinition() == definition)
			{
				return type;
			}
			if (!flag.HasValue)
			{
				flag = definition.IsInterface;
			}
			if (flag.GetValueOrDefault())
			{
				Type[] interfaces = type.GetInterfaces();
				foreach (Type type2 in interfaces)
				{
					Type type3 = FindGenericType(definition, type2);
					if (type3 != null)
					{
						return type3;
					}
				}
			}
			type = type.BaseType;
		}
		return null;
	}
}
