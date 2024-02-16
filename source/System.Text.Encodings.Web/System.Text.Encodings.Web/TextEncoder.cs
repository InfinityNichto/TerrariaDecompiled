using System.Buffers;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Unicode;

namespace System.Text.Encodings.Web;

public abstract class TextEncoder
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public abstract int MaxOutputCharactersPerInputCharacter { get; }

	[CLSCompliant(false)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public unsafe abstract bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe bool TryEncodeUnicodeScalar(uint unicodeScalar, Span<char> buffer, out int charsWritten)
	{
		fixed (char* buffer2 = &MemoryMarshal.GetReference(buffer))
		{
			return TryEncodeUnicodeScalar((int)unicodeScalar, buffer2, buffer.Length, out charsWritten);
		}
	}

	private bool TryEncodeUnicodeScalarUtf8(uint unicodeScalar, Span<char> utf16ScratchBuffer, Span<byte> utf8Destination, out int bytesWritten)
	{
		if (!TryEncodeUnicodeScalar(unicodeScalar, utf16ScratchBuffer, out var charsWritten))
		{
			ThrowArgumentException_MaxOutputCharsPerInputChar();
		}
		utf16ScratchBuffer = utf16ScratchBuffer.Slice(0, charsWritten);
		int num = 0;
		while (!utf16ScratchBuffer.IsEmpty)
		{
			if (Rune.DecodeFromUtf16(utf16ScratchBuffer, out var result, out var charsConsumed) != 0)
			{
				ThrowArgumentException_MaxOutputCharsPerInputChar();
			}
			uint num2 = (uint)UnicodeHelpers.GetUtf8RepresentationForScalarValue((uint)result.Value);
			do
			{
				if (SpanUtility.IsValidIndex(utf8Destination, num))
				{
					utf8Destination[num++] = (byte)num2;
					continue;
				}
				bytesWritten = 0;
				return false;
			}
			while ((num2 >>= 8) != 0);
			utf16ScratchBuffer = utf16ScratchBuffer.Slice(charsConsumed);
		}
		bytesWritten = num;
		return true;
	}

	[CLSCompliant(false)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public unsafe abstract int FindFirstCharacterToEncode(char* text, int textLength);

	[EditorBrowsable(EditorBrowsableState.Never)]
	public abstract bool WillEncode(int unicodeScalar);

	public virtual string Encode(string value)
	{
		if (value == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
		}
		int num = FindFirstCharacterToEncode(value.AsSpan());
		if (num < 0)
		{
			return value;
		}
		return EncodeToNewString(value.AsSpan(), num);
	}

	private string EncodeToNewString(ReadOnlySpan<char> value, int indexOfFirstCharToEncode)
	{
		ReadOnlySpan<char> source = value.Slice(indexOfFirstCharToEncode);
		Span<char> initialBuffer = stackalloc char[1024];
		System.Text.ValueStringBuilder valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
		int val = Math.Max(MaxOutputCharactersPerInputCharacter, 1024);
		do
		{
			Span<char> destination = valueStringBuilder.AppendSpan(Math.Max(source.Length, val));
			EncodeCore(source, destination, out var charsConsumed, out var charsWritten, isFinalBlock: true);
			if (charsWritten == 0 || (uint)charsWritten > (uint)destination.Length)
			{
				ThrowArgumentException_MaxOutputCharsPerInputChar();
			}
			source = source.Slice(charsConsumed);
			valueStringBuilder.Length -= destination.Length - charsWritten;
		}
		while (!source.IsEmpty);
		string result = string.Concat(value.Slice(0, indexOfFirstCharToEncode), valueStringBuilder.AsSpan());
		valueStringBuilder.Dispose();
		return result;
	}

	public void Encode(TextWriter output, string value)
	{
		Encode(output, value, 0, value.Length);
	}

	public virtual void Encode(TextWriter output, string value, int startIndex, int characterCount)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (output == null)
		{
			throw new ArgumentNullException("output");
		}
		ValidateRanges(startIndex, characterCount, value.Length);
		int num = FindFirstCharacterToEncode(value.AsSpan(startIndex, characterCount));
		if (num < 0)
		{
			num = characterCount;
		}
		output.WritePartialString(value, startIndex, num);
		if (num != characterCount)
		{
			EncodeCore(output, value.AsSpan(startIndex + num, characterCount - num));
		}
	}

	public virtual void Encode(TextWriter output, char[] value, int startIndex, int characterCount)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (output == null)
		{
			throw new ArgumentNullException("output");
		}
		ValidateRanges(startIndex, characterCount, value.Length);
		int num = FindFirstCharacterToEncode(value.AsSpan(startIndex, characterCount));
		if (num < 0)
		{
			num = characterCount;
		}
		output.Write(value, startIndex, num);
		if (num != characterCount)
		{
			EncodeCore(output, value.AsSpan(startIndex + num, characterCount - num));
		}
	}

	public virtual OperationStatus EncodeUtf8(ReadOnlySpan<byte> utf8Source, Span<byte> utf8Destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
	{
		ReadOnlySpan<byte> utf8Text = utf8Source;
		if (utf8Destination.Length < utf8Source.Length)
		{
			utf8Text = utf8Source.Slice(0, utf8Destination.Length);
		}
		int num = FindFirstCharacterToEncodeUtf8(utf8Text);
		if (num < 0)
		{
			num = utf8Text.Length;
		}
		utf8Source.Slice(0, num).CopyTo(utf8Destination);
		if (num == utf8Source.Length)
		{
			bytesConsumed = utf8Source.Length;
			bytesWritten = utf8Source.Length;
			return OperationStatus.Done;
		}
		int bytesConsumed2;
		int bytesWritten2;
		OperationStatus result = EncodeUtf8Core(utf8Source.Slice(num), utf8Destination.Slice(num), out bytesConsumed2, out bytesWritten2, isFinalBlock);
		bytesConsumed = num + bytesConsumed2;
		bytesWritten = num + bytesWritten2;
		return result;
	}

	private protected virtual OperationStatus EncodeUtf8Core(ReadOnlySpan<byte> utf8Source, Span<byte> utf8Destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)
	{
		int length = utf8Source.Length;
		int length2 = utf8Destination.Length;
		Span<char> utf16ScratchBuffer = stackalloc char[24];
		OperationStatus result2;
		while (true)
		{
			int bytesConsumed2;
			int num2;
			if (!utf8Source.IsEmpty)
			{
				Rune result;
				OperationStatus operationStatus = Rune.DecodeFromUtf8(utf8Source, out result, out bytesConsumed2);
				if (operationStatus != 0)
				{
					if (!isFinalBlock && operationStatus == OperationStatus.NeedMoreData)
					{
						result2 = OperationStatus.NeedMoreData;
						break;
					}
				}
				else if (!WillEncode(result.Value))
				{
					uint num = (uint)UnicodeHelpers.GetUtf8RepresentationForScalarValue((uint)result.Value);
					num2 = 0;
					while ((uint)num2 < (uint)utf8Destination.Length)
					{
						utf8Destination[num2++] = (byte)num;
						if ((num >>= 8) != 0)
						{
							continue;
						}
						goto IL_008d;
					}
					goto IL_00f9;
				}
				if (TryEncodeUnicodeScalarUtf8((uint)result.Value, utf16ScratchBuffer, utf8Destination, out var bytesWritten2))
				{
					utf8Source = utf8Source.Slice(bytesConsumed2);
					utf8Destination = utf8Destination.Slice(bytesWritten2);
					continue;
				}
				goto IL_00f9;
			}
			result2 = OperationStatus.Done;
			break;
			IL_008d:
			utf8Source = utf8Source.Slice(bytesConsumed2);
			utf8Destination = utf8Destination.Slice(num2);
			continue;
			IL_00f9:
			result2 = OperationStatus.DestinationTooSmall;
			break;
		}
		bytesConsumed = length - utf8Source.Length;
		bytesWritten = length2 - utf8Destination.Length;
		return result2;
	}

	public virtual OperationStatus Encode(ReadOnlySpan<char> source, Span<char> destination, out int charsConsumed, out int charsWritten, bool isFinalBlock = true)
	{
		ReadOnlySpan<char> text = source;
		if (destination.Length < source.Length)
		{
			text = source.Slice(0, destination.Length);
		}
		int num = FindFirstCharacterToEncode(text);
		if (num < 0)
		{
			num = text.Length;
		}
		source.Slice(0, num).CopyTo(destination);
		if (num == source.Length)
		{
			charsConsumed = source.Length;
			charsWritten = source.Length;
			return OperationStatus.Done;
		}
		int charsConsumed2;
		int charsWritten2;
		OperationStatus result = EncodeCore(source.Slice(num), destination.Slice(num), out charsConsumed2, out charsWritten2, isFinalBlock);
		charsConsumed = num + charsConsumed2;
		charsWritten = num + charsWritten2;
		return result;
	}

	private protected virtual OperationStatus EncodeCore(ReadOnlySpan<char> source, Span<char> destination, out int charsConsumed, out int charsWritten, bool isFinalBlock)
	{
		int length = source.Length;
		int length2 = destination.Length;
		OperationStatus result2;
		while (true)
		{
			if (!source.IsEmpty)
			{
				Rune result;
				int charsConsumed2;
				OperationStatus operationStatus = Rune.DecodeFromUtf16(source, out result, out charsConsumed2);
				if (operationStatus != 0)
				{
					if (!isFinalBlock && operationStatus == OperationStatus.NeedMoreData)
					{
						result2 = OperationStatus.NeedMoreData;
						break;
					}
				}
				else if (!WillEncode(result.Value))
				{
					if (result.TryEncodeToUtf16(destination, out var _))
					{
						source = source.Slice(charsConsumed2);
						destination = destination.Slice(charsConsumed2);
						continue;
					}
					goto IL_00ad;
				}
				if (TryEncodeUnicodeScalar((uint)result.Value, destination, out var charsWritten3))
				{
					source = source.Slice(charsConsumed2);
					destination = destination.Slice(charsWritten3);
					continue;
				}
				goto IL_00ad;
			}
			result2 = OperationStatus.Done;
			break;
			IL_00ad:
			result2 = OperationStatus.DestinationTooSmall;
			break;
		}
		charsConsumed = length - source.Length;
		charsWritten = length2 - destination.Length;
		return result2;
	}

	private void EncodeCore(TextWriter output, ReadOnlySpan<char> value)
	{
		int val = Math.Max(MaxOutputCharactersPerInputCharacter, 1024);
		char[] array = ArrayPool<char>.Shared.Rent(Math.Max(value.Length, val));
		Span<char> destination = array;
		do
		{
			EncodeCore(value, destination, out var charsConsumed, out var charsWritten, isFinalBlock: true);
			if (charsWritten == 0 || (uint)charsWritten > (uint)destination.Length)
			{
				ThrowArgumentException_MaxOutputCharsPerInputChar();
			}
			output.Write(array, 0, charsWritten);
			value = value.Slice(charsConsumed);
		}
		while (!value.IsEmpty);
		ArrayPool<char>.Shared.Return(array);
	}

	private protected unsafe virtual int FindFirstCharacterToEncode(ReadOnlySpan<char> text)
	{
		fixed (char* text2 = &MemoryMarshal.GetReference(text))
		{
			return FindFirstCharacterToEncode(text2, text.Length);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public virtual int FindFirstCharacterToEncodeUtf8(ReadOnlySpan<byte> utf8Text)
	{
		int length = utf8Text.Length;
		Rune result;
		int bytesConsumed;
		while (!utf8Text.IsEmpty && Rune.DecodeFromUtf8(utf8Text, out result, out bytesConsumed) == OperationStatus.Done && !WillEncode(result.Value))
		{
			utf8Text = utf8Text.Slice(bytesConsumed);
		}
		if (!utf8Text.IsEmpty)
		{
			return length - utf8Text.Length;
		}
		return -1;
	}

	private static void ValidateRanges(int startIndex, int characterCount, int actualInputLength)
	{
		if (startIndex < 0 || startIndex > actualInputLength)
		{
			throw new ArgumentOutOfRangeException("startIndex");
		}
		if (characterCount < 0 || characterCount > actualInputLength - startIndex)
		{
			throw new ArgumentOutOfRangeException("characterCount");
		}
	}

	[DoesNotReturn]
	private static void ThrowArgumentException_MaxOutputCharsPerInputChar()
	{
		throw new ArgumentException(System.SR.TextEncoderDoesNotImplementMaxOutputCharsPerInputChar);
	}
}
