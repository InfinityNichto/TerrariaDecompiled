using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335;

internal static class TypeOrMethodDefTag
{
	internal const int NumberOfBits = 1;

	internal const int LargeRowSize = 32768;

	internal const uint TypeDef = 0u;

	internal const uint MethodDef = 1u;

	internal const uint TagMask = 1u;

	internal const uint TagToTokenTypeByteVector = 1538u;

	internal const TableMask TablesReferenced = TableMask.TypeDef | TableMask.MethodDef;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static EntityHandle ConvertToHandle(uint typeOrMethodDef)
	{
		uint num = (uint)(1538 >>> (int)((typeOrMethodDef & 1) << 3) << 24);
		uint num2 = typeOrMethodDef >> 1;
		if ((num2 & 0xFF000000u) != 0)
		{
			Throw.InvalidCodedIndex();
		}
		return new EntityHandle(num | num2);
	}

	internal static uint ConvertTypeDefRowIdToTag(TypeDefinitionHandle typeDef)
	{
		return (uint)(typeDef.RowId << 1) | 0u;
	}

	internal static uint ConvertMethodDefToTag(MethodDefinitionHandle methodDef)
	{
		return (uint)(methodDef.RowId << 1) | 1u;
	}
}
