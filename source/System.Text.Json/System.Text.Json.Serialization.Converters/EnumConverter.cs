using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;

namespace System.Text.Json.Serialization.Converters;

internal sealed class EnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
	private static readonly TypeCode s_enumTypeCode = Type.GetTypeCode(typeof(T));

	private static readonly string s_negativeSign = (((int)s_enumTypeCode % 2 == 0) ? null : NumberFormatInfo.CurrentInfo.NegativeSign);

	private readonly EnumConverterOptions _converterOptions;

	private readonly JsonNamingPolicy _namingPolicy;

	private readonly ConcurrentDictionary<ulong, JsonEncodedText> _nameCache;

	private ConcurrentDictionary<ulong, JsonEncodedText> _dictionaryKeyPolicyCache;

	public override bool CanConvert(Type type)
	{
		return type.IsEnum;
	}

	public EnumConverter(EnumConverterOptions converterOptions, JsonSerializerOptions serializerOptions)
		: this(converterOptions, (JsonNamingPolicy)null, serializerOptions)
	{
	}

	public EnumConverter(EnumConverterOptions converterOptions, JsonNamingPolicy namingPolicy, JsonSerializerOptions serializerOptions)
	{
		_converterOptions = converterOptions;
		_namingPolicy = namingPolicy;
		_nameCache = new ConcurrentDictionary<ulong, JsonEncodedText>();
		string[] names = Enum.GetNames(TypeToConvert);
		Array values = Enum.GetValues(TypeToConvert);
		JavaScriptEncoder encoder = serializerOptions.Encoder;
		for (int i = 0; i < names.Length; i++)
		{
			if (_nameCache.Count >= 64)
			{
				break;
			}
			T val = (T)values.GetValue(i);
			ulong key = ConvertToUInt64(val);
			string value = names[i];
			_nameCache.TryAdd(key, (namingPolicy == null) ? JsonEncodedText.Encode(value, encoder) : FormatEnumValue(value, encoder));
		}
	}

	public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
		case JsonTokenType.String:
			if (!_converterOptions.HasFlag(EnumConverterOptions.AllowStrings))
			{
				ThrowHelper.ThrowJsonException();
				return default(T);
			}
			return ReadAsPropertyNameCore(ref reader, typeToConvert, options);
		case JsonTokenType.Number:
			if (_converterOptions.HasFlag(EnumConverterOptions.AllowNumbers))
			{
				switch (s_enumTypeCode)
				{
				case TypeCode.Int32:
				{
					if (reader.TryGetInt32(out var value8))
					{
						return Unsafe.As<int, T>(ref value8);
					}
					break;
				}
				case TypeCode.UInt32:
				{
					if (reader.TryGetUInt32(out var value4))
					{
						return Unsafe.As<uint, T>(ref value4);
					}
					break;
				}
				case TypeCode.UInt64:
				{
					if (reader.TryGetUInt64(out var value6))
					{
						return Unsafe.As<ulong, T>(ref value6);
					}
					break;
				}
				case TypeCode.Int64:
				{
					if (reader.TryGetInt64(out var value2))
					{
						return Unsafe.As<long, T>(ref value2);
					}
					break;
				}
				case TypeCode.SByte:
				{
					if (reader.TryGetSByte(out var value7))
					{
						return Unsafe.As<sbyte, T>(ref value7);
					}
					break;
				}
				case TypeCode.Byte:
				{
					if (reader.TryGetByte(out var value5))
					{
						return Unsafe.As<byte, T>(ref value5);
					}
					break;
				}
				case TypeCode.Int16:
				{
					if (reader.TryGetInt16(out var value3))
					{
						return Unsafe.As<short, T>(ref value3);
					}
					break;
				}
				case TypeCode.UInt16:
				{
					if (reader.TryGetUInt16(out var value))
					{
						return Unsafe.As<ushort, T>(ref value);
					}
					break;
				}
				}
				ThrowHelper.ThrowJsonException();
				return default(T);
			}
			goto default;
		default:
			ThrowHelper.ThrowJsonException();
			return default(T);
		}
	}

	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		if (_converterOptions.HasFlag(EnumConverterOptions.AllowStrings))
		{
			ulong key = ConvertToUInt64(value);
			if (_nameCache.TryGetValue(key, out var value2))
			{
				writer.WriteStringValue(value2);
				return;
			}
			string text = value.ToString();
			if (IsValidIdentifier(text))
			{
				JavaScriptEncoder encoder = options.Encoder;
				if (_nameCache.Count < 64)
				{
					value2 = ((_namingPolicy == null) ? JsonEncodedText.Encode(text, encoder) : FormatEnumValue(text, encoder));
					writer.WriteStringValue(value2);
					_nameCache.TryAdd(key, value2);
				}
				else
				{
					writer.WriteStringValue((_namingPolicy == null) ? text : FormatEnumValueToString(text, encoder));
				}
				return;
			}
		}
		if (!_converterOptions.HasFlag(EnumConverterOptions.AllowNumbers))
		{
			ThrowHelper.ThrowJsonException();
		}
		switch (s_enumTypeCode)
		{
		case TypeCode.Int32:
			writer.WriteNumberValue(Unsafe.As<T, int>(ref value));
			break;
		case TypeCode.UInt32:
			writer.WriteNumberValue(Unsafe.As<T, uint>(ref value));
			break;
		case TypeCode.UInt64:
			writer.WriteNumberValue(Unsafe.As<T, ulong>(ref value));
			break;
		case TypeCode.Int64:
			writer.WriteNumberValue(Unsafe.As<T, long>(ref value));
			break;
		case TypeCode.Int16:
			writer.WriteNumberValue(Unsafe.As<T, short>(ref value));
			break;
		case TypeCode.UInt16:
			writer.WriteNumberValue(Unsafe.As<T, ushort>(ref value));
			break;
		case TypeCode.Byte:
			writer.WriteNumberValue(Unsafe.As<T, byte>(ref value));
			break;
		case TypeCode.SByte:
			writer.WriteNumberValue(Unsafe.As<T, sbyte>(ref value));
			break;
		default:
			ThrowHelper.ThrowJsonException();
			break;
		}
	}

	private static ulong ConvertToUInt64(object value)
	{
		return s_enumTypeCode switch
		{
			TypeCode.Int32 => (ulong)(int)value, 
			TypeCode.UInt32 => (uint)value, 
			TypeCode.UInt64 => (ulong)value, 
			TypeCode.Int64 => (ulong)(long)value, 
			TypeCode.SByte => (ulong)(sbyte)value, 
			TypeCode.Byte => (byte)value, 
			TypeCode.Int16 => (ulong)(short)value, 
			TypeCode.UInt16 => (ushort)value, 
			_ => throw new InvalidOperationException(), 
		};
	}

	private static bool IsValidIdentifier(string value)
	{
		if (value[0] >= 'A')
		{
			if (s_negativeSign != null)
			{
				return !value.StartsWith(s_negativeSign);
			}
			return true;
		}
		return false;
	}

	private JsonEncodedText FormatEnumValue(string value, JavaScriptEncoder encoder)
	{
		string value2 = FormatEnumValueToString(value, encoder);
		return JsonEncodedText.Encode(value2, encoder);
	}

	private string FormatEnumValueToString(string value, JavaScriptEncoder encoder)
	{
		if (!value.Contains(", "))
		{
			return _namingPolicy.ConvertName(value);
		}
		string[] array = value.Split(", ");
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = _namingPolicy.ConvertName(array[i]);
		}
		return string.Join(", ", array);
	}

	internal override T ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		string @string = reader.GetString();
		if (!Enum.TryParse<T>(@string, out var result) && !Enum.TryParse<T>(@string, ignoreCase: true, out result))
		{
			ThrowHelper.ThrowJsonException();
		}
		return result;
	}

	internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, T value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
	{
		ulong key = ConvertToUInt64(value);
		JsonEncodedText value3;
		if (options.DictionaryKeyPolicy != null)
		{
			if (_dictionaryKeyPolicyCache != null && _dictionaryKeyPolicyCache.TryGetValue(key, out var value2))
			{
				writer.WritePropertyName(value2);
				return;
			}
		}
		else if (_nameCache.TryGetValue(key, out value3))
		{
			writer.WritePropertyName(value3);
			return;
		}
		string text = value.ToString();
		if (IsValidIdentifier(text))
		{
			if (options.DictionaryKeyPolicy != null)
			{
				text = options.DictionaryKeyPolicy.ConvertName(text);
				if (text == null)
				{
					ThrowHelper.ThrowInvalidOperationException_NamingPolicyReturnNull(options.DictionaryKeyPolicy);
				}
				if (_dictionaryKeyPolicyCache == null)
				{
					_dictionaryKeyPolicyCache = new ConcurrentDictionary<ulong, JsonEncodedText>();
				}
				if (_dictionaryKeyPolicyCache.Count < 64)
				{
					JavaScriptEncoder encoder = options.Encoder;
					JsonEncodedText jsonEncodedText = JsonEncodedText.Encode(text, encoder);
					writer.WritePropertyName(jsonEncodedText);
					_dictionaryKeyPolicyCache.TryAdd(key, jsonEncodedText);
				}
				else
				{
					writer.WritePropertyName(text);
				}
			}
			else
			{
				JavaScriptEncoder encoder2 = options.Encoder;
				if (_nameCache.Count < 64)
				{
					JsonEncodedText jsonEncodedText2 = JsonEncodedText.Encode(text, encoder2);
					writer.WritePropertyName(jsonEncodedText2);
					_nameCache.TryAdd(key, jsonEncodedText2);
				}
				else
				{
					writer.WritePropertyName(text);
				}
			}
			return;
		}
		switch (s_enumTypeCode)
		{
		case TypeCode.Int32:
			writer.WritePropertyName(Unsafe.As<T, int>(ref value));
			break;
		case TypeCode.UInt32:
			writer.WritePropertyName(Unsafe.As<T, uint>(ref value));
			break;
		case TypeCode.UInt64:
			writer.WritePropertyName(Unsafe.As<T, ulong>(ref value));
			break;
		case TypeCode.Int64:
			writer.WritePropertyName(Unsafe.As<T, long>(ref value));
			break;
		case TypeCode.Int16:
			writer.WritePropertyName(Unsafe.As<T, short>(ref value));
			break;
		case TypeCode.UInt16:
			writer.WritePropertyName(Unsafe.As<T, ushort>(ref value));
			break;
		case TypeCode.Byte:
			writer.WritePropertyName(Unsafe.As<T, byte>(ref value));
			break;
		case TypeCode.SByte:
			writer.WritePropertyName(Unsafe.As<T, sbyte>(ref value));
			break;
		default:
			ThrowHelper.ThrowJsonException();
			break;
		}
	}
}
