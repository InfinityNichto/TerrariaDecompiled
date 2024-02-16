using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class ByteEqualityComparer : EqualityComparer<byte>
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(byte x, byte y)
	{
		return x == y;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode(byte b)
	{
		return b.GetHashCode();
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
