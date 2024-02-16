using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace System.Text;

public abstract class EncoderFallbackBuffer
{
	internal unsafe char* charStart;

	internal unsafe char* charEnd;

	internal EncoderNLS encoder;

	internal bool setEncoder;

	internal bool bUsedEncoder;

	internal bool bFallingBack;

	internal int iRecursionCount;

	private Encoding encoding;

	private int originalCharCount;

	public abstract int Remaining { get; }

	public abstract bool Fallback(char charUnknown, int index);

	public abstract bool Fallback(char charUnknownHigh, char charUnknownLow, int index);

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
		charStart = null;
		bFallingBack = false;
		iRecursionCount = 0;
		Reset();
	}

	internal unsafe void InternalInitialize(char* charStart, char* charEnd, EncoderNLS encoder, bool setEncoder)
	{
		this.charStart = charStart;
		this.charEnd = charEnd;
		this.encoder = encoder;
		this.setEncoder = setEncoder;
		bUsedEncoder = false;
		bFallingBack = false;
		iRecursionCount = 0;
	}

	internal static EncoderFallbackBuffer CreateAndInitialize(Encoding encoding, EncoderNLS encoder, int originalCharCount)
	{
		EncoderFallbackBuffer encoderFallbackBuffer = ((encoder == null) ? encoding.EncoderFallback.CreateFallbackBuffer() : encoder.FallbackBuffer);
		encoderFallbackBuffer.encoding = encoding;
		encoderFallbackBuffer.encoder = encoder;
		encoderFallbackBuffer.originalCharCount = originalCharCount;
		return encoderFallbackBuffer;
	}

	internal char InternalGetNextChar()
	{
		char nextChar = GetNextChar();
		bFallingBack = nextChar != '\0';
		if (nextChar == '\0')
		{
			iRecursionCount = 0;
		}
		return nextChar;
	}

	private bool InternalFallback(ReadOnlySpan<char> chars, out int charsConsumed)
	{
		char c = chars[0];
		char c2 = '\0';
		if (!chars.IsEmpty)
		{
			c = chars[0];
			if (chars.Length > 1)
			{
				c2 = chars[1];
			}
		}
		int index = originalCharCount - chars.Length;
		if (!char.IsSurrogatePair(c, c2))
		{
			charsConsumed = 1;
			return Fallback(c, index);
		}
		charsConsumed = 2;
		return Fallback(c, c2, index);
	}

	internal int InternalFallbackGetByteCount(ReadOnlySpan<char> chars, out int charsConsumed)
	{
		int result = 0;
		if (InternalFallback(chars, out charsConsumed))
		{
			result = DrainRemainingDataForGetByteCount();
		}
		return result;
	}

	internal bool TryInternalFallbackGetBytes(ReadOnlySpan<char> chars, Span<byte> bytes, out int charsConsumed, out int bytesWritten)
	{
		if (InternalFallback(chars, out charsConsumed))
		{
			return TryDrainRemainingDataForGetBytes(bytes, out bytesWritten);
		}
		bytesWritten = 0;
		return true;
	}

	internal bool TryDrainRemainingDataForGetBytes(Span<byte> bytes, out int bytesWritten)
	{
		int length = bytes.Length;
		while (true)
		{
			Rune nextRune;
			Rune rune = (nextRune = GetNextRune());
			if (rune.Value == 0)
			{
				break;
			}
			int bytesWritten2;
			switch (encoding.EncodeRune(nextRune, bytes, out bytesWritten2))
			{
			case OperationStatus.Done:
				bytes = bytes.Slice(bytesWritten2);
				break;
			case OperationStatus.DestinationTooSmall:
			{
				for (int i = 0; i < nextRune.Utf16SequenceLength; i++)
				{
					MovePrevious();
				}
				bytesWritten = length - bytes.Length;
				return false;
			}
			case OperationStatus.InvalidData:
				ThrowLastCharRecursive(nextRune.Value);
				break;
			}
		}
		bytesWritten = length - bytes.Length;
		return true;
	}

	internal int DrainRemainingDataForGetByteCount()
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
			if (!encoding.TryGetByteCount(nextRune, out var byteCount))
			{
				ThrowLastCharRecursive(nextRune.Value);
			}
			num += byteCount;
			if (num < 0)
			{
				InternalReset();
				Encoding.ThrowConversionOverflow();
			}
		}
		return num;
	}

	private Rune GetNextRune()
	{
		char nextChar = GetNextChar();
		if (Rune.TryCreate(nextChar, out var result) || Rune.TryCreate(nextChar, GetNextChar(), out result))
		{
			return result;
		}
		throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex);
	}

	internal unsafe virtual bool InternalFallback(char ch, ref char* chars)
	{
		int index = (int)(chars - charStart) - 1;
		if (char.IsHighSurrogate(ch))
		{
			if (chars >= charEnd)
			{
				if (encoder != null && !encoder.MustFlush)
				{
					if (setEncoder)
					{
						bUsedEncoder = true;
						encoder._charLeftOver = ch;
					}
					bFallingBack = false;
					return false;
				}
			}
			else
			{
				char c = *chars;
				if (char.IsLowSurrogate(c))
				{
					if (bFallingBack && iRecursionCount++ > 250)
					{
						ThrowLastCharRecursive(char.ConvertToUtf32(ch, c));
					}
					chars++;
					bFallingBack = Fallback(ch, c, index);
					return bFallingBack;
				}
			}
		}
		if (bFallingBack && iRecursionCount++ > 250)
		{
			ThrowLastCharRecursive(ch);
		}
		bFallingBack = Fallback(ch, index);
		return bFallingBack;
	}

	[DoesNotReturn]
	internal static void ThrowLastCharRecursive(int charRecursive)
	{
		throw new ArgumentException(SR.Format(SR.Argument_RecursiveFallback, charRecursive), "chars");
	}
}
