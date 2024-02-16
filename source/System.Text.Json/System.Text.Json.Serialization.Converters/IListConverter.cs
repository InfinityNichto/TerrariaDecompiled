using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class IListConverter<TCollection> : JsonCollectionConverter<TCollection, object> where TCollection : IList
{
	internal override Type RuntimeType
	{
		get
		{
			if (TypeToConvert.IsAbstract || TypeToConvert.IsInterface)
			{
				return typeof(List<object>);
			}
			return TypeToConvert;
		}
	}

	protected override void Add(in object value, ref ReadStack state)
	{
		TCollection val = (TCollection)state.Current.ReturnValue;
		val.Add(value);
		if (base.IsValueType)
		{
			state.Current.ReturnValue = val;
		}
	}

	protected override void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
	{
		JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
		if (TypeToConvert.IsInterface || TypeToConvert.IsAbstract)
		{
			if (!TypeToConvert.IsAssignableFrom(RuntimeType))
			{
				ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(TypeToConvert, ref reader, ref state);
			}
			state.Current.ReturnValue = new List<object>();
			return;
		}
		if (jsonTypeInfo.CreateObject == null)
		{
			ThrowHelper.ThrowNotSupportedException_DeserializeNoConstructor(TypeToConvert, ref reader, ref state);
		}
		TCollection val = (TCollection)jsonTypeInfo.CreateObject();
		if (val.IsReadOnly)
		{
			ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(TypeToConvert, ref reader, ref state);
		}
		state.Current.ReturnValue = val;
	}

	protected override bool OnWriteResume(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options, ref WriteStack state)
	{
		IList list = value;
		int i = state.Current.EnumeratorIndex;
		JsonConverter<object> elementConverter = JsonCollectionConverter<TCollection, object>.GetElementConverter(ref state);
		if (elementConverter.CanUseDirectReadOrWrite && !state.Current.NumberHandling.HasValue)
		{
			for (; i < list.Count; i++)
			{
				elementConverter.Write(writer, list[i], options);
			}
		}
		else
		{
			for (; i < list.Count; i++)
			{
				object value2 = list[i];
				if (!elementConverter.TryWrite(writer, in value2, options, ref state))
				{
					state.Current.EnumeratorIndex = i;
					return false;
				}
				if (ShouldFlush(writer, ref state))
				{
					i = (state.Current.EnumeratorIndex = i + 1);
					return false;
				}
			}
		}
		return true;
	}
}
