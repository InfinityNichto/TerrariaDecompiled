using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Data.SqlTypes;

[XmlSchemaProvider("GetXsdType")]
public sealed class SqlChars : INullable, IXmlSerializable, ISerializable
{
	internal char[] _rgchBuf;

	private long _lCurLen;

	internal SqlStreamChars _stream;

	private SqlBytesCharsState _state;

	private char[] _rgchWorkBuf;

	public bool IsNull => _state == SqlBytesCharsState.Null;

	public char[]? Buffer
	{
		get
		{
			if (FStream())
			{
				CopyStreamToBuffer();
			}
			return _rgchBuf;
		}
	}

	public long Length => _state switch
	{
		SqlBytesCharsState.Null => throw new SqlNullValueException(), 
		SqlBytesCharsState.Stream => _stream.Length, 
		_ => _lCurLen, 
	};

	public long MaxLength
	{
		get
		{
			SqlBytesCharsState state = _state;
			if (state == SqlBytesCharsState.Stream)
			{
				return -1L;
			}
			return (_rgchBuf == null) ? (-1) : _rgchBuf.Length;
		}
	}

	public char[] Value
	{
		get
		{
			char[] array;
			switch (_state)
			{
			case SqlBytesCharsState.Null:
				throw new SqlNullValueException();
			case SqlBytesCharsState.Stream:
				if (_stream.Length > int.MaxValue)
				{
					throw new SqlTypeException(System.SR.SqlMisc_BufferInsufficientMessage);
				}
				array = new char[_stream.Length];
				if (_stream.Position != 0L)
				{
					_stream.Seek(0L, SeekOrigin.Begin);
				}
				_stream.Read(array, 0, checked((int)_stream.Length));
				break;
			default:
				array = new char[_lCurLen];
				Array.Copy(_rgchBuf, array, (int)_lCurLen);
				break;
			}
			return array;
		}
	}

	public char this[long offset]
	{
		get
		{
			if (offset < 0 || offset >= Length)
			{
				throw new ArgumentOutOfRangeException("offset");
			}
			if (_rgchWorkBuf == null)
			{
				_rgchWorkBuf = new char[1];
			}
			Read(offset, _rgchWorkBuf, 0, 1);
			return _rgchWorkBuf[0];
		}
		set
		{
			if (_rgchWorkBuf == null)
			{
				_rgchWorkBuf = new char[1];
			}
			_rgchWorkBuf[0] = value;
			Write(offset, _rgchWorkBuf, 0, 1);
		}
	}

	public StorageState Storage => _state switch
	{
		SqlBytesCharsState.Null => throw new SqlNullValueException(), 
		SqlBytesCharsState.Stream => StorageState.Stream, 
		SqlBytesCharsState.Buffer => StorageState.Buffer, 
		_ => StorageState.UnmanagedBuffer, 
	};

	public static SqlChars Null => new SqlChars((char[]?)null);

	public SqlChars()
	{
		SetNull();
	}

	public SqlChars(char[]? buffer)
	{
		_rgchBuf = buffer;
		_stream = null;
		if (_rgchBuf == null)
		{
			_state = SqlBytesCharsState.Null;
			_lCurLen = -1L;
		}
		else
		{
			_state = SqlBytesCharsState.Buffer;
			_lCurLen = _rgchBuf.Length;
		}
		_rgchWorkBuf = null;
	}

	public SqlChars(SqlString value)
		: this(value.IsNull ? null : value.Value.ToCharArray())
	{
	}

	public void SetNull()
	{
		_lCurLen = -1L;
		_stream = null;
		_state = SqlBytesCharsState.Null;
	}

	public void SetLength(long value)
	{
		if (value < 0)
		{
			throw new ArgumentOutOfRangeException("value");
		}
		if (FStream())
		{
			_stream.SetLength(value);
			return;
		}
		if (_rgchBuf == null)
		{
			throw new SqlTypeException(System.SR.SqlMisc_NoBufferMessage);
		}
		if (value > _rgchBuf.Length)
		{
			throw new ArgumentOutOfRangeException("value");
		}
		if (IsNull)
		{
			_state = SqlBytesCharsState.Buffer;
		}
		_lCurLen = value;
	}

	public long Read(long offset, char[] buffer, int offsetInBuffer, int count)
	{
		if (IsNull)
		{
			throw new SqlNullValueException();
		}
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (offset > Length || offset < 0)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if (offsetInBuffer > buffer.Length || offsetInBuffer < 0)
		{
			throw new ArgumentOutOfRangeException("offsetInBuffer");
		}
		if (count < 0 || count > buffer.Length - offsetInBuffer)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (count > Length - offset)
		{
			count = (int)(Length - offset);
		}
		if (count != 0)
		{
			SqlBytesCharsState state = _state;
			if (state == SqlBytesCharsState.Stream)
			{
				if (_stream.Position != offset)
				{
					_stream.Seek(offset, SeekOrigin.Begin);
				}
				_stream.Read(buffer, offsetInBuffer, count);
			}
			else
			{
				Array.Copy(_rgchBuf, offset, buffer, offsetInBuffer, count);
			}
		}
		return count;
	}

	public void Write(long offset, char[] buffer, int offsetInBuffer, int count)
	{
		if (FStream())
		{
			if (_stream.Position != offset)
			{
				_stream.Seek(offset, SeekOrigin.Begin);
			}
			_stream.Write(buffer, offsetInBuffer, count);
			return;
		}
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (_rgchBuf == null)
		{
			throw new SqlTypeException(System.SR.SqlMisc_NoBufferMessage);
		}
		if (offset < 0)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if (offset > _rgchBuf.Length)
		{
			throw new SqlTypeException(System.SR.SqlMisc_BufferInsufficientMessage);
		}
		if (offsetInBuffer < 0 || offsetInBuffer > buffer.Length)
		{
			throw new ArgumentOutOfRangeException("offsetInBuffer");
		}
		if (count < 0 || count > buffer.Length - offsetInBuffer)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (count > _rgchBuf.Length - offset)
		{
			throw new SqlTypeException(System.SR.SqlMisc_BufferInsufficientMessage);
		}
		if (IsNull)
		{
			if (offset != 0L)
			{
				throw new SqlTypeException(System.SR.SqlMisc_WriteNonZeroOffsetOnNullMessage);
			}
			_lCurLen = 0L;
			_state = SqlBytesCharsState.Buffer;
		}
		else if (offset > _lCurLen)
		{
			throw new SqlTypeException(System.SR.SqlMisc_WriteOffsetLargerThanLenMessage);
		}
		if (count != 0)
		{
			Array.Copy(buffer, offsetInBuffer, _rgchBuf, offset, count);
			if (_lCurLen < offset + count)
			{
				_lCurLen = offset + count;
			}
		}
	}

	public SqlString ToSqlString()
	{
		if (!IsNull)
		{
			return new string(Value);
		}
		return SqlString.Null;
	}

	public static explicit operator SqlString(SqlChars value)
	{
		return value.ToSqlString();
	}

	public static explicit operator SqlChars(SqlString value)
	{
		return new SqlChars(value);
	}

	internal bool FStream()
	{
		return _state == SqlBytesCharsState.Stream;
	}

	private void CopyStreamToBuffer()
	{
		long length = _stream.Length;
		if (length >= int.MaxValue)
		{
			throw new SqlTypeException(System.SR.SqlMisc_BufferInsufficientMessage);
		}
		if (_rgchBuf == null || _rgchBuf.Length < length)
		{
			_rgchBuf = new char[length];
		}
		if (_stream.Position != 0L)
		{
			_stream.Seek(0L, SeekOrigin.Begin);
		}
		_stream.Read(_rgchBuf, 0, (int)length);
		_stream = null;
		_lCurLen = length;
		_state = SqlBytesCharsState.Buffer;
	}

	private void SetBuffer(char[] buffer)
	{
		_rgchBuf = buffer;
		_lCurLen = ((_rgchBuf == null) ? (-1) : _rgchBuf.Length);
		_stream = null;
		_state = ((_rgchBuf != null) ? SqlBytesCharsState.Buffer : SqlBytesCharsState.Null);
	}

	XmlSchema IXmlSerializable.GetSchema()
	{
		return null;
	}

	void IXmlSerializable.ReadXml(XmlReader r)
	{
		char[] array = null;
		string attribute = r.GetAttribute("nil", "http://www.w3.org/2001/XMLSchema-instance");
		if (attribute != null && XmlConvert.ToBoolean(attribute))
		{
			r.ReadElementString();
			SetNull();
		}
		else
		{
			array = r.ReadElementString().ToCharArray();
			SetBuffer(array);
		}
	}

	void IXmlSerializable.WriteXml(XmlWriter writer)
	{
		if (IsNull)
		{
			writer.WriteAttributeString("xsi", "nil", "http://www.w3.org/2001/XMLSchema-instance", "true");
			return;
		}
		char[] buffer = Buffer;
		writer.WriteString(new string(buffer, 0, (int)Length));
	}

	public static XmlQualifiedName GetXsdType(XmlSchemaSet schemaSet)
	{
		return new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema");
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}
}
