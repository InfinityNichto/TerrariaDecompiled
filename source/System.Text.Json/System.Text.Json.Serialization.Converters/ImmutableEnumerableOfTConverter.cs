using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Converters;

internal class ImmutableEnumerableOfTConverter<TCollection, TElement> : IEnumerableDefaultConverter<TCollection, TElement> where TCollection : IEnumerable<TElement>
{
	internal sealed override bool CanHaveIdMetadata => false;

	protected sealed override void Add(in TElement value, ref ReadStack state)
	{
		((List<TElement>)state.Current.ReturnValue).Add(value);
	}

	protected sealed override void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
	{
		state.Current.ReturnValue = new List<TElement>();
	}

	protected sealed override void ConvertCollection(ref ReadStack state, JsonSerializerOptions options)
	{
		JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
		Func<IEnumerable<TElement>, TCollection> func = (Func<IEnumerable<TElement>, TCollection>)jsonTypeInfo.CreateObjectWithArgs;
		state.Current.ReturnValue = func((List<TElement>)state.Current.ReturnValue);
	}
}
