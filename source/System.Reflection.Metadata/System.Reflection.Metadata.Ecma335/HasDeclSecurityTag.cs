using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335;

internal static class HasDeclSecurityTag
{
	internal const int NumberOfBits = 2;

	internal const int LargeRowSize = 16384;

	internal const uint TypeDef = 0u;

	internal const uint MethodDef = 1u;

	internal const uint Assembly = 2u;

	internal const uint TagMask = 3u;

	internal const TableMask TablesReferenced = TableMask.TypeDef | TableMask.MethodDef | TableMask.Assembly;

	internal const uint TagToTokenTypeByteVector = 2098690u;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static EntityHandle ConvertToHandle(uint hasDeclSecurity)
	{
		uint num = (uint)(2098690 >>> (int)((hasDeclSecurity & 3) << 3) << 24);
		uint num2 = hasDeclSecurity >> 2;
		if (num == 0 || (num2 & 0xFF000000u) != 0)
		{
			Throw.InvalidCodedIndex();
		}
		return new EntityHandle(num | num2);
	}

	internal static uint ConvertToTag(EntityHandle handle)
	{
		uint type = handle.Type;
		uint rowId = (uint)handle.RowId;
		return (type >> 24) switch
		{
			2u => (rowId << 2) | 0u, 
			6u => (rowId << 2) | 1u, 
			32u => (rowId << 2) | 2u, 
			_ => 0u, 
		};
	}
}
