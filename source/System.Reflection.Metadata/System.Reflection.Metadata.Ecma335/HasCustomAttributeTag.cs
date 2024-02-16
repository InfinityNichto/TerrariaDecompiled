using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335;

internal static class HasCustomAttributeTag
{
	internal const int NumberOfBits = 5;

	internal const int LargeRowSize = 2048;

	internal const uint MethodDef = 0u;

	internal const uint Field = 1u;

	internal const uint TypeRef = 2u;

	internal const uint TypeDef = 3u;

	internal const uint Param = 4u;

	internal const uint InterfaceImpl = 5u;

	internal const uint MemberRef = 6u;

	internal const uint Module = 7u;

	internal const uint DeclSecurity = 8u;

	internal const uint Property = 9u;

	internal const uint Event = 10u;

	internal const uint StandAloneSig = 11u;

	internal const uint ModuleRef = 12u;

	internal const uint TypeSpec = 13u;

	internal const uint Assembly = 14u;

	internal const uint AssemblyRef = 15u;

	internal const uint File = 16u;

	internal const uint ExportedType = 17u;

	internal const uint ManifestResource = 18u;

	internal const uint GenericParam = 19u;

	internal const uint GenericParamConstraint = 20u;

	internal const uint MethodSpec = 21u;

	internal const uint TagMask = 31u;

	internal const uint InvalidTokenType = uint.MaxValue;

	internal static uint[] TagToTokenTypeArray = new uint[32]
	{
		100663296u, 67108864u, 16777216u, 33554432u, 134217728u, 150994944u, 167772160u, 0u, 234881024u, 385875968u,
		335544320u, 285212672u, 436207616u, 452984832u, 536870912u, 587202560u, 637534208u, 654311424u, 671088640u, 704643072u,
		738197504u, 721420288u, 4294967295u, 4294967295u, 4294967295u, 4294967295u, 4294967295u, 4294967295u, 4294967295u, 4294967295u,
		4294967295u, 4294967295u
	};

	internal const TableMask TablesReferenced = TableMask.Module | TableMask.TypeRef | TableMask.TypeDef | TableMask.Field | TableMask.MethodDef | TableMask.Param | TableMask.InterfaceImpl | TableMask.MemberRef | TableMask.DeclSecurity | TableMask.StandAloneSig | TableMask.Event | TableMask.Property | TableMask.ModuleRef | TableMask.TypeSpec | TableMask.Assembly | TableMask.AssemblyRef | TableMask.File | TableMask.ExportedType | TableMask.ManifestResource | TableMask.GenericParam | TableMask.MethodSpec | TableMask.GenericParamConstraint;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static EntityHandle ConvertToHandle(uint hasCustomAttribute)
	{
		uint num = TagToTokenTypeArray[hasCustomAttribute & 0x1F];
		uint num2 = hasCustomAttribute >> 5;
		if (num == uint.MaxValue || (num2 & 0xFF000000u) != 0)
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
			6u => (rowId << 5) | 0u, 
			4u => (rowId << 5) | 1u, 
			1u => (rowId << 5) | 2u, 
			2u => (rowId << 5) | 3u, 
			8u => (rowId << 5) | 4u, 
			9u => (rowId << 5) | 5u, 
			10u => (rowId << 5) | 6u, 
			0u => (rowId << 5) | 7u, 
			14u => (rowId << 5) | 8u, 
			23u => (rowId << 5) | 9u, 
			20u => (rowId << 5) | 0xAu, 
			17u => (rowId << 5) | 0xBu, 
			26u => (rowId << 5) | 0xCu, 
			27u => (rowId << 5) | 0xDu, 
			32u => (rowId << 5) | 0xEu, 
			35u => (rowId << 5) | 0xFu, 
			38u => (rowId << 5) | 0x10u, 
			39u => (rowId << 5) | 0x11u, 
			40u => (rowId << 5) | 0x12u, 
			42u => (rowId << 5) | 0x13u, 
			44u => (rowId << 5) | 0x14u, 
			43u => (rowId << 5) | 0x15u, 
			_ => 0u, 
		};
	}
}
