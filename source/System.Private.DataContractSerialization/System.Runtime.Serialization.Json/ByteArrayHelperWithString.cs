using System.Globalization;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal sealed class ByteArrayHelperWithString : ArrayHelper<string, byte>
{
	public static readonly ByteArrayHelperWithString Instance = new ByteArrayHelperWithString();

	internal void WriteArray(XmlWriter writer, byte[] array, int offset, int count)
	{
		XmlJsonReader.CheckArray(array, offset, count);
		writer.WriteAttributeString(string.Empty, "type", string.Empty, "array");
		for (int i = 0; i < count; i++)
		{
			writer.WriteStartElement("item", string.Empty);
			writer.WriteAttributeString(string.Empty, "type", string.Empty, "number");
			writer.WriteValue(array[offset + i]);
			writer.WriteEndElement();
		}
	}

	protected override int ReadArray(XmlDictionaryReader reader, string localName, string namespaceUri, byte[] array, int offset, int count)
	{
		XmlJsonReader.CheckArray(array, offset, count);
		int i;
		for (i = 0; i < count; i++)
		{
			if (!reader.IsStartElement("item", string.Empty))
			{
				break;
			}
			array[offset + i] = ToByte(reader.ReadElementContentAsInt());
		}
		return i;
	}

	protected override void WriteArray(XmlDictionaryWriter writer, string prefix, string localName, string namespaceUri, byte[] array, int offset, int count)
	{
		WriteArray(writer, array, offset, count);
	}

	private void ThrowConversionException(string value, string type)
	{
		throw new XmlException(System.SR.Format(System.SR.XmlInvalidConversion, value, type));
	}

	private byte ToByte(int value)
	{
		if (value < 0 || value > 255)
		{
			ThrowConversionException(value.ToString(NumberFormatInfo.CurrentInfo), "Byte");
		}
		return (byte)value;
	}
}
