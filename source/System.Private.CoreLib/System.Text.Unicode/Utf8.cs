using System.Buffers;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System.Text.Unicode;

public static class Utf8
{
	public unsafe static OperationStatus FromUtf16(ReadOnlySpan<char> source, Span<byte> destination, out int charsRead, out int bytesWritten, bool replaceInvalidSequences = true, bool isFinalBlock = true)
	{
		_ = source.Length;
		_ = destination.Length;
		fixed (char* ptr = &MemoryMarshal.GetReference(source))
		{
			fixed (byte* ptr2 = &MemoryMarshal.GetReference(destination))
			{
				OperationStatus operationStatus = OperationStatus.Done;
				char* pInputBufferRemaining = ptr;
				byte* pOutputBufferRemaining = ptr2;
				while (!source.IsEmpty)
				{
					operationStatus = Utf8Utility.TranscodeToUtf8((char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source)), source.Length, (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination)), destination.Length, out pInputBufferRemaining, out pOutputBufferRemaining);
					if (operationStatus <= OperationStatus.DestinationTooSmall || (operationStatus == OperationStatus.NeedMoreData && !isFinalBlock))
					{
						break;
					}
					if (!replaceInvalidSequences)
					{
						operationStatus = OperationStatus.InvalidData;
						break;
					}
					destination = destination.Slice((int)(pOutputBufferRemaining - (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination))));
					if (destination.Length <= 2)
					{
						operationStatus = OperationStatus.DestinationTooSmall;
						break;
					}
					destination[0] = 239;
					destination[1] = 191;
					destination[2] = 189;
					destination = destination.Slice(3);
					source = source.Slice((int)(pInputBufferRemaining - (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source))) + 1);
					operationStatus = OperationStatus.Done;
					pInputBufferRemaining = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source));
					pOutputBufferRemaining = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination));
				}
				charsRead = (int)(pInputBufferRemaining - ptr);
				bytesWritten = (int)(pOutputBufferRemaining - ptr2);
				return operationStatus;
			}
		}
	}

	public unsafe static OperationStatus ToUtf16(ReadOnlySpan<byte> source, Span<char> destination, out int bytesRead, out int charsWritten, bool replaceInvalidSequences = true, bool isFinalBlock = true)
	{
		_ = source.Length;
		_ = destination.Length;
		fixed (byte* ptr = &MemoryMarshal.GetReference(source))
		{
			fixed (char* ptr2 = &MemoryMarshal.GetReference(destination))
			{
				OperationStatus operationStatus = OperationStatus.Done;
				byte* pInputBufferRemaining = ptr;
				char* pOutputBufferRemaining = ptr2;
				while (!source.IsEmpty)
				{
					operationStatus = Utf8Utility.TranscodeToUtf16((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source)), source.Length, (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination)), destination.Length, out pInputBufferRemaining, out pOutputBufferRemaining);
					if (operationStatus <= OperationStatus.DestinationTooSmall || (operationStatus == OperationStatus.NeedMoreData && !isFinalBlock))
					{
						break;
					}
					if (!replaceInvalidSequences)
					{
						operationStatus = OperationStatus.InvalidData;
						break;
					}
					destination = destination.Slice((int)(pOutputBufferRemaining - (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination))));
					if (destination.IsEmpty)
					{
						operationStatus = OperationStatus.DestinationTooSmall;
						break;
					}
					destination[0] = '\ufffd';
					destination = destination.Slice(1);
					source = source.Slice((int)(pInputBufferRemaining - (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source))));
					Rune.DecodeFromUtf8(source, out var _, out var bytesConsumed);
					source = source.Slice(bytesConsumed);
					operationStatus = OperationStatus.Done;
					pInputBufferRemaining = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source));
					pOutputBufferRemaining = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination));
				}
				bytesRead = (int)(pInputBufferRemaining - ptr);
				charsWritten = (int)(pOutputBufferRemaining - ptr2);
				return operationStatus;
			}
		}
	}
}
