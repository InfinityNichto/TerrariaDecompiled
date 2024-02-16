using System.Diagnostics.CodeAnalysis;

namespace System.Text;

public abstract class DecoderFallbackBuffer
{
	internal unsafe byte* byteStart;

	internal unsafe char* charEnd;

	internal Encoding _encoding;

	internal DecoderNLS _decoder;

	private int _originalByteCount;

	public abstract int Remaining { get; }

	public abstract bool Fallback(byte[] bytesUnknown, int index);

	public abstract char GetNextChar();

	public abstract bool MovePrevious();

	public virtual void Reset()
	{
		while (GetNextChar() != 0)
		{
		}
	}

	internal unsafe void InternalReset()
	{
		byteStart = null;
		Reset();
	}

	internal unsafe void InternalInitialize(byte* byteStart, char* charEnd)
	{
		this.byteStart = byteStart;
		this.charEnd = charEnd;
	}

	internal static DecoderFallbackBuffer CreateAndInitialize(Encoding encoding, DecoderNLS decoder, int originalByteCount)
	{
		DecoderFallbackBuffer decoderFallbackBuffer = ((decoder == null) ? encoding.DecoderFallback.CreateFallbackBuffer() : decoder.FallbackBuffer);
		decoderFallbackBuffer._encoding = encoding;
		decoderFallbackBuffer._decoder = decoder;
		decoderFallbackBuffer._originalByteCount = originalByteCount;
		return decoderFallbackBuffer;
	}

	internal unsafe virtual bool InternalFallback(byte[] bytes, byte* pBytes, ref char* chars)
	{
		if (Fallback(bytes, (int)(pBytes - byteStart - bytes.Length)))
		{
			char* ptr = chars;
			bool flag = false;
			char nextChar;
			while ((nextChar = GetNextChar()) != 0)
			{
				if (char.IsSurrogate(nextChar))
				{
					if (char.IsHighSurrogate(nextChar))
					{
						if (flag)
						{
							throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex);
						}
						flag = true;
					}
					else
					{
						if (!flag)
						{
							throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex);
						}
						flag = false;
					}
				}
				if (ptr >= charEnd)
				{
					return false;
				}
				*(ptr++) = nextChar;
			}
			if (flag)
			{
				throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex);
			}
			chars = ptr;
		}
		return true;
	}

	internal unsafe virtual int InternalFallback(byte[] bytes, byte* pBytes)
	{
		if (Fallback(bytes, (int)(pBytes - byteStart - bytes.Length)))
		{
			int num = 0;
			bool flag = false;
			char nextChar;
			while ((nextChar = GetNextChar()) != 0)
			{
				if (char.IsSurrogate(nextChar))
				{
					if (char.IsHighSurrogate(nextChar))
					{
						if (flag)
						{
							throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex);
						}
						flag = true;
					}
					else
					{
						if (!flag)
						{
							throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex);
						}
						flag = false;
					}
				}
				num++;
			}
			if (flag)
			{
				throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex);
			}
			return num;
		}
		return 0;
	}

	internal int InternalFallbackGetCharCount(ReadOnlySpan<byte> remainingBytes, int fallbackLength)
	{
		if (!Fallback(remainingBytes.Slice(0, fallbackLength).ToArray(), _originalByteCount - remainingBytes.Length))
		{
			return 0;
		}
		return DrainRemainingDataForGetCharCount();
	}

	internal bool TryInternalFallbackGetChars(ReadOnlySpan<byte> remainingBytes, int fallbackLength, Span<char> chars, out int charsWritten)
	{
		if (Fallback(remainingBytes.Slice(0, fallbackLength).ToArray(), _originalByteCount - remainingBytes.Length))
		{
			return TryDrainRemainingDataForGetChars(chars, out charsWritten);
		}
		charsWritten = 0;
		return true;
	}

	private Rune GetNextRune()
	{
		char nextChar = GetNextChar();
		if (!Rune.TryCreate(nextChar, out var result) && !Rune.TryCreate(nextChar, GetNextChar(), out result))
		{
			throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex);
		}
		return result;
	}

	internal int DrainRemainingDataForGetCharCount()
	{
		int num = 0;
		while (true)
		{
			Rune nextRune;
			Rune rune = (nextRune = GetNextRune());
			if (rune.Value == 0)
			{
				break;
			}
			num += nextRune.Utf16SequenceLength;
			if (num < 0)
			{
				InternalReset();
				Encoding.ThrowConversionOverflow();
			}
		}
		return num;
	}

	internal bool TryDrainRemainingDataForGetChars(Span<char> chars, out int charsWritten)
	{
		int length = chars.Length;
		while (true)
		{
			Rune nextRune;
			Rune rune = (nextRune = GetNextRune());
			if (rune.Value == 0)
			{
				break;
			}
			if (nextRune.TryEncodeToUtf16(chars, out var charsWritten2))
			{
				chars = chars.Slice(charsWritten2);
				continue;
			}
			InternalReset();
			charsWritten = 0;
			return false;
		}
		charsWritten = length - chars.Length;
		return true;
	}

	[DoesNotReturn]
	internal static void ThrowLastBytesRecursive(byte[] bytesUnknown)
	{
		if (bytesUnknown == null)
		{
			bytesUnknown = Array.Empty<byte>();
		}
		StringBuilder stringBuilder = new StringBuilder(bytesUnknown.Length * 3);
		int i;
		for (i = 0; i < bytesUnknown.Length && i < 20; i++)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(' ');
			}
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(2, 1, stringBuilder2);
			handler.AppendLiteral("\\x");
			handler.AppendFormatted(bytesUnknown[i], "X2");
			stringBuilder2.Append(ref handler);
		}
		if (i == 20)
		{
			stringBuilder.Append(" ...");
		}
		throw new ArgumentException(SR.Format(SR.Argument_RecursiveFallbackBytes, stringBuilder.ToString()), "bytesUnknown");
	}
}
