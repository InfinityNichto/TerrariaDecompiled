using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Serialization;

internal sealed class SurrogateDataContract : DataContract
{
	private sealed class SurrogateDataContractCriticalHelper : DataContractCriticalHelper
	{
		private readonly ISerializationSurrogate serializationSurrogate;

		internal ISerializationSurrogate SerializationSurrogate => serializationSurrogate;

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal SurrogateDataContractCriticalHelper([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, ISerializationSurrogate serializationSurrogate)
			: base(type)
		{
			this.serializationSurrogate = serializationSurrogate;
			DataContract.GetDefaultStableName(DataContract.GetClrTypeFullName(type), out var localName, out var ns);
			SetDataContractName(DataContract.CreateQualifiedName(localName, ns));
		}
	}

	private readonly SurrogateDataContractCriticalHelper _helper;

	internal ISerializationSurrogate SerializationSurrogate => _helper.SerializationSurrogate;

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	internal SurrogateDataContract(Type type, ISerializationSurrogate serializationSurrogate)
		: base(new SurrogateDataContractCriticalHelper(type, serializationSurrogate))
	{
		_helper = base.Helper as SurrogateDataContractCriticalHelper;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
	{
		SerializationInfo serInfo = new SerializationInfo(base.UnderlyingType, XmlObjectSerializer.FormatterConverter, !context.UnsafeTypeForwardingEnabled);
		SerializationSurrogateGetObjectData(obj, serInfo, context.GetStreamingContext());
		context.WriteSerializationInfo(xmlWriter, base.UnderlyingType, serInfo);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private object SerializationSurrogateSetObjectData(object obj, SerializationInfo serInfo, StreamingContext context)
	{
		return SerializationSurrogate.SetObjectData(obj, serInfo, context, null);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static object GetRealObject(IObjectReference obj, StreamingContext context)
	{
		return obj.GetRealObject(context);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private object GetUninitializedObject(Type objType)
	{
		return RuntimeHelpers.GetUninitializedObject(objType);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void SerializationSurrogateGetObjectData(object obj, SerializationInfo serInfo, StreamingContext context)
	{
		SerializationSurrogate.GetObjectData(obj, serInfo, context);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override object ReadXmlValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context)
	{
		xmlReader.Read();
		Type underlyingType = base.UnderlyingType;
		object obj = (underlyingType.IsArray ? Array.CreateInstance(underlyingType.GetElementType(), 0) : GetUninitializedObject(underlyingType));
		context.AddNewObject(obj);
		string objectId = context.GetObjectId();
		SerializationInfo serInfo = context.ReadSerializationInfo(xmlReader, underlyingType);
		object obj2 = SerializationSurrogateSetObjectData(obj, serInfo, context.GetStreamingContext());
		if (obj2 == null)
		{
			obj2 = obj;
		}
		if (obj2 is IDeserializationCallback)
		{
			((IDeserializationCallback)obj2).OnDeserialization(null);
		}
		if (obj2 is IObjectReference)
		{
			obj2 = GetRealObject((IObjectReference)obj2, context.GetStreamingContext());
		}
		context.ReplaceDeserializedObject(objectId, obj, obj2);
		xmlReader.ReadEndElement();
		return obj2;
	}
}
