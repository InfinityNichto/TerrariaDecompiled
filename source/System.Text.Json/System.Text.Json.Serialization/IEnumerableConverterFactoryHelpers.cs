using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Reflection;

namespace System.Text.Json.Serialization;

internal static class IEnumerableConverterFactoryHelpers
{
	[RequiresUnreferencedCode("System.Collections.Immutable converters use Reflection to find and create Immutable Collection types, which requires unreferenced code.")]
	public static MethodInfo GetImmutableEnumerableCreateRangeMethod(this Type type, Type elementType)
	{
		Type immutableEnumerableConstructingType = GetImmutableEnumerableConstructingType(type);
		if (immutableEnumerableConstructingType != null)
		{
			MethodInfo[] methods = immutableEnumerableConstructingType.GetMethods();
			MethodInfo[] array = methods;
			foreach (MethodInfo methodInfo in array)
			{
				if (methodInfo.Name == "CreateRange" && methodInfo.GetParameters().Length == 1 && methodInfo.IsGenericMethod && methodInfo.GetGenericArguments().Length == 1)
				{
					return methodInfo.MakeGenericMethod(elementType);
				}
			}
		}
		ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(type);
		return null;
	}

	[RequiresUnreferencedCode("System.Collections.Immutable converters use Reflection to find and create Immutable Collection types, which requires unreferenced code.")]
	public static MethodInfo GetImmutableDictionaryCreateRangeMethod(this Type type, Type keyType, Type valueType)
	{
		Type immutableDictionaryConstructingType = GetImmutableDictionaryConstructingType(type);
		if (immutableDictionaryConstructingType != null)
		{
			MethodInfo[] methods = immutableDictionaryConstructingType.GetMethods();
			MethodInfo[] array = methods;
			foreach (MethodInfo methodInfo in array)
			{
				if (methodInfo.Name == "CreateRange" && methodInfo.GetParameters().Length == 1 && methodInfo.IsGenericMethod && methodInfo.GetGenericArguments().Length == 2)
				{
					return methodInfo.MakeGenericMethod(keyType, valueType);
				}
			}
		}
		ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(type);
		return null;
	}

	[RequiresUnreferencedCode("System.Collections.Immutable converters use Reflection to find and create Immutable Collection types, which requires unreferenced code.")]
	private static Type GetImmutableEnumerableConstructingType(Type type)
	{
		string immutableEnumerableConstructingTypeName = type.GetImmutableEnumerableConstructingTypeName();
		if (immutableEnumerableConstructingTypeName != null)
		{
			return type.Assembly.GetType(immutableEnumerableConstructingTypeName);
		}
		return null;
	}

	[RequiresUnreferencedCode("System.Collections.Immutable converters use Reflection to find and create Immutable Collection types, which requires unreferenced code.")]
	private static Type GetImmutableDictionaryConstructingType(Type type)
	{
		string immutableDictionaryConstructingTypeName = type.GetImmutableDictionaryConstructingTypeName();
		if (immutableDictionaryConstructingTypeName != null)
		{
			return type.Assembly.GetType(immutableDictionaryConstructingTypeName);
		}
		return null;
	}

	public static bool IsNonGenericStackOrQueue(this Type type)
	{
		Type typeIfExists = GetTypeIfExists("System.Collections.Stack, System.Collections.NonGeneric");
		if ((object)typeIfExists != null && typeIfExists.IsAssignableFrom(type))
		{
			return true;
		}
		Type typeIfExists2 = GetTypeIfExists("System.Collections.Queue, System.Collections.NonGeneric");
		if ((object)typeIfExists2 != null && typeIfExists2.IsAssignableFrom(type))
		{
			return true;
		}
		return false;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2057:TypeGetType", Justification = "This method exists to allow for 'weak references' to the Stack and Queue types. If those types are used in the app, they will be preserved by the app and Type.GetType will return them. If those types are not used in the app, we don't want to preserve them here.")]
	private static Type GetTypeIfExists(string name)
	{
		return Type.GetType(name, throwOnError: false);
	}
}
