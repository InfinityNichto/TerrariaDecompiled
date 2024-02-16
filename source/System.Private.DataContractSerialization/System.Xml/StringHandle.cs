using System.Diagnostics.CodeAnalysis;

namespace System.Xml;

internal sealed class StringHandle : IEquatable<StringHandle>
{
	private enum StringHandleType
	{
		Dictionary,
		UTF8,
		EscapedUTF8,
		ConstString
	}

	private readonly XmlBufferReader _bufferReader;

	private StringHandleType _type;

	private int _key;

	private int _offset;

	private int _length;

	private static readonly string[] s_constStrings = new string[3] { "type", "root", "item" };

	public bool IsEmpty
	{
		get
		{
			if (_type == StringHandleType.UTF8)
			{
				return _length == 0;
			}
			return Equals2(string.Empty);
		}
	}

	public bool IsXmlns
	{
		get
		{
			if (_type == StringHandleType.UTF8)
			{
				if (_length != 5)
				{
					return false;
				}
				byte[] buffer = _bufferReader.Buffer;
				int offset = _offset;
				if (buffer[offset] == 120 && buffer[offset + 1] == 109 && buffer[offset + 2] == 108 && buffer[offset + 3] == 110)
				{
					return buffer[offset + 4] == 115;
				}
				return false;
			}
			return Equals2("xmlns");
		}
	}

	public StringHandle(XmlBufferReader bufferReader)
	{
		_bufferReader = bufferReader;
		SetValue(0, 0);
	}

	public void SetValue(int offset, int length)
	{
		_type = StringHandleType.UTF8;
		_offset = offset;
		_length = length;
	}

	public void SetConstantValue(StringHandleConstStringType constStringType)
	{
		_type = StringHandleType.ConstString;
		_key = (int)constStringType;
	}

	public void SetValue(int offset, int length, bool escaped)
	{
		_type = ((!escaped) ? StringHandleType.UTF8 : StringHandleType.EscapedUTF8);
		_offset = offset;
		_length = length;
	}

	public void SetValue(int key)
	{
		_type = StringHandleType.Dictionary;
		_key = key;
	}

	public void SetValue(StringHandle value)
	{
		_type = value._type;
		_key = value._key;
		_offset = value._offset;
		_length = value._length;
	}

	public void ToPrefixHandle(PrefixHandle prefix)
	{
		prefix.SetValue(_offset, _length);
	}

	public string GetString(XmlNameTable nameTable)
	{
		return _type switch
		{
			StringHandleType.UTF8 => _bufferReader.GetString(_offset, _length, nameTable), 
			StringHandleType.Dictionary => nameTable.Add(_bufferReader.GetDictionaryString(_key).Value), 
			_ => nameTable.Add(s_constStrings[_key]), 
		};
	}

	public string GetString()
	{
		return _type switch
		{
			StringHandleType.UTF8 => _bufferReader.GetString(_offset, _length), 
			StringHandleType.Dictionary => _bufferReader.GetDictionaryString(_key).Value, 
			_ => s_constStrings[_key], 
		};
	}

	public byte[] GetString(out int offset, out int length)
	{
		switch (_type)
		{
		case StringHandleType.UTF8:
			offset = _offset;
			length = _length;
			return _bufferReader.Buffer;
		case StringHandleType.Dictionary:
		{
			byte[] array3 = _bufferReader.GetDictionaryString(_key).ToUTF8();
			offset = 0;
			length = array3.Length;
			return array3;
		}
		case StringHandleType.ConstString:
		{
			byte[] array2 = XmlConverter.ToBytes(s_constStrings[_key]);
			offset = 0;
			length = array2.Length;
			return array2;
		}
		default:
		{
			byte[] array = XmlConverter.ToBytes(_bufferReader.GetEscapedString(_offset, _length));
			offset = 0;
			length = array.Length;
			return array;
		}
		}
	}

	public bool TryGetDictionaryString([NotNullWhen(true)] out XmlDictionaryString value)
	{
		if (_type == StringHandleType.Dictionary)
		{
			value = _bufferReader.GetDictionaryString(_key);
			return true;
		}
		if (IsEmpty)
		{
			value = XmlDictionaryString.Empty;
			return true;
		}
		value = null;
		return false;
	}

	public override string ToString()
	{
		return GetString();
	}

	private bool Equals2(int key2, XmlBufferReader bufferReader2)
	{
		return _type switch
		{
			StringHandleType.Dictionary => _bufferReader.Equals2(_key, key2, bufferReader2), 
			StringHandleType.UTF8 => _bufferReader.Equals2(_offset, _length, bufferReader2.GetDictionaryString(key2).Value), 
			_ => GetString() == _bufferReader.GetDictionaryString(key2).Value, 
		};
	}

	private bool Equals2(XmlDictionaryString xmlString2)
	{
		return _type switch
		{
			StringHandleType.Dictionary => _bufferReader.Equals2(_key, xmlString2), 
			StringHandleType.UTF8 => _bufferReader.Equals2(_offset, _length, xmlString2.ToUTF8()), 
			_ => GetString() == xmlString2.Value, 
		};
	}

	private bool Equals2(string s2)
	{
		return _type switch
		{
			StringHandleType.Dictionary => _bufferReader.GetDictionaryString(_key).Value == s2, 
			StringHandleType.UTF8 => _bufferReader.Equals2(_offset, _length, s2), 
			_ => GetString() == s2, 
		};
	}

	private bool Equals2(int offset2, int length2, XmlBufferReader bufferReader2)
	{
		return _type switch
		{
			StringHandleType.Dictionary => bufferReader2.Equals2(offset2, length2, _bufferReader.GetDictionaryString(_key).Value), 
			StringHandleType.UTF8 => _bufferReader.Equals2(_offset, _length, bufferReader2, offset2, length2), 
			_ => GetString() == _bufferReader.GetString(offset2, length2), 
		};
	}

	public bool Equals([NotNullWhen(true)] StringHandle other)
	{
		if ((object)other == null)
		{
			return false;
		}
		return other._type switch
		{
			StringHandleType.Dictionary => Equals2(other._key, other._bufferReader), 
			StringHandleType.UTF8 => Equals2(other._offset, other._length, other._bufferReader), 
			_ => Equals2(other.GetString()), 
		};
	}

	public static bool operator ==(StringHandle s1, XmlDictionaryString xmlString2)
	{
		return s1.Equals2(xmlString2);
	}

	public static bool operator !=(StringHandle s1, XmlDictionaryString xmlString2)
	{
		return !s1.Equals2(xmlString2);
	}

	public static bool operator ==(StringHandle s1, string s2)
	{
		return s1.Equals2(s2);
	}

	public static bool operator !=(StringHandle s1, string s2)
	{
		return !s1.Equals2(s2);
	}

	public static bool operator ==(StringHandle s1, StringHandle s2)
	{
		return s1.Equals(s2);
	}

	public static bool operator !=(StringHandle s1, StringHandle s2)
	{
		return !s1.Equals(s2);
	}

	public int CompareTo(StringHandle that)
	{
		if (_type == StringHandleType.UTF8 && that._type == StringHandleType.UTF8)
		{
			return _bufferReader.Compare(_offset, _length, that._offset, that._length);
		}
		return string.Compare(GetString(), that.GetString(), StringComparison.Ordinal);
	}

	public override bool Equals([NotNullWhen(true)] object obj)
	{
		return Equals(obj as StringHandle);
	}

	public override int GetHashCode()
	{
		return GetString().GetHashCode();
	}
}
