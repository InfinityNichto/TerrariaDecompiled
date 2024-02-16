using System.Collections.Generic;

namespace System.Text.Json.Serialization.Converters;

internal sealed class IEnumerableOfTConverter<TCollection, TElement> : IEnumerableDefaultConverter<TCollection, TElement> where TCollection : IEnumerable<TElement>
{
	internal override Type RuntimeType => typeof(List<TElement>);

	protected override void Add(in TElement value, ref ReadStack state)
	{
		((List<TElement>)state.Current.ReturnValue).Add(value);
	}

	protected override void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
	{
		if (!TypeToConvert.IsAssignableFrom(RuntimeType))
		{
			ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(TypeToConvert, ref reader, ref state);
		}
		state.Current.ReturnValue = new List<TElement>();
	}
}
