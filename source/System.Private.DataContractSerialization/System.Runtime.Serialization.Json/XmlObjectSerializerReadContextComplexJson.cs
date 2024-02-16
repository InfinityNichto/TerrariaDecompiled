using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal sealed class XmlObjectSerializerReadContextComplexJson : XmlObjectSerializerReadContextComplex
{
	private string _extensionDataValueType;

	private readonly DateTimeFormat _dateTimeFormat;

	private readonly bool _useSimpleDictionaryFormat;

	internal IList<Type> SerializerKnownTypeList => serializerKnownTypeList;

	public bool UseSimpleDictionaryFormat => _useSimpleDictionaryFormat;

	internal XmlObjectSerializerReadContextComplexJson(DataContractJsonSerializer serializer, DataContract rootTypeDataContract)
		: base(serializer, serializer.MaxItemsInObjectGraph, new StreamingContext(StreamingContextStates.All), serializer.IgnoreExtensionDataObject)
	{
		base.rootTypeDataContract = rootTypeDataContract;
		serializerKnownTypeList = serializer.knownTypeList;
		_dateTimeFormat = serializer.DateTimeFormat;
		_useSimpleDictionaryFormat = serializer.UseSimpleDictionaryFormat;
	}

	internal static XmlObjectSerializerReadContextComplexJson CreateContext(DataContractJsonSerializer serializer, DataContract rootTypeDataContract)
	{
		return new XmlObjectSerializerReadContextComplexJson(serializer, rootTypeDataContract);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected override object ReadDataContractValue(DataContract dataContract, XmlReaderDelegator reader)
	{
		return DataContractJsonSerializer.ReadJsonValue(dataContract, reader, this);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public int GetJsonMemberIndex(XmlReaderDelegator xmlReader, XmlDictionaryString[] memberNames, int memberIndex, ExtensionDataObject extensionData)
	{
		int num = memberNames.Length;
		if (num != 0)
		{
			int num2 = 0;
			int num3 = (memberIndex + 1) % num;
			while (num2 < num)
			{
				if (xmlReader.IsStartElement(memberNames[num3], XmlDictionaryString.Empty))
				{
					return num3;
				}
				num2++;
				num3 = (num3 + 1) % num;
			}
			if (TryGetJsonLocalName(xmlReader, out var name))
			{
				int num4 = 0;
				int num5 = (memberIndex + 1) % num;
				while (num4 < num)
				{
					if (memberNames[num5].Value == name)
					{
						return num5;
					}
					num4++;
					num5 = (num5 + 1) % num;
				}
			}
		}
		HandleMemberNotFound(xmlReader, extensionData, memberIndex);
		return num;
	}

	protected override void StartReadExtensionDataValue(XmlReaderDelegator xmlReader)
	{
		_extensionDataValueType = xmlReader.GetAttribute("type");
	}

	protected override IDataNode ReadPrimitiveExtensionDataValue(XmlReaderDelegator xmlReader, string dataContractName, string dataContractNamespace)
	{
		IDataNode result;
		switch (_extensionDataValueType)
		{
		case null:
		case "string":
			result = new DataNode<string>(xmlReader.ReadContentAsString());
			break;
		case "boolean":
			result = new DataNode<bool>(xmlReader.ReadContentAsBoolean());
			break;
		case "number":
			result = ReadNumericalPrimitiveExtensionDataValue(xmlReader);
			break;
		default:
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.Format(System.SR.JsonUnexpectedAttributeValue, _extensionDataValueType)));
		}
		xmlReader.ReadEndElement();
		return result;
	}

	private IDataNode ReadNumericalPrimitiveExtensionDataValue(XmlReaderDelegator xmlReader)
	{
		TypeCode objectTypeCode;
		object obj = JsonObjectDataContract.ParseJsonNumber(xmlReader.ReadContentAsString(), out objectTypeCode);
		return objectTypeCode switch
		{
			TypeCode.Byte => new DataNode<byte>((byte)obj), 
			TypeCode.SByte => new DataNode<sbyte>((sbyte)obj), 
			TypeCode.Int16 => new DataNode<short>((short)obj), 
			TypeCode.Int32 => new DataNode<int>((int)obj), 
			TypeCode.Int64 => new DataNode<long>((long)obj), 
			TypeCode.UInt16 => new DataNode<ushort>((ushort)obj), 
			TypeCode.UInt32 => new DataNode<uint>((uint)obj), 
			TypeCode.UInt64 => new DataNode<ulong>((ulong)obj), 
			TypeCode.Single => new DataNode<float>((float)obj), 
			TypeCode.Double => new DataNode<double>((double)obj), 
			TypeCode.Decimal => new DataNode<decimal>((decimal)obj), 
			_ => throw new InvalidOperationException(System.SR.ParseJsonNumberReturnInvalidNumber), 
		};
	}

	internal override int GetArraySize()
	{
		return -1;
	}

	internal override void ReadAttributes(XmlReaderDelegator xmlReader)
	{
		if (attributes == null)
		{
			attributes = new Attributes();
		}
		attributes.Reset();
		if (xmlReader.MoveToAttribute("type") && xmlReader.Value == "null")
		{
			attributes.XsiNil = true;
		}
		else if (xmlReader.MoveToAttribute("__type"))
		{
			XmlQualifiedName xmlQualifiedName = JsonReaderDelegator.ParseQualifiedName(xmlReader.Value);
			attributes.XsiTypeName = xmlQualifiedName.Name;
			string text = xmlQualifiedName.Namespace;
			if (!string.IsNullOrEmpty(text))
			{
				switch (text[0])
				{
				case '#':
					text = "http://schemas.datacontract.org/2004/07/" + text.AsSpan(1);
					break;
				case '\\':
					if (text.Length >= 2)
					{
						char c = text[1];
						if (c == '#' || c == '\\')
						{
							text = text.Substring(1);
						}
					}
					break;
				}
			}
			attributes.XsiTypeNamespace = text;
		}
		xmlReader.MoveToElement();
	}

	internal string TrimNamespace(string serverTypeNamespace)
	{
		if (!string.IsNullOrEmpty(serverTypeNamespace))
		{
			switch (serverTypeNamespace[0])
			{
			case '#':
				serverTypeNamespace = "http://schemas.datacontract.org/2004/07/" + serverTypeNamespace.AsSpan(1);
				break;
			case '\\':
				if (serverTypeNamespace.Length >= 2)
				{
					char c = serverTypeNamespace[1];
					if (c == '#' || c == '\\')
					{
						serverTypeNamespace = serverTypeNamespace.Substring(1);
					}
				}
				break;
			}
		}
		return serverTypeNamespace;
	}

	internal static XmlQualifiedName ParseQualifiedName(string qname)
	{
		string name;
		string ns;
		if (string.IsNullOrEmpty(qname))
		{
			name = (ns = string.Empty);
		}
		else
		{
			qname = qname.Trim();
			int num = qname.IndexOf(':');
			if (num >= 0)
			{
				name = qname.Substring(0, num);
				ns = qname.Substring(num + 1);
			}
			else
			{
				name = qname;
				ns = string.Empty;
			}
		}
		return new XmlQualifiedName(name, ns);
	}

	protected override bool IsReadingCollectionExtensionData(XmlReaderDelegator xmlReader)
	{
		return xmlReader.GetAttribute("type") == "array";
	}

	protected override bool IsReadingClassExtensionData(XmlReaderDelegator xmlReader)
	{
		return xmlReader.GetAttribute("type") == "object";
	}

	protected override XmlReaderDelegator CreateReaderDelegatorForReader(XmlReader xmlReader)
	{
		return new JsonReaderDelegator(xmlReader, _dateTimeFormat);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override DataContract GetDataContract(RuntimeTypeHandle typeHandle, Type type)
	{
		DataContract dataContract = base.GetDataContract(typeHandle, type);
		DataContractJsonSerializer.CheckIfTypeIsReference(dataContract);
		return dataContract;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override DataContract GetDataContractSkipValidation(int typeId, RuntimeTypeHandle typeHandle, Type type)
	{
		DataContract dataContractSkipValidation = base.GetDataContractSkipValidation(typeId, typeHandle, type);
		DataContractJsonSerializer.CheckIfTypeIsReference(dataContractSkipValidation);
		return dataContractSkipValidation;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal override DataContract GetDataContract(int id, RuntimeTypeHandle typeHandle)
	{
		DataContract dataContract = base.GetDataContract(id, typeHandle);
		DataContractJsonSerializer.CheckIfTypeIsReference(dataContract);
		return dataContract;
	}

	internal static bool TryGetJsonLocalName(XmlReaderDelegator xmlReader, [NotNullWhen(true)] out string name)
	{
		if (xmlReader.IsStartElement(JsonGlobals.itemDictionaryString, JsonGlobals.itemDictionaryString) && xmlReader.MoveToAttribute("item"))
		{
			name = xmlReader.Value;
			return true;
		}
		name = null;
		return false;
	}

	public static string GetJsonMemberName(XmlReaderDelegator xmlReader)
	{
		if (!TryGetJsonLocalName(xmlReader, out var name))
		{
			return xmlReader.LocalName;
		}
		return name;
	}

	public static void ThrowDuplicateMemberException(object obj, XmlDictionaryString[] memberNames, int memberIndex)
	{
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SerializationException(System.SR.Format(System.SR.JsonDuplicateMemberInInput, DataContract.GetClrTypeFullName(obj.GetType()), memberNames[memberIndex])));
	}

	public static void ThrowMissingRequiredMembers(object obj, XmlDictionaryString[] memberNames, byte[] expectedElements, byte[] requiredElements)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		for (int i = 0; i < memberNames.Length; i++)
		{
			if (IsBitSet(expectedElements, i) && IsBitSet(requiredElements, i))
			{
				if (stringBuilder.Length != 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(memberNames[i]);
				num++;
			}
		}
		if (num == 1)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SerializationException(System.SR.Format(System.SR.JsonOneRequiredMemberNotFound, DataContract.GetClrTypeFullName(obj.GetType()), stringBuilder.ToString())));
		}
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SerializationException(System.SR.Format(System.SR.JsonRequiredMembersNotFound, DataContract.GetClrTypeFullName(obj.GetType()), stringBuilder.ToString())));
	}

	private static bool IsBitSet(byte[] bytes, int bitIndex)
	{
		return BitFlagsGenerator.IsBitSet(bytes, bitIndex);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected override DataContract ResolveDataContractFromRootDataContract(XmlQualifiedName typeQName)
	{
		return XmlObjectSerializerWriteContextComplexJson.ResolveJsonDataContractFromRootDataContract(this, typeQName, rootTypeDataContract);
	}
}
