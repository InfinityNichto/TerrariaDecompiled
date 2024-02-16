using System.Runtime.Serialization;

namespace System.Text;

internal class DecoderNLS : Decoder, ISerializable
{
	protected EncodingNLS m_encoding;

	protected bool m_mustFlush;

	internal bool m_throwOnOverflow;

	internal int m_bytesUsed;

	internal DecoderFallback m_fallback;

	internal DecoderFallbackBuffer m_fallbackBuffer;

	internal new DecoderFallback Fallback => m_fallback;

	internal bool InternalHasFallbackBuffer => m_fallbackBuffer != null;

	public new DecoderFallbackBuffer FallbackBuffer
	{
		get
		{
			if (m_fallbackBuffer == null)
			{
				if (m_fallback != null)
				{
					m_fallbackBuffer = m_fallback.CreateFallbackBuffer();
				}
				else
				{
					m_fallbackBuffer = DecoderFallback.ReplacementFallback.CreateFallbackBuffer();
				}
			}
			return m_fallbackBuffer;
		}
	}

	public bool MustFlush => m_mustFlush;

	internal virtual bool HasState => false;

	internal DecoderNLS(EncodingNLS encoding)
	{
		m_encoding = encoding;
		m_fallback = m_encoding.DecoderFallback;
		Reset();
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	public override void Reset()
	{
		if (m_fallbackBuffer != null)
		{
			m_fallbackBuffer.Reset();
		}
	}

	public override int GetCharCount(byte[] bytes, int index, int count)
	{
		return GetCharCount(bytes, index, count, flush: false);
	}

	public unsafe override int GetCharCount(byte[] bytes, int index, int count, bool flush)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes", System.SR.ArgumentNull_Array);
		}
		if (index < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (bytes.Length - index < count)
		{
			throw new ArgumentOutOfRangeException("bytes", System.SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (bytes.Length == 0)
		{
			bytes = new byte[1];
		}
		fixed (byte* ptr = &bytes[0])
		{
			return GetCharCount(ptr + index, count, flush);
		}
	}

	public unsafe override int GetCharCount(byte* bytes, int count, bool flush)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes", System.SR.ArgumentNull_Array);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		m_mustFlush = flush;
		m_throwOnOverflow = true;
		return m_encoding.GetCharCount(bytes, count, this);
	}

	public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
	{
		return GetChars(bytes, byteIndex, byteCount, chars, charIndex, flush: false);
	}

	public unsafe override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, bool flush)
	{
		if (bytes == null || chars == null)
		{
			throw new ArgumentNullException((bytes == null) ? "bytes" : "chars", System.SR.ArgumentNull_Array);
		}
		if (byteIndex < 0 || byteCount < 0)
		{
			throw new ArgumentOutOfRangeException((byteIndex < 0) ? "byteIndex" : "byteCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (bytes.Length - byteIndex < byteCount)
		{
			throw new ArgumentOutOfRangeException("bytes", System.SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (charIndex < 0 || charIndex > chars.Length)
		{
			throw new ArgumentOutOfRangeException("charIndex", System.SR.ArgumentOutOfRange_Index);
		}
		if (bytes.Length == 0)
		{
			bytes = new byte[1];
		}
		int charCount = chars.Length - charIndex;
		if (chars.Length == 0)
		{
			chars = new char[1];
		}
		fixed (byte* ptr = &bytes[0])
		{
			fixed (char* ptr2 = &chars[0])
			{
				return GetChars(ptr + byteIndex, byteCount, ptr2 + charIndex, charCount, flush);
			}
		}
	}

	public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount, bool flush)
	{
		if (chars == null || bytes == null)
		{
			throw new ArgumentNullException((chars == null) ? "chars" : "bytes", System.SR.ArgumentNull_Array);
		}
		if (byteCount < 0 || charCount < 0)
		{
			throw new ArgumentOutOfRangeException((byteCount < 0) ? "byteCount" : "charCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		m_mustFlush = flush;
		m_throwOnOverflow = true;
		return m_encoding.GetChars(bytes, byteCount, chars, charCount, this);
	}

	public unsafe override void Convert(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed)
	{
		if (bytes == null || chars == null)
		{
			throw new ArgumentNullException((bytes == null) ? "bytes" : "chars", System.SR.ArgumentNull_Array);
		}
		if (byteIndex < 0 || byteCount < 0)
		{
			throw new ArgumentOutOfRangeException((byteIndex < 0) ? "byteIndex" : "byteCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (charIndex < 0 || charCount < 0)
		{
			throw new ArgumentOutOfRangeException((charIndex < 0) ? "charIndex" : "charCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (bytes.Length - byteIndex < byteCount)
		{
			throw new ArgumentOutOfRangeException("bytes", System.SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (chars.Length - charIndex < charCount)
		{
			throw new ArgumentOutOfRangeException("chars", System.SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (bytes.Length == 0)
		{
			bytes = new byte[1];
		}
		if (chars.Length == 0)
		{
			chars = new char[1];
		}
		fixed (byte* ptr = &bytes[0])
		{
			fixed (char* ptr2 = &chars[0])
			{
				Convert(ptr + byteIndex, byteCount, ptr2 + charIndex, charCount, flush, out bytesUsed, out charsUsed, out completed);
			}
		}
	}

	public unsafe override void Convert(byte* bytes, int byteCount, char* chars, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed)
	{
		if (chars == null || bytes == null)
		{
			throw new ArgumentNullException((chars == null) ? "chars" : "bytes", System.SR.ArgumentNull_Array);
		}
		if (byteCount < 0 || charCount < 0)
		{
			throw new ArgumentOutOfRangeException((byteCount < 0) ? "byteCount" : "charCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		m_mustFlush = flush;
		m_throwOnOverflow = false;
		m_bytesUsed = 0;
		charsUsed = m_encoding.GetChars(bytes, byteCount, chars, charCount, this);
		bytesUsed = m_bytesUsed;
		completed = bytesUsed == byteCount && (!flush || !HasState) && (m_fallbackBuffer == null || m_fallbackBuffer.Remaining == 0);
	}

	internal void ClearMustFlush()
	{
		m_mustFlush = false;
	}
}
