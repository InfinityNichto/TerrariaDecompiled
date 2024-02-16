using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335;

internal static class MemberForwardedTag
{
	internal const int NumberOfBits = 1;

	internal const int LargeRowSize = 32768;

	internal const uint Field = 0u;

	internal const uint MethodDef = 1u;

	internal const uint TagMask = 1u;

	internal const TableMask TablesReferenced = TableMask.Field | TableMask.MethodDef;

	internal const uint TagToTokenTypeByteVector = 1540u;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static EntityHandle ConvertToHandle(uint memberForwarded)
	{
		uint num = (uint)(1540 >>> (int)((memberForwarded & 1) << 3) << 24);
		uint num2 = memberForwarded >> 1;
		if ((num2 & 0xFF000000u) != 0)
		{
			Throw.InvalidCodedIndex();
		}
		return new EntityHandle(num | num2);
	}

	internal static uint ConvertMethodDefToTag(MethodDefinitionHandle methodDef)
	{
		return (uint)(methodDef.RowId << 1) | 1u;
	}
}
