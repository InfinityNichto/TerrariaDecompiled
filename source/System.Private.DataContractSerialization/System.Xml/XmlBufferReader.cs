using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace System.Xml;

internal sealed class XmlBufferReader
{
	private readonly XmlDictionaryReader _reader;

	private Stream _stream;

	private byte[] _streamBuffer;

	private byte[] _buffer;

	private int _offsetMin;

	private int _offsetMax;

	private IXmlDictionary _dictionary;

	private XmlBinaryReaderSession _session;

	private byte[] _guid;

	private int _offset;

	private const int maxBytesPerChar = 3;

	private char[] _chars;

	private int _windowOffset;

	private int _windowOffsetMax;

	private ValueHandle _listValue;

	private static readonly XmlBufferReader s_empty = new XmlBufferReader(Array.Empty<byte>());

	public static XmlBufferReader Empty => s_empty;

	public byte[] Buffer => _buffer;

	public bool IsStreamed => _stream != null;

	public bool EndOfFile
	{
		get
		{
			if (_offset == _offsetMax)
			{
				return !TryEnsureByte();
			}
			return false;
		}
	}

	public int Offset
	{
		get
		{
			return _offset;
		}
		set
		{
			_offset = value;
		}
	}

	public XmlBufferReader(XmlDictionaryReader reader)
	{
		_reader = reader;
	}

	public XmlBufferReader(byte[] buffer)
	{
		_reader = null;
		_buffer = buffer;
	}

	public void SetBuffer(Stream stream, IXmlDictionary dictionary, XmlBinaryReaderSession session)
	{
		if (_streamBuffer == null)
		{
			_streamBuffer = new byte[128];
		}
		SetBuffer(stream, _streamBuffer, 0, 0, dictionary, session);
		_windowOffset = 0;
		_windowOffsetMax = _streamBuffer.Length;
	}

	public void SetBuffer(byte[] buffer, int offset, int count, IXmlDictionary dictionary, XmlBinaryReaderSession session)
	{
		SetBuffer(null, buffer, offset, count, dictionary, session);
	}

	private void SetBuffer(Stream stream, byte[] buffer, int offset, int count, IXmlDictionary dictionary, XmlBinaryReaderSession session)
	{
		_stream = stream;
		_buffer = buffer;
		_offsetMin = offset;
		_offset = offset;
		_offsetMax = offset + count;
		_dictionary = dictionary;
		_session = session;
	}

	public void Close()
	{
		if (_streamBuffer != null && _streamBuffer.Length > 4096)
		{
			_streamBuffer = null;
		}
		if (_stream != null)
		{
			_stream.Dispose();
			_stream = null;
		}
		_buffer = Array.Empty<byte>();
		_offset = 0;
		_offsetMax = 0;
		_windowOffset = 0;
		_windowOffsetMax = 0;
		_dictionary = null;
		_session = null;
	}

	public byte GetByte()
	{
		int offset = _offset;
		if (offset < _offsetMax)
		{
			return _buffer[offset];
		}
		return GetByteHard();
	}

	public void SkipByte()
	{
		Advance(1);
	}

	private byte GetByteHard()
	{
		EnsureByte();
		return _buffer[_offset];
	}

	public byte[] GetBuffer(int count, out int offset)
	{
		offset = _offset;
		if (offset <= _offsetMax - count)
		{
			return _buffer;
		}
		return GetBufferHard(count, out offset);
	}

	public byte[] GetBuffer(int count, out int offset, out int offsetMax)
	{
		offset = _offset;
		if (offset <= _offsetMax - count)
		{
			offsetMax = _offset + count;
		}
		else
		{
			TryEnsureBytes(Math.Min(count, _windowOffsetMax - offset));
			offsetMax = _offsetMax;
		}
		return _buffer;
	}

	public byte[] GetBuffer(out int offset, out int offsetMax)
	{
		offset = _offset;
		offsetMax = _offsetMax;
		return _buffer;
	}

	private byte[] GetBufferHard(int count, out int offset)
	{
		offset = _offset;
		EnsureBytes(count);
		return _buffer;
	}

	private void EnsureByte()
	{
		if (!TryEnsureByte())
		{
			XmlExceptionHelper.ThrowUnexpectedEndOfFile(_reader);
		}
	}

	private bool TryEnsureByte()
	{
		if (_stream == null)
		{
			return false;
		}
		if (_offsetMax >= _buffer.Length)
		{
			return TryEnsureBytes(1);
		}
		int num = _stream.ReadByte();
		if (num == -1)
		{
			return false;
		}
		_buffer[_offsetMax++] = (byte)num;
		return true;
	}

	private void EnsureBytes(int count)
	{
		if (!TryEnsureBytes(count))
		{
			XmlExceptionHelper.ThrowUnexpectedEndOfFile(_reader);
		}
	}

	private bool TryEnsureBytes(int count)
	{
		if (_stream == null)
		{
			return false;
		}
		while (true)
		{
			int num = _offset + count;
			if (num <= _offsetMax)
			{
				break;
			}
			if (num > _buffer.Length)
			{
				byte[] array = new byte[Math.Max(256, _buffer.Length * 2)];
				System.Buffer.BlockCopy(_buffer, 0, array, 0, _offsetMax);
				num = Math.Min(num, array.Length);
				_buffer = array;
				_streamBuffer = array;
			}
			int num2 = num - _offsetMax;
			do
			{
				int num3 = _stream.Read(_buffer, _offsetMax, num2);
				if (num3 == 0)
				{
					return false;
				}
				_offsetMax += num3;
				num2 -= num3;
			}
			while (num2 > 0);
		}
		return true;
	}

	public void Advance(int count)
	{
		_offset += count;
	}

	public void InsertBytes(byte[] buffer, int offset, int count)
	{
		if (_offsetMax > buffer.Length - count)
		{
			byte[] array = new byte[_offsetMax + count];
			System.Buffer.BlockCopy(_buffer, 0, array, 0, _offsetMax);
			_buffer = array;
			_streamBuffer = array;
		}
		System.Buffer.BlockCopy(_buffer, _offset, _buffer, _offset + count, _offsetMax - _offset);
		_offsetMax += count;
		System.Buffer.BlockCopy(buffer, offset, _buffer, _offset, count);
	}

	public void SetWindow(int windowOffset, int windowLength)
	{
		if (windowOffset > int.MaxValue - windowLength)
		{
			windowLength = int.MaxValue - windowOffset;
		}
		if (_offset != windowOffset)
		{
			System.Buffer.BlockCopy(_buffer, _offset, _buffer, windowOffset, _offsetMax - _offset);
			_offsetMax = windowOffset + (_offsetMax - _offset);
			_offset = windowOffset;
		}
		_windowOffset = windowOffset;
		_windowOffsetMax = Math.Max(windowOffset + windowLength, _offsetMax);
	}

	public int ReadBytes(int count)
	{
		int offset = _offset;
		if (offset > _offsetMax - count)
		{
			EnsureBytes(count);
		}
		_offset += count;
		return offset;
	}

	public int ReadMultiByteUInt31()
	{
		int @byte = GetByte();
		Advance(1);
		if ((@byte & 0x80) == 0)
		{
			return @byte;
		}
		@byte &= 0x7F;
		int byte2 = GetByte();
		Advance(1);
		@byte |= (byte2 & 0x7F) << 7;
		if ((byte2 & 0x80) == 0)
		{
			return @byte;
		}
		int byte3 = GetByte();
		Advance(1);
		@byte |= (byte3 & 0x7F) << 14;
		if ((byte3 & 0x80) == 0)
		{
			return @byte;
		}
		int byte4 = GetByte();
		Advance(1);
		@byte |= (byte4 & 0x7F) << 21;
		if ((byte4 & 0x80) == 0)
		{
			return @byte;
		}
		int byte5 = GetByte();
		Advance(1);
		@byte |= byte5 << 28;
		if (((uint)byte5 & 0xF8u) != 0)
		{
			XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);
		}
		return @byte;
	}

	public int ReadUInt8()
	{
		byte @byte = GetByte();
		Advance(1);
		return @byte;
	}

	public int ReadInt8()
	{
		return (sbyte)ReadUInt8();
	}

	public int ReadUInt16()
	{
		int offset;
		byte[] buffer = GetBuffer(2, out offset);
		int result = buffer[offset] + (buffer[offset + 1] << 8);
		Advance(2);
		return result;
	}

	public int ReadInt16()
	{
		return (short)ReadUInt16();
	}

	public int ReadInt32()
	{
		int offset;
		byte[] buffer = GetBuffer(4, out offset);
		byte b = buffer[offset];
		byte b2 = buffer[offset + 1];
		byte b3 = buffer[offset + 2];
		byte b4 = buffer[offset + 3];
		Advance(4);
		return (((b4 << 8) + b3 << 8) + b2 << 8) + b;
	}

	public int ReadUInt31()
	{
		int num = ReadInt32();
		if (num < 0)
		{
			XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);
		}
		return num;
	}

	public long ReadInt64()
	{
		long num = (uint)ReadInt32();
		long num2 = (uint)ReadInt32();
		return (num2 << 32) + num;
	}

	public unsafe float ReadSingle()
	{
		int offset;
		byte[] buffer = GetBuffer(4, out offset);
		Unsafe.SkipInit(out float result);
		byte* ptr = (byte*)(&result);
		*ptr = buffer[offset];
		ptr[1] = buffer[offset + 1];
		ptr[2] = buffer[offset + 2];
		ptr[3] = buffer[offset + 3];
		Advance(4);
		return result;
	}

	public unsafe double ReadDouble()
	{
		int offset;
		byte[] buffer = GetBuffer(8, out offset);
		Unsafe.SkipInit(out double result);
		byte* ptr = (byte*)(&result);
		*ptr = buffer[offset];
		ptr[1] = buffer[offset + 1];
		ptr[2] = buffer[offset + 2];
		ptr[3] = buffer[offset + 3];
		ptr[4] = buffer[offset + 4];
		ptr[5] = buffer[offset + 5];
		ptr[6] = buffer[offset + 6];
		ptr[7] = buffer[offset + 7];
		Advance(8);
		return result;
	}

	public unsafe decimal ReadDecimal()
	{
		int offset;
		byte[] buffer = GetBuffer(16, out offset);
		Unsafe.SkipInit(out decimal result);
		byte* ptr = (byte*)(&result);
		for (int i = 0; i < 16; i++)
		{
			ptr[i] = buffer[offset + i];
		}
		Advance(16);
		return result;
	}

	public UniqueId ReadUniqueId()
	{
		int offset;
		byte[] buffer = GetBuffer(16, out offset);
		UniqueId result = new UniqueId(buffer, offset);
		Advance(16);
		return result;
	}

	public DateTime ReadDateTime()
	{
		long dateData = 0L;
		try
		{
			dateData = ReadInt64();
			return DateTime.FromBinary(dateData);
		}
		catch (ArgumentException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(dateData.ToString(CultureInfo.InvariantCulture), "DateTime", exception));
		}
		catch (FormatException exception2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(dateData.ToString(CultureInfo.InvariantCulture), "DateTime", exception2));
		}
		catch (OverflowException exception3)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(dateData.ToString(CultureInfo.InvariantCulture), "DateTime", exception3));
		}
	}

	public TimeSpan ReadTimeSpan()
	{
		long value = 0L;
		try
		{
			value = ReadInt64();
			return TimeSpan.FromTicks(value);
		}
		catch (ArgumentException exception)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value.ToString(CultureInfo.InvariantCulture), "TimeSpan", exception));
		}
		catch (FormatException exception2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value.ToString(CultureInfo.InvariantCulture), "TimeSpan", exception2));
		}
		catch (OverflowException exception3)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateConversionException(value.ToString(CultureInfo.InvariantCulture), "TimeSpan", exception3));
		}
	}

	public Guid ReadGuid()
	{
		GetBuffer(16, out var offset);
		Guid guid = GetGuid(offset);
		Advance(16);
		return guid;
	}

	public string ReadUTF8String(int length)
	{
		GetBuffer(length, out var offset);
		char[] charBuffer = GetCharBuffer(length);
		int chars = GetChars(offset, length, charBuffer);
		string result = new string(charBuffer, 0, chars);
		Advance(length);
		return result;
	}

	public unsafe void UnsafeReadArray(byte* dst, byte* dstMax)
	{
		UnsafeReadArray(dst, (int)(dstMax - dst));
	}

	private unsafe void UnsafeReadArray(byte* dst, int length)
	{
		if (_stream != null)
		{
			while (length >= 256)
			{
				byte[] buffer = GetBuffer(256, out _offset);
				for (int i = 0; i < 256; i++)
				{
					*(dst++) = buffer[_offset + i];
				}
				Advance(256);
				length -= 256;
			}
		}
		if (length <= 0)
		{
			return;
		}
		byte[] buffer2 = GetBuffer(length, out _offset);
		fixed (byte* ptr = &buffer2[_offset])
		{
			byte* ptr2 = ptr;
			byte* ptr3 = dst + length;
			while (dst < ptr3)
			{
				*dst = *ptr2;
				dst++;
				ptr2++;
			}
		}
		Advance(length);
	}

	private char[] GetCharBuffer(int count)
	{
		if (count > 1024)
		{
			return new char[count];
		}
		if (_chars == null || _chars.Length < count)
		{
			_chars = new char[count];
		}
		return _chars;
	}

	private int GetChars(int offset, int length, char[] chars)
	{
		byte[] buffer = _buffer;
		for (int i = 0; i < length; i++)
		{
			byte b = buffer[offset + i];
			if (b >= 128)
			{
				return i + XmlConverter.ToChars(buffer, offset + i, length - i, chars, i);
			}
			chars[i] = (char)b;
		}
		return length;
	}

	private int GetChars(int offset, int length, char[] chars, int charOffset)
	{
		byte[] buffer = _buffer;
		for (int i = 0; i < length; i++)
		{
			byte b = buffer[offset + i];
			if (b >= 128)
			{
				return i + XmlConverter.ToChars(buffer, offset + i, length - i, chars, charOffset + i);
			}
			chars[charOffset + i] = (char)b;
		}
		return length;
	}

	public string GetString(int offset, int length)
	{
		char[] charBuffer = GetCharBuffer(length);
		int chars = GetChars(offset, length, charBuffer);
		return new string(charBuffer, 0, chars);
	}

	public string GetUnicodeString(int offset, int length)
	{
		return XmlConverter.ToStringUnicode(_buffer, offset, length);
	}

	public string GetString(int offset, int length, XmlNameTable nameTable)
	{
		char[] charBuffer = GetCharBuffer(length);
		int chars = GetChars(offset, length, charBuffer);
		return nameTable.Add(charBuffer, 0, chars);
	}

	public int GetEscapedChars(int offset, int length, char[] chars)
	{
		byte[] buffer = _buffer;
		int num = 0;
		int num2 = offset;
		int num3 = offset + length;
		while (true)
		{
			if (offset < num3 && IsAttrChar(buffer[offset]))
			{
				offset++;
				continue;
			}
			num += GetChars(num2, offset - num2, chars, num);
			if (offset == num3)
			{
				break;
			}
			num2 = offset;
			if (buffer[offset] == 38)
			{
				while (offset < num3 && buffer[offset] != 59)
				{
					offset++;
				}
				offset++;
				int charEntity = GetCharEntity(num2, offset - num2);
				num2 = offset;
				if (charEntity > 65535)
				{
					SurrogateChar surrogateChar = new SurrogateChar(charEntity);
					chars[num++] = surrogateChar.HighChar;
					chars[num++] = surrogateChar.LowChar;
				}
				else
				{
					chars[num++] = (char)charEntity;
				}
			}
			else if (buffer[offset] == 10 || buffer[offset] == 9)
			{
				chars[num++] = ' ';
				offset++;
				num2 = offset;
			}
			else
			{
				chars[num++] = ' ';
				offset++;
				if (offset < num3 && buffer[offset] == 10)
				{
					offset++;
				}
				num2 = offset;
			}
		}
		return num;
	}

	private bool IsAttrChar(int ch)
	{
		if ((uint)(ch - 9) <= 1u || ch == 13 || ch == 38)
		{
			return false;
		}
		return true;
	}

	public string GetEscapedString(int offset, int length)
	{
		char[] charBuffer = GetCharBuffer(length);
		int escapedChars = GetEscapedChars(offset, length, charBuffer);
		return new string(charBuffer, 0, escapedChars);
	}

	private int GetLessThanCharEntity(int offset, int length)
	{
		byte[] buffer = _buffer;
		if (length != 4 || buffer[offset + 1] != 108 || buffer[offset + 2] != 116)
		{
			XmlExceptionHelper.ThrowInvalidCharRef(_reader);
		}
		return 60;
	}

	private int GetGreaterThanCharEntity(int offset, int length)
	{
		byte[] buffer = _buffer;
		if (length != 4 || buffer[offset + 1] != 103 || buffer[offset + 2] != 116)
		{
			XmlExceptionHelper.ThrowInvalidCharRef(_reader);
		}
		return 62;
	}

	private int GetQuoteCharEntity(int offset, int length)
	{
		byte[] buffer = _buffer;
		if (length != 6 || buffer[offset + 1] != 113 || buffer[offset + 2] != 117 || buffer[offset + 3] != 111 || buffer[offset + 4] != 116)
		{
			XmlExceptionHelper.ThrowInvalidCharRef(_reader);
		}
		return 34;
	}

	private int GetAmpersandCharEntity(int offset, int length)
	{
		byte[] buffer = _buffer;
		if (length != 5 || buffer[offset + 1] != 97 || buffer[offset + 2] != 109 || buffer[offset + 3] != 112)
		{
			XmlExceptionHelper.ThrowInvalidCharRef(_reader);
		}
		return 38;
	}

	private int GetApostropheCharEntity(int offset, int length)
	{
		byte[] buffer = _buffer;
		if (length != 6 || buffer[offset + 1] != 97 || buffer[offset + 2] != 112 || buffer[offset + 3] != 111 || buffer[offset + 4] != 115)
		{
			XmlExceptionHelper.ThrowInvalidCharRef(_reader);
		}
		return 39;
	}

	private int GetDecimalCharEntity(int offset, int length)
	{
		byte[] buffer = _buffer;
		int num = 0;
		for (int i = 2; i < length - 1; i++)
		{
			byte b = buffer[offset + i];
			if (b < 48 || b > 57)
			{
				XmlExceptionHelper.ThrowInvalidCharRef(_reader);
			}
			num = num * 10 + (b - 48);
			if (num > 1114111)
			{
				XmlExceptionHelper.ThrowInvalidCharRef(_reader);
			}
		}
		return num;
	}

	private int GetHexCharEntity(int offset, int length)
	{
		byte[] buffer = _buffer;
		int num = 0;
		for (int i = 3; i < length - 1; i++)
		{
			byte c = buffer[offset + i];
			int num2 = System.HexConverter.FromChar(c);
			if (num2 == 255)
			{
				XmlExceptionHelper.ThrowInvalidCharRef(_reader);
			}
			num = num * 16 + num2;
			if (num > 1114111)
			{
				XmlExceptionHelper.ThrowInvalidCharRef(_reader);
			}
		}
		return num;
	}

	public int GetCharEntity(int offset, int length)
	{
		if (length < 3)
		{
			XmlExceptionHelper.ThrowInvalidCharRef(_reader);
		}
		byte[] buffer = _buffer;
		switch (buffer[offset + 1])
		{
		case 108:
			return GetLessThanCharEntity(offset, length);
		case 103:
			return GetGreaterThanCharEntity(offset, length);
		case 97:
			if (buffer[offset + 2] == 109)
			{
				return GetAmpersandCharEntity(offset, length);
			}
			return GetApostropheCharEntity(offset, length);
		case 113:
			return GetQuoteCharEntity(offset, length);
		case 35:
			if (buffer[offset + 2] == 120)
			{
				return GetHexCharEntity(offset, length);
			}
			return GetDecimalCharEntity(offset, length);
		default:
			XmlExceptionHelper.ThrowInvalidCharRef(_reader);
			return 0;
		}
	}

	public bool IsWhitespaceKey(int key)
	{
		string value = GetDictionaryString(key).Value;
		for (int i = 0; i < value.Length; i++)
		{
			if (!XmlConverter.IsWhitespace(value[i]))
			{
				return false;
			}
		}
		return true;
	}

	public bool IsWhitespaceUTF8(int offset, int length)
	{
		byte[] buffer = _buffer;
		for (int i = 0; i < length; i++)
		{
			if (!XmlConverter.IsWhitespace((char)buffer[offset + i]))
			{
				return false;
			}
		}
		return true;
	}

	public bool IsWhitespaceUnicode(int offset, int length)
	{
		for (int i = 0; i < length; i += 2)
		{
			char ch = (char)GetInt16(offset + i);
			if (!XmlConverter.IsWhitespace(ch))
			{
				return false;
			}
		}
		return true;
	}

	public bool Equals2(int key1, int key2, XmlBufferReader bufferReader2)
	{
		if (key1 == key2)
		{
			return true;
		}
		return GetDictionaryString(key1).Value == bufferReader2.GetDictionaryString(key2).Value;
	}

	public bool Equals2(int key1, XmlDictionaryString xmlString2)
	{
		if ((key1 & 1) == 0 && xmlString2.Dictionary == _dictionary)
		{
			return xmlString2.Key == key1 >> 1;
		}
		return GetDictionaryString(key1).Value == xmlString2.Value;
	}

	public bool Equals2(int offset1, int length1, byte[] buffer2)
	{
		int num = buffer2.Length;
		if (length1 != num)
		{
			return false;
		}
		byte[] buffer3 = _buffer;
		for (int i = 0; i < length1; i++)
		{
			if (buffer3[offset1 + i] != buffer2[i])
			{
				return false;
			}
		}
		return true;
	}

	public bool Equals2(int offset1, int length1, XmlBufferReader bufferReader2, int offset2, int length2)
	{
		if (length1 != length2)
		{
			return false;
		}
		byte[] buffer = _buffer;
		byte[] buffer2 = bufferReader2._buffer;
		for (int i = 0; i < length1; i++)
		{
			if (buffer[offset1 + i] != buffer2[offset2 + i])
			{
				return false;
			}
		}
		return true;
	}

	public bool Equals2(int offset1, int length1, int offset2, int length2)
	{
		if (length1 != length2)
		{
			return false;
		}
		if (offset1 == offset2)
		{
			return true;
		}
		byte[] buffer = _buffer;
		for (int i = 0; i < length1; i++)
		{
			if (buffer[offset1 + i] != buffer[offset2 + i])
			{
				return false;
			}
		}
		return true;
	}

	public unsafe bool Equals2(int offset1, int length1, string s2)
	{
		int length2 = s2.Length;
		if (length1 < length2 || length1 > length2 * 3)
		{
			return false;
		}
		byte[] buffer = _buffer;
		if (length1 < 8)
		{
			int num = Math.Min(length1, length2);
			for (int i = 0; i < num; i++)
			{
				byte b = buffer[offset1 + i];
				if (b >= 128)
				{
					return XmlConverter.ToString(buffer, offset1, length1) == s2;
				}
				if (s2[i] != b)
				{
					return false;
				}
			}
			return length1 == length2;
		}
		int num2 = Math.Min(length1, length2);
		fixed (byte* ptr = &buffer[offset1])
		{
			byte* ptr2 = ptr;
			byte* ptr3 = ptr2 + num2;
			fixed (char* ptr4 = s2)
			{
				char* ptr5 = ptr4;
				int num3 = 0;
				while (ptr2 < ptr3 && *ptr2 < 128)
				{
					num3 = *ptr2 - (byte)(*ptr5);
					if (num3 != 0)
					{
						break;
					}
					ptr2++;
					ptr5++;
				}
				if (num3 != 0)
				{
					return false;
				}
				if (ptr2 == ptr3)
				{
					return length1 == length2;
				}
			}
		}
		return XmlConverter.ToString(buffer, offset1, length1) == s2;
	}

	public int Compare(int offset1, int length1, int offset2, int length2)
	{
		byte[] buffer = _buffer;
		int num = Math.Min(length1, length2);
		for (int i = 0; i < num; i++)
		{
			int num2 = buffer[offset1 + i] - buffer[offset2 + i];
			if (num2 != 0)
			{
				return num2;
			}
		}
		return length1 - length2;
	}

	public byte GetByte(int offset)
	{
		return _buffer[offset];
	}

	public int GetInt8(int offset)
	{
		return (sbyte)GetByte(offset);
	}

	public int GetInt16(int offset)
	{
		byte[] buffer = _buffer;
		return (short)(buffer[offset] + (buffer[offset + 1] << 8));
	}

	public int GetInt32(int offset)
	{
		byte[] buffer = _buffer;
		byte b = buffer[offset];
		byte b2 = buffer[offset + 1];
		byte b3 = buffer[offset + 2];
		byte b4 = buffer[offset + 3];
		return (((b4 << 8) + b3 << 8) + b2 << 8) + b;
	}

	public long GetInt64(int offset)
	{
		byte[] buffer = _buffer;
		byte b = buffer[offset];
		byte b2 = buffer[offset + 1];
		byte b3 = buffer[offset + 2];
		byte b4 = buffer[offset + 3];
		long num = (uint)((((b4 << 8) + b3 << 8) + b2 << 8) + b);
		b = buffer[offset + 4];
		b2 = buffer[offset + 5];
		b3 = buffer[offset + 6];
		b4 = buffer[offset + 7];
		long num2 = (uint)((((b4 << 8) + b3 << 8) + b2 << 8) + b);
		return (num2 << 32) + num;
	}

	public ulong GetUInt64(int offset)
	{
		return (ulong)GetInt64(offset);
	}

	public unsafe float GetSingle(int offset)
	{
		byte[] buffer = _buffer;
		Unsafe.SkipInit(out float result);
		byte* ptr = (byte*)(&result);
		*ptr = buffer[offset];
		ptr[1] = buffer[offset + 1];
		ptr[2] = buffer[offset + 2];
		ptr[3] = buffer[offset + 3];
		return result;
	}

	public unsafe double GetDouble(int offset)
	{
		byte[] buffer = _buffer;
		Unsafe.SkipInit(out double result);
		byte* ptr = (byte*)(&result);
		*ptr = buffer[offset];
		ptr[1] = buffer[offset + 1];
		ptr[2] = buffer[offset + 2];
		ptr[3] = buffer[offset + 3];
		ptr[4] = buffer[offset + 4];
		ptr[5] = buffer[offset + 5];
		ptr[6] = buffer[offset + 6];
		ptr[7] = buffer[offset + 7];
		return result;
	}

	public unsafe decimal GetDecimal(int offset)
	{
		byte[] buffer = _buffer;
		Unsafe.SkipInit(out decimal result);
		byte* ptr = (byte*)(&result);
		for (int i = 0; i < 16; i++)
		{
			ptr[i] = buffer[offset + i];
		}
		return result;
	}

	public UniqueId GetUniqueId(int offset)
	{
		return new UniqueId(_buffer, offset);
	}

	public Guid GetGuid(int offset)
	{
		if (_guid == null)
		{
			_guid = new byte[16];
		}
		System.Buffer.BlockCopy(_buffer, offset, _guid, 0, _guid.Length);
		return new Guid(_guid);
	}

	public void GetBase64(int srcOffset, byte[] buffer, int dstOffset, int count)
	{
		System.Buffer.BlockCopy(_buffer, srcOffset, buffer, dstOffset, count);
	}

	public XmlBinaryNodeType GetNodeType()
	{
		return (XmlBinaryNodeType)GetByte();
	}

	public void SkipNodeType()
	{
		SkipByte();
	}

	public object[] GetList(int offset, int count)
	{
		int offset2 = Offset;
		Offset = offset;
		try
		{
			object[] array = new object[count];
			for (int i = 0; i < count; i++)
			{
				XmlBinaryNodeType nodeType = GetNodeType();
				SkipNodeType();
				ReadValue(nodeType, _listValue);
				array[i] = _listValue.ToObject();
			}
			return array;
		}
		finally
		{
			Offset = offset2;
		}
	}

	public XmlDictionaryString GetDictionaryString(int key)
	{
		IXmlDictionary xmlDictionary = (((key & 1) == 0) ? _dictionary : _session);
		if (!xmlDictionary.TryLookup(key >> 1, out XmlDictionaryString result))
		{
			XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);
		}
		return result;
	}

	public int ReadDictionaryKey()
	{
		int num = ReadMultiByteUInt31();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			if (_session == null)
			{
				XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);
			}
			int num2 = num >> 1;
			if (!_session.TryLookup(num2, out XmlDictionaryString _))
			{
				if (num2 < 0 || num2 > 536870911)
				{
					XmlExceptionHelper.ThrowXmlDictionaryStringIDOutOfRange(_reader);
				}
				XmlExceptionHelper.ThrowXmlDictionaryStringIDUndefinedSession(_reader, num2);
			}
		}
		else
		{
			if (_dictionary == null)
			{
				XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);
			}
			int num3 = num >> 1;
			if (!_dictionary.TryLookup(num3, out XmlDictionaryString _))
			{
				if (num3 < 0 || num3 > 536870911)
				{
					XmlExceptionHelper.ThrowXmlDictionaryStringIDOutOfRange(_reader);
				}
				XmlExceptionHelper.ThrowXmlDictionaryStringIDUndefinedStatic(_reader, num3);
			}
		}
		return num;
	}

	public void ReadValue(XmlBinaryNodeType nodeType, ValueHandle value)
	{
		switch (nodeType)
		{
		case XmlBinaryNodeType.EmptyText:
			value.SetValue(ValueHandleType.Empty);
			break;
		case XmlBinaryNodeType.MinText:
			value.SetValue(ValueHandleType.Zero);
			break;
		case XmlBinaryNodeType.OneText:
			value.SetValue(ValueHandleType.One);
			break;
		case XmlBinaryNodeType.TrueText:
			value.SetValue(ValueHandleType.True);
			break;
		case XmlBinaryNodeType.FalseText:
			value.SetValue(ValueHandleType.False);
			break;
		case XmlBinaryNodeType.BoolText:
			value.SetValue((ReadUInt8() != 0) ? ValueHandleType.True : ValueHandleType.False);
			break;
		case XmlBinaryNodeType.Chars8Text:
			ReadValue(value, ValueHandleType.UTF8, ReadUInt8());
			break;
		case XmlBinaryNodeType.Chars16Text:
			ReadValue(value, ValueHandleType.UTF8, ReadUInt16());
			break;
		case XmlBinaryNodeType.Chars32Text:
			ReadValue(value, ValueHandleType.UTF8, ReadUInt31());
			break;
		case XmlBinaryNodeType.UnicodeChars8Text:
			ReadUnicodeValue(value, ReadUInt8());
			break;
		case XmlBinaryNodeType.UnicodeChars16Text:
			ReadUnicodeValue(value, ReadUInt16());
			break;
		case XmlBinaryNodeType.UnicodeChars32Text:
			ReadUnicodeValue(value, ReadUInt31());
			break;
		case XmlBinaryNodeType.Bytes8Text:
			ReadValue(value, ValueHandleType.Base64, ReadUInt8());
			break;
		case XmlBinaryNodeType.Bytes16Text:
			ReadValue(value, ValueHandleType.Base64, ReadUInt16());
			break;
		case XmlBinaryNodeType.Bytes32Text:
			ReadValue(value, ValueHandleType.Base64, ReadUInt31());
			break;
		case XmlBinaryNodeType.DictionaryText:
			value.SetDictionaryValue(ReadDictionaryKey());
			break;
		case XmlBinaryNodeType.UniqueIdText:
			ReadValue(value, ValueHandleType.UniqueId, 16);
			break;
		case XmlBinaryNodeType.GuidText:
			ReadValue(value, ValueHandleType.Guid, 16);
			break;
		case XmlBinaryNodeType.DecimalText:
			ReadValue(value, ValueHandleType.Decimal, 16);
			break;
		case XmlBinaryNodeType.Int8Text:
			ReadValue(value, ValueHandleType.Int8, 1);
			break;
		case XmlBinaryNodeType.Int16Text:
			ReadValue(value, ValueHandleType.Int16, 2);
			break;
		case XmlBinaryNodeType.Int32Text:
			ReadValue(value, ValueHandleType.Int32, 4);
			break;
		case XmlBinaryNodeType.Int64Text:
			ReadValue(value, ValueHandleType.Int64, 8);
			break;
		case XmlBinaryNodeType.UInt64Text:
			ReadValue(value, ValueHandleType.UInt64, 8);
			break;
		case XmlBinaryNodeType.FloatText:
			ReadValue(value, ValueHandleType.Single, 4);
			break;
		case XmlBinaryNodeType.DoubleText:
			ReadValue(value, ValueHandleType.Double, 8);
			break;
		case XmlBinaryNodeType.TimeSpanText:
			ReadValue(value, ValueHandleType.TimeSpan, 8);
			break;
		case XmlBinaryNodeType.DateTimeText:
			ReadValue(value, ValueHandleType.DateTime, 8);
			break;
		case XmlBinaryNodeType.StartListText:
			ReadList(value);
			break;
		case XmlBinaryNodeType.QNameDictionaryText:
			ReadQName(value);
			break;
		default:
			XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);
			break;
		}
	}

	private void ReadValue(ValueHandle value, ValueHandleType type, int length)
	{
		int offset = ReadBytes(length);
		value.SetValue(type, offset, length);
	}

	private void ReadUnicodeValue(ValueHandle value, int length)
	{
		if (((uint)length & (true ? 1u : 0u)) != 0)
		{
			XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);
		}
		ReadValue(value, ValueHandleType.Unicode, length);
	}

	private void ReadList(ValueHandle value)
	{
		if (_listValue == null)
		{
			_listValue = new ValueHandle(this);
		}
		int num = 0;
		int offset = Offset;
		while (true)
		{
			XmlBinaryNodeType nodeType = GetNodeType();
			SkipNodeType();
			if (nodeType == XmlBinaryNodeType.StartListText)
			{
				XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);
			}
			if (nodeType == XmlBinaryNodeType.EndListText)
			{
				break;
			}
			ReadValue(nodeType, _listValue);
			num++;
		}
		value.SetValue(ValueHandleType.List, offset, num);
	}

	public void ReadQName(ValueHandle value)
	{
		int num = ReadUInt8();
		if (num >= 26)
		{
			XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);
		}
		int key = ReadDictionaryKey();
		value.SetQNameValue(num, key);
	}

	public int[] GetRows()
	{
		if (_buffer == null)
		{
			return new int[1];
		}
		List<int> list = new List<int>();
		list.Add(_offsetMin);
		for (int i = _offsetMin; i < _offsetMax; i++)
		{
			if (_buffer[i] == 13 || _buffer[i] == 10)
			{
				if (i + 1 < _offsetMax && _buffer[i + 1] == 10)
				{
					i++;
				}
				list.Add(i + 1);
			}
		}
		return list.ToArray();
	}
}
