using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal sealed class JsonReaderDelegator : XmlReaderDelegator
{
	private sealed class DateTimeArrayJsonHelperWithString : ArrayHelper<string, DateTime>
	{
		private readonly DateTimeFormat _dateTimeFormat;

		public DateTimeArrayJsonHelperWithString(DateTimeFormat dateTimeFormat)
		{
			_dateTimeFormat = dateTimeFormat;
		}

		protected override int ReadArray(XmlDictionaryReader reader, string localName, string namespaceUri, DateTime[] array, int offset, int count)
		{
			XmlJsonReader.CheckArray(array, offset, count);
			int i;
			for (i = 0; i < count; i++)
			{
				if (!reader.IsStartElement("item", string.Empty))
				{
					break;
				}
				array[offset + i] = ParseJsonDate(reader.ReadElementContentAsString(), _dateTimeFormat);
			}
			return i;
		}

		protected override void WriteArray(XmlDictionaryWriter writer, string prefix, string localName, string namespaceUri, DateTime[] array, int offset, int count)
		{
			throw System.NotImplemented.ByDesign;
		}
	}

	private readonly DateTimeFormat _dateTimeFormat;

	private DateTimeArrayJsonHelperWithString _dateTimeArrayHelper;

	internal XmlDictionaryReaderQuotas ReaderQuotas
	{
		get
		{
			if (dictionaryReader == null)
			{
				return null;
			}
			return dictionaryReader.Quotas;
		}
	}

	private DateTimeArrayJsonHelperWithString DateTimeArrayHelper
	{
		get
		{
			if (_dateTimeArrayHelper == null)
			{
				_dateTimeArrayHelper = new DateTimeArrayJsonHelperWithString(_dateTimeFormat);
			}
			return _dateTimeArrayHelper;
		}
	}

	public JsonReaderDelegator(XmlReader reader)
		: base(reader)
	{
	}

	public JsonReaderDelegator(XmlReader reader, DateTimeFormat dateTimeFormat)
		: this(reader)
	{
		_dateTimeFormat = dateTimeFormat;
	}

	internal static XmlQualifiedName ParseQualifiedName(string qname)
	{
		string name;
		string ns;
		if (string.IsNullOrEmpty(qname))
		{
			name = (ns = string.Empty);
		}
		else
		{
			qname = qname.Trim();
			int num = qname.IndexOf(':');
			if (num >= 0)
			{
				name = qname.Substring(0, num);
				ns = qname.Substring(num + 1);
			}
			else
			{
				name = qname;
				ns = string.Empty;
			}
		}
		return new XmlQualifiedName(name, ns);
	}

	internal override char ReadContentAsChar()
	{
		return XmlConvert.ToChar(ReadContentAsString());
	}

	internal override XmlQualifiedName ReadContentAsQName()
	{
		return ParseQualifiedName(ReadContentAsString());
	}

	internal override char ReadElementContentAsChar()
	{
		return XmlConvert.ToChar(ReadElementContentAsString());
	}

	public override byte[] ReadContentAsBase64()
	{
		if (isEndOfEmptyElement)
		{
			return Array.Empty<byte>();
		}
		if (dictionaryReader == null)
		{
			XmlDictionaryReader xmlDictionaryReader = XmlDictionaryReader.CreateDictionaryReader(reader);
			return ByteArrayHelperWithString.Instance.ReadArray(xmlDictionaryReader, "item", string.Empty, xmlDictionaryReader.Quotas.MaxArrayLength);
		}
		return ByteArrayHelperWithString.Instance.ReadArray(dictionaryReader, "item", string.Empty, dictionaryReader.Quotas.MaxArrayLength);
	}

	internal override byte[] ReadElementContentAsBase64()
	{
		if (isEndOfEmptyElement)
		{
			throw new XmlException(System.SR.Format(System.SR.XmlStartElementExpected, "EndElement"));
		}
		byte[] result;
		if (reader.IsStartElement() && reader.IsEmptyElement)
		{
			reader.Read();
			result = Array.Empty<byte>();
		}
		else
		{
			reader.ReadStartElement();
			result = ReadContentAsBase64();
			reader.ReadEndElement();
		}
		return result;
	}

	internal override DateTime ReadContentAsDateTime()
	{
		return ParseJsonDate(ReadContentAsString(), _dateTimeFormat);
	}

	internal static DateTime ParseJsonDate(string originalDateTimeValue, DateTimeFormat dateTimeFormat)
	{
		if (dateTimeFormat == null)
		{
			return ParseJsonDateInDefaultFormat(originalDateTimeValue);
		}
		return DateTime.ParseExact(originalDateTimeValue, dateTimeFormat.FormatString, dateTimeFormat.FormatProvider, dateTimeFormat.DateTimeStyles);
	}

	internal static DateTime ParseJsonDateInDefaultFormat(string originalDateTimeValue)
	{
		string text = (string.IsNullOrEmpty(originalDateTimeValue) ? originalDateTimeValue : originalDateTimeValue.Trim());
		if (string.IsNullOrEmpty(text) || !text.StartsWith("/Date(", StringComparison.Ordinal) || !text.EndsWith(")/", StringComparison.Ordinal))
		{
			throw new FormatException(System.SR.Format(System.SR.JsonInvalidDateTimeString, originalDateTimeValue, "\\/Date(", ")\\/"));
		}
		string text2 = text.Substring(6, text.Length - 8);
		DateTimeKind dateTimeKind = DateTimeKind.Utc;
		int num = text2.IndexOf('+', 1);
		if (num == -1)
		{
			num = text2.IndexOf('-', 1);
		}
		if (num != -1)
		{
			dateTimeKind = DateTimeKind.Local;
			text2 = text2.Substring(0, num);
		}
		long num2;
		try
		{
			num2 = long.Parse(text2, CultureInfo.InvariantCulture);
		}
		catch (ArgumentException exception)
		{
			throw XmlExceptionHelper.CreateConversionException(text2, "Int64", exception);
		}
		catch (FormatException exception2)
		{
			throw XmlExceptionHelper.CreateConversionException(text2, "Int64", exception2);
		}
		catch (OverflowException exception3)
		{
			throw XmlExceptionHelper.CreateConversionException(text2, "Int64", exception3);
		}
		long ticks = num2 * 10000 + JsonGlobals.unixEpochTicks;
		try
		{
			DateTime dateTime = new DateTime(ticks, DateTimeKind.Utc);
			return dateTimeKind switch
			{
				DateTimeKind.Local => dateTime.ToLocalTime(), 
				DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime.ToLocalTime(), DateTimeKind.Unspecified), 
				_ => dateTime, 
			};
		}
		catch (ArgumentException exception4)
		{
			throw XmlExceptionHelper.CreateConversionException(text2, "DateTime", exception4);
		}
	}

	internal override DateTime ReadElementContentAsDateTime()
	{
		return ParseJsonDate(ReadElementContentAsString(), _dateTimeFormat);
	}

	internal override bool TryReadDateTimeArray(XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNamespace, int arrayLength, [NotNullWhen(true)] out DateTime[] array)
	{
		return TryReadJsonDateTimeArray(context, itemName, itemNamespace, arrayLength, out array);
	}

	internal bool TryReadJsonDateTimeArray(XmlObjectSerializerReadContext context, XmlDictionaryString itemName, XmlDictionaryString itemNamespace, int arrayLength, [NotNullWhen(true)] out DateTime[] array)
	{
		if (dictionaryReader == null || arrayLength != -1)
		{
			array = null;
			return false;
		}
		array = DateTimeArrayHelper.ReadArray(dictionaryReader, XmlDictionaryString.GetString(itemName), XmlDictionaryString.GetString(itemNamespace), GetArrayLengthQuota(context));
		context.IncrementItemCount(array.Length);
		return true;
	}

	internal override ulong ReadContentAsUnsignedLong()
	{
		string text = reader.ReadContentAsString();
		if (text == null || text.Length == 0)
		{
			throw new XmlException(XmlObjectSerializer.TryAddLineInfo(this, System.SR.Format(System.SR.XmlInvalidConversion, text, "UInt64")));
		}
		try
		{
			return ulong.Parse(text, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
		}
		catch (ArgumentException exception)
		{
			throw XmlExceptionHelper.CreateConversionException(text, "UInt64", exception);
		}
		catch (FormatException exception2)
		{
			throw XmlExceptionHelper.CreateConversionException(text, "UInt64", exception2);
		}
		catch (OverflowException exception3)
		{
			throw XmlExceptionHelper.CreateConversionException(text, "UInt64", exception3);
		}
	}

	internal override ulong ReadElementContentAsUnsignedLong()
	{
		if (isEndOfEmptyElement)
		{
			throw new XmlException(System.SR.Format(System.SR.XmlStartElementExpected, "EndElement"));
		}
		string text = reader.ReadElementContentAsString();
		if (text == null || text.Length == 0)
		{
			throw new XmlException(XmlObjectSerializer.TryAddLineInfo(this, System.SR.Format(System.SR.XmlInvalidConversion, text, "UInt64")));
		}
		try
		{
			return ulong.Parse(text, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
		}
		catch (ArgumentException exception)
		{
			throw XmlExceptionHelper.CreateConversionException(text, "UInt64", exception);
		}
		catch (FormatException exception2)
		{
			throw XmlExceptionHelper.CreateConversionException(text, "UInt64", exception2);
		}
		catch (OverflowException exception3)
		{
			throw XmlExceptionHelper.CreateConversionException(text, "UInt64", exception3);
		}
	}
}
