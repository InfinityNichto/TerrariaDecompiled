using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal sealed class ReflectionJsonFormatWriter
{
	private readonly ReflectionJsonClassWriter _reflectionClassWriter = new ReflectionJsonClassWriter();

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public void ReflectionWriteClass(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContextComplexJson context, ClassDataContract classContract, XmlDictionaryString[] memberNames)
	{
		_reflectionClassWriter.ReflectionWriteClass(xmlWriter, obj, context, classContract, memberNames);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public void ReflectionWriteCollection(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContextComplexJson context, CollectionDataContract collectionContract)
	{
		if (!(xmlWriter is JsonWriterDelegator jsonWriterDelegator))
		{
			throw new ArgumentException("xmlWriter");
		}
		XmlDictionaryString collectionItemName = context.CollectionItemName;
		if (collectionContract.Kind == CollectionKind.Array)
		{
			context.IncrementArrayCount(jsonWriterDelegator, (Array)obj);
			Type itemType = collectionContract.ItemType;
			if (!ReflectionTryWritePrimitiveArray(jsonWriterDelegator, obj, collectionContract.UnderlyingType, itemType, collectionItemName))
			{
				ReflectionWriteArrayAttribute(jsonWriterDelegator);
				Array array = (Array)obj;
				PrimitiveDataContract primitiveDataContract = PrimitiveDataContract.GetPrimitiveDataContract(itemType);
				for (int i = 0; i < array.Length; i++)
				{
					_reflectionClassWriter.ReflectionWriteStartElement(jsonWriterDelegator, collectionItemName);
					_reflectionClassWriter.ReflectionWriteValue(jsonWriterDelegator, context, itemType, array.GetValue(i), writeXsiType: false, primitiveDataContract);
					_reflectionClassWriter.ReflectionWriteEndElement(jsonWriterDelegator);
				}
			}
			return;
		}
		collectionContract.IncrementCollectionCount(jsonWriterDelegator, obj, context);
		IEnumerator enumeratorForCollection = collectionContract.GetEnumeratorForCollection(obj);
		bool flag = collectionContract.Kind == CollectionKind.GenericDictionary || collectionContract.Kind == CollectionKind.Dictionary;
		bool useSimpleDictionaryFormat = context.UseSimpleDictionaryFormat;
		if (flag && useSimpleDictionaryFormat)
		{
			ReflectionWriteObjectAttribute(jsonWriterDelegator);
			Type[] genericArguments = collectionContract.ItemType.GetGenericArguments();
			Type type = ((genericArguments.Length == 2) ? genericArguments[1] : null);
			while (enumeratorForCollection.MoveNext())
			{
				object current = enumeratorForCollection.Current;
				object key = ((IKeyValue)current).Key;
				object value = ((IKeyValue)current).Value;
				_reflectionClassWriter.ReflectionWriteStartElement(jsonWriterDelegator, key.ToString());
				_reflectionClassWriter.ReflectionWriteValue(jsonWriterDelegator, context, type ?? value.GetType(), value, writeXsiType: false, null);
				_reflectionClassWriter.ReflectionWriteEndElement(jsonWriterDelegator);
			}
			return;
		}
		ReflectionWriteArrayAttribute(jsonWriterDelegator);
		PrimitiveDataContract primitiveDataContract2 = PrimitiveDataContract.GetPrimitiveDataContract(collectionContract.UnderlyingType);
		if (primitiveDataContract2 != null && primitiveDataContract2.UnderlyingType != Globals.TypeOfObject)
		{
			while (enumeratorForCollection.MoveNext())
			{
				object current2 = enumeratorForCollection.Current;
				context.IncrementItemCount(1);
				primitiveDataContract2.WriteXmlElement(jsonWriterDelegator, current2, context, collectionItemName, null);
			}
			return;
		}
		Type collectionElementType = collectionContract.GetCollectionElementType();
		bool flag2 = collectionContract.Kind == CollectionKind.Dictionary || collectionContract.Kind == CollectionKind.GenericDictionary;
		DataContract dataContract = null;
		JsonDataContract jsonDataContract = null;
		if (flag2)
		{
			dataContract = XmlObjectSerializerWriteContextComplexJson.GetRevisedItemContract(collectionContract.ItemContract);
			jsonDataContract = JsonDataContract.GetJsonDataContract(dataContract);
		}
		while (enumeratorForCollection.MoveNext())
		{
			object current3 = enumeratorForCollection.Current;
			context.IncrementItemCount(1);
			_reflectionClassWriter.ReflectionWriteStartElement(jsonWriterDelegator, collectionItemName);
			if (flag2)
			{
				jsonDataContract.WriteJsonValue(jsonWriterDelegator, current3, context, collectionContract.ItemType.TypeHandle);
			}
			else
			{
				_reflectionClassWriter.ReflectionWriteValue(jsonWriterDelegator, context, collectionElementType, current3, writeXsiType: false, null);
			}
			_reflectionClassWriter.ReflectionWriteEndElement(jsonWriterDelegator);
		}
	}

	private void ReflectionWriteObjectAttribute(XmlWriterDelegator xmlWriter)
	{
		xmlWriter.WriteAttributeString(null, "type", null, "object");
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private bool ReflectionTryWritePrimitiveArray(JsonWriterDelegator jsonWriter, object obj, Type underlyingType, Type itemType, XmlDictionaryString collectionItemName)
	{
		PrimitiveDataContract primitiveDataContract = PrimitiveDataContract.GetPrimitiveDataContract(itemType);
		if (primitiveDataContract == null)
		{
			return false;
		}
		XmlDictionaryString itemNamespace = null;
		switch (itemType.GetTypeCode())
		{
		case TypeCode.Boolean:
			ReflectionWriteArrayAttribute(jsonWriter);
			jsonWriter.WriteJsonBooleanArray((bool[])obj, collectionItemName, itemNamespace);
			break;
		case TypeCode.DateTime:
			ReflectionWriteArrayAttribute(jsonWriter);
			jsonWriter.WriteJsonDateTimeArray((DateTime[])obj, collectionItemName, itemNamespace);
			break;
		case TypeCode.Decimal:
			ReflectionWriteArrayAttribute(jsonWriter);
			jsonWriter.WriteJsonDecimalArray((decimal[])obj, collectionItemName, itemNamespace);
			break;
		case TypeCode.Int32:
			ReflectionWriteArrayAttribute(jsonWriter);
			jsonWriter.WriteJsonInt32Array((int[])obj, collectionItemName, itemNamespace);
			break;
		case TypeCode.Int64:
			ReflectionWriteArrayAttribute(jsonWriter);
			jsonWriter.WriteJsonInt64Array((long[])obj, collectionItemName, itemNamespace);
			break;
		case TypeCode.Single:
			ReflectionWriteArrayAttribute(jsonWriter);
			jsonWriter.WriteJsonSingleArray((float[])obj, collectionItemName, itemNamespace);
			break;
		case TypeCode.Double:
			ReflectionWriteArrayAttribute(jsonWriter);
			jsonWriter.WriteJsonDoubleArray((double[])obj, collectionItemName, itemNamespace);
			break;
		default:
			return false;
		}
		return true;
	}

	private void ReflectionWriteArrayAttribute(XmlWriterDelegator xmlWriter)
	{
		xmlWriter.WriteAttributeString(null, "type", string.Empty, "array");
	}
}
