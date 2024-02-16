using System.Runtime.Serialization;

namespace System.Text;

internal class EncoderNLS : Encoder, ISerializable
{
	internal char charLeftOver;

	protected EncodingNLS m_encoding;

	protected bool m_mustFlush;

	internal bool m_throwOnOverflow;

	internal int m_charsUsed;

	internal EncoderFallback m_fallback;

	internal EncoderFallbackBuffer m_fallbackBuffer;

	internal new EncoderFallback Fallback => m_fallback;

	internal bool InternalHasFallbackBuffer => m_fallbackBuffer != null;

	public new EncoderFallbackBuffer FallbackBuffer
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
					m_fallbackBuffer = EncoderFallback.ReplacementFallback.CreateFallbackBuffer();
				}
			}
			return m_fallbackBuffer;
		}
	}

	public Encoding Encoding => m_encoding;

	public bool MustFlush => m_mustFlush;

	internal virtual bool HasState => charLeftOver != '\0';

	internal EncoderNLS(EncodingNLS encoding)
	{
		m_encoding = encoding;
		m_fallback = m_encoding.EncoderFallback;
		Reset();
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	public override void Reset()
	{
		charLeftOver = '\0';
		if (m_fallbackBuffer != null)
		{
			m_fallbackBuffer.Reset();
		}
	}

	public unsafe override int GetByteCount(char[] chars, int index, int count, bool flush)
	{
		if (chars == null)
		{
			throw new ArgumentNullException("chars", System.SR.ArgumentNull_Array);
		}
		if (index < 0 || count < 0)
		{
			throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (chars.Length - index < count)
		{
			throw new ArgumentOutOfRangeException("chars", System.SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (chars.Length == 0)
		{
			chars = new char[1];
		}
		int num = -1;
		fixed (char* ptr = &chars[0])
		{
			num = GetByteCount(ptr + index, count, flush);
		}
		return num;
	}

	public unsafe override int GetByteCount(char* chars, int count, bool flush)
	{
		if (chars == null)
		{
			throw new ArgumentNullException("chars", System.SR.ArgumentNull_Array);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		m_mustFlush = flush;
		m_throwOnOverflow = true;
		return m_encoding.GetByteCount(chars, count, this);
	}

	public unsafe override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush)
	{
		if (chars == null || bytes == null)
		{
			throw new ArgumentNullException((chars == null) ? "chars" : "bytes", System.SR.ArgumentNull_Array);
		}
		if (charIndex < 0 || charCount < 0)
		{
			throw new ArgumentOutOfRangeException((charIndex < 0) ? "charIndex" : "charCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (chars.Length - charIndex < charCount)
		{
			throw new ArgumentOutOfRangeException("chars", System.SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (byteIndex < 0 || byteIndex > bytes.Length)
		{
			throw new ArgumentOutOfRangeException("byteIndex", System.SR.ArgumentOutOfRange_Index);
		}
		if (chars.Length == 0)
		{
			chars = new char[1];
		}
		int byteCount = bytes.Length - byteIndex;
		if (bytes.Length == 0)
		{
			bytes = new byte[1];
		}
		fixed (char* ptr = &chars[0])
		{
			fixed (byte* ptr2 = &bytes[0])
			{
				return GetBytes(ptr + charIndex, charCount, ptr2 + byteIndex, byteCount, flush);
			}
		}
	}

	public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount, bool flush)
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
		return m_encoding.GetBytes(chars, charCount, bytes, byteCount, this);
	}

	public unsafe override void Convert(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, int byteCount, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
	{
		if (chars == null || bytes == null)
		{
			throw new ArgumentNullException((chars == null) ? "chars" : "bytes", System.SR.ArgumentNull_Array);
		}
		if (charIndex < 0 || charCount < 0)
		{
			throw new ArgumentOutOfRangeException((charIndex < 0) ? "charIndex" : "charCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (byteIndex < 0 || byteCount < 0)
		{
			throw new ArgumentOutOfRangeException((byteIndex < 0) ? "byteIndex" : "byteCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (chars.Length - charIndex < charCount)
		{
			throw new ArgumentOutOfRangeException("chars", System.SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (bytes.Length - byteIndex < byteCount)
		{
			throw new ArgumentOutOfRangeException("bytes", System.SR.ArgumentOutOfRange_IndexCountBuffer);
		}
		if (chars.Length == 0)
		{
			chars = new char[1];
		}
		if (bytes.Length == 0)
		{
			bytes = new byte[1];
		}
		fixed (char* ptr = &chars[0])
		{
			fixed (byte* ptr2 = &bytes[0])
			{
				Convert(ptr + charIndex, charCount, ptr2 + byteIndex, byteCount, flush, out charsUsed, out bytesUsed, out completed);
			}
		}
	}

	public unsafe override void Convert(char* chars, int charCount, byte* bytes, int byteCount, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
	{
		if (bytes == null || chars == null)
		{
			throw new ArgumentNullException((bytes == null) ? "bytes" : "chars", System.SR.ArgumentNull_Array);
		}
		if (charCount < 0 || byteCount < 0)
		{
			throw new ArgumentOutOfRangeException((charCount < 0) ? "charCount" : "byteCount", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		m_mustFlush = flush;
		m_throwOnOverflow = false;
		m_charsUsed = 0;
		bytesUsed = m_encoding.GetBytes(chars, charCount, bytes, byteCount, this);
		charsUsed = m_charsUsed;
		completed = charsUsed == charCount && (!flush || !HasState) && (m_fallbackBuffer == null || m_fallbackBuffer.Remaining == 0);
	}

	internal void ClearMustFlush()
	{
		m_mustFlush = false;
	}
}
