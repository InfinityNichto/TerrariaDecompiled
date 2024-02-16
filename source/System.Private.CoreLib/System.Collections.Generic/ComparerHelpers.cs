namespace System.Collections.Generic;

internal static class ComparerHelpers
{
	internal static object CreateDefaultComparer(Type type)
	{
		object obj = null;
		RuntimeType runtimeType = (RuntimeType)type;
		if (typeof(IComparable<>).MakeGenericType(type).IsAssignableFrom(type))
		{
			obj = RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(GenericComparer<int>), runtimeType);
		}
		else if (type.IsGenericType)
		{
			if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				obj = TryCreateNullableComparer(runtimeType);
			}
		}
		else if (type.IsEnum)
		{
			obj = TryCreateEnumComparer(runtimeType);
		}
		return obj ?? RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(ObjectComparer<object>), runtimeType);
	}

	private static object TryCreateNullableComparer(RuntimeType nullableType)
	{
		RuntimeType runtimeType = (RuntimeType)nullableType.GetGenericArguments()[0];
		if (typeof(IComparable<>).MakeGenericType(runtimeType).IsAssignableFrom(runtimeType))
		{
			return RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(NullableComparer<int>), runtimeType);
		}
		return null;
	}

	private static object TryCreateEnumComparer(RuntimeType enumType)
	{
		TypeCode typeCode = Type.GetTypeCode(Enum.GetUnderlyingType(enumType));
		if ((uint)(typeCode - 5) <= 7u)
		{
			return RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(EnumComparer<>), enumType);
		}
		return null;
	}

	internal static object CreateDefaultEqualityComparer(Type type)
	{
		object obj = null;
		RuntimeType runtimeType = (RuntimeType)type;
		if (type == typeof(byte))
		{
			obj = new ByteEqualityComparer();
		}
		else if (type == typeof(string))
		{
			obj = new GenericEqualityComparer<string>();
		}
		else if (type.IsAssignableTo(typeof(IEquatable<>).MakeGenericType(type)))
		{
			obj = RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(GenericEqualityComparer<string>), runtimeType);
		}
		else if (type.IsGenericType)
		{
			if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				obj = TryCreateNullableEqualityComparer(runtimeType);
			}
		}
		else if (type.IsEnum)
		{
			obj = TryCreateEnumEqualityComparer(runtimeType);
		}
		return obj ?? RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(ObjectEqualityComparer<object>), runtimeType);
	}

	private static object TryCreateNullableEqualityComparer(RuntimeType nullableType)
	{
		RuntimeType runtimeType = (RuntimeType)nullableType.GetGenericArguments()[0];
		if (typeof(IEquatable<>).MakeGenericType(runtimeType).IsAssignableFrom(runtimeType))
		{
			return RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(NullableEqualityComparer<int>), runtimeType);
		}
		return null;
	}

	private static object TryCreateEnumEqualityComparer(RuntimeType enumType)
	{
		TypeCode typeCode = Type.GetTypeCode(Enum.GetUnderlyingType(enumType));
		if ((uint)(typeCode - 5) <= 7u)
		{
			return RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(EnumEqualityComparer<>), enumType);
		}
		return null;
	}
}
