using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335;

internal static class HasSemanticsTag
{
	internal const int NumberOfBits = 1;

	internal const int LargeRowSize = 32768;

	internal const uint Event = 0u;

	internal const uint Property = 1u;

	internal const uint TagMask = 1u;

	internal const TableMask TablesReferenced = TableMask.Event | TableMask.Property;

	internal const uint TagToTokenTypeByteVector = 5908u;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static EntityHandle ConvertToHandle(uint hasSemantic)
	{
		uint num = (uint)(5908 >>> (int)((hasSemantic & 1) << 3) << 24);
		uint num2 = hasSemantic >> 1;
		if ((num2 & 0xFF000000u) != 0)
		{
			Throw.InvalidCodedIndex();
		}
		return new EntityHandle(num | num2);
	}

	internal static uint ConvertEventHandleToTag(EventDefinitionHandle eventDef)
	{
		return (uint)(eventDef.RowId << 1) | 0u;
	}

	internal static uint ConvertPropertyHandleToTag(PropertyDefinitionHandle propertyDef)
	{
		return (uint)(propertyDef.RowId << 1) | 1u;
	}
}
