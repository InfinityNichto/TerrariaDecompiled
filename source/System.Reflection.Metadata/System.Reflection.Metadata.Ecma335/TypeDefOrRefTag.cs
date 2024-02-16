using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335;

internal static class TypeDefOrRefTag
{
	internal const int NumberOfBits = 2;

	internal const int LargeRowSize = 16384;

	internal const uint TypeDef = 0u;

	internal const uint TypeRef = 1u;

	internal const uint TypeSpec = 2u;

	internal const uint TagMask = 3u;

	internal const uint TagToTokenTypeByteVector = 1769730u;

	internal const TableMask TablesReferenced = TableMask.TypeRef | TableMask.TypeDef | TableMask.TypeSpec;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static EntityHandle ConvertToHandle(uint typeDefOrRefTag)
	{
		uint num = (uint)(1769730 >>> (int)((typeDefOrRefTag & 3) << 3) << 24);
		uint num2 = typeDefOrRefTag >> 2;
		if (num == 0 || (num2 & 0xFF000000u) != 0)
		{
			Throw.InvalidCodedIndex();
		}
		return new EntityHandle(num | num2);
	}
}
