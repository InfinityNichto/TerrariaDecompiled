using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.InteropServices;

public static class SequenceMarshal
{
	public static bool TryGetReadOnlySequenceSegment<T>(ReadOnlySequence<T> sequence, [NotNullWhen(true)] out ReadOnlySequenceSegment<T>? startSegment, out int startIndex, [NotNullWhen(true)] out ReadOnlySequenceSegment<T>? endSegment, out int endIndex)
	{
		return sequence.TryGetReadOnlySequenceSegment(out startSegment, out startIndex, out endSegment, out endIndex);
	}

	public static bool TryGetArray<T>(ReadOnlySequence<T> sequence, out ArraySegment<T> segment)
	{
		return sequence.TryGetArray(out segment);
	}

	public static bool TryGetReadOnlyMemory<T>(ReadOnlySequence<T> sequence, out ReadOnlyMemory<T> memory)
	{
		if (!sequence.IsSingleSegment)
		{
			memory = default(ReadOnlyMemory<T>);
			return false;
		}
		memory = sequence.First;
		return true;
	}

	public static bool TryRead<T>(ref SequenceReader<byte> reader, out T value) where T : unmanaged
	{
		return reader.TryRead<T>(out value);
	}
}
