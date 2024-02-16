using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization;

internal sealed class ReflectionXmlFormatWriter
{
	private readonly ReflectionXmlClassWriter _reflectionClassWriter = new ReflectionXmlClassWriter();

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public void ReflectionWriteClass(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, ClassDataContract classContract)
	{
		_reflectionClassWriter.ReflectionWriteClass(xmlWriter, obj, context, classContract, null);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public void ReflectionWriteCollection(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, CollectionDataContract collectionDataContract)
	{
		XmlDictionaryString @namespace = collectionDataContract.Namespace;
		XmlDictionaryString collectionItemName = collectionDataContract.CollectionItemName;
		if (collectionDataContract.ChildElementNamespace != null)
		{
			xmlWriter.WriteNamespaceDecl(collectionDataContract.ChildElementNamespace);
		}
		if (collectionDataContract.Kind == CollectionKind.Array)
		{
			context.IncrementArrayCount(xmlWriter, (Array)obj);
			Type itemType = collectionDataContract.ItemType;
			if (!ReflectionTryWritePrimitiveArray(xmlWriter, obj, collectionDataContract.UnderlyingType, itemType, collectionItemName, @namespace))
			{
				Array array = (Array)obj;
				PrimitiveDataContract primitiveDataContract = PrimitiveDataContract.GetPrimitiveDataContract(itemType);
				for (int i = 0; i < array.Length; i++)
				{
					_reflectionClassWriter.ReflectionWriteStartElement(xmlWriter, itemType, @namespace, @namespace.Value, collectionItemName.Value, 0);
					_reflectionClassWriter.ReflectionWriteValue(xmlWriter, context, itemType, array.GetValue(i), writeXsiType: false, primitiveDataContract);
					_reflectionClassWriter.ReflectionWriteEndElement(xmlWriter);
				}
			}
			return;
		}
		collectionDataContract.IncrementCollectionCount(xmlWriter, obj, context);
		IEnumerator enumeratorForCollection = collectionDataContract.GetEnumeratorForCollection(obj);
		PrimitiveDataContract primitiveDataContract2 = PrimitiveDataContract.GetPrimitiveDataContract(collectionDataContract.UnderlyingType);
		if (primitiveDataContract2 != null && primitiveDataContract2.UnderlyingType != Globals.TypeOfObject)
		{
			while (enumeratorForCollection.MoveNext())
			{
				object current = enumeratorForCollection.Current;
				context.IncrementItemCount(1);
				primitiveDataContract2.WriteXmlElement(xmlWriter, current, context, collectionItemName, @namespace);
			}
			return;
		}
		Type collectionElementType = collectionDataContract.GetCollectionElementType();
		bool flag = collectionDataContract.Kind == CollectionKind.Dictionary || collectionDataContract.Kind == CollectionKind.GenericDictionary;
		while (enumeratorForCollection.MoveNext())
		{
			object current2 = enumeratorForCollection.Current;
			context.IncrementItemCount(1);
			_reflectionClassWriter.ReflectionWriteStartElement(xmlWriter, collectionElementType, @namespace, @namespace.Value, collectionItemName.Value, 0);
			if (flag)
			{
				collectionDataContract.ItemContract.WriteXmlValue(xmlWriter, current2, context);
			}
			else
			{
				_reflectionClassWriter.ReflectionWriteValue(xmlWriter, context, collectionElementType, current2, writeXsiType: false, null);
			}
			_reflectionClassWriter.ReflectionWriteEndElement(xmlWriter);
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private bool ReflectionTryWritePrimitiveArray(XmlWriterDelegator xmlWriter, object obj, Type type, Type itemType, XmlDictionaryString collectionItemName, XmlDictionaryString itemNamespace)
	{
		PrimitiveDataContract primitiveDataContract = PrimitiveDataContract.GetPrimitiveDataContract(itemType);
		if (primitiveDataContract == null)
		{
			return false;
		}
		switch (itemType.GetTypeCode())
		{
		case TypeCode.Boolean:
			xmlWriter.WriteBooleanArray((bool[])obj, collectionItemName, itemNamespace);
			break;
		case TypeCode.DateTime:
			xmlWriter.WriteDateTimeArray((DateTime[])obj, collectionItemName, itemNamespace);
			break;
		case TypeCode.Decimal:
			xmlWriter.WriteDecimalArray((decimal[])obj, collectionItemName, itemNamespace);
			break;
		case TypeCode.Int32:
			xmlWriter.WriteInt32Array((int[])obj, collectionItemName, itemNamespace);
			break;
		case TypeCode.Int64:
			xmlWriter.WriteInt64Array((long[])obj, collectionItemName, itemNamespace);
			break;
		case TypeCode.Single:
			xmlWriter.WriteSingleArray((float[])obj, collectionItemName, itemNamespace);
			break;
		case TypeCode.Double:
			xmlWriter.WriteDoubleArray((double[])obj, collectionItemName, itemNamespace);
			break;
		default:
			return false;
		}
		return true;
	}
}
