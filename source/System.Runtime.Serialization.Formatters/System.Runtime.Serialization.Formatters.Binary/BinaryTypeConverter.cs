using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization.Formatters.Binary;

internal static class BinaryTypeConverter
{
	internal static BinaryTypeEnum GetBinaryTypeInfo(Type type, WriteObjectInfo objectInfo, string typeName, ObjectWriter objectWriter, out object typeInformation, out int assemId)
	{
		assemId = 0;
		typeInformation = null;
		BinaryTypeEnum result;
		if ((object)type == Converter.s_typeofString)
		{
			result = BinaryTypeEnum.String;
		}
		else if ((objectInfo == null || (objectInfo != null && !objectInfo._isSi)) && (object)type == Converter.s_typeofObject)
		{
			result = BinaryTypeEnum.Object;
		}
		else if ((object)type == Converter.s_typeofStringArray)
		{
			result = BinaryTypeEnum.StringArray;
		}
		else if ((object)type == Converter.s_typeofObjectArray)
		{
			result = BinaryTypeEnum.ObjectArray;
		}
		else if (Converter.IsPrimitiveArray(type, out typeInformation))
		{
			result = BinaryTypeEnum.PrimitiveArray;
		}
		else
		{
			InternalPrimitiveTypeE internalPrimitiveTypeE = objectWriter.ToCode(type);
			if (internalPrimitiveTypeE == InternalPrimitiveTypeE.Invalid)
			{
				string text = null;
				if (objectInfo == null)
				{
					text = type.Assembly.FullName;
					typeInformation = type.FullName;
				}
				else
				{
					text = objectInfo.GetAssemblyString();
					typeInformation = objectInfo.GetTypeFullName();
				}
				if (text.Equals(Converter.s_urtAssemblyString) || text.Equals(Converter.s_urtAlternativeAssemblyString))
				{
					result = BinaryTypeEnum.ObjectUrt;
					assemId = 0;
				}
				else
				{
					result = BinaryTypeEnum.ObjectUser;
					assemId = (int)objectInfo._assemId;
					if (assemId == 0)
					{
						throw new SerializationException(System.SR.Format(System.SR.Serialization_AssemblyId, typeInformation));
					}
				}
			}
			else
			{
				result = BinaryTypeEnum.Primitive;
				typeInformation = internalPrimitiveTypeE;
			}
		}
		return result;
	}

	internal static BinaryTypeEnum GetParserBinaryTypeInfo(Type type, out object typeInformation)
	{
		typeInformation = null;
		BinaryTypeEnum result;
		if ((object)type == Converter.s_typeofString)
		{
			result = BinaryTypeEnum.String;
		}
		else if ((object)type == Converter.s_typeofObject)
		{
			result = BinaryTypeEnum.Object;
		}
		else if ((object)type == Converter.s_typeofObjectArray)
		{
			result = BinaryTypeEnum.ObjectArray;
		}
		else if ((object)type == Converter.s_typeofStringArray)
		{
			result = BinaryTypeEnum.StringArray;
		}
		else if (Converter.IsPrimitiveArray(type, out typeInformation))
		{
			result = BinaryTypeEnum.PrimitiveArray;
		}
		else
		{
			InternalPrimitiveTypeE internalPrimitiveTypeE = Converter.ToCode(type);
			if (internalPrimitiveTypeE == InternalPrimitiveTypeE.Invalid)
			{
				result = ((type.Assembly == Converter.s_urtAssembly) ? BinaryTypeEnum.ObjectUrt : BinaryTypeEnum.ObjectUser);
				typeInformation = type.FullName;
			}
			else
			{
				result = BinaryTypeEnum.Primitive;
				typeInformation = internalPrimitiveTypeE;
			}
		}
		return result;
	}

	internal static void WriteTypeInfo(BinaryTypeEnum binaryTypeEnum, object typeInformation, int assemId, BinaryFormatterWriter output)
	{
		switch (binaryTypeEnum)
		{
		case BinaryTypeEnum.Primitive:
		case BinaryTypeEnum.PrimitiveArray:
			output.WriteByte((byte)(InternalPrimitiveTypeE)typeInformation);
			break;
		case BinaryTypeEnum.ObjectUrt:
			output.WriteString(typeInformation.ToString());
			break;
		case BinaryTypeEnum.ObjectUser:
			output.WriteString(typeInformation.ToString());
			output.WriteInt32(assemId);
			break;
		default:
			throw new SerializationException(System.SR.Format(System.SR.Serialization_TypeWrite, binaryTypeEnum.ToString()));
		case BinaryTypeEnum.String:
		case BinaryTypeEnum.Object:
		case BinaryTypeEnum.ObjectArray:
		case BinaryTypeEnum.StringArray:
			break;
		}
	}

	internal static object ReadTypeInfo(BinaryTypeEnum binaryTypeEnum, BinaryParser input, out int assemId)
	{
		object result = null;
		int num = 0;
		switch (binaryTypeEnum)
		{
		case BinaryTypeEnum.Primitive:
		case BinaryTypeEnum.PrimitiveArray:
			result = (InternalPrimitiveTypeE)input.ReadByte();
			break;
		case BinaryTypeEnum.ObjectUrt:
			result = input.ReadString();
			break;
		case BinaryTypeEnum.ObjectUser:
			result = input.ReadString();
			num = input.ReadInt32();
			break;
		default:
			throw new SerializationException(System.SR.Format(System.SR.Serialization_TypeRead, binaryTypeEnum.ToString()));
		case BinaryTypeEnum.String:
		case BinaryTypeEnum.Object:
		case BinaryTypeEnum.ObjectArray:
		case BinaryTypeEnum.StringArray:
			break;
		}
		assemId = num;
		return result;
	}

	[RequiresUnreferencedCode("Types might be removed")]
	internal static void TypeFromInfo(BinaryTypeEnum binaryTypeEnum, object typeInformation, ObjectReader objectReader, BinaryAssemblyInfo assemblyInfo, out InternalPrimitiveTypeE primitiveTypeEnum, out string typeString, out Type type, out bool isVariant)
	{
		isVariant = false;
		primitiveTypeEnum = InternalPrimitiveTypeE.Invalid;
		typeString = null;
		type = null;
		switch (binaryTypeEnum)
		{
		case BinaryTypeEnum.Primitive:
			primitiveTypeEnum = (InternalPrimitiveTypeE)typeInformation;
			typeString = Converter.ToComType(primitiveTypeEnum);
			type = Converter.ToType(primitiveTypeEnum);
			break;
		case BinaryTypeEnum.String:
			type = Converter.s_typeofString;
			break;
		case BinaryTypeEnum.Object:
			type = Converter.s_typeofObject;
			isVariant = true;
			break;
		case BinaryTypeEnum.ObjectArray:
			type = Converter.s_typeofObjectArray;
			break;
		case BinaryTypeEnum.StringArray:
			type = Converter.s_typeofStringArray;
			break;
		case BinaryTypeEnum.PrimitiveArray:
			primitiveTypeEnum = (InternalPrimitiveTypeE)typeInformation;
			type = Converter.ToArrayType(primitiveTypeEnum);
			break;
		case BinaryTypeEnum.ObjectUrt:
		case BinaryTypeEnum.ObjectUser:
			if (typeInformation != null)
			{
				typeString = typeInformation.ToString();
				type = objectReader.GetType(assemblyInfo, typeString);
				if ((object)type == Converter.s_typeofObject)
				{
					isVariant = true;
				}
			}
			break;
		default:
			throw new SerializationException(System.SR.Format(System.SR.Serialization_TypeRead, binaryTypeEnum.ToString()));
		}
	}
}
