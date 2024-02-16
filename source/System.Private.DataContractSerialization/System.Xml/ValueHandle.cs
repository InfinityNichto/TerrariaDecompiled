using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text;

namespace System.Xml;

internal sealed class ValueHandle
{
	private readonly XmlBufferReader _bufferReader;

	private ValueHandleType _type;

	private int _offset;

	private int _length;

	private static Base64Encoding s_base64Encoding;

	private static readonly string[] s_constStrings = new string[6] { "string", "number", "array", "object", "boolean", "null" };

	private static Base64Encoding Base64Encoding => s_base64Encoding ?? (s_base64Encoding = new Base64Encoding());

	public ValueHandle(XmlBufferReader bufferReader)
	{
		_bufferReader = bufferReader;
		_type = ValueHandleType.Empty;
	}

	public void SetConstantValue(ValueHandleConstStringType constStringType)
	{
		_type = ValueHandleType.ConstString;
		_offset = (int)constStringType;
	}

	public void SetValue(ValueHandleType type)
	{
		_type = type;
	}

	public void SetDictionaryValue(int key)
	{
		SetValue(ValueHandleType.Dictionary, key, 0);
	}

	public void SetCharValue(int ch)
	{
		SetValue(ValueHandleType.Char, ch, 0);
	}

	public void SetQNameValue(int prefix, int key)
	{
		SetValue(ValueHandleType.QName, key, prefix);
	}

	public void SetValue(ValueHandleType type, int offset, int length)
	{
		_type = type;
		_offset = offset;
		_length = length;
	}

	public bool IsWhitespace()
	{
		switch (_type)
		{
		case ValueHandleType.UTF8:
			return _bufferReader.IsWhitespaceUTF8(_offset, _length);
		case ValueHandleType.Dictionary:
			return _bufferReader.IsWhitespaceKey(_offset);
		case ValueHandleType.Char:
		{
			int @char = GetChar();
			if (@char <= 65535)
			{
				return XmlConverter.IsWhitespace((char)@char);
			}
			return false;
		}
		case ValueHandleType.EscapedUTF8:
			return _bufferReader.IsWhitespaceUTF8(_offset, _length);
		case ValueHandleType.Unicode:
			return _bufferReader.IsWhitespaceUnicode(_offset, _length);
		case ValueHandleType.True:
		case ValueHandleType.False:
		case ValueHandleType.Zero:
		case ValueHandleType.One:
			return false;
		case ValueHandleType.ConstString:
			return s_constStrings[_offset].Length == 0;
		default:
			return _length == 0;
		}
	}

	public Type ToType()
	{
		switch (_type)
		{
		case ValueHandleType.True:
		case ValueHandleType.False:
			return typeof(bool);
		case ValueHandleType.Zero:
		case ValueHandleType.One:
		case ValueHandleType.Int8:
		case ValueHandleType.Int16:
		case ValueHandleType.Int32:
			return typeof(int);
		case ValueHandleType.Int64:
			return typeof(long);
		case ValueHandleType.UInt64:
			return typeof(ulong);
		case ValueHandleType.Single:
			return typeof(float);
		case ValueHandleType.Double:
			return typeof(double);
		case ValueHandleType.Decimal:
			return typeof(decimal);
		case ValueHandleType.DateTime:
			return typeof(DateTime);
		case ValueHandleType.Empty:
		case ValueHandleType.UTF8:
		case ValueHandleType.EscapedUTF8:
		case ValueHandleType.Dictionary:
		case ValueHandleType.Char:
		case ValueHandleType.Unicode:
		case ValueHandleType.QName:
		case ValueHandleType.ConstString:
			return typeof(string);
		case ValueHandleType.Base64:
			return typeof(byte[]);
		case ValueHandleType.List:
			return typeof(object[]);
		case ValueHandleType.UniqueId:
			return typeof(UniqueId);
		case ValueHandleType.Guid:
			return typeof(Guid);
		case ValueHandleType.TimeSpan:
			return typeof(TimeSpan);
		default:
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException());
		}
	}

	public bool ToBoolean()
	{
		switch (_type)
		{
		case ValueHandleType.False:
			return false;
		case ValueHandleType.True:
			return true;
		case ValueHandleType.UTF8:
			return XmlConverter.ToBoolean(_bufferReader.Buffer, _offset, _length);
		case ValueHandleType.Int8:
			switch (GetInt8())
			{
			case 0:
				return false;
			case 1:
				return true;
			}
			break;
		}
		return XmlConverter.ToBoolean(GetString());
	}

	public int ToInt()
	{
		ValueHandleType type = _type;
		switch (type)
		{
		case ValueHandleType.Zero:
			return 0;
		case ValueHandleType.One:
			return 1;
		case ValueHandleType.Int8:
			return GetInt8();
		case ValueHandleType.Int16:
			return GetInt16();
		case ValueHandleType.Int32:
			return GetInt32();
		case ValueHandleType.Int64:
		{
			long @int = GetInt64();
			if (@int >= int.MinValue && @int <= int.MaxValue)
			{
				return (int)@int;
			}
			break;
		}
		}
		if (type == ValueHandleType.UInt64)
		{
			ulong uInt = GetUInt64();
			if (uInt <= int.MaxValue)
			{
				return (int)uInt;
			}
		}
		if (type == ValueHandleType.UTF8)
		{
			return XmlConverter.ToInt32(_bufferReader.Buffer, _offset, _length);
		}
		return XmlConverter.ToInt32(GetString());
	}

	public long ToLong()
	{
		ValueHandleType type = _type;
		switch (type)
		{
		case ValueHandleType.Zero:
			return 0L;
		case ValueHandleType.One:
			return 1L;
		case ValueHandleType.Int8:
			return GetInt8();
		case ValueHandleType.Int16:
			return GetInt16();
		case ValueHandleType.Int32:
			return GetInt32();
		case ValueHandleType.Int64:
			return GetInt64();
		case ValueHandleType.UInt64:
		{
			ulong uInt = GetUInt64();
			if (uInt <= long.MaxValue)
			{
				return (long)uInt;
			}
			break;
		}
		}
		if (type == ValueHandleType.UTF8)
		{
			return XmlConverter.ToInt64(_bufferReader.Buffer, _offset, _length);
		}
		return XmlConverter.ToInt64(GetString());
	}

	public ulong ToULong()
	{
		ValueHandleType type = _type;
		switch (type)
		{
		case ValueHandleType.Zero:
			return 0uL;
		case ValueHandleType.One:
			return 1uL;
		case ValueHandleType.Int8:
		case ValueHandleType.Int16:
		case ValueHandleType.Int32:
		case ValueHandleType.Int64:
		{
			long num = ToLong();
			if (num >= 0)
			{
				return (ulong)num;
			}
			break;
		}
		}
		return type switch
		{
			ValueHandleType.UInt64 => GetUInt64(), 
			ValueHandleType.UTF8 => XmlConverter.ToUInt64(_bufferReader.Buffer, _offset, _length), 
			_ => XmlConverter.ToUInt64(GetString()), 
		};
	}

	public float ToSingle()
	{
		ValueHandleType type = _type;
		switch (type)
		{
		case ValueHandleType.Single:
			return GetSingle();
		case ValueHandleType.Double:
		{
			double @double = GetDouble();
			if ((@double >= -3.4028234663852886E+38 && @double <= 3.4028234663852886E+38) || !double.IsFinite(@double))
			{
				return (float)@double;
			}
			break;
		}
		}
		return type switch
		{
			ValueHandleType.Zero => 0f, 
			ValueHandleType.One => 1f, 
			ValueHandleType.Int8 => GetInt8(), 
			ValueHandleType.Int16 => GetInt16(), 
			ValueHandleType.UTF8 => XmlConverter.ToSingle(_bufferReader.Buffer, _offset, _length), 
			_ => XmlConverter.ToSingle(GetString()), 
		};
	}

	public double ToDouble()
	{
		return _type switch
		{
			ValueHandleType.Double => GetDouble(), 
			ValueHandleType.Single => GetSingle(), 
			ValueHandleType.Zero => 0.0, 
			ValueHandleType.One => 1.0, 
			ValueHandleType.Int8 => GetInt8(), 
			ValueHandleType.Int16 => GetInt16(), 
			ValueHandleType.Int32 => GetInt32(), 
			ValueHandleType.UTF8 => XmlConverter.ToDouble(_bufferReader.Buffer, _offset, _length), 
			_ => XmlConverter.ToDouble(GetString()), 
		};
	}

	public decimal ToDecimal()
	{
		ValueHandleType type = _type;
		switch (type)
		{
		case ValueHandleType.Decimal:
			return GetDecimal();
		case ValueHandleType.Zero:
			return 0m;
		case ValueHandleType.One:
			return 1m;
		case ValueHandleType.Int8:
		case ValueHandleType.Int16:
		case ValueHandleType.Int32:
		case ValueHandleType.Int64:
			return ToLong();
		default:
			return type switch
			{
				ValueHandleType.UInt64 => GetUInt64(), 
				ValueHandleType.UTF8 => XmlConverter.ToDecimal(_bufferReader.Buffer, _offset, _length), 
				_ => XmlConverter.ToDecimal(GetString()), 
			};
		}
	}

	public DateTime ToDateTime()
	{
		if (_type == ValueHandleType.DateTime)
		{
			return XmlConverter.ToDateTime(GetInt64());
		}
		if (_type == ValueHandleType.UTF8)
		{
			return XmlConverter.ToDateTime(_bufferReader.Buffer, _offset, _length);
		}
		return XmlConverter.ToDateTime(GetString());
	}

	public UniqueId ToUniqueId()
	{
		if (_type == ValueHandleType.UniqueId)
		{
			return GetUniqueId();
		}
		if (_type == ValueHandleType.UTF8)
		{
			return XmlConverter.ToUniqueId(_bufferReader.Buffer, _offset, _length);
		}
		return XmlConverter.ToUniqueId(GetString());
	}

	public TimeSpan ToTimeSpan()
	{
		if (_type == ValueHandleType.TimeSpan)
		{
			return new TimeSpan(GetInt64());
		}
		if (_type == ValueHandleType.UTF8)
		{
			return XmlConverter.ToTimeSpan(_bufferReader.Buffer, _offset, _length);
		}
		return XmlConverter.ToTimeSpan(GetString());
	}

	public Guid ToGuid()
	{
		if (_type == ValueHandleType.Guid)
		{
			return GetGuid();
		}
		if (_type == ValueHandleType.UTF8)
		{
			return XmlConverter.ToGuid(_bufferReader.Buffer, _offset, _length);
		}
		return XmlConverter.ToGuid(GetString());
	}

	public override string ToString()
	{
		return GetString();
	}

	public byte[] ToByteArray()
	{
		if (_type == ValueHandleType.Base64)
		{
			byte[] array = new byte[_length];
			GetBase64(array, 0, _length);
			return array;
		}
		if (_type == ValueHandleType.UTF8 && _length % 4 == 0)
		{
			try
			{
				int num = _length / 4 * 3;
				if (_length > 0 && _bufferReader.Buffer[_offset + _length - 1] == 61)
				{
					num--;
					if (_bufferReader.Buffer[_offset + _length - 2] == 61)
					{
						num--;
					}
				}
				byte[] array2 = new byte[num];
				int bytes = Base64Encoding.GetBytes(_bufferReader.Buffer, _offset, _length, array2, 0);
				if (bytes != array2.Length)
				{
					byte[] array3 = new byte[bytes];
					Buffer.BlockCopy(array2, 0, array3, 0, bytes);
					array2 = array3;
				}
				return array2;
			}
			catch (FormatException)
			{
			}
		}
		try
		{
			return Base64Encoding.GetBytes(XmlConverter.StripWhitespace(GetString()));
		}
		catch (FormatException ex2)
		{
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(ex2.Message, ex2.InnerException));
		}
	}

	public string GetString()
	{
		switch (_type)
		{
		case ValueHandleType.UTF8:
			return GetCharsText();
		case ValueHandleType.False:
			return "false";
		case ValueHandleType.True:
			return "true";
		case ValueHandleType.Zero:
			return "0";
		case ValueHandleType.One:
			return "1";
		case ValueHandleType.Int8:
		case ValueHandleType.Int16:
		case ValueHandleType.Int32:
			return XmlConverter.ToString(ToInt());
		case ValueHandleType.Int64:
			return XmlConverter.ToString(GetInt64());
		case ValueHandleType.UInt64:
			return XmlConverter.ToString(GetUInt64());
		case ValueHandleType.Single:
			return XmlConverter.ToString(GetSingle());
		case ValueHandleType.Double:
			return XmlConverter.ToString(GetDouble());
		case ValueHandleType.Decimal:
			return XmlConverter.ToString(GetDecimal());
		case ValueHandleType.DateTime:
			return XmlConverter.ToString(ToDateTime());
		case ValueHandleType.Empty:
			return string.Empty;
		case ValueHandleType.Unicode:
			return GetUnicodeCharsText();
		case ValueHandleType.EscapedUTF8:
			return GetEscapedCharsText();
		case ValueHandleType.Char:
			return GetCharText();
		case ValueHandleType.Dictionary:
			return GetDictionaryString().Value;
		case ValueHandleType.Base64:
		{
			byte[] array = ToByteArray();
			return Base64Encoding.GetString(array, 0, array.Length);
		}
		case ValueHandleType.List:
			return XmlConverter.ToString(ToList());
		case ValueHandleType.UniqueId:
			return XmlConverter.ToString(ToUniqueId());
		case ValueHandleType.Guid:
			return XmlConverter.ToString(ToGuid());
		case ValueHandleType.TimeSpan:
			return XmlConverter.ToString(ToTimeSpan());
		case ValueHandleType.QName:
			return GetQNameDictionaryText();
		case ValueHandleType.ConstString:
			return s_constStrings[_offset];
		default:
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException());
		}
	}

	public bool Equals2(string str, bool checkLower)
	{
		if (_type != ValueHandleType.UTF8)
		{
			return GetString() == str;
		}
		if (_length != str.Length)
		{
			return false;
		}
		byte[] buffer = _bufferReader.Buffer;
		for (int i = 0; i < _length; i++)
		{
			byte b = buffer[i + _offset];
			if (b != str[i] && (!checkLower || char.ToLowerInvariant((char)b) != str[i]))
			{
				return false;
			}
		}
		return true;
	}

	public void Sign(XmlSigningNodeWriter writer)
	{
		switch (_type)
		{
		case ValueHandleType.Int8:
		case ValueHandleType.Int16:
		case ValueHandleType.Int32:
			writer.WriteInt32Text(ToInt());
			break;
		case ValueHandleType.Int64:
			writer.WriteInt64Text(GetInt64());
			break;
		case ValueHandleType.UInt64:
			writer.WriteUInt64Text(GetUInt64());
			break;
		case ValueHandleType.Single:
			writer.WriteFloatText(GetSingle());
			break;
		case ValueHandleType.Double:
			writer.WriteDoubleText(GetDouble());
			break;
		case ValueHandleType.Decimal:
			writer.WriteDecimalText(GetDecimal());
			break;
		case ValueHandleType.DateTime:
			writer.WriteDateTimeText(ToDateTime());
			break;
		case ValueHandleType.UTF8:
			writer.WriteEscapedText(_bufferReader.Buffer, _offset, _length);
			break;
		case ValueHandleType.Base64:
			writer.WriteBase64Text(_bufferReader.Buffer, 0, _bufferReader.Buffer, _offset, _length);
			break;
		case ValueHandleType.UniqueId:
			writer.WriteUniqueIdText(ToUniqueId());
			break;
		case ValueHandleType.Guid:
			writer.WriteGuidText(ToGuid());
			break;
		case ValueHandleType.TimeSpan:
			writer.WriteTimeSpanText(ToTimeSpan());
			break;
		default:
			writer.WriteEscapedText(GetString());
			break;
		case ValueHandleType.Empty:
			break;
		}
	}

	public object[] ToList()
	{
		return _bufferReader.GetList(_offset, _length);
	}

	public object ToObject()
	{
		switch (_type)
		{
		case ValueHandleType.True:
		case ValueHandleType.False:
			return ToBoolean();
		case ValueHandleType.Zero:
		case ValueHandleType.One:
		case ValueHandleType.Int8:
		case ValueHandleType.Int16:
		case ValueHandleType.Int32:
			return ToInt();
		case ValueHandleType.Int64:
			return ToLong();
		case ValueHandleType.UInt64:
			return GetUInt64();
		case ValueHandleType.Single:
			return ToSingle();
		case ValueHandleType.Double:
			return ToDouble();
		case ValueHandleType.Decimal:
			return ToDecimal();
		case ValueHandleType.DateTime:
			return ToDateTime();
		case ValueHandleType.Empty:
		case ValueHandleType.UTF8:
		case ValueHandleType.EscapedUTF8:
		case ValueHandleType.Dictionary:
		case ValueHandleType.Char:
		case ValueHandleType.Unicode:
		case ValueHandleType.ConstString:
			return ToString();
		case ValueHandleType.Base64:
			return ToByteArray();
		case ValueHandleType.List:
			return ToList();
		case ValueHandleType.UniqueId:
			return ToUniqueId();
		case ValueHandleType.Guid:
			return ToGuid();
		case ValueHandleType.TimeSpan:
			return ToTimeSpan();
		default:
			throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException());
		}
	}

	public bool TryReadBase64(byte[] buffer, int offset, int count, out int actual)
	{
		if (_type == ValueHandleType.Base64)
		{
			actual = Math.Min(_length, count);
			GetBase64(buffer, offset, actual);
			_offset += actual;
			_length -= actual;
			return true;
		}
		if (_type == ValueHandleType.UTF8 && count >= 3 && _length % 4 == 0)
		{
			try
			{
				int num = Math.Min(count / 3 * 4, _length);
				actual = Base64Encoding.GetBytes(_bufferReader.Buffer, _offset, num, buffer, offset);
				_offset += num;
				_length -= num;
				return true;
			}
			catch (FormatException)
			{
			}
		}
		actual = 0;
		return false;
	}

	public bool TryReadChars(char[] chars, int offset, int count, out int actual)
	{
		if (_type == ValueHandleType.Unicode)
		{
			return TryReadUnicodeChars(chars, offset, count, out actual);
		}
		if (_type != ValueHandleType.UTF8)
		{
			actual = 0;
			return false;
		}
		int num = offset;
		int num2 = count;
		byte[] buffer = _bufferReader.Buffer;
		int num3 = _offset;
		int num4 = _length;
		bool flag = false;
		UTF8Encoding uTF8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
		while (true)
		{
			if (num2 > 0 && num4 > 0)
			{
				byte b = buffer[num3];
				if (b < 128)
				{
					chars[num] = (char)b;
					num3++;
					num4--;
					num++;
					num2--;
					continue;
				}
			}
			if (num2 == 0 || num4 == 0 || flag)
			{
				break;
			}
			int chars2;
			int num5;
			try
			{
				if (num2 >= uTF8Encoding.GetMaxCharCount(num4) || num2 >= uTF8Encoding.GetCharCount(buffer, num3, num4))
				{
					chars2 = uTF8Encoding.GetChars(buffer, num3, num4, chars, num);
					num5 = num4;
				}
				else
				{
					Decoder decoder = uTF8Encoding.GetDecoder();
					num5 = Math.Min(num2, num4);
					chars2 = decoder.GetChars(buffer, num3, num5, chars, num);
					while (chars2 == 0)
					{
						if (num5 >= 3 && num2 < 2)
						{
							flag = true;
							break;
						}
						chars2 = decoder.GetChars(buffer, num3 + num5, 1, chars, num);
						num5++;
					}
					num5 = uTF8Encoding.GetByteCount(chars, num, chars2);
				}
			}
			catch (FormatException exception)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateEncodingException(buffer, num3, num4, exception));
			}
			num3 += num5;
			num4 -= num5;
			num += chars2;
			num2 -= chars2;
		}
		_offset = num3;
		_length = num4;
		actual = count - num2;
		return true;
	}

	private bool TryReadUnicodeChars(char[] chars, int offset, int count, out int actual)
	{
		int num = Math.Min(count, _length / 2);
		for (int i = 0; i < num; i++)
		{
			chars[offset + i] = (char)_bufferReader.GetInt16(_offset + i * 2);
		}
		_offset += num * 2;
		_length -= num * 2;
		actual = num;
		return true;
	}

	public bool TryGetDictionaryString([NotNullWhen(true)] out XmlDictionaryString value)
	{
		if (_type == ValueHandleType.Dictionary)
		{
			value = GetDictionaryString();
			return true;
		}
		value = null;
		return false;
	}

	public bool TryGetByteArrayLength(out int length)
	{
		if (_type == ValueHandleType.Base64)
		{
			length = _length;
			return true;
		}
		length = 0;
		return false;
	}

	private string GetCharsText()
	{
		if (_length == 1 && _bufferReader.GetByte(_offset) == 49)
		{
			return "1";
		}
		return _bufferReader.GetString(_offset, _length);
	}

	private string GetUnicodeCharsText()
	{
		return _bufferReader.GetUnicodeString(_offset, _length);
	}

	private string GetEscapedCharsText()
	{
		return _bufferReader.GetEscapedString(_offset, _length);
	}

	private string GetCharText()
	{
		int @char = GetChar();
		if (@char > 65535)
		{
			SurrogateChar surrogateChar = new SurrogateChar(@char);
			Span<char> span = stackalloc char[2] { surrogateChar.HighChar, surrogateChar.LowChar };
			return new string(span);
		}
		return ((char)@char).ToString();
	}

	private int GetChar()
	{
		return _offset;
	}

	private int GetInt8()
	{
		return _bufferReader.GetInt8(_offset);
	}

	private int GetInt16()
	{
		return _bufferReader.GetInt16(_offset);
	}

	private int GetInt32()
	{
		return _bufferReader.GetInt32(_offset);
	}

	private long GetInt64()
	{
		return _bufferReader.GetInt64(_offset);
	}

	private ulong GetUInt64()
	{
		return _bufferReader.GetUInt64(_offset);
	}

	private float GetSingle()
	{
		return _bufferReader.GetSingle(_offset);
	}

	private double GetDouble()
	{
		return _bufferReader.GetDouble(_offset);
	}

	private decimal GetDecimal()
	{
		return _bufferReader.GetDecimal(_offset);
	}

	private UniqueId GetUniqueId()
	{
		return _bufferReader.GetUniqueId(_offset);
	}

	private Guid GetGuid()
	{
		return _bufferReader.GetGuid(_offset);
	}

	private void GetBase64(byte[] buffer, int offset, int count)
	{
		_bufferReader.GetBase64(_offset, buffer, offset, count);
	}

	private XmlDictionaryString GetDictionaryString()
	{
		return _bufferReader.GetDictionaryString(_offset);
	}

	private string GetQNameDictionaryText()
	{
		return PrefixHandle.GetString(PrefixHandle.GetAlphaPrefix(_length)) + ":" + _bufferReader.GetDictionaryString(_offset);
	}
}
