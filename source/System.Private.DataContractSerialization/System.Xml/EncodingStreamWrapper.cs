using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace System.Xml;

internal sealed class EncodingStreamWrapper : Stream
{
	private enum SupportedEncoding
	{
		UTF8,
		UTF16LE,
		UTF16BE,
		None
	}

	private static readonly UTF8Encoding s_safeUTF8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

	private static readonly UnicodeEncoding s_safeUTF16 = new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: false);

	private static readonly UnicodeEncoding s_safeBEUTF16 = new UnicodeEncoding(bigEndian: true, byteOrderMark: false, throwOnInvalidBytes: false);

	private static readonly UTF8Encoding s_validatingUTF8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	private static readonly UnicodeEncoding s_validatingUTF16 = new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);

	private static readonly UnicodeEncoding s_validatingBEUTF16 = new UnicodeEncoding(bigEndian: true, byteOrderMark: false, throwOnInvalidBytes: true);

	private const int BufferLength = 128;

	private static readonly byte[] s_encodingAttr = new byte[8] { 101, 110, 99, 111, 100, 105, 110, 103 };

	private static readonly byte[] s_encodingUTF8 = new byte[5] { 117, 116, 102, 45, 56 };

	private static readonly byte[] s_encodingUnicode = new byte[6] { 117, 116, 102, 45, 49, 54 };

	private static readonly byte[] s_encodingUnicodeLE = new byte[8] { 117, 116, 102, 45, 49, 54, 108, 101 };

	private static readonly byte[] s_encodingUnicodeBE = new byte[8] { 117, 116, 102, 45, 49, 54, 98, 101 };

	private SupportedEncoding _encodingCode;

	private Encoding _encoding;

	private readonly Encoder _enc;

	private readonly Decoder _dec;

	private readonly bool _isReading;

	private readonly Stream _stream;

	private char[] _chars;

	private byte[] _bytes;

	private int _byteOffset;

	private int _byteCount;

	private readonly byte[] _byteBuffer = new byte[1];

	public override bool CanRead
	{
		get
		{
			if (!_isReading)
			{
				return false;
			}
			return _stream.CanRead;
		}
	}

	public override bool CanSeek => false;

	public override bool CanWrite
	{
		get
		{
			if (_isReading)
			{
				return false;
			}
			return _stream.CanWrite;
		}
	}

	public override long Position
	{
		get
		{
			throw new NotSupportedException();
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public override bool CanTimeout => _stream.CanTimeout;

	public override long Length => _stream.Length;

	public override int ReadTimeout
	{
		get
		{
			return _stream.ReadTimeout;
		}
		set
		{
			_stream.ReadTimeout = value;
		}
	}

	public override int WriteTimeout
	{
		get
		{
			return _stream.WriteTimeout;
		}
		set
		{
			_stream.WriteTimeout = value;
		}
	}

	public EncodingStreamWrapper(Stream stream, Encoding encoding)
	{
		try
		{
			_isReading = true;
			_stream = stream;
			SupportedEncoding supportedEncoding = GetSupportedEncoding(encoding);
			SupportedEncoding supportedEncoding2 = ReadBOMEncoding(encoding == null);
			if (supportedEncoding != SupportedEncoding.None && supportedEncoding != supportedEncoding2)
			{
				ThrowExpectedEncodingMismatch(supportedEncoding, supportedEncoding2);
			}
			if (supportedEncoding2 == SupportedEncoding.UTF8)
			{
				FillBuffer(2);
				if (_bytes[_byteOffset + 1] == 63 && _bytes[_byteOffset] == 60)
				{
					FillBuffer(128);
					CheckUTF8DeclarationEncoding(_bytes, _byteOffset, _byteCount, supportedEncoding2, supportedEncoding);
				}
				return;
			}
			EnsureBuffers();
			FillBuffer(254);
			SetReadDocumentEncoding(supportedEncoding2);
			CleanupCharBreak();
			int chars = _encoding.GetChars(_bytes, _byteOffset, _byteCount, _chars, 0);
			_byteOffset = 0;
			_byteCount = s_validatingUTF8.GetBytes(_chars, 0, chars, _bytes, 0);
			if (_bytes[1] == 63 && _bytes[0] == 60)
			{
				CheckUTF8DeclarationEncoding(_bytes, 0, _byteCount, supportedEncoding2, supportedEncoding);
			}
			else if (supportedEncoding == SupportedEncoding.None)
			{
				throw new XmlException(System.SR.XmlDeclarationRequired);
			}
		}
		catch (DecoderFallbackException innerException)
		{
			throw new XmlException(System.SR.XmlInvalidBytes, innerException);
		}
	}

	[MemberNotNull("_encoding")]
	private void SetReadDocumentEncoding(SupportedEncoding e)
	{
		EnsureBuffers();
		_encodingCode = e;
		_encoding = GetEncoding(e);
	}

	private static Encoding GetEncoding(SupportedEncoding e)
	{
		return e switch
		{
			SupportedEncoding.UTF8 => s_validatingUTF8, 
			SupportedEncoding.UTF16LE => s_validatingUTF16, 
			SupportedEncoding.UTF16BE => s_validatingBEUTF16, 
			_ => throw new XmlException(System.SR.XmlEncodingNotSupported), 
		};
	}

	private static Encoding GetSafeEncoding(SupportedEncoding e)
	{
		return e switch
		{
			SupportedEncoding.UTF8 => s_safeUTF8, 
			SupportedEncoding.UTF16LE => s_safeUTF16, 
			SupportedEncoding.UTF16BE => s_safeBEUTF16, 
			_ => throw new XmlException(System.SR.XmlEncodingNotSupported), 
		};
	}

	private static string GetEncodingName(SupportedEncoding enc)
	{
		return enc switch
		{
			SupportedEncoding.UTF8 => "utf-8", 
			SupportedEncoding.UTF16LE => "utf-16LE", 
			SupportedEncoding.UTF16BE => "utf-16BE", 
			_ => throw new XmlException(System.SR.XmlEncodingNotSupported), 
		};
	}

	private static SupportedEncoding GetSupportedEncoding(Encoding encoding)
	{
		if (encoding == null)
		{
			return SupportedEncoding.None;
		}
		if (encoding.WebName == s_validatingUTF8.WebName)
		{
			return SupportedEncoding.UTF8;
		}
		if (encoding.WebName == s_validatingUTF16.WebName)
		{
			return SupportedEncoding.UTF16LE;
		}
		if (encoding.WebName == s_validatingBEUTF16.WebName)
		{
			return SupportedEncoding.UTF16BE;
		}
		throw new XmlException(System.SR.XmlEncodingNotSupported);
	}

	public EncodingStreamWrapper(Stream stream, Encoding encoding, bool emitBOM)
	{
		_isReading = false;
		_encoding = encoding;
		_stream = stream;
		_encodingCode = GetSupportedEncoding(encoding);
		if (_encodingCode == SupportedEncoding.UTF8)
		{
			return;
		}
		EnsureBuffers();
		_dec = s_validatingUTF8.GetDecoder();
		_enc = _encoding.GetEncoder();
		if (emitBOM)
		{
			ReadOnlySpan<byte> preamble = _encoding.Preamble;
			if (preamble.Length > 0)
			{
				_stream.Write(preamble);
			}
		}
	}

	[MemberNotNull("_bytes")]
	private SupportedEncoding ReadBOMEncoding(bool notOutOfBand)
	{
		int num = _stream.ReadByte();
		int num2 = _stream.ReadByte();
		int num3 = _stream.ReadByte();
		int num4 = _stream.ReadByte();
		if (num4 == -1)
		{
			throw new XmlException(System.SR.UnexpectedEndOfFile);
		}
		int preserve;
		SupportedEncoding result = ReadBOMEncoding((byte)num, (byte)num2, (byte)num3, (byte)num4, notOutOfBand, out preserve);
		EnsureByteBuffer();
		switch (preserve)
		{
		case 1:
			_bytes[0] = (byte)num4;
			break;
		case 2:
			_bytes[0] = (byte)num3;
			_bytes[1] = (byte)num4;
			break;
		case 4:
			_bytes[0] = (byte)num;
			_bytes[1] = (byte)num2;
			_bytes[2] = (byte)num3;
			_bytes[3] = (byte)num4;
			break;
		}
		_byteCount = preserve;
		return result;
	}

	private static SupportedEncoding ReadBOMEncoding(byte b1, byte b2, byte b3, byte b4, bool notOutOfBand, out int preserve)
	{
		SupportedEncoding result = SupportedEncoding.UTF8;
		preserve = 0;
		if (b1 == 60 && b2 != 0)
		{
			result = SupportedEncoding.UTF8;
			preserve = 4;
		}
		else if (b1 == byte.MaxValue && b2 == 254)
		{
			result = SupportedEncoding.UTF16LE;
			preserve = 2;
		}
		else if (b1 == 254 && b2 == byte.MaxValue)
		{
			result = SupportedEncoding.UTF16BE;
			preserve = 2;
		}
		else if (b1 == 0 && b2 == 60)
		{
			result = SupportedEncoding.UTF16BE;
			if (notOutOfBand && (b3 != 0 || b4 != 63))
			{
				throw new XmlException(System.SR.XmlDeclMissing);
			}
			preserve = 4;
		}
		else if (b1 == 60 && b2 == 0)
		{
			result = SupportedEncoding.UTF16LE;
			if (notOutOfBand && (b3 != 63 || b4 != 0))
			{
				throw new XmlException(System.SR.XmlDeclMissing);
			}
			preserve = 4;
		}
		else if (b1 == 239 && b2 == 187)
		{
			if (notOutOfBand && b3 != 191)
			{
				throw new XmlException(System.SR.XmlBadBOM);
			}
			preserve = 1;
		}
		else
		{
			preserve = 4;
		}
		return result;
	}

	private void FillBuffer(int count)
	{
		count -= _byteCount;
		while (count > 0)
		{
			int num = _stream.Read(_bytes, _byteOffset + _byteCount, count);
			if (num != 0)
			{
				_byteCount += num;
				count -= num;
				continue;
			}
			break;
		}
	}

	[MemberNotNull("_bytes")]
	[MemberNotNull("_chars")]
	private void EnsureBuffers()
	{
		EnsureByteBuffer();
		if (_chars == null)
		{
			_chars = new char[128];
		}
	}

	[MemberNotNull("_bytes")]
	private void EnsureByteBuffer()
	{
		if (_bytes == null)
		{
			_bytes = new byte[512];
			_byteOffset = 0;
			_byteCount = 0;
		}
	}

	private static void CheckUTF8DeclarationEncoding(byte[] buffer, int offset, int count, SupportedEncoding e, SupportedEncoding expectedEnc)
	{
		byte b = 0;
		int num = -1;
		int num2 = offset + Math.Min(count, 128);
		int num3 = 0;
		int num4 = 0;
		for (num3 = offset + 2; num3 < num2; num3++)
		{
			if (b != 0)
			{
				if (buffer[num3] == b)
				{
					b = 0;
				}
			}
			else if (buffer[num3] == 39 || buffer[num3] == 34)
			{
				b = buffer[num3];
			}
			else if (buffer[num3] == 61)
			{
				if (num4 == 1)
				{
					num = num3;
					break;
				}
				num4++;
			}
			else if (buffer[num3] == 63)
			{
				break;
			}
		}
		if (num == -1)
		{
			if (e != 0 && expectedEnc == SupportedEncoding.None)
			{
				throw new XmlException(System.SR.XmlDeclarationRequired);
			}
			return;
		}
		if (num < 28)
		{
			throw new XmlException(System.SR.XmlMalformedDecl);
		}
		num3 = num - 1;
		while (IsWhitespace(buffer[num3]))
		{
			num3--;
		}
		if (!Compare(s_encodingAttr, buffer, num3 - s_encodingAttr.Length + 1))
		{
			if (e == SupportedEncoding.UTF8 || expectedEnc != SupportedEncoding.None)
			{
				return;
			}
			throw new XmlException(System.SR.XmlDeclarationRequired);
		}
		for (num3 = num + 1; num3 < num2 && IsWhitespace(buffer[num3]); num3++)
		{
		}
		if (buffer[num3] != 39 && buffer[num3] != 34)
		{
			throw new XmlException(System.SR.XmlMalformedDecl);
		}
		b = buffer[num3];
		int num5 = num3++;
		for (; buffer[num3] != b && num3 < num2; num3++)
		{
		}
		if (buffer[num3] != b)
		{
			throw new XmlException(System.SR.XmlMalformedDecl);
		}
		int num6 = num5 + 1;
		int num7 = num3 - num6;
		SupportedEncoding supportedEncoding = e;
		if (num7 == s_encodingUTF8.Length && CompareCaseInsensitive(s_encodingUTF8, buffer, num6))
		{
			supportedEncoding = SupportedEncoding.UTF8;
		}
		else if (num7 == s_encodingUnicodeLE.Length && CompareCaseInsensitive(s_encodingUnicodeLE, buffer, num6))
		{
			supportedEncoding = SupportedEncoding.UTF16LE;
		}
		else if (num7 == s_encodingUnicodeBE.Length && CompareCaseInsensitive(s_encodingUnicodeBE, buffer, num6))
		{
			supportedEncoding = SupportedEncoding.UTF16BE;
		}
		else if (num7 == s_encodingUnicode.Length && CompareCaseInsensitive(s_encodingUnicode, buffer, num6))
		{
			if (e == SupportedEncoding.UTF8)
			{
				ThrowEncodingMismatch(s_safeUTF8.GetString(buffer, num6, num7), s_safeUTF8.GetString(s_encodingUTF8, 0, s_encodingUTF8.Length));
			}
		}
		else
		{
			ThrowEncodingMismatch(s_safeUTF8.GetString(buffer, num6, num7), e);
		}
		if (e != supportedEncoding)
		{
			ThrowEncodingMismatch(s_safeUTF8.GetString(buffer, num6, num7), e);
		}
	}

	private static bool CompareCaseInsensitive(byte[] key, byte[] buffer, int offset)
	{
		for (int i = 0; i < key.Length; i++)
		{
			if (key[i] != buffer[offset + i] && key[i] != char.ToLowerInvariant((char)buffer[offset + i]))
			{
				return false;
			}
		}
		return true;
	}

	private static bool Compare(byte[] key, byte[] buffer, int offset)
	{
		for (int i = 0; i < key.Length; i++)
		{
			if (key[i] != buffer[offset + i])
			{
				return false;
			}
		}
		return true;
	}

	private static bool IsWhitespace(byte ch)
	{
		if (ch != 32 && ch != 10 && ch != 9)
		{
			return ch == 13;
		}
		return true;
	}

	internal static ArraySegment<byte> ProcessBuffer(byte[] buffer, int offset, int count, Encoding encoding)
	{
		if (count < 4)
		{
			throw new XmlException(System.SR.UnexpectedEndOfFile);
		}
		try
		{
			SupportedEncoding supportedEncoding = GetSupportedEncoding(encoding);
			int preserve;
			SupportedEncoding supportedEncoding2 = ReadBOMEncoding(buffer[offset], buffer[offset + 1], buffer[offset + 2], buffer[offset + 3], encoding == null, out preserve);
			if (supportedEncoding != SupportedEncoding.None && supportedEncoding != supportedEncoding2)
			{
				ThrowExpectedEncodingMismatch(supportedEncoding, supportedEncoding2);
			}
			offset += 4 - preserve;
			count -= 4 - preserve;
			if (supportedEncoding2 == SupportedEncoding.UTF8)
			{
				if (buffer[offset + 1] != 63 || buffer[offset] != 60)
				{
					return new ArraySegment<byte>(buffer, offset, count);
				}
				CheckUTF8DeclarationEncoding(buffer, offset, count, supportedEncoding2, supportedEncoding);
				return new ArraySegment<byte>(buffer, offset, count);
			}
			Encoding safeEncoding = GetSafeEncoding(supportedEncoding2);
			int byteCount = Math.Min(count, 256);
			char[] chars = new char[safeEncoding.GetMaxCharCount(byteCount)];
			int chars2 = safeEncoding.GetChars(buffer, offset, byteCount, chars, 0);
			byte[] array = new byte[s_validatingUTF8.GetMaxByteCount(chars2)];
			int bytes = s_validatingUTF8.GetBytes(chars, 0, chars2, array, 0);
			if (array[1] == 63 && array[0] == 60)
			{
				CheckUTF8DeclarationEncoding(array, 0, bytes, supportedEncoding2, supportedEncoding);
			}
			else if (supportedEncoding == SupportedEncoding.None)
			{
				throw new XmlException(System.SR.XmlDeclarationRequired);
			}
			return new ArraySegment<byte>(s_validatingUTF8.GetBytes(GetEncoding(supportedEncoding2).GetChars(buffer, offset, count)));
		}
		catch (DecoderFallbackException innerException)
		{
			throw new XmlException(System.SR.XmlInvalidBytes, innerException);
		}
	}

	private static void ThrowExpectedEncodingMismatch(SupportedEncoding expEnc, SupportedEncoding actualEnc)
	{
		throw new XmlException(System.SR.Format(System.SR.XmlExpectedEncoding, GetEncodingName(expEnc), GetEncodingName(actualEnc)));
	}

	private static void ThrowEncodingMismatch(string declEnc, SupportedEncoding enc)
	{
		ThrowEncodingMismatch(declEnc, GetEncodingName(enc));
	}

	private static void ThrowEncodingMismatch(string declEnc, string docEnc)
	{
		throw new XmlException(System.SR.Format(System.SR.XmlEncodingMismatch, declEnc, docEnc));
	}

	protected override void Dispose(bool disposing)
	{
		if (_stream.CanWrite)
		{
			Flush();
		}
		_stream.Dispose();
		base.Dispose(disposing);
	}

	public override void Flush()
	{
		_stream.Flush();
	}

	public override int ReadByte()
	{
		if (_byteCount == 0 && _encodingCode == SupportedEncoding.UTF8)
		{
			return _stream.ReadByte();
		}
		if (Read(_byteBuffer, 0, 1) == 0)
		{
			return -1;
		}
		return _byteBuffer[0];
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		try
		{
			if (_byteCount == 0)
			{
				if (_encodingCode == SupportedEncoding.UTF8)
				{
					return _stream.Read(buffer, offset, count);
				}
				_byteOffset = 0;
				_byteCount = _stream.Read(_bytes, _byteCount, (_chars.Length - 1) * 2);
				if (_byteCount == 0)
				{
					return 0;
				}
				CleanupCharBreak();
				int chars = _encoding.GetChars(_bytes, 0, _byteCount, _chars, 0);
				_byteCount = Encoding.UTF8.GetBytes(_chars, 0, chars, _bytes, 0);
			}
			if (_byteCount < count)
			{
				count = _byteCount;
			}
			Buffer.BlockCopy(_bytes, _byteOffset, buffer, offset, count);
			_byteOffset += count;
			_byteCount -= count;
			return count;
		}
		catch (DecoderFallbackException innerException)
		{
			throw new XmlException(System.SR.XmlInvalidBytes, innerException);
		}
	}

	private void CleanupCharBreak()
	{
		int num = _byteOffset + _byteCount;
		if (_byteCount % 2 != 0)
		{
			int num2 = _stream.ReadByte();
			if (num2 < 0)
			{
				throw new XmlException(System.SR.UnexpectedEndOfFile);
			}
			_bytes[num++] = (byte)num2;
			_byteCount++;
		}
		int num3 = ((_encodingCode != SupportedEncoding.UTF16LE) ? (_bytes[num - 1] + (_bytes[num - 2] << 8)) : (_bytes[num - 2] + (_bytes[num - 1] << 8)));
		if ((num3 & 0xDC00) != 56320 && num3 >= 55296 && num3 <= 56319)
		{
			int num4 = _stream.ReadByte();
			int num5 = _stream.ReadByte();
			if (num5 < 0)
			{
				throw new XmlException(System.SR.UnexpectedEndOfFile);
			}
			_bytes[num++] = (byte)num4;
			_bytes[num++] = (byte)num5;
			_byteCount += 2;
		}
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException();
	}

	public override void WriteByte(byte b)
	{
		if (_encodingCode == SupportedEncoding.UTF8)
		{
			_stream.WriteByte(b);
			return;
		}
		_byteBuffer[0] = b;
		Write(_byteBuffer, 0, 1);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		if (_encodingCode == SupportedEncoding.UTF8)
		{
			_stream.Write(buffer, offset, count);
			return;
		}
		while (count > 0)
		{
			int num = ((_chars.Length < count) ? _chars.Length : count);
			int chars = _dec.GetChars(buffer, offset, num, _chars, 0, flush: false);
			_byteCount = _enc.GetBytes(_chars, 0, chars, _bytes, 0, flush: false);
			_stream.Write(_bytes, 0, _byteCount);
			offset += num;
			count -= num;
		}
	}

	public override void SetLength(long value)
	{
		throw new NotSupportedException();
	}
}
