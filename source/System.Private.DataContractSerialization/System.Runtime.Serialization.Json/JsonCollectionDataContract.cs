using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal sealed class JsonCollectionDataContract : JsonDataContract
{
	private sealed class JsonCollectionDataContractCriticalHelper : JsonDataContractCriticalHelper
	{
		private JsonFormatCollectionReaderDelegate _jsonFormatReaderDelegate;

		private JsonFormatGetOnlyCollectionReaderDelegate _jsonFormatGetOnlyReaderDelegate;

		private JsonFormatCollectionWriterDelegate _jsonFormatWriterDelegate;

		private readonly CollectionDataContract _traditionalCollectionDataContract;

		internal JsonFormatCollectionReaderDelegate JsonFormatReaderDelegate
		{
			get
			{
				return _jsonFormatReaderDelegate;
			}
			set
			{
				_jsonFormatReaderDelegate = value;
			}
		}

		internal JsonFormatGetOnlyCollectionReaderDelegate JsonFormatGetOnlyReaderDelegate
		{
			get
			{
				return _jsonFormatGetOnlyReaderDelegate;
			}
			set
			{
				_jsonFormatGetOnlyReaderDelegate = value;
			}
		}

		internal JsonFormatCollectionWriterDelegate JsonFormatWriterDelegate
		{
			get
			{
				return _jsonFormatWriterDelegate;
			}
			set
			{
				_jsonFormatWriterDelegate = value;
			}
		}

		internal CollectionDataContract TraditionalCollectionDataContract => _traditionalCollectionDataContract;

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		public JsonCollectionDataContractCriticalHelper(CollectionDataContract traditionalDataContract)
			: base(traditionalDataContract)
		{
			_traditionalCollectionDataContract = traditionalDataContract;
		}
	}

	private readonly JsonCollectionDataContractCriticalHelper _helper;

	internal JsonFormatCollectionReaderDelegate JsonFormatReaderDelegate
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (_helper.JsonFormatReaderDelegate == null)
			{
				lock (this)
				{
					if (_helper.JsonFormatReaderDelegate == null)
					{
						JsonFormatCollectionReaderDelegate jsonFormatReaderDelegate = ((DataContractSerializer.Option != SerializationOption.ReflectionOnly) ? new JsonFormatReaderGenerator().GenerateCollectionReader(TraditionalCollectionDataContract) : new JsonFormatCollectionReaderDelegate(new ReflectionJsonCollectionReader().ReflectionReadCollection));
						Interlocked.MemoryBarrier();
						_helper.JsonFormatReaderDelegate = jsonFormatReaderDelegate;
					}
				}
			}
			return _helper.JsonFormatReaderDelegate;
		}
	}

	internal JsonFormatGetOnlyCollectionReaderDelegate JsonFormatGetOnlyReaderDelegate
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (_helper.JsonFormatGetOnlyReaderDelegate == null)
			{
				lock (this)
				{
					if (_helper.JsonFormatGetOnlyReaderDelegate == null)
					{
						CollectionKind kind = TraditionalCollectionDataContract.Kind;
						if (base.TraditionalDataContract.UnderlyingType.IsInterface && (kind == CollectionKind.Enumerable || kind == CollectionKind.Collection || kind == CollectionKind.GenericEnumerable))
						{
							throw new InvalidDataContractException(System.SR.Format(System.SR.GetOnlyCollectionMustHaveAddMethod, DataContract.GetClrTypeFullName(base.TraditionalDataContract.UnderlyingType)));
						}
						JsonFormatGetOnlyCollectionReaderDelegate jsonFormatGetOnlyReaderDelegate = ((DataContractSerializer.Option != SerializationOption.ReflectionOnly) ? new JsonFormatReaderGenerator().GenerateGetOnlyCollectionReader(TraditionalCollectionDataContract) : new JsonFormatGetOnlyCollectionReaderDelegate(new ReflectionJsonCollectionReader().ReflectionReadGetOnlyCollection));
						Interlocked.MemoryBarrier();
						_helper.JsonFormatGetOnlyReaderDelegate = jsonFormatGetOnlyReaderDelegate;
					}
				}
			}
			return _helper.JsonFormatGetOnlyReaderDelegate;
		}
	}

	internal JsonFormatCollectionWriterDelegate JsonFormatWriterDelegate
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (_helper.JsonFormatWriterDelegate == null)
			{
				lock (this)
				{
					if (_helper.JsonFormatWriterDelegate == null)
					{
						JsonFormatCollectionWriterDelegate jsonFormatWriterDelegate = ((DataContractSerializer.Option != SerializationOption.ReflectionOnly) ? new JsonFormatWriterGenerator().GenerateCollectionWriter(TraditionalCollectionDataContract) : new JsonFormatCollectionWriterDelegate(new ReflectionJsonFormatWriter().ReflectionWriteCollection));
						Interlocked.MemoryBarrier();
						_helper.JsonFormatWriterDelegate = jsonFormatWriterDelegate;
					}
				}
			}
			return _helper.JsonFormatWriterDelegate;
		}
	}

	private CollectionDataContract TraditionalCollectionDataContract => _helper.TraditionalCollectionDataContract;

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public JsonCollectionDataContract(CollectionDataContract traditionalDataContract)
		: base(new JsonCollectionDataContractCriticalHelper(traditionalDataContract))
	{
		_helper = base.Helper as JsonCollectionDataContractCriticalHelper;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override object ReadJsonValueCore(XmlReaderDelegator jsonReader, XmlObjectSerializerReadContextComplexJson context)
	{
		jsonReader.Read();
		object result = null;
		if (context.IsGetOnlyCollection)
		{
			context.IsGetOnlyCollection = false;
			JsonFormatGetOnlyReaderDelegate(jsonReader, context, XmlDictionaryString.Empty, JsonGlobals.itemDictionaryString, TraditionalCollectionDataContract);
		}
		else
		{
			result = JsonFormatReaderDelegate(jsonReader, context, XmlDictionaryString.Empty, JsonGlobals.itemDictionaryString, TraditionalCollectionDataContract);
		}
		jsonReader.ReadEndElement();
		return result;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteJsonValueCore(XmlWriterDelegator jsonWriter, object obj, XmlObjectSerializerWriteContextComplexJson context, RuntimeTypeHandle declaredTypeHandle)
	{
		context.IsGetOnlyCollection = false;
		JsonFormatWriterDelegate(jsonWriter, obj, context, TraditionalCollectionDataContract);
	}
}
