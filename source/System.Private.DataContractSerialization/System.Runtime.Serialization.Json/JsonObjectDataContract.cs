using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal sealed class JsonObjectDataContract : JsonDataContract
{
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public JsonObjectDataContract(DataContract traditionalDataContract)
		: base(traditionalDataContract)
	{
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override object ReadJsonValueCore(XmlReaderDelegator jsonReader, XmlObjectSerializerReadContextComplexJson context)
	{
		string attribute = jsonReader.GetAttribute("type");
		object obj;
		switch (attribute)
		{
		case "null":
			jsonReader.Skip();
			obj = null;
			break;
		case "boolean":
			obj = jsonReader.ReadElementContentAsBoolean();
			break;
		case "string":
		case null:
			obj = jsonReader.ReadElementContentAsString();
			break;
		case "number":
			obj = ParseJsonNumber(jsonReader.ReadElementContentAsString());
			break;
		case "object":
			jsonReader.Skip();
			obj = new object();
			break;
		case "array":
			return DataContractJsonSerializer.ReadJsonValue(DataContract.GetDataContract(Globals.TypeOfObjectArray), jsonReader, context);
		default:
			throw XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.JsonUnexpectedAttributeValue, attribute));
		}
		context?.AddNewObject(obj);
		return obj;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteJsonValueCore(XmlWriterDelegator jsonWriter, object obj, XmlObjectSerializerWriteContextComplexJson context, RuntimeTypeHandle declaredTypeHandle)
	{
		jsonWriter.WriteAttributeString(null, "type", null, "object");
	}

	internal static object ParseJsonNumber(string value, out TypeCode objectTypeCode)
	{
		if (value == null)
		{
			throw new XmlException(System.SR.Format(System.SR.XmlInvalidConversion, value, Globals.TypeOfInt));
		}
		if (value.IndexOfAny(JsonGlobals.FloatingPointCharacters) == -1)
		{
			if (int.TryParse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var result))
			{
				objectTypeCode = TypeCode.Int32;
				return result;
			}
			if (long.TryParse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var result2))
			{
				objectTypeCode = TypeCode.Int64;
				return result2;
			}
		}
		if (decimal.TryParse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var result3))
		{
			objectTypeCode = TypeCode.Decimal;
			if (result3 == 0m)
			{
				double num = XmlConverter.ToDouble(value);
				if (num != 0.0)
				{
					objectTypeCode = TypeCode.Double;
					return num;
				}
			}
			return result3;
		}
		objectTypeCode = TypeCode.Double;
		return XmlConverter.ToDouble(value);
	}

	private static object ParseJsonNumber(string value)
	{
		TypeCode objectTypeCode;
		return ParseJsonNumber(value, out objectTypeCode);
	}
}
