using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal sealed class JsonWriterDelegator : XmlWriterDelegator
{
	private readonly DateTimeFormat _dateTimeFormat;

	public JsonWriterDelegator(XmlWriter writer)
		: base(writer)
	{
	}

	public JsonWriterDelegator(XmlWriter writer, DateTimeFormat dateTimeFormat)
		: this(writer)
	{
		_dateTimeFormat = dateTimeFormat;
	}

	internal override void WriteChar(char value)
	{
		WriteString(XmlConvert.ToString(value));
	}

	internal override void WriteBase64(byte[] bytes)
	{
		if (bytes != null)
		{
			ByteArrayHelperWithString.Instance.WriteArray(base.Writer, bytes, 0, bytes.Length);
		}
	}

	internal override void WriteQName(XmlQualifiedName value)
	{
		if (value != XmlQualifiedName.Empty)
		{
			writer.WriteString(value.Name);
			writer.WriteString(":");
			writer.WriteString(value.Namespace);
		}
	}

	internal override void WriteUnsignedLong(ulong value)
	{
		WriteDecimal(value);
	}

	internal override void WriteDecimal(decimal value)
	{
		writer.WriteAttributeString("type", "number");
		base.WriteDecimal(value);
	}

	internal override void WriteDouble(double value)
	{
		writer.WriteAttributeString("type", "number");
		base.WriteDouble(value);
	}

	internal override void WriteFloat(float value)
	{
		writer.WriteAttributeString("type", "number");
		base.WriteFloat(value);
	}

	internal override void WriteLong(long value)
	{
		writer.WriteAttributeString("type", "number");
		base.WriteLong(value);
	}

	internal override void WriteSignedByte(sbyte value)
	{
		writer.WriteAttributeString("type", "number");
		base.WriteSignedByte(value);
	}

	internal override void WriteUnsignedInt(uint value)
	{
		writer.WriteAttributeString("type", "number");
		base.WriteUnsignedInt(value);
	}

	internal override void WriteUnsignedShort(ushort value)
	{
		writer.WriteAttributeString("type", "number");
		base.WriteUnsignedShort(value);
	}

	internal override void WriteUnsignedByte(byte value)
	{
		writer.WriteAttributeString("type", "number");
		base.WriteUnsignedByte(value);
	}

	internal override void WriteShort(short value)
	{
		writer.WriteAttributeString("type", "number");
		base.WriteShort(value);
	}

	internal override void WriteBoolean(bool value)
	{
		writer.WriteAttributeString("type", "boolean");
		base.WriteBoolean(value);
	}

	internal override void WriteInt(int value)
	{
		writer.WriteAttributeString("type", "number");
		base.WriteInt(value);
	}

	internal void WriteJsonBooleanArray(bool[] value, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
	{
		for (int i = 0; i < value.Length; i++)
		{
			WriteBoolean(value[i], itemName, itemNamespace);
		}
	}

	internal void WriteJsonDateTimeArray(DateTime[] value, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
	{
		for (int i = 0; i < value.Length; i++)
		{
			WriteDateTime(value[i], itemName, itemNamespace);
		}
	}

	internal void WriteJsonDecimalArray(decimal[] value, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
	{
		for (int i = 0; i < value.Length; i++)
		{
			WriteDecimal(value[i], itemName, itemNamespace);
		}
	}

	internal void WriteJsonInt32Array(int[] value, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
	{
		for (int i = 0; i < value.Length; i++)
		{
			WriteInt(value[i], itemName, itemNamespace);
		}
	}

	internal void WriteJsonInt64Array(long[] value, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
	{
		for (int i = 0; i < value.Length; i++)
		{
			WriteLong(value[i], itemName, itemNamespace);
		}
	}

	internal override void WriteDateTime(DateTime value)
	{
		if (_dateTimeFormat == null)
		{
			WriteDateTimeInDefaultFormat(value);
		}
		else
		{
			writer.WriteString(value.ToString(_dateTimeFormat.FormatString, _dateTimeFormat.FormatProvider));
		}
	}

	private void WriteDateTimeInDefaultFormat(DateTime value)
	{
		if (value.Kind != DateTimeKind.Utc)
		{
			long num = 864000000000L;
			long num2 = 3155378111999999999L;
			long ticks = value.Ticks;
			if (num > ticks || num2 < ticks)
			{
				ticks -= TimeZoneInfo.Local.GetUtcOffset(value).Ticks;
				if (ticks > DateTime.MaxValue.Ticks || ticks < DateTime.MinValue.Ticks)
				{
					throw XmlObjectSerializer.CreateSerializationException(System.SR.JsonDateTimeOutOfRange, new ArgumentOutOfRangeException("value"));
				}
			}
		}
		writer.WriteString("/Date(");
		writer.WriteValue((value.ToUniversalTime().Ticks - JsonGlobals.unixEpochTicks) / 10000);
		switch (value.Kind)
		{
		case DateTimeKind.Unspecified:
		case DateTimeKind.Local:
		{
			TimeSpan utcOffset = TimeZoneInfo.Local.GetUtcOffset(value.ToLocalTime());
			XmlWriter xmlWriter = writer;
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(0, 2, invariantCulture);
			handler.AppendFormatted(utcOffset.Hours, "+00;-00");
			handler.AppendFormatted(utcOffset.Minutes, "00;00");
			xmlWriter.WriteString(string.Create(invariantCulture, ref handler));
			break;
		}
		}
		writer.WriteString(")/");
	}

	internal void WriteJsonSingleArray(float[] value, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
	{
		for (int i = 0; i < value.Length; i++)
		{
			WriteFloat(value[i], itemName, itemNamespace);
		}
	}

	internal void WriteJsonDoubleArray(double[] value, XmlDictionaryString itemName, XmlDictionaryString itemNamespace)
	{
		for (int i = 0; i < value.Length; i++)
		{
			WriteDouble(value[i], itemName, itemNamespace);
		}
	}

	internal override void WriteStartElement(string prefix, string localName, string ns)
	{
		if (localName != null && localName.Length == 0)
		{
			WriteStartElement("item", "item");
			WriteAttributeString(null, "item", null, localName);
		}
		else
		{
			base.WriteStartElement(prefix, localName, ns);
		}
	}
}
