using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal class JsonDataContract
{
	internal class JsonDataContractCriticalHelper
	{
		private static readonly object s_cacheLock = new object();

		private static readonly object s_createDataContractLock = new object();

		private static JsonDataContract[] s_dataContractCache = new JsonDataContract[32];

		private static int s_dataContractID;

		private static readonly TypeHandleRef s_typeHandleRef = new TypeHandleRef();

		private static readonly Dictionary<TypeHandleRef, IntRef> s_typeToIDCache = new Dictionary<TypeHandleRef, IntRef>(new TypeHandleRefEqualityComparer());

		private Dictionary<XmlQualifiedName, DataContract> _knownDataContracts;

		private readonly DataContract _traditionalDataContract;

		private readonly string _typeName;

		internal Dictionary<XmlQualifiedName, DataContract> KnownDataContracts => _knownDataContracts;

		internal DataContract TraditionalDataContract => _traditionalDataContract;

		internal virtual string TypeName => _typeName;

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		internal JsonDataContractCriticalHelper(DataContract traditionalDataContract)
		{
			_traditionalDataContract = traditionalDataContract;
			AddCollectionItemContractsToKnownDataContracts();
			_typeName = (string.IsNullOrEmpty(traditionalDataContract.Namespace.Value) ? traditionalDataContract.Name.Value : (traditionalDataContract.Name.Value + ":" + XmlObjectSerializerWriteContextComplexJson.TruncateDefaultDataContractNamespace(traditionalDataContract.Namespace.Value)));
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		public static JsonDataContract GetJsonDataContract(DataContract traditionalDataContract)
		{
			int id = GetId(traditionalDataContract.UnderlyingType.TypeHandle);
			JsonDataContract jsonDataContract = s_dataContractCache[id];
			if (jsonDataContract == null)
			{
				jsonDataContract = CreateJsonDataContract(id, traditionalDataContract);
				s_dataContractCache[id] = jsonDataContract;
			}
			return jsonDataContract;
		}

		internal static int GetId(RuntimeTypeHandle typeHandle)
		{
			lock (s_cacheLock)
			{
				s_typeHandleRef.Value = typeHandle;
				if (!s_typeToIDCache.TryGetValue(s_typeHandleRef, out var value))
				{
					int num = s_dataContractID++;
					if (num >= s_dataContractCache.Length)
					{
						int num2 = ((num < 1073741823) ? (num * 2) : int.MaxValue);
						if (num2 <= num)
						{
							throw new SerializationException(System.SR.DataContractCacheOverflow);
						}
						Array.Resize(ref s_dataContractCache, num2);
					}
					value = new IntRef(num);
					try
					{
						s_typeToIDCache.Add(new TypeHandleRef(typeHandle), value);
					}
					catch (Exception ex)
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperFatal(ex.Message, ex);
					}
				}
				return value.Value;
			}
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private static JsonDataContract CreateJsonDataContract(int id, DataContract traditionalDataContract)
		{
			lock (s_createDataContractLock)
			{
				JsonDataContract jsonDataContract = s_dataContractCache[id];
				if (jsonDataContract == null)
				{
					Type type = traditionalDataContract.GetType();
					if (type == typeof(ObjectDataContract))
					{
						jsonDataContract = new JsonObjectDataContract(traditionalDataContract);
					}
					else if (type == typeof(StringDataContract))
					{
						jsonDataContract = new JsonStringDataContract((StringDataContract)traditionalDataContract);
					}
					else if (type == typeof(UriDataContract))
					{
						jsonDataContract = new JsonUriDataContract((UriDataContract)traditionalDataContract);
					}
					else if (type == typeof(QNameDataContract))
					{
						jsonDataContract = new JsonQNameDataContract((QNameDataContract)traditionalDataContract);
					}
					else if (type == typeof(ByteArrayDataContract))
					{
						jsonDataContract = new JsonByteArrayDataContract((ByteArrayDataContract)traditionalDataContract);
					}
					else if (traditionalDataContract.IsPrimitive || traditionalDataContract.UnderlyingType == Globals.TypeOfXmlQualifiedName)
					{
						jsonDataContract = new JsonDataContract(traditionalDataContract);
					}
					else if (type == typeof(ClassDataContract))
					{
						jsonDataContract = new JsonClassDataContract((ClassDataContract)traditionalDataContract);
					}
					else if (type == typeof(EnumDataContract))
					{
						jsonDataContract = new JsonEnumDataContract((EnumDataContract)traditionalDataContract);
					}
					else if (type == typeof(GenericParameterDataContract) || type == typeof(SpecialTypeDataContract))
					{
						jsonDataContract = new JsonDataContract(traditionalDataContract);
					}
					else if (type == typeof(CollectionDataContract))
					{
						jsonDataContract = new JsonCollectionDataContract((CollectionDataContract)traditionalDataContract);
					}
					else
					{
						if (!(type == typeof(XmlDataContract)))
						{
							throw new ArgumentException(System.SR.Format(System.SR.JsonTypeNotSupportedByDataContractJsonSerializer, traditionalDataContract.UnderlyingType), "traditionalDataContract");
						}
						jsonDataContract = new JsonXmlDataContract((XmlDataContract)traditionalDataContract);
					}
				}
				return jsonDataContract;
			}
		}

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		private void AddCollectionItemContractsToKnownDataContracts()
		{
			if (_traditionalDataContract.KnownDataContracts == null)
			{
				return;
			}
			foreach (KeyValuePair<XmlQualifiedName, DataContract> knownDataContract in _traditionalDataContract.KnownDataContracts)
			{
				CollectionDataContract collectionDataContract = knownDataContract.Value as CollectionDataContract;
				while (collectionDataContract != null)
				{
					DataContract itemContract = collectionDataContract.ItemContract;
					if (_knownDataContracts == null)
					{
						_knownDataContracts = new Dictionary<XmlQualifiedName, DataContract>();
					}
					_knownDataContracts.TryAdd(itemContract.StableName, itemContract);
					if (collectionDataContract.ItemType.IsGenericType && collectionDataContract.ItemType.GetGenericTypeDefinition() == typeof(KeyValue<, >))
					{
						DataContract dataContract = DataContract.GetDataContract(Globals.TypeOfKeyValuePair.MakeGenericType(collectionDataContract.ItemType.GenericTypeArguments));
						_knownDataContracts.TryAdd(dataContract.StableName, dataContract);
					}
					if (!(itemContract is CollectionDataContract))
					{
						break;
					}
					collectionDataContract = itemContract as CollectionDataContract;
				}
			}
		}
	}

	private readonly JsonDataContractCriticalHelper _helper;

	internal virtual string TypeName => null;

	protected JsonDataContractCriticalHelper Helper => _helper;

	protected DataContract TraditionalDataContract => _helper.TraditionalDataContract;

	private Dictionary<XmlQualifiedName, DataContract> KnownDataContracts => _helper.KnownDataContracts;

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	protected JsonDataContract(DataContract traditionalDataContract)
	{
		_helper = new JsonDataContractCriticalHelper(traditionalDataContract);
	}

	protected JsonDataContract(JsonDataContractCriticalHelper helper)
	{
		_helper = helper;
	}

	public static JsonReadWriteDelegates GetGeneratedReadWriteDelegates(DataContract c)
	{
		if (!JsonReadWriteDelegates.GetJsonDelegates().TryGetValue(c, out var value))
		{
			return null;
		}
		return value;
	}

	internal static JsonReadWriteDelegates GetReadWriteDelegatesFromGeneratedAssembly(DataContract c)
	{
		JsonReadWriteDelegates generatedReadWriteDelegates = GetGeneratedReadWriteDelegates(c);
		if (generatedReadWriteDelegates == null)
		{
			throw new InvalidDataContractException(System.SR.Format(System.SR.SerializationCodeIsMissingForType, c.UnderlyingType));
		}
		return generatedReadWriteDelegates;
	}

	internal static JsonReadWriteDelegates TryGetReadWriteDelegatesFromGeneratedAssembly(DataContract c)
	{
		return GetGeneratedReadWriteDelegates(c);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public static JsonDataContract GetJsonDataContract(DataContract traditionalDataContract)
	{
		return JsonDataContractCriticalHelper.GetJsonDataContract(traditionalDataContract);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public object ReadJsonValue(XmlReaderDelegator jsonReader, XmlObjectSerializerReadContextComplexJson context)
	{
		PushKnownDataContracts(context);
		object result = ReadJsonValueCore(jsonReader, context);
		PopKnownDataContracts(context);
		return result;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual object ReadJsonValueCore(XmlReaderDelegator jsonReader, XmlObjectSerializerReadContextComplexJson context)
	{
		return TraditionalDataContract.ReadXmlValue(jsonReader, context);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public void WriteJsonValue(XmlWriterDelegator jsonWriter, object obj, XmlObjectSerializerWriteContextComplexJson context, RuntimeTypeHandle declaredTypeHandle)
	{
		PushKnownDataContracts(context);
		WriteJsonValueCore(jsonWriter, obj, context, declaredTypeHandle);
		PopKnownDataContracts(context);
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public virtual void WriteJsonValueCore(XmlWriterDelegator jsonWriter, object obj, XmlObjectSerializerWriteContextComplexJson context, RuntimeTypeHandle declaredTypeHandle)
	{
		TraditionalDataContract.WriteXmlValue(jsonWriter, obj, context);
	}

	protected static object HandleReadValue(object obj, XmlObjectSerializerReadContext context)
	{
		context.AddNewObject(obj);
		return obj;
	}

	protected static bool TryReadNullAtTopLevel(XmlReaderDelegator reader)
	{
		if (reader.MoveToAttribute("type") && reader.Value == "null")
		{
			reader.Skip();
			reader.MoveToElement();
			return true;
		}
		reader.MoveToElement();
		return false;
	}

	protected void PopKnownDataContracts(XmlObjectSerializerContext context)
	{
		if (KnownDataContracts != null)
		{
			context.scopedKnownTypes.Pop();
		}
	}

	protected void PushKnownDataContracts(XmlObjectSerializerContext context)
	{
		if (KnownDataContracts != null)
		{
			context.scopedKnownTypes.Push(KnownDataContracts);
		}
	}
}
