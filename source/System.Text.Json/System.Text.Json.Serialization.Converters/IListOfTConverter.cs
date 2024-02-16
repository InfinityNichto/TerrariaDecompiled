using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal sealed class IListOfTConverter<TCollection, TElement> : IEnumerableDefaultConverter<TCollection, TElement> where TCollection : IList<TElement>
{
	internal override Type RuntimeType
	{
		get
		{
			if (TypeToConvert.IsAbstract || TypeToConvert.IsInterface)
			{
				return typeof(List<TElement>);
			}
			return TypeToConvert;
		}
	}

	protected override void Add(in TElement value, ref ReadStack state)
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
			state.Current.ReturnValue = new List<TElement>();
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
}
