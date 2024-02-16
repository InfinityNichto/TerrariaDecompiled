using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text;

public class ASCIIEncoding : Encoding
{
	internal sealed class ASCIIEncodingSealed : ASCIIEncoding
	{
		public override object Clone()
		{
			return new ASCIIEncoding
			{
				IsReadOnly = false
			};
		}
	}

	internal static readonly ASCIIEncodingSealed s_default = new ASCIIEncodingSealed();

	public override bool IsSingleByte => true;

	public ASCIIEncoding()
		: base(20127)
	{
	}

	internal sealed override void SetDefaultFallbacks()
	{
		encoderFallback = System.Text.EncoderFallback.ReplacementFallback;
		decoderFallback = System.Text.DecoderFallback.ReplacementFallback;
	}

	public unsafe override int GetByteCount(char[] chars, int index, int count)
	{
		if (chars == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.chars, ExceptionResource.ArgumentNull_Array);
		}
		if ((index | count) < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException((index < 0) ? ExceptionArgument.index : ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (chars.Length - index < count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.chars, ExceptionResource.ArgumentOutOfRange_IndexCountBuffer);
		}
		fixed (char* ptr = chars)
		{
			return GetByteCountCommon(ptr + index, count);
		}
	}

	public unsafe override int GetByteCount(string chars)
	{
		if (chars == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.chars);
		}
		fixed (char* pChars = chars)
		{
			return GetByteCountCommon(pChars, chars.Length);
		}
	}

	[CLSCompliant(false)]
	public unsafe override int GetByteCount(char* chars, int count)
	{
		if (chars == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.chars);
		}
		if (count < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		}
		return GetByteCountCommon(chars, count);
	}

	public unsafe override int GetByteCount(ReadOnlySpan<char> chars)
	{
		fixed (char* pChars = &MemoryMarshal.GetReference(chars))
		{
			return GetByteCountCommon(pChars, chars.Length);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe int GetByteCountCommon(char* pChars, int charCount)
	{
		int charsConsumed;
		int num = GetByteCountFast(pChars, charCount, base.EncoderFallback, out charsConsumed);
		if (charsConsumed != charCount)
		{
			num += GetByteCountWithFallback(pChars, charCount, charsConsumed);
			if (num < 0)
			{
				Encoding.ThrowConversionOverflow();
			}
		}
		return num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private protected unsafe sealed override int GetByteCountFast(char* pChars, int charsLength, EncoderFallback fallback, out int charsConsumed)
	{
		int num = charsLength;
		if (!(fallback is EncoderReplacementFallback { MaxCharCount: 1 } encoderReplacementFallback) || encoderReplacementFallback.DefaultString[0] > '\u007f')
		{
			num = (int)ASCIIUtility.GetIndexOfFirstNonAsciiChar(pChars, (uint)charsLength);
		}
		charsConsumed = num;
		return num;
	}

	public unsafe override int GetBytes(string chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
	{
		if (chars == null || bytes == null)
		{
			ThrowHelper.ThrowArgumentNullException((chars == null) ? ExceptionArgument.chars : ExceptionArgument.bytes, ExceptionResource.ArgumentNull_Array);
		}
		if ((charIndex | charCount) < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException((charIndex < 0) ? ExceptionArgument.charIndex : ExceptionArgument.charCount, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (chars.Length - charIndex < charCount)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.chars, ExceptionResource.ArgumentOutOfRange_IndexCount);
		}
		if ((uint)byteIndex > bytes.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.byteIndex, ExceptionResource.ArgumentOutOfRange_Index);
		}
		fixed (char* ptr2 = chars)
		{
			fixed (byte[] array = bytes)
			{
				byte* ptr = (byte*)((bytes != null && array.Length != 0) ? System.Runtime.CompilerServices.Unsafe.AsPointer(ref array[0]) : null);
				return GetBytesCommon(ptr2 + charIndex, charCount, ptr + byteIndex, bytes.Length - byteIndex);
			}
		}
	}

	public unsafe override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
	{
		if (chars == null || bytes == null)
		{
			ThrowHelper.ThrowArgumentNullException((chars == null) ? ExceptionArgument.chars : ExceptionArgument.bytes, ExceptionResource.ArgumentNull_Array);
		}
		if ((charIndex | charCount) < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException((charIndex < 0) ? ExceptionArgument.charIndex : ExceptionArgument.charCount, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (chars.Length - charIndex < charCount)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.chars, ExceptionResource.ArgumentOutOfRange_IndexCount);
		}
		if ((uint)byteIndex > bytes.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.byteIndex, ExceptionResource.ArgumentOutOfRange_Index);
		}
		fixed (char* ptr = chars)
		{
			fixed (byte* ptr2 = bytes)
			{
				return GetBytesCommon(ptr + charIndex, charCount, ptr2 + byteIndex, bytes.Length - byteIndex);
			}
		}
	}

	[CLSCompliant(false)]
	public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
	{
		if (chars == null || bytes == null)
		{
			ThrowHelper.ThrowArgumentNullException((chars == null) ? ExceptionArgument.chars : ExceptionArgument.bytes, ExceptionResource.ArgumentNull_Array);
		}
		if ((charCount | byteCount) < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException((charCount < 0) ? ExceptionArgument.charCount : ExceptionArgument.byteCount, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		}
		return GetBytesCommon(chars, charCount, bytes, byteCount);
	}

	public unsafe override int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes)
	{
		fixed (char* pChars = &MemoryMarshal.GetReference(chars))
		{
			fixed (byte* pBytes = &MemoryMarshal.GetReference(bytes))
			{
				return GetBytesCommon(pChars, chars.Length, pBytes, bytes.Length);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe int GetBytesCommon(char* pChars, int charCount, byte* pBytes, int byteCount)
	{
		int charsConsumed;
		int bytesFast = GetBytesFast(pChars, charCount, pBytes, byteCount, out charsConsumed);
		if (charsConsumed == charCount)
		{
			return bytesFast;
		}
		return GetBytesWithFallback(pChars, charCount, pBytes, byteCount, charsConsumed, bytesFast);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private protected unsafe sealed override int GetBytesFast(char* pChars, int charsLength, byte* pBytes, int bytesLength, out int charsConsumed)
	{
		return charsConsumed = (int)ASCIIUtility.NarrowUtf16ToAscii(pChars, pBytes, (uint)Math.Min(charsLength, bytesLength));
	}

	private protected unsafe sealed override int GetBytesWithFallback(ReadOnlySpan<char> chars, int originalCharsLength, Span<byte> bytes, int originalBytesLength, EncoderNLS encoder)
	{
		if (((encoder == null) ? base.EncoderFallback : encoder.Fallback) is EncoderReplacementFallback { MaxCharCount: 1 } encoderReplacementFallback && encoderReplacementFallback.DefaultString[0] <= '\u007f')
		{
			byte b = (byte)encoderReplacementFallback.DefaultString[0];
			int num = Math.Min(chars.Length, bytes.Length);
			int num2 = 0;
			fixed (char* ptr2 = &MemoryMarshal.GetReference(chars))
			{
				fixed (byte* ptr = &MemoryMarshal.GetReference(bytes))
				{
					while (num2 < num)
					{
						ptr[num2++] = b;
						if (num2 < num)
						{
							num2 += (int)ASCIIUtility.NarrowUtf16ToAscii(ptr2 + num2, ptr + num2, (uint)(num - num2));
						}
					}
				}
			}
			chars = chars.Slice(num);
			bytes = bytes.Slice(num);
		}
		if (chars.IsEmpty)
		{
			return originalBytesLength - bytes.Length;
		}
		return base.GetBytesWithFallback(chars, originalCharsLength, bytes, originalBytesLength, encoder);
	}

	public unsafe override int GetCharCount(byte[] bytes, int index, int count)
	{
		if (bytes == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bytes, ExceptionResource.ArgumentNull_Array);
		}
		if ((index | count) < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException((index < 0) ? ExceptionArgument.index : ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (bytes.Length - index < count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.bytes, ExceptionResource.ArgumentOutOfRange_IndexCountBuffer);
		}
		fixed (byte* ptr = bytes)
		{
			return GetCharCountCommon(ptr + index, count);
		}
	}

	[CLSCompliant(false)]
	public unsafe override int GetCharCount(byte* bytes, int count)
	{
		if (bytes == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bytes, ExceptionResource.ArgumentNull_Array);
		}
		if (count < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		}
		return GetCharCountCommon(bytes, count);
	}

	public unsafe override int GetCharCount(ReadOnlySpan<byte> bytes)
	{
		fixed (byte* pBytes = &MemoryMarshal.GetReference(bytes))
		{
			return GetCharCountCommon(pBytes, bytes.Length);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe int GetCharCountCommon(byte* pBytes, int byteCount)
	{
		int bytesConsumed;
		int num = GetCharCountFast(pBytes, byteCount, base.DecoderFallback, out bytesConsumed);
		if (bytesConsumed != byteCount)
		{
			num += GetCharCountWithFallback(pBytes, byteCount, bytesConsumed);
			if (num < 0)
			{
				Encoding.ThrowConversionOverflow();
			}
		}
		return num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private protected unsafe sealed override int GetCharCountFast(byte* pBytes, int bytesLength, DecoderFallback fallback, out int bytesConsumed)
	{
		int num = bytesLength;
		if (!(fallback is DecoderReplacementFallback { MaxCharCount: 1 }))
		{
			num = (int)ASCIIUtility.GetIndexOfFirstNonAsciiByte(pBytes, (uint)bytesLength);
		}
		bytesConsumed = num;
		return num;
	}

	public unsafe override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
	{
		if (bytes == null || chars == null)
		{
			ThrowHelper.ThrowArgumentNullException((bytes == null) ? ExceptionArgument.bytes : ExceptionArgument.chars, ExceptionResource.ArgumentNull_Array);
		}
		if ((byteIndex | byteCount) < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException((byteIndex < 0) ? ExceptionArgument.byteIndex : ExceptionArgument.byteCount, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (bytes.Length - byteIndex < byteCount)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.bytes, ExceptionResource.ArgumentOutOfRange_IndexCountBuffer);
		}
		if ((uint)charIndex > (uint)chars.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.charIndex, ExceptionResource.ArgumentOutOfRange_Index);
		}
		fixed (byte* ptr = bytes)
		{
			fixed (char* ptr2 = chars)
			{
				return GetCharsCommon(ptr + byteIndex, byteCount, ptr2 + charIndex, chars.Length - charIndex);
			}
		}
	}

	[CLSCompliant(false)]
	public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
	{
		if (bytes == null || chars == null)
		{
			ThrowHelper.ThrowArgumentNullException((bytes == null) ? ExceptionArgument.bytes : ExceptionArgument.chars, ExceptionResource.ArgumentNull_Array);
		}
		if ((byteCount | charCount) < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException((byteCount < 0) ? ExceptionArgument.byteCount : ExceptionArgument.charCount, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		}
		return GetCharsCommon(bytes, byteCount, chars, charCount);
	}

	public unsafe override int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars)
	{
		fixed (byte* pBytes = &MemoryMarshal.GetReference(bytes))
		{
			fixed (char* pChars = &MemoryMarshal.GetReference(chars))
			{
				return GetCharsCommon(pBytes, bytes.Length, pChars, chars.Length);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe int GetCharsCommon(byte* pBytes, int byteCount, char* pChars, int charCount)
	{
		int bytesConsumed;
		int charsFast = GetCharsFast(pBytes, byteCount, pChars, charCount, out bytesConsumed);
		if (bytesConsumed == byteCount)
		{
			return charsFast;
		}
		return GetCharsWithFallback(pBytes, byteCount, pChars, charCount, bytesConsumed, charsFast);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private protected unsafe sealed override int GetCharsFast(byte* pBytes, int bytesLength, char* pChars, int charsLength, out int bytesConsumed)
	{
		return bytesConsumed = (int)ASCIIUtility.WidenAsciiToUtf16(pBytes, pChars, (uint)Math.Min(bytesLength, charsLength));
	}

	private protected unsafe sealed override int GetCharsWithFallback(ReadOnlySpan<byte> bytes, int originalBytesLength, Span<char> chars, int originalCharsLength, DecoderNLS decoder)
	{
		if (((decoder == null) ? base.DecoderFallback : decoder.Fallback) is DecoderReplacementFallback { MaxCharCount: 1 } decoderReplacementFallback)
		{
			char c = decoderReplacementFallback.DefaultString[0];
			int num = Math.Min(bytes.Length, chars.Length);
			int num2 = 0;
			fixed (byte* ptr2 = &MemoryMarshal.GetReference(bytes))
			{
				fixed (char* ptr = &MemoryMarshal.GetReference(chars))
				{
					while (num2 < num)
					{
						ptr[num2++] = c;
						if (num2 < num)
						{
							num2 += (int)ASCIIUtility.WidenAsciiToUtf16(ptr2 + num2, ptr + num2, (uint)(num - num2));
						}
					}
				}
			}
			bytes = bytes.Slice(num);
			chars = chars.Slice(num);
		}
		if (bytes.IsEmpty)
		{
			return originalCharsLength - chars.Length;
		}
		return base.GetCharsWithFallback(bytes, originalBytesLength, chars, originalCharsLength, decoder);
	}

	public unsafe override string GetString(byte[] bytes, int byteIndex, int byteCount)
	{
		if (bytes == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bytes, ExceptionResource.ArgumentNull_Array);
		}
		if ((byteIndex | byteCount) < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException((byteIndex < 0) ? ExceptionArgument.byteIndex : ExceptionArgument.byteCount, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (bytes.Length - byteIndex < byteCount)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.bytes, ExceptionResource.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (byteCount == 0)
		{
			return string.Empty;
		}
		fixed (byte* ptr = bytes)
		{
			return string.CreateStringFromEncoding(ptr + byteIndex, byteCount, this);
		}
	}

	internal sealed override bool TryGetByteCount(Rune value, out int byteCount)
	{
		if (value.IsAscii)
		{
			byteCount = 1;
			return true;
		}
		byteCount = 0;
		return false;
	}

	internal sealed override OperationStatus EncodeRune(Rune value, Span<byte> bytes, out int bytesWritten)
	{
		if (value.IsAscii)
		{
			if (!bytes.IsEmpty)
			{
				bytes[0] = (byte)value.Value;
				bytesWritten = 1;
				return OperationStatus.Done;
			}
			bytesWritten = 0;
			return OperationStatus.DestinationTooSmall;
		}
		bytesWritten = 0;
		return OperationStatus.InvalidData;
	}

	internal sealed override OperationStatus DecodeFirstRune(ReadOnlySpan<byte> bytes, out Rune value, out int bytesConsumed)
	{
		if (!bytes.IsEmpty)
		{
			byte b = bytes[0];
			if (b <= 127)
			{
				value = new Rune(b);
				bytesConsumed = 1;
				return OperationStatus.Done;
			}
			value = Rune.ReplacementChar;
			bytesConsumed = 1;
			return OperationStatus.InvalidData;
		}
		value = Rune.ReplacementChar;
		bytesConsumed = 0;
		return OperationStatus.NeedMoreData;
	}

	public override int GetMaxByteCount(int charCount)
	{
		if (charCount < 0)
		{
			throw new ArgumentOutOfRangeException("charCount", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		long num = (long)charCount + 1L;
		if (base.EncoderFallback.MaxCharCount > 1)
		{
			num *= base.EncoderFallback.MaxCharCount;
		}
		if (num > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("charCount", SR.ArgumentOutOfRange_GetByteCountOverflow);
		}
		return (int)num;
	}

	public override int GetMaxCharCount(int byteCount)
	{
		if (byteCount < 0)
		{
			throw new ArgumentOutOfRangeException("byteCount", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		long num = byteCount;
		if (base.DecoderFallback.MaxCharCount > 1)
		{
			num *= base.DecoderFallback.MaxCharCount;
		}
		if (num > int.MaxValue)
		{
			throw new ArgumentOutOfRangeException("byteCount", SR.ArgumentOutOfRange_GetCharCountOverflow);
		}
		return (int)num;
	}

	public override Decoder GetDecoder()
	{
		return new DecoderNLS(this);
	}

	public override Encoder GetEncoder()
	{
		return new EncoderNLS(this);
	}
}
