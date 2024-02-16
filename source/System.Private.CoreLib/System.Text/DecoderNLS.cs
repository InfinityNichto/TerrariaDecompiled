using System.Buffers;
using System.Runtime.InteropServices;

namespace System.Text;

internal class DecoderNLS : Decoder
{
	private readonly Encoding _encoding;

	private bool _mustFlush;

	internal bool _throwOnOverflow;

	internal int _bytesUsed;

	private int _leftoverBytes;

	private int _leftoverByteCount;

	public bool MustFlush => _mustFlush;

	internal virtual bool HasState => _leftoverByteCount != 0;

	internal bool HasLeftoverData => _leftoverByteCount != 0;

	internal DecoderNLS(Encoding encoding)
	{
		_encoding = encoding;
		_fallback = _encoding.DecoderFallback;
		Reset();
	}

	public override void Reset()
	{
		ClearLeftoverData();
		_fallbackBuffer?.Reset();
	}

	public override int GetCharCount(byte[] bytes, int index, int count)
	{
		return GetCharCount(bytes, index, count, flush: false);
	}

	public unsafe override int GetCharCount(byte[] bytes, int index, int count, bool flush)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes", SR.ArgumentNull_Array);
		}
		if (index < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (bytes.Length - index < count)
		{
			throw new ArgumentOutOfRangeException("bytes", SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		fixed (byte* ptr = &MemoryMarshal.GetReference<byte>(bytes))
		{
			return GetCharCount(ptr + index, count, flush);
		}
	}

	public unsafe override int GetCharCount(byte* bytes, int count, bool flush)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes", SR.ArgumentNull_Array);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		_mustFlush = flush;
		_throwOnOverflow = true;
		return _encoding.GetCharCount(bytes, count, this);
	}

	public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
	{
		return GetChars(bytes, byteIndex, byteCount, chars, charIndex, flush: false);
	}

	public unsafe override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, bool flush)
	{
		if (bytes == null || chars == null)
		{
			throw new ArgumentNullException((bytes == null) ? "bytes" : "chars", SR.ArgumentNull_Array);
		}
		if (byteIndex < 0 || byteCount < 0)
		{
			throw new ArgumentOutOfRangeException((byteIndex < 0) ? "byteIndex" : "byteCount", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (bytes.Length - byteIndex < byteCount)
		{
			throw new ArgumentOutOfRangeException("bytes", SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (charIndex < 0 || charIndex > chars.Length)
		{
			throw new ArgumentOutOfRangeException("charIndex", SR.ArgumentOutOfRange_Index);
		}
		int charCount = chars.Length - charIndex;
		fixed (byte* ptr = &MemoryMarshal.GetReference<byte>(bytes))
		{
			fixed (char* ptr2 = &MemoryMarshal.GetReference<char>(chars))
			{
				return GetChars(ptr + byteIndex, byteCount, ptr2 + charIndex, charCount, flush);
			}
		}
	}

	public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount, bool flush)
	{
		if (chars == null || bytes == null)
		{
			throw new ArgumentNullException((chars == null) ? "chars" : "bytes", SR.ArgumentNull_Array);
		}
		if (byteCount < 0 || charCount < 0)
		{
			throw new ArgumentOutOfRangeException((byteCount < 0) ? "byteCount" : "charCount", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		_mustFlush = flush;
		_throwOnOverflow = true;
		return _encoding.GetChars(bytes, byteCount, chars, charCount, this);
	}

	public unsafe override void Convert(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed)
	{
		if (bytes == null || chars == null)
		{
			throw new ArgumentNullException((bytes == null) ? "bytes" : "chars", SR.ArgumentNull_Array);
		}
		if (byteIndex < 0 || byteCount < 0)
		{
			throw new ArgumentOutOfRangeException((byteIndex < 0) ? "byteIndex" : "byteCount", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (charIndex < 0 || charCount < 0)
		{
			throw new ArgumentOutOfRangeException((charIndex < 0) ? "charIndex" : "charCount", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (bytes.Length - byteIndex < byteCount)
		{
			throw new ArgumentOutOfRangeException("bytes", SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (chars.Length - charIndex < charCount)
		{
			throw new ArgumentOutOfRangeException("chars", SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		fixed (byte* ptr = &MemoryMarshal.GetReference<byte>(bytes))
		{
			fixed (char* ptr2 = &MemoryMarshal.GetReference<char>(chars))
			{
				Convert(ptr + byteIndex, byteCount, ptr2 + charIndex, charCount, flush, out bytesUsed, out charsUsed, out completed);
			}
		}
	}

	public unsafe override void Convert(byte* bytes, int byteCount, char* chars, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed)
	{
		if (chars == null || bytes == null)
		{
			throw new ArgumentNullException((chars == null) ? "chars" : "bytes", SR.ArgumentNull_Array);
		}
		if (byteCount < 0 || charCount < 0)
		{
			throw new ArgumentOutOfRangeException((byteCount < 0) ? "byteCount" : "charCount", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		_mustFlush = flush;
		_throwOnOverflow = false;
		_bytesUsed = 0;
		charsUsed = _encoding.GetChars(bytes, byteCount, chars, charCount, this);
		bytesUsed = _bytesUsed;
		completed = bytesUsed == byteCount && (!flush || !HasState) && (_fallbackBuffer == null || _fallbackBuffer.Remaining == 0);
	}

	internal void ClearMustFlush()
	{
		_mustFlush = false;
	}

	internal ReadOnlySpan<byte> GetLeftoverData()
	{
		return MemoryMarshal.AsBytes(new ReadOnlySpan<int>(ref _leftoverBytes, 1)).Slice(0, _leftoverByteCount);
	}

	internal void SetLeftoverData(ReadOnlySpan<byte> bytes)
	{
		bytes.CopyTo(MemoryMarshal.AsBytes(new Span<int>(ref _leftoverBytes, 1)));
		_leftoverByteCount = bytes.Length;
	}

	internal void ClearLeftoverData()
	{
		_leftoverByteCount = 0;
	}

	internal int DrainLeftoverDataForGetCharCount(ReadOnlySpan<byte> bytes, out int bytesConsumed)
	{
		Span<byte> span = stackalloc byte[4];
		span = span.Slice(0, ConcatInto(GetLeftoverData(), bytes, span));
		int result = 0;
		Rune value;
		int bytesConsumed2;
		switch (_encoding.DecodeFirstRune(span, out value, out bytesConsumed2))
		{
		case OperationStatus.Done:
			result = value.Utf16SequenceLength;
			break;
		case OperationStatus.NeedMoreData:
			if (!MustFlush)
			{
				break;
			}
			goto default;
		default:
			if (base.FallbackBuffer.Fallback(span.Slice(0, bytesConsumed2).ToArray(), -_leftoverByteCount))
			{
				result = _fallbackBuffer.DrainRemainingDataForGetCharCount();
			}
			break;
		}
		bytesConsumed = bytesConsumed2 - _leftoverByteCount;
		return result;
	}

	internal int DrainLeftoverDataForGetChars(ReadOnlySpan<byte> bytes, Span<char> chars, out int bytesConsumed)
	{
		Span<byte> span = stackalloc byte[4];
		span = span.Slice(0, ConcatInto(GetLeftoverData(), bytes, span));
		int charsWritten = 0;
		bool flag = false;
		Rune value;
		int bytesConsumed2;
		switch (_encoding.DecodeFirstRune(span, out value, out bytesConsumed2))
		{
		case OperationStatus.Done:
			if (!value.TryEncodeToUtf16(chars, out charsWritten))
			{
				break;
			}
			goto IL_00aa;
		case OperationStatus.NeedMoreData:
			if (MustFlush)
			{
				goto default;
			}
			flag = true;
			goto IL_00aa;
		default:
			{
				if (base.FallbackBuffer.Fallback(span.Slice(0, bytesConsumed2).ToArray(), -_leftoverByteCount) && !_fallbackBuffer.TryDrainRemainingDataForGetChars(chars, out charsWritten))
				{
					break;
				}
				goto IL_00aa;
			}
			IL_00aa:
			bytesConsumed = bytesConsumed2 - _leftoverByteCount;
			if (flag)
			{
				SetLeftoverData(span);
			}
			else
			{
				ClearLeftoverData();
			}
			return charsWritten;
		}
		_encoding.ThrowCharsOverflow(this, nothingDecoded: true);
		throw null;
	}

	private static int ConcatInto(ReadOnlySpan<byte> srcLeft, ReadOnlySpan<byte> srcRight, Span<byte> dest)
	{
		int num = 0;
		int num2 = 0;
		while (true)
		{
			if (num2 < srcLeft.Length)
			{
				if ((uint)num >= (uint)dest.Length)
				{
					break;
				}
				dest[num++] = srcLeft[num2];
				num2++;
				continue;
			}
			for (int i = 0; i < srcRight.Length; i++)
			{
				if ((uint)num >= (uint)dest.Length)
				{
					break;
				}
				dest[num++] = srcRight[i];
			}
			break;
		}
		return num;
	}
}
