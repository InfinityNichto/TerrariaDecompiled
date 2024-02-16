using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335;

internal static class HasFieldMarshalTag
{
	internal const int NumberOfBits = 1;

	internal const int LargeRowSize = 32768;

	internal const uint Field = 0u;

	internal const uint Param = 1u;

	internal const uint TagMask = 1u;

	internal const TableMask TablesReferenced = TableMask.Field | TableMask.Param;

	internal const uint TagToTokenTypeByteVector = 2052u;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static EntityHandle ConvertToHandle(uint hasFieldMarshal)
	{
		uint num = (uint)(2052 >>> (int)((hasFieldMarshal & 1) << 3) << 24);
		uint num2 = hasFieldMarshal >> 1;
		if ((num2 & 0xFF000000u) != 0)
		{
			Throw.InvalidCodedIndex();
		}
		return new EntityHandle(num | num2);
	}

	internal static uint ConvertToTag(EntityHandle handle)
	{
		if (handle.Type == 67108864)
		{
			return (uint)(handle.RowId << 1) | 0u;
		}
		if (handle.Type == 134217728)
		{
			return (uint)(handle.RowId << 1) | 1u;
		}
		return 0u;
	}
}
