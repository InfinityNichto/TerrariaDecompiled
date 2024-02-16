using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization.Metadata;

internal sealed class JsonTypeInfoInternal<T> : JsonTypeInfo<T>
{
	public JsonTypeInfoInternal(JsonSerializerOptions options)
		: base(typeof(T), options)
	{
	}

	public JsonTypeInfoInternal(JsonSerializerOptions options, JsonObjectInfoValues<T> objectInfo)
		: base(typeof(T), options)
	{
		JsonConverter converter;
		if (objectInfo.ObjectWithParameterizedConstructorCreator != null)
		{
			converter = new JsonMetadataServicesConverter<T>(() => new LargeObjectWithParameterizedConstructorConverter<T>(), ConverterStrategy.Object);
			base.CreateObjectWithArgs = objectInfo.ObjectWithParameterizedConstructorCreator;
			CtorParamInitFunc = objectInfo.ConstructorParameterMetadataInitializer;
		}
		else
		{
			converter = new JsonMetadataServicesConverter<T>(() => new ObjectDefaultConverter<T>(), ConverterStrategy.Object);
			SetCreateObjectFunc(objectInfo.ObjectCreator);
		}
		PropInitFunc = objectInfo.PropertyMetadataInitializer;
		base.SerializeHandler = objectInfo.SerializeHandler;
		base.PropertyInfoForTypeInfo = JsonMetadataServices.CreateJsonPropertyInfoForClassInfo(typeof(T), this, converter, base.Options);
		base.NumberHandling = objectInfo.NumberHandling;
	}

	public JsonTypeInfoInternal(JsonSerializerOptions options, JsonCollectionInfoValues<T> collectionInfo, Func<JsonConverter<T>> converterCreator, object createObjectWithArgs = null, object addFunc = null)
		: base(typeof(T), options)
	{
		if (collectionInfo == null)
		{
			throw new ArgumentNullException("collectionInfo");
		}
		JsonConverter<T> jsonConverter = new JsonMetadataServicesConverter<T>(converterCreator, (collectionInfo.KeyInfo == null) ? ConverterStrategy.Enumerable : ConverterStrategy.Dictionary);
		base.KeyType = jsonConverter.KeyType;
		base.ElementType = jsonConverter.ElementType;
		base.KeyTypeInfo = collectionInfo.KeyInfo;
		base.ElementTypeInfo = collectionInfo.ElementInfo ?? throw new ArgumentNullException("ElementInfo");
		base.NumberHandling = collectionInfo.NumberHandling;
		base.PropertyInfoForTypeInfo = JsonMetadataServices.CreateJsonPropertyInfoForClassInfo(typeof(T), this, jsonConverter, options);
		base.SerializeHandler = collectionInfo.SerializeHandler;
		base.CreateObjectWithArgs = createObjectWithArgs;
		base.AddMethodDelegate = addFunc;
		SetCreateObjectFunc(collectionInfo.ObjectCreator);
	}

	private void SetCreateObjectFunc(Func<T> createObjectFunc)
	{
		if (createObjectFunc != null)
		{
			base.CreateObject = () => createObjectFunc();
		}
	}
}
