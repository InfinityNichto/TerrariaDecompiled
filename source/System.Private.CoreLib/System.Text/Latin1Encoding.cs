using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text;

internal class Latin1Encoding : Encoding
{
	internal sealed class Latin1EncodingSealed : Latin1Encoding
	{
		public override object Clone()
		{
			return new Latin1Encoding
			{
				IsReadOnly = false
			};
		}
	}

	internal static readonly Latin1EncodingSealed s_default = new Latin1EncodingSealed();

	public override ReadOnlySpan<byte> Preamble => default(ReadOnlySpan<byte>);

	public override bool IsSingleByte => true;

	public Latin1Encoding()
		: base(28591)
	{
	}

	internal override void SetDefaultFallbacks()
	{
		encoderFallback = EncoderLatin1BestFitFallback.SingletonInstance;
		decoderFallback = System.Text.DecoderFallback.ReplacementFallback;
	}

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

	public unsafe override int GetByteCount(ReadOnlySpan<char> chars)
	{
		fixed (char* pChars = &MemoryMarshal.GetReference(chars))
		{
			return GetByteCountCommon(pChars, chars.Length);
		}
	}

	public unsafe override int GetByteCount(string s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		fixed (char* pChars = s)
		{
			return GetByteCountCommon(pChars, s.Length);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe int GetByteCountCommon(char* pChars, int charCount)
	{
		int charsConsumed;
		int num = GetByteCountFast(pChars, charCount, null, out charsConsumed);
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
		if (!FallbackSupportsFastGetByteCount(fallback))
		{
			num = (int)Latin1Utility.GetIndexOfFirstNonLatin1Char(pChars, (uint)charsLength);
		}
		charsConsumed = num;
		return num;
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

	public unsafe override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex)
	{
		if (s == null || bytes == null)
		{
			ThrowHelper.ThrowArgumentNullException((s == null) ? ExceptionArgument.s : ExceptionArgument.bytes, ExceptionResource.ArgumentNull_Array);
		}
		if ((charIndex | charCount) < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException((charIndex < 0) ? ExceptionArgument.charIndex : ExceptionArgument.charCount, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (s.Length - charIndex < charCount)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.s, ExceptionResource.ArgumentOutOfRange_IndexCount);
		}
		if ((uint)byteIndex > bytes.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.byteIndex, ExceptionResource.ArgumentOutOfRange_Index);
		}
		fixed (char* ptr2 = s)
		{
			fixed (byte[] array = bytes)
			{
				byte* ptr = (byte*)((bytes != null && array.Length != 0) ? System.Runtime.CompilerServices.Unsafe.AsPointer(ref array[0]) : null);
				return GetBytesCommon(ptr2 + charIndex, charCount, ptr + byteIndex, bytes.Length - byteIndex);
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
		return charsConsumed = (int)Latin1Utility.NarrowUtf16ToLatin1(pChars, pBytes, (uint)Math.Min(charsLength, bytesLength));
	}

	public unsafe override int GetCharCount(byte* bytes, int count)
	{
		if (bytes == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bytes);
		}
		if (count < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		}
		return count;
	}

	public override int GetCharCount(byte[] bytes)
	{
		if (bytes == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bytes);
		}
		return bytes.Length;
	}

	public override int GetCharCount(byte[] bytes, int index, int count)
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
		return count;
	}

	public override int GetCharCount(ReadOnlySpan<byte> bytes)
	{
		return bytes.Length;
	}

	private protected unsafe override int GetCharCountFast(byte* pBytes, int bytesLength, DecoderFallback fallback, out int bytesConsumed)
	{
		bytesConsumed = bytesLength;
		return bytesLength;
	}

	public override int GetMaxCharCount(int byteCount)
	{
		if (byteCount < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.byteCount, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		}
		return byteCount;
	}

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

	public unsafe override char[] GetChars(byte[] bytes)
	{
		if (bytes == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bytes);
		}
		if (bytes.Length == 0)
		{
			return Array.Empty<char>();
		}
		char[] array = new char[bytes.Length];
		fixed (byte* pBytes = bytes)
		{
			fixed (char* pChars = array)
			{
				GetCharsCommon(pBytes, bytes.Length, pChars, array.Length);
			}
		}
		return array;
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

	public unsafe override char[] GetChars(byte[] bytes, int index, int count)
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
		char[] array = new char[count];
		fixed (byte* ptr = bytes)
		{
			fixed (char* pChars = array)
			{
				GetCharsCommon(ptr + index, count, pChars, array.Length);
			}
		}
		return array;
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

	public unsafe override string GetString(byte[] bytes)
	{
		if (bytes == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.bytes);
		}
		return string.Create(bytes.Length, (this, bytes), delegate(Span<char> chars, (Latin1Encoding encoding, byte[] bytes) args)
		{
			fixed (byte* pBytes = args.bytes)
			{
				fixed (char* pChars = chars)
				{
					args.encoding.GetCharsCommon(pBytes, args.bytes.Length, pChars, chars.Length);
				}
			}
		});
	}

	public unsafe override string GetString(byte[] bytes, int index, int count)
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
		return string.Create(count, (this, bytes, index), delegate(Span<char> chars, (Latin1Encoding encoding, byte[] bytes, int index) args)
		{
			fixed (byte* ptr = args.bytes)
			{
				fixed (char* pChars = chars)
				{
					args.encoding.GetCharsCommon(ptr + args.index, chars.Length, pChars, chars.Length);
				}
			}
		});
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe int GetCharsCommon(byte* pBytes, int byteCount, char* pChars, int charCount)
	{
		if (byteCount > charCount)
		{
			ThrowCharsOverflow();
		}
		Latin1Utility.WidenLatin1ToUtf16(pBytes, pChars, (uint)byteCount);
		return byteCount;
	}

	private protected unsafe sealed override int GetCharsFast(byte* pBytes, int bytesLength, char* pChars, int charsLength, out int bytesConsumed)
	{
		int num = Math.Min(bytesLength, charsLength);
		Latin1Utility.WidenLatin1ToUtf16(pBytes, pChars, (uint)num);
		bytesConsumed = num;
		return num;
	}

	public override Decoder GetDecoder()
	{
		return new DecoderNLS(this);
	}

	public override Encoder GetEncoder()
	{
		return new EncoderNLS(this);
	}

	internal sealed override bool TryGetByteCount(Rune value, out int byteCount)
	{
		if (value.Value <= 255)
		{
			byteCount = 1;
			return true;
		}
		byteCount = 0;
		return false;
	}

	internal sealed override OperationStatus EncodeRune(Rune value, Span<byte> bytes, out int bytesWritten)
	{
		if (value.Value <= 255)
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
			if (b <= byte.MaxValue)
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

	public override bool IsAlwaysNormalized(NormalizationForm form)
	{
		return form == NormalizationForm.FormC;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool FallbackSupportsFastGetByteCount(EncoderFallback fallback)
	{
		if (fallback == null)
		{
			return false;
		}
		if (fallback is EncoderLatin1BestFitFallback)
		{
			return true;
		}
		if (fallback is EncoderReplacementFallback { MaxCharCount: 1 } encoderReplacementFallback && encoderReplacementFallback.DefaultString[0] <= 'ÿ')
		{
			return true;
		}
		return false;
	}
}
