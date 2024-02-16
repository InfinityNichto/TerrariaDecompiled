using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System;

[Serializable]
[NonVersionable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public struct Nullable<T> where T : struct
{
	private readonly bool hasValue;

	internal T value;

	public readonly bool HasValue
	{
		[NonVersionable]
		get
		{
			return hasValue;
		}
	}

	public readonly T Value
	{
		get
		{
			if (!hasValue)
			{
				ThrowHelper.ThrowInvalidOperationException_InvalidOperation_NoValue();
			}
			return value;
		}
	}

	[NonVersionable]
	public Nullable(T value)
	{
		this.value = value;
		hasValue = true;
	}

	[NonVersionable]
	public readonly T GetValueOrDefault()
	{
		return value;
	}

	[NonVersionable]
	public readonly T GetValueOrDefault(T defaultValue)
	{
		if (!hasValue)
		{
			return defaultValue;
		}
		return value;
	}

	public override bool Equals(object? other)
	{
		if (!hasValue)
		{
			return other == null;
		}
		if (other == null)
		{
			return false;
		}
		return value.Equals(other);
	}

	public override int GetHashCode()
	{
		if (!hasValue)
		{
			return 0;
		}
		return value.GetHashCode();
	}

	public override string? ToString()
	{
		if (!hasValue)
		{
			return "";
		}
		return value.ToString();
	}

	[NonVersionable]
	public static implicit operator T?(T value)
	{
		return value;
	}

	[NonVersionable]
	public static explicit operator T(T? value)
	{
		return value.Value;
	}
}
public static class Nullable
{
	public static int Compare<T>(T? n1, T? n2) where T : struct
	{
		if (n1.HasValue)
		{
			if (n2.HasValue)
			{
				return Comparer<T>.Default.Compare(n1.value, n2.value);
			}
			return 1;
		}
		if (n2.HasValue)
		{
			return -1;
		}
		return 0;
	}

	public static bool Equals<T>(T? n1, T? n2) where T : struct
	{
		if (n1.HasValue)
		{
			if (n2.HasValue)
			{
				return EqualityComparer<T>.Default.Equals(n1.value, n2.value);
			}
			return false;
		}
		if (n2.HasValue)
		{
			return false;
		}
		return true;
	}

	public static Type? GetUnderlyingType(Type nullableType)
	{
		if ((object)nullableType == null)
		{
			throw new ArgumentNullException("nullableType");
		}
		if (nullableType.IsGenericType && !nullableType.IsGenericTypeDefinition)
		{
			Type genericTypeDefinition = nullableType.GetGenericTypeDefinition();
			if ((object)genericTypeDefinition == typeof(Nullable<>))
			{
				return nullableType.GetGenericArguments()[0];
			}
		}
		return null;
	}
}
