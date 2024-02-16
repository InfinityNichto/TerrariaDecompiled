using System.Collections.Immutable;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct CustomAttributeDecoder<TType>
{
	private struct ArgumentTypeInfo
	{
		public TType Type;

		public TType ElementType;

		public SerializationTypeCode TypeCode;

		public SerializationTypeCode ElementTypeCode;
	}

	private readonly ICustomAttributeTypeProvider<TType> _provider;

	private readonly MetadataReader _reader;

	public CustomAttributeDecoder(ICustomAttributeTypeProvider<TType> provider, MetadataReader reader)
	{
		_reader = reader;
		_provider = provider;
	}

	public CustomAttributeValue<TType> DecodeValue(EntityHandle constructor, BlobHandle value)
	{
		BlobHandle handle = constructor.Kind switch
		{
			HandleKind.MethodDefinition => _reader.GetMethodDefinition((MethodDefinitionHandle)constructor).Signature, 
			HandleKind.MemberReference => _reader.GetMemberReference((MemberReferenceHandle)constructor).Signature, 
			_ => throw new BadImageFormatException(), 
		};
		BlobReader signatureReader = _reader.GetBlobReader(handle);
		BlobReader valueReader = _reader.GetBlobReader(value);
		ushort num = valueReader.ReadUInt16();
		if (num != 1)
		{
			throw new BadImageFormatException();
		}
		SignatureHeader signatureHeader = signatureReader.ReadSignatureHeader();
		if (signatureHeader.Kind != 0 || signatureHeader.IsGeneric)
		{
			throw new BadImageFormatException();
		}
		int count = signatureReader.ReadCompressedInteger();
		SignatureTypeCode signatureTypeCode = signatureReader.ReadSignatureTypeCode();
		if (signatureTypeCode != SignatureTypeCode.Void)
		{
			throw new BadImageFormatException();
		}
		ImmutableArray<CustomAttributeTypedArgument<TType>> fixedArguments = DecodeFixedArguments(ref signatureReader, ref valueReader, count);
		ImmutableArray<CustomAttributeNamedArgument<TType>> namedArguments = DecodeNamedArguments(ref valueReader);
		return new CustomAttributeValue<TType>(fixedArguments, namedArguments);
	}

	private ImmutableArray<CustomAttributeTypedArgument<TType>> DecodeFixedArguments(ref BlobReader signatureReader, ref BlobReader valueReader, int count)
	{
		if (count == 0)
		{
			return ImmutableArray<CustomAttributeTypedArgument<TType>>.Empty;
		}
		ImmutableArray<CustomAttributeTypedArgument<TType>>.Builder builder = ImmutableArray.CreateBuilder<CustomAttributeTypedArgument<TType>>(count);
		for (int i = 0; i < count; i++)
		{
			ArgumentTypeInfo info = DecodeFixedArgumentType(ref signatureReader);
			builder.Add(DecodeArgument(ref valueReader, info));
		}
		return builder.MoveToImmutable();
	}

	private ImmutableArray<CustomAttributeNamedArgument<TType>> DecodeNamedArguments(ref BlobReader valueReader)
	{
		int num = valueReader.ReadUInt16();
		if (num == 0)
		{
			return ImmutableArray<CustomAttributeNamedArgument<TType>>.Empty;
		}
		ImmutableArray<CustomAttributeNamedArgument<TType>>.Builder builder = ImmutableArray.CreateBuilder<CustomAttributeNamedArgument<TType>>(num);
		for (int i = 0; i < num; i++)
		{
			CustomAttributeNamedArgumentKind customAttributeNamedArgumentKind = (CustomAttributeNamedArgumentKind)valueReader.ReadSerializationTypeCode();
			if (customAttributeNamedArgumentKind != CustomAttributeNamedArgumentKind.Field && customAttributeNamedArgumentKind != CustomAttributeNamedArgumentKind.Property)
			{
				throw new BadImageFormatException();
			}
			ArgumentTypeInfo info = DecodeNamedArgumentType(ref valueReader);
			string name = valueReader.ReadSerializedString();
			CustomAttributeTypedArgument<TType> customAttributeTypedArgument = DecodeArgument(ref valueReader, info);
			builder.Add(new CustomAttributeNamedArgument<TType>(name, customAttributeNamedArgumentKind, customAttributeTypedArgument.Type, customAttributeTypedArgument.Value));
		}
		return builder.MoveToImmutable();
	}

	private ArgumentTypeInfo DecodeFixedArgumentType(ref BlobReader signatureReader, bool isElementType = false)
	{
		SignatureTypeCode signatureTypeCode = signatureReader.ReadSignatureTypeCode();
		ArgumentTypeInfo argumentTypeInfo = default(ArgumentTypeInfo);
		argumentTypeInfo.TypeCode = (SerializationTypeCode)signatureTypeCode;
		ArgumentTypeInfo result = argumentTypeInfo;
		switch (signatureTypeCode)
		{
		case SignatureTypeCode.Boolean:
		case SignatureTypeCode.Char:
		case SignatureTypeCode.SByte:
		case SignatureTypeCode.Byte:
		case SignatureTypeCode.Int16:
		case SignatureTypeCode.UInt16:
		case SignatureTypeCode.Int32:
		case SignatureTypeCode.UInt32:
		case SignatureTypeCode.Int64:
		case SignatureTypeCode.UInt64:
		case SignatureTypeCode.Single:
		case SignatureTypeCode.Double:
		case SignatureTypeCode.String:
			result.Type = _provider.GetPrimitiveType((PrimitiveTypeCode)signatureTypeCode);
			break;
		case SignatureTypeCode.Object:
			result.TypeCode = SerializationTypeCode.TaggedObject;
			result.Type = _provider.GetPrimitiveType(PrimitiveTypeCode.Object);
			break;
		case SignatureTypeCode.TypeHandle:
		{
			EntityHandle handle = signatureReader.ReadTypeHandle();
			result.Type = GetTypeFromHandle(handle);
			result.TypeCode = (SerializationTypeCode)(_provider.IsSystemType(result.Type) ? ((PrimitiveTypeCode)80) : _provider.GetUnderlyingEnumType(result.Type));
			break;
		}
		case SignatureTypeCode.SZArray:
		{
			if (isElementType)
			{
				throw new BadImageFormatException();
			}
			ArgumentTypeInfo argumentTypeInfo2 = DecodeFixedArgumentType(ref signatureReader, isElementType: true);
			result.ElementType = argumentTypeInfo2.Type;
			result.ElementTypeCode = argumentTypeInfo2.TypeCode;
			result.Type = _provider.GetSZArrayType(result.ElementType);
			break;
		}
		default:
			throw new BadImageFormatException();
		}
		return result;
	}

	private ArgumentTypeInfo DecodeNamedArgumentType(ref BlobReader valueReader, bool isElementType = false)
	{
		ArgumentTypeInfo argumentTypeInfo = default(ArgumentTypeInfo);
		argumentTypeInfo.TypeCode = valueReader.ReadSerializationTypeCode();
		ArgumentTypeInfo result = argumentTypeInfo;
		switch (result.TypeCode)
		{
		case SerializationTypeCode.Boolean:
		case SerializationTypeCode.Char:
		case SerializationTypeCode.SByte:
		case SerializationTypeCode.Byte:
		case SerializationTypeCode.Int16:
		case SerializationTypeCode.UInt16:
		case SerializationTypeCode.Int32:
		case SerializationTypeCode.UInt32:
		case SerializationTypeCode.Int64:
		case SerializationTypeCode.UInt64:
		case SerializationTypeCode.Single:
		case SerializationTypeCode.Double:
		case SerializationTypeCode.String:
			result.Type = _provider.GetPrimitiveType((PrimitiveTypeCode)result.TypeCode);
			break;
		case SerializationTypeCode.Type:
			result.Type = _provider.GetSystemType();
			break;
		case SerializationTypeCode.TaggedObject:
			result.Type = _provider.GetPrimitiveType(PrimitiveTypeCode.Object);
			break;
		case SerializationTypeCode.SZArray:
		{
			if (isElementType)
			{
				throw new BadImageFormatException();
			}
			ArgumentTypeInfo argumentTypeInfo2 = DecodeNamedArgumentType(ref valueReader, isElementType: true);
			result.ElementType = argumentTypeInfo2.Type;
			result.ElementTypeCode = argumentTypeInfo2.TypeCode;
			result.Type = _provider.GetSZArrayType(result.ElementType);
			break;
		}
		case SerializationTypeCode.Enum:
		{
			string name = valueReader.ReadSerializedString();
			result.Type = _provider.GetTypeFromSerializedName(name);
			result.TypeCode = (SerializationTypeCode)_provider.GetUnderlyingEnumType(result.Type);
			break;
		}
		default:
			throw new BadImageFormatException();
		}
		return result;
	}

	private CustomAttributeTypedArgument<TType> DecodeArgument(ref BlobReader valueReader, ArgumentTypeInfo info)
	{
		if (info.TypeCode == SerializationTypeCode.TaggedObject)
		{
			info = DecodeNamedArgumentType(ref valueReader);
		}
		object value;
		switch (info.TypeCode)
		{
		case SerializationTypeCode.Boolean:
			value = valueReader.ReadBoolean();
			break;
		case SerializationTypeCode.Byte:
			value = valueReader.ReadByte();
			break;
		case SerializationTypeCode.Char:
			value = valueReader.ReadChar();
			break;
		case SerializationTypeCode.Double:
			value = valueReader.ReadDouble();
			break;
		case SerializationTypeCode.Int16:
			value = valueReader.ReadInt16();
			break;
		case SerializationTypeCode.Int32:
			value = valueReader.ReadInt32();
			break;
		case SerializationTypeCode.Int64:
			value = valueReader.ReadInt64();
			break;
		case SerializationTypeCode.SByte:
			value = valueReader.ReadSByte();
			break;
		case SerializationTypeCode.Single:
			value = valueReader.ReadSingle();
			break;
		case SerializationTypeCode.UInt16:
			value = valueReader.ReadUInt16();
			break;
		case SerializationTypeCode.UInt32:
			value = valueReader.ReadUInt32();
			break;
		case SerializationTypeCode.UInt64:
			value = valueReader.ReadUInt64();
			break;
		case SerializationTypeCode.String:
			value = valueReader.ReadSerializedString();
			break;
		case SerializationTypeCode.Type:
		{
			string name = valueReader.ReadSerializedString();
			value = _provider.GetTypeFromSerializedName(name);
			break;
		}
		case SerializationTypeCode.SZArray:
			value = DecodeArrayArgument(ref valueReader, info);
			break;
		default:
			throw new BadImageFormatException();
		}
		return new CustomAttributeTypedArgument<TType>(info.Type, value);
	}

	private ImmutableArray<CustomAttributeTypedArgument<TType>>? DecodeArrayArgument(ref BlobReader blobReader, ArgumentTypeInfo info)
	{
		int num = blobReader.ReadInt32();
		if (num == -1)
		{
			return null;
		}
		if (num == 0)
		{
			return ImmutableArray<CustomAttributeTypedArgument<TType>>.Empty;
		}
		if (num < 0)
		{
			throw new BadImageFormatException();
		}
		ArgumentTypeInfo argumentTypeInfo = default(ArgumentTypeInfo);
		argumentTypeInfo.Type = info.ElementType;
		argumentTypeInfo.TypeCode = info.ElementTypeCode;
		ArgumentTypeInfo info2 = argumentTypeInfo;
		ImmutableArray<CustomAttributeTypedArgument<TType>>.Builder builder = ImmutableArray.CreateBuilder<CustomAttributeTypedArgument<TType>>(num);
		for (int i = 0; i < num; i++)
		{
			builder.Add(DecodeArgument(ref blobReader, info2));
		}
		return builder.MoveToImmutable();
	}

	private TType GetTypeFromHandle(EntityHandle handle)
	{
		return handle.Kind switch
		{
			HandleKind.TypeDefinition => _provider.GetTypeFromDefinition(_reader, (TypeDefinitionHandle)handle, 0), 
			HandleKind.TypeReference => _provider.GetTypeFromReference(_reader, (TypeReferenceHandle)handle, 0), 
			_ => throw new BadImageFormatException(System.SR.NotTypeDefOrRefHandle), 
		};
	}
}
