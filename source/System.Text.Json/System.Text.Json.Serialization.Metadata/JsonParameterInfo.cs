using System.Runtime.CompilerServices;

namespace System.Text.Json.Serialization.Metadata;

internal abstract class JsonParameterInfo
{
	private JsonTypeInfo _runtimeTypeInfo;

	[CompilerGenerated]
	private bool _003CMatchingPropertyCanBeNull_003Ek__BackingField;

	public JsonParameterInfoValues ClrInfo;

	public JsonConverter ConverterBase { get; private set; }

	private bool MatchingPropertyCanBeNull
	{
		[CompilerGenerated]
		set
		{
			_003CMatchingPropertyCanBeNull_003Ek__BackingField = value;
		}
	}

	public object DefaultValue { get; private protected set; }

	public bool IgnoreDefaultValuesOnRead { get; private set; }

	public JsonSerializerOptions Options { get; set; }

	public byte[] NameAsUtf8Bytes { get; private set; }

	public JsonNumberHandling? NumberHandling { get; private set; }

	public JsonTypeInfo RuntimeTypeInfo
	{
		get
		{
			if (_runtimeTypeInfo == null)
			{
				_runtimeTypeInfo = Options.GetOrAddClass(RuntimePropertyType);
			}
			return _runtimeTypeInfo;
		}
	}

	public Type RuntimePropertyType { get; set; }

	public bool ShouldDeserialize { get; private set; }

	public virtual void Initialize(JsonParameterInfoValues parameterInfo, JsonPropertyInfo matchingProperty, JsonSerializerOptions options)
	{
		ClrInfo = parameterInfo;
		Options = options;
		ShouldDeserialize = true;
		RuntimePropertyType = matchingProperty.RuntimePropertyType;
		NameAsUtf8Bytes = matchingProperty.NameAsUtf8Bytes;
		ConverterBase = matchingProperty.ConverterBase;
		IgnoreDefaultValuesOnRead = matchingProperty.IgnoreDefaultValuesOnRead;
		NumberHandling = matchingProperty.NumberHandling;
		MatchingPropertyCanBeNull = matchingProperty.PropertyTypeCanBeNull;
	}

	public static JsonParameterInfo CreateIgnoredParameterPlaceholder(JsonParameterInfoValues parameterInfo, JsonPropertyInfo matchingProperty, bool sourceGenMode)
	{
		JsonParameterInfo jsonParameterInfo = new JsonParameterInfo<sbyte>();
		jsonParameterInfo.ClrInfo = parameterInfo;
		jsonParameterInfo.RuntimePropertyType = matchingProperty.RuntimePropertyType;
		jsonParameterInfo.NameAsUtf8Bytes = matchingProperty.NameAsUtf8Bytes;
		if (sourceGenMode)
		{
			jsonParameterInfo.DefaultValue = matchingProperty.DefaultValue;
		}
		else
		{
			Type parameterType = parameterInfo.ParameterType;
			JsonTypeInfo jsonTypeInfo;
			GenericMethodHolder genericMethodHolder = ((!matchingProperty.Options.TryGetClass(parameterType, out jsonTypeInfo)) ? GenericMethodHolder.CreateHolder(parameterInfo.ParameterType) : jsonTypeInfo.GenericMethods);
			jsonParameterInfo.DefaultValue = genericMethodHolder.DefaultValue;
		}
		return jsonParameterInfo;
	}
}
internal sealed class JsonParameterInfo<T> : JsonParameterInfo
{
	public T TypedDefaultValue { get; private set; }

	public override void Initialize(JsonParameterInfoValues parameterInfo, JsonPropertyInfo matchingProperty, JsonSerializerOptions options)
	{
		base.Initialize(parameterInfo, matchingProperty, options);
		InitializeDefaultValue(matchingProperty);
	}

	private void InitializeDefaultValue(JsonPropertyInfo matchingProperty)
	{
		if (ClrInfo.HasDefaultValue)
		{
			object defaultValue = ClrInfo.DefaultValue;
			if (defaultValue == null && !matchingProperty.PropertyTypeCanBeNull)
			{
				base.DefaultValue = TypedDefaultValue;
				return;
			}
			base.DefaultValue = defaultValue;
			TypedDefaultValue = (T)defaultValue;
		}
		else
		{
			base.DefaultValue = TypedDefaultValue;
		}
	}
}
