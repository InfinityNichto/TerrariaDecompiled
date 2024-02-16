using System.Collections.Immutable;

namespace System.Reflection.Metadata;

public readonly struct EventDefinition
{
	private readonly MetadataReader _reader;

	private readonly int _rowId;

	private EventDefinitionHandle Handle => EventDefinitionHandle.FromRowId(_rowId);

	public StringHandle Name => _reader.EventTable.GetName(Handle);

	public EventAttributes Attributes => _reader.EventTable.GetFlags(Handle);

	public EntityHandle Type => _reader.EventTable.GetEventType(Handle);

	internal EventDefinition(MetadataReader reader, EventDefinitionHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, Handle);
	}

	public EventAccessors GetAccessors()
	{
		int adderRowId = 0;
		int removerRowId = 0;
		int raiserRowId = 0;
		ImmutableArray<MethodDefinitionHandle>.Builder builder = null;
		ushort methodCount;
		int num = _reader.MethodSemanticsTable.FindSemanticMethodsForEvent(Handle, out methodCount);
		for (ushort num2 = 0; num2 < methodCount; num2++)
		{
			int rowId = num + num2;
			switch (_reader.MethodSemanticsTable.GetSemantics(rowId))
			{
			case MethodSemanticsAttributes.Adder:
				adderRowId = _reader.MethodSemanticsTable.GetMethod(rowId).RowId;
				break;
			case MethodSemanticsAttributes.Remover:
				removerRowId = _reader.MethodSemanticsTable.GetMethod(rowId).RowId;
				break;
			case MethodSemanticsAttributes.Raiser:
				raiserRowId = _reader.MethodSemanticsTable.GetMethod(rowId).RowId;
				break;
			case MethodSemanticsAttributes.Other:
				if (builder == null)
				{
					builder = ImmutableArray.CreateBuilder<MethodDefinitionHandle>();
				}
				builder.Add(_reader.MethodSemanticsTable.GetMethod(rowId));
				break;
			}
		}
		ImmutableArray<MethodDefinitionHandle> others = builder?.ToImmutable() ?? ImmutableArray<MethodDefinitionHandle>.Empty;
		return new EventAccessors(adderRowId, removerRowId, raiserRowId, others);
	}
}
