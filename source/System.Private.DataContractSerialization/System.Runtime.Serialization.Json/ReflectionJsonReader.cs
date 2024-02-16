using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal sealed class ReflectionJsonReader : ReflectionReader
{
	private enum KeyParseMode
	{
		Fail,
		AsString,
		UsingParseEnum,
		UsingCustomParse
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected override void ReflectionReadMembers(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, XmlDictionaryString[] memberNames, XmlDictionaryString[] memberNamespaces, ClassDataContract classContract, ref object obj)
	{
		XmlObjectSerializerReadContextComplexJson xmlObjectSerializerReadContextComplexJson = context as XmlObjectSerializerReadContextComplexJson;
		int num = classContract.MemberNames.Length;
		context.IncrementItemCount(num);
		DataMember[] array = new DataMember[num];
		ReflectionGetMembers(classContract, array);
		int num2 = -1;
		ExtensionDataObject extensionData = null;
		if (classContract.HasExtensionData)
		{
			extensionData = new ExtensionDataObject();
			((IExtensibleDataObject)obj).ExtensionData = extensionData;
		}
		while (XmlObjectSerializerReadContext.MoveToNextElement(xmlReader))
		{
			num2 = xmlObjectSerializerReadContextComplexJson.GetJsonMemberIndex(xmlReader, memberNames, num2, extensionData);
			if (num2 < array.Length)
			{
				ReflectionReadMember(xmlReader, context, classContract, ref obj, num2, array);
			}
		}
	}

	protected override string GetClassContractNamespace(ClassDataContract classContract)
	{
		return string.Empty;
	}

	protected override string GetCollectionContractItemName(CollectionDataContract collectionContract)
	{
		return "item";
	}

	protected override string GetCollectionContractNamespace(CollectionDataContract collectionContract)
	{
		return string.Empty;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected override object ReflectionReadDictionaryItem(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, CollectionDataContract collectionContract)
	{
		XmlObjectSerializerReadContextComplexJson context2 = context as XmlObjectSerializerReadContextComplexJson;
		context.ReadAttributes(xmlReader);
		DataContract revisedItemContract = XmlObjectSerializerWriteContextComplexJson.GetRevisedItemContract(collectionContract.ItemContract);
		return DataContractJsonSerializer.ReadJsonValue(revisedItemContract, xmlReader, context2);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected override bool ReflectionReadSpecialCollection(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, CollectionDataContract collectionContract, object resultCollection)
	{
		XmlObjectSerializerReadContextComplexJson xmlObjectSerializerReadContextComplexJson = context as XmlObjectSerializerReadContextComplexJson;
		if ((collectionContract.Kind == CollectionKind.Dictionary || collectionContract.Kind == CollectionKind.GenericDictionary) && xmlObjectSerializerReadContextComplexJson.UseSimpleDictionaryFormat)
		{
			ReadSimpleDictionary(xmlReader, context, collectionContract, collectionContract.ItemType, resultCollection);
		}
		return false;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void ReadSimpleDictionary(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context, CollectionDataContract collectionContract, Type keyValueType, object dictionary)
	{
		Type[] genericArguments = keyValueType.GetGenericArguments();
		Type type = genericArguments[0];
		Type type2 = genericArguments[1];
		int num = 0;
		while (type.IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfNullable)
		{
			num++;
			type = type.GetGenericArguments()[0];
		}
		ClassDataContract classDataContract = (ClassDataContract)collectionContract.ItemContract;
		DataContract memberTypeContract = classDataContract.Members[0].MemberTypeContract;
		KeyParseMode keyParseMode = KeyParseMode.Fail;
		if (type == Globals.TypeOfString || type == Globals.TypeOfObject)
		{
			keyParseMode = KeyParseMode.AsString;
		}
		else if (type.IsEnum)
		{
			keyParseMode = KeyParseMode.UsingParseEnum;
		}
		else if (memberTypeContract.ParseMethod != null)
		{
			keyParseMode = KeyParseMode.UsingCustomParse;
		}
		if (keyParseMode == KeyParseMode.Fail)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(System.SR.Format(System.SR.KeyTypeCannotBeParsedInSimpleDictionary, DataContract.GetClrTypeFullName(collectionContract.UnderlyingType), DataContract.GetClrTypeFullName(type))));
		}
		while (true)
		{
			switch (xmlReader.MoveToContent())
			{
			case XmlNodeType.EndElement:
				return;
			default:
				throw XmlObjectSerializerReadContext.CreateUnexpectedStateException(XmlNodeType.Element, xmlReader);
			case XmlNodeType.Element:
			{
				context.IncrementItemCount(1);
				string jsonMemberName = XmlObjectSerializerReadContextComplexJson.GetJsonMemberName(xmlReader);
				object key = keyParseMode switch
				{
					KeyParseMode.UsingParseEnum => Enum.Parse(type, jsonMemberName), 
					KeyParseMode.UsingCustomParse => Type.GetTypeCode(memberTypeContract.UnderlyingType) switch
					{
						TypeCode.Boolean => bool.Parse(jsonMemberName), 
						TypeCode.Int16 => short.Parse(jsonMemberName), 
						TypeCode.Int32 => int.Parse(jsonMemberName), 
						TypeCode.Int64 => long.Parse(jsonMemberName), 
						TypeCode.Char => char.Parse(jsonMemberName), 
						TypeCode.Byte => byte.Parse(jsonMemberName), 
						TypeCode.SByte => sbyte.Parse(jsonMemberName), 
						TypeCode.Double => double.Parse(jsonMemberName), 
						TypeCode.Decimal => decimal.Parse(jsonMemberName), 
						TypeCode.Single => float.Parse(jsonMemberName), 
						TypeCode.UInt16 => ushort.Parse(jsonMemberName), 
						TypeCode.UInt32 => uint.Parse(jsonMemberName), 
						TypeCode.UInt64 => ulong.Parse(jsonMemberName), 
						_ => memberTypeContract.ParseMethod.Invoke(null, new object[1] { jsonMemberName }), 
					}, 
					_ => jsonMemberName, 
				};
				if (num > 0)
				{
					throw new NotImplementedException(System.SR.Format(System.SR.MustBeGreaterThanZero, num));
				}
				object value = ReflectionReadValue(xmlReader, context, type2, string.Empty, string.Empty);
				((IDictionary)dictionary).Add(key, value);
				break;
			}
			}
		}
	}
}
