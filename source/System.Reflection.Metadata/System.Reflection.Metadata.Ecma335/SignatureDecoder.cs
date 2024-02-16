using System.Collections.Immutable;

namespace System.Reflection.Metadata.Ecma335;

public readonly struct SignatureDecoder<TType, TGenericContext>
{
	private readonly ISignatureTypeProvider<TType, TGenericContext> _provider;

	private readonly MetadataReader _metadataReaderOpt;

	private readonly TGenericContext _genericContext;

	public SignatureDecoder(ISignatureTypeProvider<TType, TGenericContext> provider, MetadataReader metadataReader, TGenericContext genericContext)
	{
		if (provider == null)
		{
			Throw.ArgumentNull("provider");
		}
		_metadataReaderOpt = metadataReader;
		_provider = provider;
		_genericContext = genericContext;
	}

	public TType DecodeType(ref BlobReader blobReader, bool allowTypeSpecifications = false)
	{
		return DecodeType(ref blobReader, allowTypeSpecifications, blobReader.ReadCompressedInteger());
	}

	private TType DecodeType(ref BlobReader blobReader, bool allowTypeSpecifications, int typeCode)
	{
		switch (typeCode)
		{
		case 1:
		case 2:
		case 3:
		case 4:
		case 5:
		case 6:
		case 7:
		case 8:
		case 9:
		case 10:
		case 11:
		case 12:
		case 13:
		case 14:
		case 22:
		case 24:
		case 25:
		case 28:
			return _provider.GetPrimitiveType((PrimitiveTypeCode)typeCode);
		case 15:
		{
			TType elementType = DecodeType(ref blobReader);
			return _provider.GetPointerType(elementType);
		}
		case 16:
		{
			TType elementType = DecodeType(ref blobReader);
			return _provider.GetByReferenceType(elementType);
		}
		case 69:
		{
			TType elementType = DecodeType(ref blobReader);
			return _provider.GetPinnedType(elementType);
		}
		case 29:
		{
			TType elementType = DecodeType(ref blobReader);
			return _provider.GetSZArrayType(elementType);
		}
		case 27:
		{
			MethodSignature<TType> signature = DecodeMethodSignature(ref blobReader);
			return _provider.GetFunctionPointerType(signature);
		}
		case 20:
			return DecodeArrayType(ref blobReader);
		case 31:
			return DecodeModifiedType(ref blobReader, isRequired: true);
		case 32:
			return DecodeModifiedType(ref blobReader, isRequired: false);
		case 21:
			return DecodeGenericTypeInstance(ref blobReader);
		case 19:
		{
			int index = blobReader.ReadCompressedInteger();
			return _provider.GetGenericTypeParameter(_genericContext, index);
		}
		case 30:
		{
			int index = blobReader.ReadCompressedInteger();
			return _provider.GetGenericMethodParameter(_genericContext, index);
		}
		case 17:
		case 18:
			return DecodeTypeHandle(ref blobReader, (byte)typeCode, allowTypeSpecifications);
		default:
			throw new BadImageFormatException(System.SR.Format(System.SR.UnexpectedSignatureTypeCode, typeCode));
		}
	}

	private ImmutableArray<TType> DecodeTypeSequence(ref BlobReader blobReader)
	{
		int num = blobReader.ReadCompressedInteger();
		if (num == 0)
		{
			throw new BadImageFormatException(System.SR.SignatureTypeSequenceMustHaveAtLeastOneElement);
		}
		ImmutableArray<TType>.Builder builder = ImmutableArray.CreateBuilder<TType>(num);
		for (int i = 0; i < num; i++)
		{
			builder.Add(DecodeType(ref blobReader));
		}
		return builder.MoveToImmutable();
	}

	public MethodSignature<TType> DecodeMethodSignature(ref BlobReader blobReader)
	{
		SignatureHeader header = blobReader.ReadSignatureHeader();
		CheckMethodOrPropertyHeader(header);
		int genericParameterCount = 0;
		if (header.IsGeneric)
		{
			genericParameterCount = blobReader.ReadCompressedInteger();
		}
		int num = blobReader.ReadCompressedInteger();
		TType returnType = DecodeType(ref blobReader);
		int requiredParameterCount;
		ImmutableArray<TType> parameterTypes;
		if (num == 0)
		{
			requiredParameterCount = 0;
			parameterTypes = ImmutableArray<TType>.Empty;
		}
		else
		{
			ImmutableArray<TType>.Builder builder = ImmutableArray.CreateBuilder<TType>(num);
			int i;
			for (i = 0; i < num; i++)
			{
				int num2 = blobReader.ReadCompressedInteger();
				if (num2 == 65)
				{
					break;
				}
				builder.Add(DecodeType(ref blobReader, allowTypeSpecifications: false, num2));
			}
			requiredParameterCount = i;
			for (; i < num; i++)
			{
				builder.Add(DecodeType(ref blobReader));
			}
			parameterTypes = builder.MoveToImmutable();
		}
		return new MethodSignature<TType>(header, returnType, requiredParameterCount, genericParameterCount, parameterTypes);
	}

	public ImmutableArray<TType> DecodeMethodSpecificationSignature(ref BlobReader blobReader)
	{
		SignatureHeader header = blobReader.ReadSignatureHeader();
		CheckHeader(header, SignatureKind.MethodSpecification);
		return DecodeTypeSequence(ref blobReader);
	}

	public ImmutableArray<TType> DecodeLocalSignature(ref BlobReader blobReader)
	{
		SignatureHeader header = blobReader.ReadSignatureHeader();
		CheckHeader(header, SignatureKind.LocalVariables);
		return DecodeTypeSequence(ref blobReader);
	}

	public TType DecodeFieldSignature(ref BlobReader blobReader)
	{
		SignatureHeader header = blobReader.ReadSignatureHeader();
		CheckHeader(header, SignatureKind.Field);
		return DecodeType(ref blobReader);
	}

	private TType DecodeArrayType(ref BlobReader blobReader)
	{
		TType elementType = DecodeType(ref blobReader);
		int rank = blobReader.ReadCompressedInteger();
		ImmutableArray<int> sizes = ImmutableArray<int>.Empty;
		ImmutableArray<int> lowerBounds = ImmutableArray<int>.Empty;
		int num = blobReader.ReadCompressedInteger();
		if (num > 0)
		{
			ImmutableArray<int>.Builder builder = ImmutableArray.CreateBuilder<int>(num);
			for (int i = 0; i < num; i++)
			{
				builder.Add(blobReader.ReadCompressedInteger());
			}
			sizes = builder.MoveToImmutable();
		}
		int num2 = blobReader.ReadCompressedInteger();
		if (num2 > 0)
		{
			ImmutableArray<int>.Builder builder2 = ImmutableArray.CreateBuilder<int>(num2);
			for (int j = 0; j < num2; j++)
			{
				builder2.Add(blobReader.ReadCompressedSignedInteger());
			}
			lowerBounds = builder2.MoveToImmutable();
		}
		ArrayShape shape = new ArrayShape(rank, sizes, lowerBounds);
		return _provider.GetArrayType(elementType, shape);
	}

	private TType DecodeGenericTypeInstance(ref BlobReader blobReader)
	{
		TType genericType = DecodeType(ref blobReader);
		ImmutableArray<TType> typeArguments = DecodeTypeSequence(ref blobReader);
		return _provider.GetGenericInstantiation(genericType, typeArguments);
	}

	private TType DecodeModifiedType(ref BlobReader blobReader, bool isRequired)
	{
		TType modifier = DecodeTypeHandle(ref blobReader, 0, allowTypeSpecifications: true);
		TType unmodifiedType = DecodeType(ref blobReader);
		return _provider.GetModifiedType(modifier, unmodifiedType, isRequired);
	}

	private TType DecodeTypeHandle(ref BlobReader blobReader, byte rawTypeKind, bool allowTypeSpecifications)
	{
		EntityHandle entityHandle = blobReader.ReadTypeHandle();
		if (!entityHandle.IsNil)
		{
			switch (entityHandle.Kind)
			{
			case HandleKind.TypeDefinition:
				return _provider.GetTypeFromDefinition(_metadataReaderOpt, (TypeDefinitionHandle)entityHandle, rawTypeKind);
			case HandleKind.TypeReference:
				return _provider.GetTypeFromReference(_metadataReaderOpt, (TypeReferenceHandle)entityHandle, rawTypeKind);
			case HandleKind.TypeSpecification:
				if (!allowTypeSpecifications)
				{
					throw new BadImageFormatException(System.SR.NotTypeDefOrRefHandle);
				}
				return _provider.GetTypeFromSpecification(_metadataReaderOpt, _genericContext, (TypeSpecificationHandle)entityHandle, rawTypeKind);
			}
		}
		throw new BadImageFormatException(System.SR.NotTypeDefOrRefOrSpecHandle);
	}

	private void CheckHeader(SignatureHeader header, SignatureKind expectedKind)
	{
		if (header.Kind != expectedKind)
		{
			throw new BadImageFormatException(System.SR.Format(System.SR.UnexpectedSignatureHeader, expectedKind, header.Kind, header.RawValue));
		}
	}

	private void CheckMethodOrPropertyHeader(SignatureHeader header)
	{
		SignatureKind kind = header.Kind;
		if (kind != 0 && kind != SignatureKind.Property)
		{
			throw new BadImageFormatException(System.SR.Format(System.SR.UnexpectedSignatureHeader2, SignatureKind.Property, SignatureKind.Method, header.Kind, header.RawValue));
		}
	}
}
