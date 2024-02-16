using System.Diagnostics.CodeAnalysis;

namespace System.Xml;

internal sealed class PrefixHandle : IEquatable<PrefixHandle>
{
	private readonly XmlBufferReader _bufferReader;

	private PrefixHandleType _type;

	private int _offset;

	private int _length;

	private static readonly string[] s_prefixStrings = new string[27]
	{
		"", "a", "b", "c", "d", "e", "f", "g", "h", "i",
		"j", "k", "l", "m", "n", "o", "p", "q", "r", "s",
		"t", "u", "v", "w", "x", "y", "z"
	};

	private static readonly byte[] s_prefixBuffer = new byte[26]
	{
		97, 98, 99, 100, 101, 102, 103, 104, 105, 106,
		107, 108, 109, 110, 111, 112, 113, 114, 115, 116,
		117, 118, 119, 120, 121, 122
	};

	public bool IsEmpty => _type == PrefixHandleType.Empty;

	public bool IsXmlns
	{
		get
		{
			if (_type != PrefixHandleType.Buffer)
			{
				return false;
			}
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
	}

	public bool IsXml
	{
		get
		{
			if (_type != PrefixHandleType.Buffer)
			{
				return false;
			}
			if (_length != 3)
			{
				return false;
			}
			byte[] buffer = _bufferReader.Buffer;
			int offset = _offset;
			if (buffer[offset] == 120 && buffer[offset + 1] == 109)
			{
				return buffer[offset + 2] == 108;
			}
			return false;
		}
	}

	public PrefixHandle(XmlBufferReader bufferReader)
	{
		_bufferReader = bufferReader;
	}

	public void SetValue(PrefixHandleType type)
	{
		_type = type;
	}

	public void SetValue(PrefixHandle prefix)
	{
		_type = prefix._type;
		_offset = prefix._offset;
		_length = prefix._length;
	}

	public void SetValue(int offset, int length)
	{
		switch (length)
		{
		case 0:
			SetValue(PrefixHandleType.Empty);
			return;
		case 1:
		{
			byte @byte = _bufferReader.GetByte(offset);
			if (@byte >= 97 && @byte <= 122)
			{
				SetValue(GetAlphaPrefix(@byte - 97));
				return;
			}
			break;
		}
		}
		_type = PrefixHandleType.Buffer;
		_offset = offset;
		_length = length;
	}

	public bool TryGetShortPrefix(out PrefixHandleType type)
	{
		type = _type;
		return type != PrefixHandleType.Buffer;
	}

	public static string GetString(PrefixHandleType type)
	{
		return s_prefixStrings[(int)type];
	}

	public static PrefixHandleType GetAlphaPrefix(int index)
	{
		return (PrefixHandleType)(1 + index);
	}

	public static byte[] GetString(PrefixHandleType type, out int offset, out int length)
	{
		if (type == PrefixHandleType.Empty)
		{
			offset = 0;
			length = 0;
		}
		else
		{
			length = 1;
			offset = (int)(type - 1);
		}
		return s_prefixBuffer;
	}

	public string GetString(XmlNameTable nameTable)
	{
		PrefixHandleType type = _type;
		if (type != PrefixHandleType.Buffer)
		{
			return GetString(type);
		}
		return _bufferReader.GetString(_offset, _length, nameTable);
	}

	public string GetString()
	{
		PrefixHandleType type = _type;
		if (type != PrefixHandleType.Buffer)
		{
			return GetString(type);
		}
		return _bufferReader.GetString(_offset, _length);
	}

	public byte[] GetString(out int offset, out int length)
	{
		PrefixHandleType type = _type;
		if (type != PrefixHandleType.Buffer)
		{
			return GetString(type, out offset, out length);
		}
		offset = _offset;
		length = _length;
		return _bufferReader.Buffer;
	}

	public int CompareTo(PrefixHandle that)
	{
		return GetString().CompareTo(that.GetString());
	}

	public bool Equals([NotNullWhen(true)] PrefixHandle prefix2)
	{
		if ((object)prefix2 == null)
		{
			return false;
		}
		PrefixHandleType type = _type;
		PrefixHandleType type2 = prefix2._type;
		if (type != type2)
		{
			return false;
		}
		if (type != PrefixHandleType.Buffer)
		{
			return true;
		}
		if (_bufferReader == prefix2._bufferReader)
		{
			return _bufferReader.Equals2(_offset, _length, prefix2._offset, prefix2._length);
		}
		return _bufferReader.Equals2(_offset, _length, prefix2._bufferReader, prefix2._offset, prefix2._length);
	}

	private bool Equals2(string prefix2)
	{
		PrefixHandleType type = _type;
		if (type != PrefixHandleType.Buffer)
		{
			return GetString(type) == prefix2;
		}
		return _bufferReader.Equals2(_offset, _length, prefix2);
	}

	private bool Equals2(XmlDictionaryString prefix2)
	{
		return Equals2(prefix2.Value);
	}

	public static bool operator ==(PrefixHandle prefix1, string prefix2)
	{
		return prefix1.Equals2(prefix2);
	}

	public static bool operator !=(PrefixHandle prefix1, string prefix2)
	{
		return !prefix1.Equals2(prefix2);
	}

	public static bool operator ==(PrefixHandle prefix1, XmlDictionaryString prefix2)
	{
		return prefix1.Equals2(prefix2);
	}

	public static bool operator !=(PrefixHandle prefix1, XmlDictionaryString prefix2)
	{
		return !prefix1.Equals2(prefix2);
	}

	public static bool operator ==(PrefixHandle prefix1, PrefixHandle prefix2)
	{
		return prefix1.Equals(prefix2);
	}

	public static bool operator !=(PrefixHandle prefix1, PrefixHandle prefix2)
	{
		return !prefix1.Equals(prefix2);
	}

	public override bool Equals([NotNullWhen(true)] object obj)
	{
		return Equals(obj as PrefixHandle);
	}

	public override string ToString()
	{
		return GetString();
	}

	public override int GetHashCode()
	{
		return GetString().GetHashCode();
	}
}
