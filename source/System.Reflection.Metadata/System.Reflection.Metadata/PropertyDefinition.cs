using System.Collections.Immutable;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata;

public readonly struct PropertyDefinition
{
	private readonly MetadataReader _reader;

	private readonly int _rowId;

	private PropertyDefinitionHandle Handle => PropertyDefinitionHandle.FromRowId(_rowId);

	public StringHandle Name => _reader.PropertyTable.GetName(Handle);

	public PropertyAttributes Attributes => _reader.PropertyTable.GetFlags(Handle);

	public BlobHandle Signature => _reader.PropertyTable.GetSignature(Handle);

	internal PropertyDefinition(MetadataReader reader, PropertyDefinitionHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}

	public MethodSignature<TType> DecodeSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
	{
		SignatureDecoder<TType, TGenericContext> signatureDecoder = new SignatureDecoder<TType, TGenericContext>(provider, _reader, genericContext);
		BlobReader blobReader = _reader.GetBlobReader(Signature);
		return signatureDecoder.DecodeMethodSignature(ref blobReader);
	}

	public ConstantHandle GetDefaultValue()
	{
		return _reader.ConstantTable.FindConstant(Handle);
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, Handle);
	}

	public PropertyAccessors GetAccessors()
	{
		int getterRowId = 0;
		int setterRowId = 0;
		ImmutableArray<MethodDefinitionHandle>.Builder builder = null;
		ushort methodCount;
		int num = _reader.MethodSemanticsTable.FindSemanticMethodsForProperty(Handle, out methodCount);
		for (ushort num2 = 0; num2 < methodCount; num2++)
		{
			int rowId = num + num2;
			switch (_reader.MethodSemanticsTable.GetSemantics(rowId))
			{
			case MethodSemanticsAttributes.Getter:
				getterRowId = _reader.MethodSemanticsTable.GetMethod(rowId).RowId;
				break;
			case MethodSemanticsAttributes.Setter:
				setterRowId = _reader.MethodSemanticsTable.GetMethod(rowId).RowId;
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
		return new PropertyAccessors(getterRowId, setterRowId, others);
	}
}
