using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata;

public readonly struct TypeSpecification
{
	private readonly MetadataReader _reader;

	private readonly int _rowId;

	private TypeSpecificationHandle Handle => TypeSpecificationHandle.FromRowId(_rowId);

	public BlobHandle Signature => _reader.TypeSpecTable.GetSignature(Handle);

	internal TypeSpecification(MetadataReader reader, TypeSpecificationHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}

	public TType DecodeSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
	{
		SignatureDecoder<TType, TGenericContext> signatureDecoder = new SignatureDecoder<TType, TGenericContext>(provider, _reader, genericContext);
		BlobReader blobReader = _reader.GetBlobReader(Signature);
		return signatureDecoder.DecodeType(ref blobReader);
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, Handle);
	}
}
