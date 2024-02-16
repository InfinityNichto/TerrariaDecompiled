using System.Collections.Generic;

namespace System.Text.Json.Serialization.Converters;

internal sealed class ArrayConverter<TCollection, TElement> : IEnumerableDefaultConverter<TElement[], TElement>
{
	internal override bool CanHaveIdMetadata => false;

	protected override void Add(in TElement value, ref ReadStack state)
	{
		((List<TElement>)state.Current.ReturnValue).Add(value);
	}

	protected override void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
	{
		state.Current.ReturnValue = new List<TElement>();
	}

	protected override void ConvertCollection(ref ReadStack state, JsonSerializerOptions options)
	{
		List<TElement> list = (List<TElement>)state.Current.ReturnValue;
		state.Current.ReturnValue = list.ToArray();
	}

	protected override bool OnWriteResume(Utf8JsonWriter writer, TElement[] array, JsonSerializerOptions options, ref WriteStack state)
	{
		int i = state.Current.EnumeratorIndex;
		JsonConverter<TElement> elementConverter = JsonCollectionConverter<TElement[], TElement>.GetElementConverter(ref state);
		if (elementConverter.CanUseDirectReadOrWrite && !state.Current.NumberHandling.HasValue)
		{
			for (; i < array.Length; i++)
			{
				elementConverter.Write(writer, array[i], options);
			}
		}
		else
		{
			for (; i < array.Length; i++)
			{
				TElement value = array[i];
				if (!elementConverter.TryWrite(writer, in value, options, ref state))
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
