using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Unicode;

namespace System.Text;

public class UTF8Encoding : Encoding
{
	internal sealed class UTF8EncodingSealed : UTF8Encoding
	{
		public override ReadOnlySpan<byte> Preamble
		{
			get
			{
				if (!_emitUTF8Identifier)
				{
					return default(ReadOnlySpan<byte>);
				}
				return PreambleSpan;
			}
		}

		public UTF8EncodingSealed(bool encoderShouldEmitUTF8Identifier)
			: base(encoderShouldEmitUTF8Identifier)
		{
		}

		public override object Clone()
		{
			return new UTF8Encoding(_emitUTF8Identifier)
			{
				IsReadOnly = false
			};
		}

		public override byte[] GetBytes(string s)
		{
			if (s != null && s.Length <= 32)
			{
				return GetBytesForSmallInput(s);
			}
			return base.GetBytes(s);
		}

		private unsafe byte[] GetBytesForSmallInput(string s)
		{
			byte* ptr = stackalloc byte[96];
			int length = s.Length;
			int bytesCommon;
			fixed (char* pChars = s)
			{
				bytesCommon = GetBytesCommon(pChars, length, ptr, 96);
			}
			return new Span<byte>(ref *ptr, bytesCommon).ToArray();
		}

		public override string GetString(byte[] bytes)
		{
			if (bytes != null && bytes.Length <= 32)
			{
				return GetStringForSmallInput(bytes);
			}
			return base.GetString(bytes);
		}

		private unsafe string GetStringForSmallInput(byte[] bytes)
		{
			char* ptr = stackalloc char[32];
			int byteCount = bytes.Length;
			int charsCommon;
			fixed (byte* pBytes = bytes)
			{
				charsCommon = GetCharsCommon(pBytes, byteCount, ptr, 32);
			}
			return new string(new ReadOnlySpan<char>(ref *ptr, charsCommon));
		}
	}

	internal static readonly UTF8EncodingSealed s_default = new UTF8EncodingSealed(encoderShouldEmitUTF8Identifier: true);

	private readonly bool _emitUTF8Identifier;

	private readonly bool _isThrowException;

	internal static ReadOnlySpan<byte> PreambleSpan => "\ufeff"u8;

	public override ReadOnlySpan<byte> Preamble
	{
		get
		{
			if (!(GetType() != typeof(UTF8Encoding)))
			{
				if (!_emitUTF8Identifier)
				{
					return default(ReadOnlySpan<byte>);
				}
				return PreambleSpan;
			}
			return new ReadOnlySpan<byte>(GetPreamble());
		}
	}

	public UTF8Encoding()
		: base(65001)
	{
	}

	public UTF8Encoding(bool encoderShouldEmitUTF8Identifier)
		: this()
	{
		_emitUTF8Identifier = encoderShouldEmitUTF8Identifier;
	}

	public UTF8Encoding(bool encoderShouldEmitUTF8Identifier, bool throwOnInvalidBytes)
		: this(encoderShouldEmitUTF8Identifier)
	{
		_isThrowException = throwOnInvalidBytes;
		if (_isThrowException)
		{
			SetDefaultFallbacks();
		}
	}

	internal sealed override void SetDefaultFallbacks()
	{
		if (_isThrowException)
		{
			encoderFallback = System.Text.EncoderFallback.ExceptionFallback;
			decoderFallback = System.Text.DecoderFallback.ExceptionFallback;
		}
		else
		{
			encoderFallback = new EncoderReplacementFallback("\ufffd");
			decoderFallback = new DecoderReplacementFallback("\ufffd");
		}
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
		long utf8CodeUnitCountAdjustment;
		int scalarCountAdjustment;
		char* pointerToFirstInvalidChar = Utf16Utility.GetPointerToFirstInvalidChar(pChars, charsLength, out utf8CodeUnitCountAdjustment, out scalarCountAdjustment);
		long num = (charsConsumed = (int)(pointerToFirstInvalidChar - pChars)) + utf8CodeUnitCountAdjustment;
		if ((ulong)num > 2147483647uL)
		{
			Encoding.ThrowConversionOverflow();
		}
		return (int)num;
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
		Utf8Utility.TranscodeToUtf8(pChars, charsLength, pBytes, bytesLength, out var pInputBufferRemaining, out var pOutputBufferRemaining);
		charsConsumed = (int)(pInputBufferRemaining - pChars);
		return (int)(pOutputBufferRemaining - pBytes);
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
		Utf8Utility.TranscodeToUtf16(pBytes, bytesLength, pChars, charsLength, out var pInputBufferRemaining, out var pOutputBufferRemaining);
		bytesConsumed = (int)(pInputBufferRemaining - pBytes);
		return (int)(pOutputBufferRemaining - pChars);
	}

	private protected sealed override int GetCharsWithFallback(ReadOnlySpan<byte> bytes, int originalBytesLength, Span<char> chars, int originalCharsLength, DecoderNLS decoder)
	{
		if (((decoder == null) ? base.DecoderFallback : decoder.Fallback) is DecoderReplacementFallback { MaxCharCount: 1 } decoderReplacementFallback && decoderReplacementFallback.DefaultString[0] == '\ufffd')
		{
			Utf8.ToUtf16(bytes, chars, out var bytesRead, out var charsWritten, replaceInvalidSequences: true, decoder?.MustFlush ?? true);
			bytes = bytes.Slice(bytesRead);
			chars = chars.Slice(charsWritten);
		}
		if (bytes.IsEmpty)
		{
			return originalCharsLength - chars.Length;
		}
		return base.GetCharsWithFallback(bytes, originalBytesLength, chars, originalCharsLength, decoder);
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
		if (count == 0)
		{
			return string.Empty;
		}
		fixed (byte* ptr = bytes)
		{
			return string.CreateStringFromEncoding(ptr + index, count, this);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe int GetCharCountCommon(byte* pBytes, int byteCount)
	{
		int bytesConsumed;
		int num = GetCharCountFast(pBytes, byteCount, null, out bytesConsumed);
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
		int utf16CodeUnitCountAdjustment;
		int scalarCountAdjustment;
		byte* pointerToFirstInvalidByte = Utf8Utility.GetPointerToFirstInvalidByte(pBytes, bytesLength, out utf16CodeUnitCountAdjustment, out scalarCountAdjustment);
		return (bytesConsumed = (int)(pointerToFirstInvalidByte - pBytes)) + utf16CodeUnitCountAdjustment;
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
		byteCount = value.Utf8SequenceLength;
		return true;
	}

	internal sealed override OperationStatus EncodeRune(Rune value, Span<byte> bytes, out int bytesWritten)
	{
		if (!value.TryEncodeToUtf8(bytes, out bytesWritten))
		{
			return OperationStatus.DestinationTooSmall;
		}
		return OperationStatus.Done;
	}

	internal sealed override OperationStatus DecodeFirstRune(ReadOnlySpan<byte> bytes, out Rune value, out int bytesConsumed)
	{
		return Rune.DecodeFromUtf8(bytes, out value, out bytesConsumed);
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
		num *= 3;
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
		long num = (long)byteCount + 1L;
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

	public override byte[] GetPreamble()
	{
		if (_emitUTF8Identifier)
		{
			return new byte[3] { 239, 187, 191 };
		}
		return Array.Empty<byte>();
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value is UTF8Encoding uTF8Encoding)
		{
			if (_emitUTF8Identifier == uTF8Encoding._emitUTF8Identifier && base.EncoderFallback.Equals(uTF8Encoding.EncoderFallback))
			{
				return base.DecoderFallback.Equals(uTF8Encoding.DecoderFallback);
			}
			return false;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.EncoderFallback.GetHashCode() + base.DecoderFallback.GetHashCode() + 65001 + (_emitUTF8Identifier ? 1 : 0);
	}
}
