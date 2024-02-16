using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335;

internal static class HasConstantTag
{
	internal const int NumberOfBits = 2;

	internal const int LargeRowSize = 16384;

	internal const uint Field = 0u;

	internal const uint Param = 1u;

	internal const uint Property = 2u;

	internal const uint TagMask = 3u;

	internal const TableMask TablesReferenced = TableMask.Field | TableMask.Param | TableMask.Property;

	internal const uint TagToTokenTypeByteVector = 1509380u;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static EntityHandle ConvertToHandle(uint hasConstant)
	{
		uint num = (uint)(1509380 >>> (int)((hasConstant & 3) << 3) << 24);
		uint num2 = hasConstant >> 2;
		if (num == 0 || (num2 & 0xFF000000u) != 0)
		{
			Throw.InvalidCodedIndex();
		}
		return new EntityHandle(num | num2);
	}

	internal static uint ConvertToTag(EntityHandle token)
	{
		HandleKind kind = token.Kind;
		uint rowId = (uint)token.RowId;
		return kind switch
		{
			HandleKind.FieldDefinition => (rowId << 2) | 0u, 
			HandleKind.Parameter => (rowId << 2) | 1u, 
			HandleKind.PropertyDefinition => (rowId << 2) | 2u, 
			_ => 0u, 
		};
	}
}
