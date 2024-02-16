namespace System.Reflection.Metadata.Ecma335;

public static class ExportedTypeExtensions
{
	public static int GetTypeDefinitionId(this ExportedType exportedType)
	{
		return exportedType.reader.ExportedTypeTable.GetTypeDefId(exportedType.rowId);
	}
}
