using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Collections.Generic;

[Serializable]
internal sealed class EnumComparer<T> : Comparer<T>, ISerializable where T : struct, Enum
{
	public override int Compare(T x, T y)
	{
		return RuntimeHelpers.EnumCompareTo(x, y);
	}

	public EnumComparer()
	{
	}

	private EnumComparer(SerializationInfo info, StreamingContext context)
	{
	}

	public override bool Equals([NotNullWhen(true)] object obj)
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

	public void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		info.SetType(typeof(ObjectComparer<T>));
	}
}
