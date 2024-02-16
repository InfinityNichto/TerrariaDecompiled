using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace System.Text.Json.Reflection;

internal static class ReflectionExtensions
{
	private static readonly Type s_nullableType = typeof(Nullable<>);

	public static Type GetCompatibleGenericBaseClass(this Type type, Type baseType, Type objectType = null, bool sourceGenType = false)
	{
		if ((object)objectType == null)
		{
			objectType = typeof(object);
		}
		Type type2 = type;
		while (type2 != null && type2 != typeof(object))
		{
			if (type2.IsGenericType)
			{
				Type genericTypeDefinition = type2.GetGenericTypeDefinition();
				if (genericTypeDefinition == baseType || (sourceGenType && OpenGenericTypesHaveSamePrefix(baseType, genericTypeDefinition)))
				{
					return type2;
				}
			}
			type2 = type2.BaseType;
		}
		return null;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern", Justification = "The 'interfaceType' must exist and so trimmer kept it. In which case It also kept it on any type which implements it. The below call to GetInterfaces may return fewer results when trimmed but it will return the 'interfaceType' if the type implemented it, even after trimming.")]
	public static Type GetCompatibleGenericInterface(this Type type, Type interfaceType)
	{
		Type type2 = type;
		if (type2.IsGenericType)
		{
			type2 = type2.GetGenericTypeDefinition();
		}
		if (type2 == interfaceType)
		{
			return type;
		}
		Type[] interfaces = type.GetInterfaces();
		foreach (Type type3 in interfaces)
		{
			if (type3.IsGenericType)
			{
				Type genericTypeDefinition = type3.GetGenericTypeDefinition();
				if (genericTypeDefinition == interfaceType)
				{
					return type3;
				}
			}
		}
		return null;
	}

	public static bool IsImmutableDictionaryType(this Type type, bool sourceGenType = false)
	{
		if (!type.IsGenericType || !type.Assembly.FullName.StartsWith("System.Collections.Immutable", StringComparison.Ordinal))
		{
			return false;
		}
		switch (GetBaseNameFromGenericType(type, sourceGenType))
		{
		case "System.Collections.Immutable.ImmutableDictionary`2":
		case "System.Collections.Immutable.IImmutableDictionary`2":
		case "System.Collections.Immutable.ImmutableSortedDictionary`2":
			return true;
		default:
			return false;
		}
	}

	public static bool IsImmutableEnumerableType(this Type type, bool sourceGenType = false)
	{
		if (!type.IsGenericType || !type.Assembly.FullName.StartsWith("System.Collections.Immutable", StringComparison.Ordinal))
		{
			return false;
		}
		switch (GetBaseNameFromGenericType(type, sourceGenType))
		{
		case "System.Collections.Immutable.ImmutableArray`1":
		case "System.Collections.Immutable.ImmutableList`1":
		case "System.Collections.Immutable.IImmutableList`1":
		case "System.Collections.Immutable.ImmutableStack`1":
		case "System.Collections.Immutable.IImmutableStack`1":
		case "System.Collections.Immutable.ImmutableQueue`1":
		case "System.Collections.Immutable.IImmutableQueue`1":
		case "System.Collections.Immutable.ImmutableSortedSet`1":
		case "System.Collections.Immutable.ImmutableHashSet`1":
		case "System.Collections.Immutable.IImmutableSet`1":
			return true;
		default:
			return false;
		}
	}

	public static string GetImmutableDictionaryConstructingTypeName(this Type type, bool sourceGenType = false)
	{
		switch (GetBaseNameFromGenericType(type, sourceGenType))
		{
		case "System.Collections.Immutable.ImmutableDictionary`2":
		case "System.Collections.Immutable.IImmutableDictionary`2":
			return "System.Collections.Immutable.ImmutableDictionary";
		case "System.Collections.Immutable.ImmutableSortedDictionary`2":
			return "System.Collections.Immutable.ImmutableSortedDictionary";
		default:
			return null;
		}
	}

	public static string GetImmutableEnumerableConstructingTypeName(this Type type, bool sourceGenType = false)
	{
		switch (GetBaseNameFromGenericType(type, sourceGenType))
		{
		case "System.Collections.Immutable.ImmutableArray`1":
			return "System.Collections.Immutable.ImmutableArray";
		case "System.Collections.Immutable.ImmutableList`1":
		case "System.Collections.Immutable.IImmutableList`1":
			return "System.Collections.Immutable.ImmutableList";
		case "System.Collections.Immutable.ImmutableStack`1":
		case "System.Collections.Immutable.IImmutableStack`1":
			return "System.Collections.Immutable.ImmutableStack";
		case "System.Collections.Immutable.ImmutableQueue`1":
		case "System.Collections.Immutable.IImmutableQueue`1":
			return "System.Collections.Immutable.ImmutableQueue";
		case "System.Collections.Immutable.ImmutableSortedSet`1":
			return "System.Collections.Immutable.ImmutableSortedSet";
		case "System.Collections.Immutable.ImmutableHashSet`1":
		case "System.Collections.Immutable.IImmutableSet`1":
			return "System.Collections.Immutable.ImmutableHashSet";
		default:
			return null;
		}
	}

	private static bool OpenGenericTypesHaveSamePrefix(Type t1, Type t2)
	{
		return t1.FullName == GetBaseNameFromGenericTypeDef(t2);
	}

	private static string GetBaseNameFromGenericType(Type genericType, bool sourceGenType)
	{
		Type genericTypeDefinition = genericType.GetGenericTypeDefinition();
		if (!sourceGenType)
		{
			return genericTypeDefinition.FullName;
		}
		return GetBaseNameFromGenericTypeDef(genericTypeDefinition);
	}

	private static string GetBaseNameFromGenericTypeDef(Type genericTypeDef)
	{
		string fullName = genericTypeDef.FullName;
		return fullName[..(fullName.IndexOf("`") + 2)];
	}

	public static bool IsVirtual(this PropertyInfo propertyInfo)
	{
		if (propertyInfo != null)
		{
			MethodInfo? getMethod = propertyInfo.GetMethod;
			if ((object)getMethod == null || !getMethod.IsVirtual)
			{
				return propertyInfo.SetMethod?.IsVirtual ?? false;
			}
			return true;
		}
		return false;
	}

	public static bool IsKeyValuePair(this Type type, Type keyValuePairType = null)
	{
		if (!type.IsGenericType)
		{
			return false;
		}
		if ((object)keyValuePairType == null)
		{
			keyValuePairType = typeof(KeyValuePair<, >);
		}
		Type genericTypeDefinition = type.GetGenericTypeDefinition();
		return genericTypeDefinition == keyValuePairType;
	}

	public static bool TryGetDeserializationConstructor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] this Type type, bool useDefaultCtorInAnnotatedStructs, out ConstructorInfo deserializationCtor)
	{
		ConstructorInfo constructorInfo = null;
		ConstructorInfo constructorInfo2 = null;
		ConstructorInfo constructorInfo3 = null;
		ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
		if (constructors.Length == 1)
		{
			constructorInfo3 = constructors[0];
		}
		ConstructorInfo[] array = constructors;
		foreach (ConstructorInfo constructorInfo4 in array)
		{
			if (HasJsonConstructorAttribute(constructorInfo4))
			{
				if (constructorInfo != null)
				{
					deserializationCtor = null;
					return false;
				}
				constructorInfo = constructorInfo4;
			}
			else if (constructorInfo4.GetParameters().Length == 0)
			{
				constructorInfo2 = constructorInfo4;
			}
		}
		ConstructorInfo constructorInfo5 = constructorInfo;
		constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
		ConstructorInfo[] array2 = constructors;
		foreach (ConstructorInfo constructorInfo6 in array2)
		{
			if (HasJsonConstructorAttribute(constructorInfo6))
			{
				if (constructorInfo5 != null)
				{
					deserializationCtor = null;
					return false;
				}
				constructorInfo5 = constructorInfo6;
			}
		}
		if (useDefaultCtorInAnnotatedStructs && type.IsValueType && constructorInfo == null)
		{
			deserializationCtor = null;
			return true;
		}
		deserializationCtor = constructorInfo ?? constructorInfo2 ?? constructorInfo3;
		return true;
	}

	public static object GetDefaultValue(this ParameterInfo parameterInfo)
	{
		object defaultValue = parameterInfo.DefaultValue;
		if (defaultValue == DBNull.Value && parameterInfo.ParameterType != typeof(DBNull))
		{
			return null;
		}
		return defaultValue;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsNullableOfT(this Type type)
	{
		if (type.IsGenericType)
		{
			return type.GetGenericTypeDefinition() == s_nullableType;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool CanBeNull(this Type type)
	{
		if (type.IsValueType)
		{
			return type.IsNullableOfT();
		}
		return true;
	}

	public static bool IsAssignableFromInternal(this Type type, Type from)
	{
		if (from.IsNullableOfT() && type.IsInterface)
		{
			return type.IsAssignableFrom(from.GetGenericArguments()[0]);
		}
		return type.IsAssignableFrom(from);
	}

	private static bool HasJsonConstructorAttribute(ConstructorInfo constructorInfo)
	{
		return constructorInfo.GetCustomAttribute<JsonConstructorAttribute>() != null;
	}
}
