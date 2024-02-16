using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace System.Text.Json;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly struct JsonElement
{
	[DebuggerDisplay("{Current,nq}")]
	public struct ArrayEnumerator : IEnumerable<JsonElement>, IEnumerable, IEnumerator<JsonElement>, IEnumerator, IDisposable
	{
		private readonly JsonElement _target;

		private int _curIdx;

		private readonly int _endIdxOrVersion;

		public JsonElement Current
		{
			get
			{
				if (_curIdx < 0)
				{
					return default(JsonElement);
				}
				return new JsonElement(_target._parent, _curIdx);
			}
		}

		object IEnumerator.Current => Current;

		internal ArrayEnumerator(JsonElement target)
		{
			_target = target;
			_curIdx = -1;
			_endIdxOrVersion = target._parent.GetEndIndex(_target._idx, includeEndElement: false);
		}

		public ArrayEnumerator GetEnumerator()
		{
			ArrayEnumerator result = this;
			result._curIdx = -1;
			return result;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator<JsonElement> IEnumerable<JsonElement>.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Dispose()
		{
			_curIdx = _endIdxOrVersion;
		}

		public void Reset()
		{
			_curIdx = -1;
		}

		public bool MoveNext()
		{
			if (_curIdx >= _endIdxOrVersion)
			{
				return false;
			}
			if (_curIdx < 0)
			{
				_curIdx = _target._idx + 12;
			}
			else
			{
				_curIdx = _target._parent.GetEndIndex(_curIdx, includeEndElement: true);
			}
			return _curIdx < _endIdxOrVersion;
		}
	}

	[DebuggerDisplay("{Current,nq}")]
	public struct ObjectEnumerator : IEnumerable<JsonProperty>, IEnumerable, IEnumerator<JsonProperty>, IEnumerator, IDisposable
	{
		private readonly JsonElement _target;

		private int _curIdx;

		private readonly int _endIdxOrVersion;

		public JsonProperty Current
		{
			get
			{
				if (_curIdx < 0)
				{
					return default(JsonProperty);
				}
				return new JsonProperty(new JsonElement(_target._parent, _curIdx));
			}
		}

		object IEnumerator.Current => Current;

		internal ObjectEnumerator(JsonElement target)
		{
			_target = target;
			_curIdx = -1;
			_endIdxOrVersion = target._parent.GetEndIndex(_target._idx, includeEndElement: false);
		}

		public ObjectEnumerator GetEnumerator()
		{
			ObjectEnumerator result = this;
			result._curIdx = -1;
			return result;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator<JsonProperty> IEnumerable<JsonProperty>.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Dispose()
		{
			_curIdx = _endIdxOrVersion;
		}

		public void Reset()
		{
			_curIdx = -1;
		}

		public bool MoveNext()
		{
			if (_curIdx >= _endIdxOrVersion)
			{
				return false;
			}
			if (_curIdx < 0)
			{
				_curIdx = _target._idx + 12;
			}
			else
			{
				_curIdx = _target._parent.GetEndIndex(_curIdx, includeEndElement: true);
			}
			_curIdx += 12;
			return _curIdx < _endIdxOrVersion;
		}
	}

	private readonly JsonDocument _parent;

	private readonly int _idx;

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private JsonTokenType TokenType => _parent?.GetJsonTokenType(_idx) ?? JsonTokenType.None;

	public JsonValueKind ValueKind => TokenType.ToValueKind();

	public JsonElement this[int index]
	{
		get
		{
			CheckValidInstance();
			return _parent.GetArrayIndexElement(_idx, index);
		}
	}

	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private string DebuggerDisplay => $"ValueKind = {ValueKind} : \"{ToString()}\"";

	internal JsonElement(JsonDocument parent, int idx)
	{
		_parent = parent;
		_idx = idx;
	}

	public int GetArrayLength()
	{
		CheckValidInstance();
		return _parent.GetArrayLength(_idx);
	}

	public JsonElement GetProperty(string propertyName)
	{
		if (propertyName == null)
		{
			throw new ArgumentNullException("propertyName");
		}
		if (TryGetProperty(propertyName, out var value))
		{
			return value;
		}
		throw new KeyNotFoundException();
	}

	public JsonElement GetProperty(ReadOnlySpan<char> propertyName)
	{
		if (TryGetProperty(propertyName, out var value))
		{
			return value;
		}
		throw new KeyNotFoundException();
	}

	public JsonElement GetProperty(ReadOnlySpan<byte> utf8PropertyName)
	{
		if (TryGetProperty(utf8PropertyName, out var value))
		{
			return value;
		}
		throw new KeyNotFoundException();
	}

	public bool TryGetProperty(string propertyName, out JsonElement value)
	{
		if (propertyName == null)
		{
			throw new ArgumentNullException("propertyName");
		}
		return TryGetProperty(propertyName.AsSpan(), out value);
	}

	public bool TryGetProperty(ReadOnlySpan<char> propertyName, out JsonElement value)
	{
		CheckValidInstance();
		return _parent.TryGetNamedPropertyValue(_idx, propertyName, out value);
	}

	public bool TryGetProperty(ReadOnlySpan<byte> utf8PropertyName, out JsonElement value)
	{
		CheckValidInstance();
		return _parent.TryGetNamedPropertyValue(_idx, utf8PropertyName, out value);
	}

	public bool GetBoolean()
	{
		JsonTokenType tokenType = TokenType;
		return tokenType switch
		{
			JsonTokenType.False => false, 
			JsonTokenType.True => true, 
			_ => throw ThrowHelper.GetJsonElementWrongTypeException("Boolean", tokenType), 
		};
	}

	public string? GetString()
	{
		CheckValidInstance();
		return _parent.GetString(_idx, JsonTokenType.String);
	}

	public bool TryGetBytesFromBase64([NotNullWhen(true)] out byte[]? value)
	{
		CheckValidInstance();
		return _parent.TryGetValue(_idx, out value);
	}

	public byte[] GetBytesFromBase64()
	{
		if (TryGetBytesFromBase64(out byte[] value))
		{
			return value;
		}
		throw ThrowHelper.GetFormatException();
	}

	[CLSCompliant(false)]
	public bool TryGetSByte(out sbyte value)
	{
		CheckValidInstance();
		return _parent.TryGetValue(_idx, out value);
	}

	[CLSCompliant(false)]
	public sbyte GetSByte()
	{
		if (TryGetSByte(out var value))
		{
			return value;
		}
		throw new FormatException();
	}

	public bool TryGetByte(out byte value)
	{
		CheckValidInstance();
		return _parent.TryGetValue(_idx, out value);
	}

	public byte GetByte()
	{
		if (TryGetByte(out var value))
		{
			return value;
		}
		throw new FormatException();
	}

	public bool TryGetInt16(out short value)
	{
		CheckValidInstance();
		return _parent.TryGetValue(_idx, out value);
	}

	public short GetInt16()
	{
		if (TryGetInt16(out var value))
		{
			return value;
		}
		throw new FormatException();
	}

	[CLSCompliant(false)]
	public bool TryGetUInt16(out ushort value)
	{
		CheckValidInstance();
		return _parent.TryGetValue(_idx, out value);
	}

	[CLSCompliant(false)]
	public ushort GetUInt16()
	{
		if (TryGetUInt16(out var value))
		{
			return value;
		}
		throw new FormatException();
	}

	public bool TryGetInt32(out int value)
	{
		CheckValidInstance();
		return _parent.TryGetValue(_idx, out value);
	}

	public int GetInt32()
	{
		if (TryGetInt32(out var value))
		{
			return value;
		}
		throw ThrowHelper.GetFormatException();
	}

	[CLSCompliant(false)]
	public bool TryGetUInt32(out uint value)
	{
		CheckValidInstance();
		return _parent.TryGetValue(_idx, out value);
	}

	[CLSCompliant(false)]
	public uint GetUInt32()
	{
		if (TryGetUInt32(out var value))
		{
			return value;
		}
		throw ThrowHelper.GetFormatException();
	}

	public bool TryGetInt64(out long value)
	{
		CheckValidInstance();
		return _parent.TryGetValue(_idx, out value);
	}

	public long GetInt64()
	{
		if (TryGetInt64(out var value))
		{
			return value;
		}
		throw ThrowHelper.GetFormatException();
	}

	[CLSCompliant(false)]
	public bool TryGetUInt64(out ulong value)
	{
		CheckValidInstance();
		return _parent.TryGetValue(_idx, out value);
	}

	[CLSCompliant(false)]
	public ulong GetUInt64()
	{
		if (TryGetUInt64(out var value))
		{
			return value;
		}
		throw ThrowHelper.GetFormatException();
	}

	public bool TryGetDouble(out double value)
	{
		CheckValidInstance();
		return _parent.TryGetValue(_idx, out value);
	}

	public double GetDouble()
	{
		if (TryGetDouble(out var value))
		{
			return value;
		}
		throw ThrowHelper.GetFormatException();
	}

	public bool TryGetSingle(out float value)
	{
		CheckValidInstance();
		return _parent.TryGetValue(_idx, out value);
	}

	public float GetSingle()
	{
		if (TryGetSingle(out var value))
		{
			return value;
		}
		throw ThrowHelper.GetFormatException();
	}

	public bool TryGetDecimal(out decimal value)
	{
		CheckValidInstance();
		return _parent.TryGetValue(_idx, out value);
	}

	public decimal GetDecimal()
	{
		if (TryGetDecimal(out var value))
		{
			return value;
		}
		throw ThrowHelper.GetFormatException();
	}

	public bool TryGetDateTime(out DateTime value)
	{
		CheckValidInstance();
		return _parent.TryGetValue(_idx, out value);
	}

	public DateTime GetDateTime()
	{
		if (TryGetDateTime(out var value))
		{
			return value;
		}
		throw ThrowHelper.GetFormatException();
	}

	public bool TryGetDateTimeOffset(out DateTimeOffset value)
	{
		CheckValidInstance();
		return _parent.TryGetValue(_idx, out value);
	}

	public DateTimeOffset GetDateTimeOffset()
	{
		if (TryGetDateTimeOffset(out var value))
		{
			return value;
		}
		throw ThrowHelper.GetFormatException();
	}

	public bool TryGetGuid(out Guid value)
	{
		CheckValidInstance();
		return _parent.TryGetValue(_idx, out value);
	}

	public Guid GetGuid()
	{
		if (TryGetGuid(out var value))
		{
			return value;
		}
		throw ThrowHelper.GetFormatException();
	}

	internal string GetPropertyName()
	{
		CheckValidInstance();
		return _parent.GetNameOfPropertyValue(_idx);
	}

	public string GetRawText()
	{
		CheckValidInstance();
		return _parent.GetRawValueAsString(_idx);
	}

	internal ReadOnlyMemory<byte> GetRawValue()
	{
		CheckValidInstance();
		return _parent.GetRawValue(_idx, includeQuotes: true);
	}

	internal string GetPropertyRawText()
	{
		CheckValidInstance();
		return _parent.GetPropertyRawValueAsString(_idx);
	}

	public bool ValueEquals(string? text)
	{
		if (TokenType == JsonTokenType.Null)
		{
			return text == null;
		}
		return TextEqualsHelper(text.AsSpan(), isPropertyName: false);
	}

	public bool ValueEquals(ReadOnlySpan<byte> utf8Text)
	{
		if (TokenType == JsonTokenType.Null)
		{
			return utf8Text == default(ReadOnlySpan<byte>);
		}
		return TextEqualsHelper(utf8Text, isPropertyName: false, shouldUnescape: true);
	}

	public bool ValueEquals(ReadOnlySpan<char> text)
	{
		if (TokenType == JsonTokenType.Null)
		{
			return text == default(ReadOnlySpan<char>);
		}
		return TextEqualsHelper(text, isPropertyName: false);
	}

	internal bool TextEqualsHelper(ReadOnlySpan<byte> utf8Text, bool isPropertyName, bool shouldUnescape)
	{
		CheckValidInstance();
		return _parent.TextEquals(_idx, utf8Text, isPropertyName, shouldUnescape);
	}

	internal bool TextEqualsHelper(ReadOnlySpan<char> text, bool isPropertyName)
	{
		CheckValidInstance();
		return _parent.TextEquals(_idx, text, isPropertyName);
	}

	public void WriteTo(Utf8JsonWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		CheckValidInstance();
		_parent.WriteElementTo(_idx, writer);
	}

	public ArrayEnumerator EnumerateArray()
	{
		CheckValidInstance();
		JsonTokenType tokenType = TokenType;
		if (tokenType != JsonTokenType.StartArray)
		{
			throw ThrowHelper.GetJsonElementWrongTypeException(JsonTokenType.StartArray, tokenType);
		}
		return new ArrayEnumerator(this);
	}

	public ObjectEnumerator EnumerateObject()
	{
		CheckValidInstance();
		JsonTokenType tokenType = TokenType;
		if (tokenType != JsonTokenType.StartObject)
		{
			throw ThrowHelper.GetJsonElementWrongTypeException(JsonTokenType.StartObject, tokenType);
		}
		return new ObjectEnumerator(this);
	}

	public override string ToString()
	{
		switch (TokenType)
		{
		case JsonTokenType.None:
		case JsonTokenType.Null:
			return string.Empty;
		case JsonTokenType.True:
			return bool.TrueString;
		case JsonTokenType.False:
			return bool.FalseString;
		case JsonTokenType.StartObject:
		case JsonTokenType.StartArray:
		case JsonTokenType.Number:
			return _parent.GetRawValueAsString(_idx);
		case JsonTokenType.String:
			return GetString();
		default:
			return string.Empty;
		}
	}

	public JsonElement Clone()
	{
		CheckValidInstance();
		if (!_parent.IsDisposable)
		{
			return this;
		}
		return _parent.CloneElement(_idx);
	}

	private void CheckValidInstance()
	{
		if (_parent == null)
		{
			throw new InvalidOperationException();
		}
	}

	public static JsonElement ParseValue(ref Utf8JsonReader reader)
	{
		JsonDocument document;
		bool flag = JsonDocument.TryParseValue(ref reader, out document, shouldThrow: true, useArrayPools: false);
		return document.RootElement;
	}

	internal static JsonElement ParseValue(Stream utf8Json, JsonDocumentOptions options)
	{
		JsonDocument jsonDocument = JsonDocument.ParseValue(utf8Json, options);
		return jsonDocument.RootElement;
	}

	internal static JsonElement ParseValue(ReadOnlySpan<byte> utf8Json, JsonDocumentOptions options)
	{
		JsonDocument jsonDocument = JsonDocument.ParseValue(utf8Json, options);
		return jsonDocument.RootElement;
	}

	internal static JsonElement ParseValue(string json, JsonDocumentOptions options)
	{
		JsonDocument jsonDocument = JsonDocument.ParseValue(json, options);
		return jsonDocument.RootElement;
	}

	public static bool TryParseValue(ref Utf8JsonReader reader, [NotNullWhen(true)] out JsonElement? element)
	{
		JsonDocument document;
		bool result = JsonDocument.TryParseValue(ref reader, out document, shouldThrow: false, useArrayPools: false);
		element = document?.RootElement;
		return result;
	}
}
