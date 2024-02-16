namespace System.Text;

internal sealed class EncodingByteBuffer
{
	private unsafe byte* _bytes;

	private unsafe readonly byte* _byteStart;

	private unsafe readonly byte* _byteEnd;

	private unsafe char* _chars;

	private unsafe readonly char* _charStart;

	private unsafe readonly char* _charEnd;

	private int _byteCountResult;

	private readonly EncodingNLS _enc;

	private readonly System.Text.EncoderNLS _encoder;

	internal EncoderFallbackBuffer fallbackBuffer;

	internal EncoderFallbackBufferHelper fallbackBufferHelper;

	internal unsafe bool MoreData
	{
		get
		{
			if (fallbackBuffer.Remaining <= 0)
			{
				return _chars < _charEnd;
			}
			return true;
		}
	}

	internal unsafe int CharsUsed => (int)(_chars - _charStart);

	internal int Count => _byteCountResult;

	internal unsafe EncodingByteBuffer(EncodingNLS inEncoding, System.Text.EncoderNLS inEncoder, byte* inByteStart, int inByteCount, char* inCharStart, int inCharCount)
	{
		_enc = inEncoding;
		_encoder = inEncoder;
		_charStart = inCharStart;
		_chars = inCharStart;
		_charEnd = inCharStart + inCharCount;
		_bytes = inByteStart;
		_byteStart = inByteStart;
		_byteEnd = inByteStart + inByteCount;
		if (_encoder == null)
		{
			fallbackBuffer = _enc.EncoderFallback.CreateFallbackBuffer();
		}
		else
		{
			fallbackBuffer = _encoder.FallbackBuffer;
			if (_encoder.m_throwOnOverflow && _encoder.InternalHasFallbackBuffer && fallbackBuffer.Remaining > 0)
			{
				throw new ArgumentException(System.SR.Format(System.SR.Argument_EncoderFallbackNotEmpty, _encoder.Encoding.EncodingName, _encoder.Fallback.GetType()));
			}
		}
		fallbackBufferHelper = new EncoderFallbackBufferHelper(fallbackBuffer);
		fallbackBufferHelper.InternalInitialize(_chars, _charEnd, _encoder, _bytes != null);
	}

	internal unsafe bool AddByte(byte b, int moreBytesExpected)
	{
		if (_bytes != null)
		{
			if (_bytes >= _byteEnd - moreBytesExpected)
			{
				MovePrevious(bThrow: true);
				return false;
			}
			*(_bytes++) = b;
		}
		_byteCountResult++;
		return true;
	}

	internal bool AddByte(byte b1)
	{
		return AddByte(b1, 0);
	}

	internal bool AddByte(byte b1, byte b2)
	{
		return AddByte(b1, b2, 0);
	}

	internal bool AddByte(byte b1, byte b2, int moreBytesExpected)
	{
		if (AddByte(b1, 1 + moreBytesExpected))
		{
			return AddByte(b2, moreBytesExpected);
		}
		return false;
	}

	internal bool AddByte(byte b1, byte b2, byte b3)
	{
		return AddByte(b1, b2, b3, 0);
	}

	internal bool AddByte(byte b1, byte b2, byte b3, int moreBytesExpected)
	{
		if (AddByte(b1, 2 + moreBytesExpected) && AddByte(b2, 1 + moreBytesExpected))
		{
			return AddByte(b3, moreBytesExpected);
		}
		return false;
	}

	internal bool AddByte(byte b1, byte b2, byte b3, byte b4)
	{
		if (AddByte(b1, 3) && AddByte(b2, 2) && AddByte(b3, 1))
		{
			return AddByte(b4, 0);
		}
		return false;
	}

	internal unsafe void MovePrevious(bool bThrow)
	{
		if (fallbackBufferHelper.bFallingBack)
		{
			fallbackBuffer.MovePrevious();
		}
		else if (_chars > _charStart)
		{
			_chars--;
		}
		if (bThrow)
		{
			_enc.ThrowBytesOverflow(_encoder, _bytes == _byteStart);
		}
	}

	internal unsafe bool Fallback(char charFallback)
	{
		return fallbackBufferHelper.InternalFallback(charFallback, ref _chars);
	}

	internal unsafe char GetNextChar()
	{
		char c = fallbackBufferHelper.InternalGetNextChar();
		if (c == '\0' && _chars < _charEnd)
		{
			c = *(_chars++);
		}
		return c;
	}
}
