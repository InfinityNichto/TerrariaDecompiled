using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Collections.Generic;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class EnumEqualityComparer<T> : EqualityComparer<T>, ISerializable where T : struct, Enum
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(T x, T y)
	{
		return RuntimeHelpers.EnumEquals(x, y);
	}

	internal override int IndexOf(T[] array, T value, int startIndex, int count)
	{
		int num = startIndex + count;
		for (int i = startIndex; i < num; i++)
		{
			if (RuntimeHelpers.EnumEquals(array[i], value))
			{
				return i;
			}
		}
		return -1;
	}

	internal override int LastIndexOf(T[] array, T value, int startIndex, int count)
	{
		int num = startIndex - count + 1;
		for (int num2 = startIndex; num2 >= num; num2--)
		{
			if (RuntimeHelpers.EnumEquals(array[num2], value))
			{
				return num2;
			}
		}
		return -1;
	}

	public EnumEqualityComparer()
	{
	}

	private EnumEqualityComparer(SerializationInfo information, StreamingContext context)
	{
	}

	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		if (Type.GetTypeCode(Enum.GetUnderlyingType(typeof(T))) != TypeCode.Int32)
		{
			info.SetType(typeof(ObjectEqualityComparer<T>));
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode(T obj)
	{
		return obj.GetHashCode();
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj != null)
		{
			return GetType() == obj.GetType();
		}
		return false;
	}

	public override int GetHashCode()
	{
		return GetType().GetHashCode();
	}
}
