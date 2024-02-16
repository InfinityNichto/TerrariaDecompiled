using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class IDictionaryOfTKeyTValueConverter<TDictionary, TKey, TValue> : DictionaryDefaultConverter<TDictionary, TKey, TValue> where TDictionary : IDictionary<TKey, TValue>
{
	internal override Type RuntimeType
	{
		get
		{
			if (TypeToConvert.IsAbstract || TypeToConvert.IsInterface)
			{
				return typeof(Dictionary<TKey, TValue>);
			}
			return TypeToConvert;
		}
	}

	protected override void Add(TKey key, in TValue value, JsonSerializerOptions options, ref ReadStack state)
	{
		TDictionary val = (TDictionary)state.Current.ReturnValue;
		val[key] = value;
		if (base.IsValueType)
		{
			state.Current.ReturnValue = val;
		}
	}

	protected override void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state)
	{
		JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
		if (TypeToConvert.IsInterface || TypeToConvert.IsAbstract)
		{
			if (!TypeToConvert.IsAssignableFrom(RuntimeType))
			{
				ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(TypeToConvert, ref reader, ref state);
			}
			state.Current.ReturnValue = new Dictionary<TKey, TValue>();
			return;
		}
		if (jsonTypeInfo.CreateObject == null)
		{
			ThrowHelper.ThrowNotSupportedException_DeserializeNoConstructor(TypeToConvert, ref reader, ref state);
		}
		TDictionary val = (TDictionary)jsonTypeInfo.CreateObject();
		if (val.IsReadOnly)
		{
			ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(TypeToConvert, ref reader, ref state);
		}
		state.Current.ReturnValue = val;
	}
}
