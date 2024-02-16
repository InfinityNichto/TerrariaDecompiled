using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335;

internal static class MethodDefOrRefTag
{
	internal const int NumberOfBits = 1;

	internal const int LargeRowSize = 32768;

	internal const uint MethodDef = 0u;

	internal const uint MemberRef = 1u;

	internal const uint TagMask = 1u;

	internal const TableMask TablesReferenced = TableMask.MethodDef | TableMask.MemberRef;

	internal const uint TagToTokenTypeByteVector = 2566u;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static EntityHandle ConvertToHandle(uint methodDefOrRef)
	{
		uint num = (uint)(2566 >>> (int)((methodDefOrRef & 1) << 3) << 24);
		uint num2 = methodDefOrRef >> 1;
		if ((num2 & 0xFF000000u) != 0)
		{
			Throw.InvalidCodedIndex();
		}
		return new EntityHandle(num | num2);
	}
}
