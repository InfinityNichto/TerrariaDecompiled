using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Converters;

internal sealed class IAsyncEnumerableOfTConverter<TAsyncEnumerable, TElement> : JsonCollectionConverter<TAsyncEnumerable, TElement> where TAsyncEnumerable : IAsyncEnumerable<TElement>
{
	private sealed class BufferedAsyncEnumerable : IAsyncEnumerable<TElement>
	{
		public readonly List<TElement> _buffer = new List<TElement>();

		public async IAsyncEnumerator<TElement> GetAsyncEnumerator(CancellationToken _)
		{
			foreach (TElement item in _buffer)
			{
				yield return item;
			}
		}
	}

	internal override bool OnTryRead(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, ref ReadStack state, out TAsyncEnumerable value)
	{
		if (!typeToConvert.IsAssignableFrom(typeof(IAsyncEnumerable<TElement>)))
		{
			ThrowHelper.ThrowNotSupportedException_CannotPopulateCollection(TypeToConvert, ref reader, ref state);
		}
		return base.OnTryRead(ref reader, typeToConvert, options, ref state, out value);
	}

	protected override void Add(in TElement value, ref ReadStack state)
	{
		((BufferedAsyncEnumerable)state.Current.ReturnValue)._buffer.Add(value);
	}

	protected override void CreateCollection(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
	{
		state.Current.ReturnValue = new BufferedAsyncEnumerable();
	}

	internal override bool OnTryWrite(Utf8JsonWriter writer, TAsyncEnumerable value, JsonSerializerOptions options, ref WriteStack state)
	{
		if (!state.SupportContinuation)
		{
			ThrowHelper.ThrowNotSupportedException_TypeRequiresAsyncSerialization(TypeToConvert);
		}
		return base.OnTryWrite(writer, value, options, ref state);
	}

	protected override bool OnWriteResume(Utf8JsonWriter writer, TAsyncEnumerable value, JsonSerializerOptions options, ref WriteStack state)
	{
		IAsyncEnumerator<TElement> asyncEnumerator;
		ValueTask<bool> valueTask;
		if (state.Current.AsyncDisposable == null)
		{
			asyncEnumerator = value.GetAsyncEnumerator(state.CancellationToken);
			state.Current.AsyncDisposable = asyncEnumerator;
			valueTask = asyncEnumerator.MoveNextAsync();
			if (!valueTask.IsCompleted)
			{
				state.SuppressFlush = true;
				goto IL_00fb;
			}
		}
		else
		{
			asyncEnumerator = (IAsyncEnumerator<TElement>)state.Current.AsyncDisposable;
			if (state.Current.AsyncEnumeratorIsPendingCompletion)
			{
				valueTask = new ValueTask<bool>((Task<bool>)state.PendingTask);
				state.Current.AsyncEnumeratorIsPendingCompletion = false;
				state.PendingTask = null;
			}
			else
			{
				valueTask = new ValueTask<bool>(result: true);
			}
		}
		JsonConverter<TElement> elementConverter = JsonCollectionConverter<TAsyncEnumerable, TElement>.GetElementConverter(ref state);
		do
		{
			if (!valueTask.Result)
			{
				state.Current.AsyncDisposable = null;
				state.AddCompletedAsyncDisposable(asyncEnumerator);
				return true;
			}
			if (ShouldFlush(writer, ref state))
			{
				return false;
			}
			TElement value2 = asyncEnumerator.Current;
			if (!elementConverter.TryWrite(writer, in value2, options, ref state))
			{
				return false;
			}
			valueTask = asyncEnumerator.MoveNextAsync();
		}
		while (valueTask.IsCompleted);
		goto IL_00fb;
		IL_00fb:
		state.PendingTask = valueTask.AsTask();
		state.Current.AsyncEnumeratorIsPendingCompletion = true;
		return false;
	}
}
