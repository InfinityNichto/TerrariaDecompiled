using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Xml;

namespace System.Runtime.Serialization;

internal abstract class ReflectionClassWriter
{
	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public void ReflectionWriteClass(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, ClassDataContract classContract, XmlDictionaryString[] memberNames)
	{
		InvokeOnSerializing(obj, context, classContract);
		obj = ResolveAdapterType(obj, classContract);
		if (classContract.IsISerializable)
		{
			context.WriteISerializable(xmlWriter, (ISerializable)obj);
		}
		else
		{
			if (classContract.HasExtensionData)
			{
				context.WriteExtensionData(xmlWriter, ((IExtensibleDataObject)obj).ExtensionData, -1);
			}
			ReflectionWriteMembers(xmlWriter, obj, context, classContract, classContract, 0, memberNames);
		}
		InvokeOnSerialized(obj, context, classContract);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public void ReflectionWriteValue(XmlWriterDelegator xmlWriter, XmlObjectSerializerWriteContext context, Type type, object value, bool writeXsiType, PrimitiveDataContract primitiveContractForParamType)
	{
		Type type2 = type;
		object obj = value;
		bool flag = type2.IsGenericType && type2.GetGenericTypeDefinition() == Globals.TypeOfNullable;
		if (type2.IsValueType && !flag)
		{
			if (primitiveContractForParamType != null && !writeXsiType)
			{
				primitiveContractForParamType.WriteXmlValue(xmlWriter, obj, context);
			}
			else
			{
				ReflectionInternalSerialize(xmlWriter, context, obj, obj.GetType().TypeHandle.Equals(type2.TypeHandle), writeXsiType, type2);
			}
			return;
		}
		if (flag)
		{
			if (obj == null)
			{
				type2 = Nullable.GetUnderlyingType(type2);
			}
			else
			{
				MethodInfo method = type2.GetMethod("get_Value", Type.EmptyTypes);
				obj = method.Invoke(obj, Array.Empty<object>());
				type2 = obj.GetType();
			}
		}
		if (obj == null)
		{
			context.WriteNull(xmlWriter, type2, DataContract.IsTypeSerializable(type2));
			return;
		}
		PrimitiveDataContract primitiveDataContract = (flag ? PrimitiveDataContract.GetPrimitiveDataContract(type2) : primitiveContractForParamType);
		if (primitiveDataContract != null && primitiveDataContract.UnderlyingType != Globals.TypeOfObject && !writeXsiType)
		{
			primitiveDataContract.WriteXmlValue(xmlWriter, obj, context);
		}
		else if (obj == null && (type2 == Globals.TypeOfObject || (flag && type2.IsValueType)))
		{
			context.WriteNull(xmlWriter, type2, DataContract.IsTypeSerializable(type2));
		}
		else
		{
			ReflectionInternalSerialize(xmlWriter, context, obj, obj.GetType().TypeHandle.Equals(type2.TypeHandle), writeXsiType, type2, flag);
		}
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected abstract int ReflectionWriteMembers(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, ClassDataContract classContract, ClassDataContract derivedMostClassContract, int childElementIndex, XmlDictionaryString[] memberNames);

	protected object ReflectionGetMemberValue(object obj, DataMember dataMember)
	{
		return dataMember.Getter(obj);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected bool ReflectionTryWritePrimitive(XmlWriterDelegator xmlWriter, XmlObjectSerializerWriteContext context, Type type, object value, XmlDictionaryString name, XmlDictionaryString ns, PrimitiveDataContract primitiveContract)
	{
		if (primitiveContract == null || primitiveContract.UnderlyingType == Globals.TypeOfObject)
		{
			return false;
		}
		primitiveContract.WriteXmlElement(xmlWriter, value, context, name, ns);
		return true;
	}

	private void InvokeOnSerializing(object obj, XmlObjectSerializerWriteContext context, ClassDataContract classContract)
	{
		if (classContract.BaseContract != null)
		{
			InvokeOnSerializing(obj, context, classContract.BaseContract);
		}
		if (classContract.OnSerializing != null)
		{
			StreamingContext streamingContext = context.GetStreamingContext();
			classContract.OnSerializing.Invoke(obj, new object[1] { streamingContext });
		}
	}

	private void InvokeOnSerialized(object obj, XmlObjectSerializerWriteContext context, ClassDataContract classContract)
	{
		if (classContract.BaseContract != null)
		{
			InvokeOnSerialized(obj, context, classContract.BaseContract);
		}
		if (classContract.OnSerialized != null)
		{
			StreamingContext streamingContext = context.GetStreamingContext();
			classContract.OnSerialized.Invoke(obj, new object[1] { streamingContext });
		}
	}

	private object ResolveAdapterType(object obj, ClassDataContract classContract)
	{
		Type type = obj.GetType();
		if (type == Globals.TypeOfDateTimeOffset)
		{
			obj = DateTimeOffsetAdapter.GetDateTimeOffsetAdapter((DateTimeOffset)obj);
		}
		else if (type == Globals.TypeOfMemoryStream)
		{
			obj = MemoryStreamAdapter.GetMemoryStreamAdapter((MemoryStream)obj);
		}
		else if (type.IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfKeyValuePair)
		{
			obj = classContract.KeyValuePairAdapterConstructorInfo.Invoke(new object[1] { obj });
		}
		return obj;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	private void ReflectionInternalSerialize(XmlWriterDelegator xmlWriter, XmlObjectSerializerWriteContext context, object obj, bool isDeclaredType, bool writeXsiType, Type memberType, bool isNullableOfT = false)
	{
		if (isNullableOfT)
		{
			context.InternalSerialize(xmlWriter, obj, isDeclaredType, writeXsiType, DataContract.GetId(memberType.TypeHandle), memberType.TypeHandle);
		}
		else
		{
			context.InternalSerializeReference(xmlWriter, obj, isDeclaredType, writeXsiType, DataContract.GetId(memberType.TypeHandle), memberType.TypeHandle);
		}
	}
}
