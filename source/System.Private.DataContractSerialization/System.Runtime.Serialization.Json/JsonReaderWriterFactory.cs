using System.IO;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization.Json;

public static class JsonReaderWriterFactory
{
	private const string DefaultIndentChars = "  ";

	public static XmlDictionaryReader CreateJsonReader(Stream stream, XmlDictionaryReaderQuotas quotas)
	{
		return CreateJsonReader(stream, null, quotas, null);
	}

	public static XmlDictionaryReader CreateJsonReader(byte[] buffer, XmlDictionaryReaderQuotas quotas)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		return CreateJsonReader(buffer, 0, buffer.Length, null, quotas, null);
	}

	public static XmlDictionaryReader CreateJsonReader(Stream stream, Encoding? encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose? onClose)
	{
		XmlJsonReader xmlJsonReader = new XmlJsonReader();
		xmlJsonReader.SetInput(stream, encoding, quotas, onClose);
		return xmlJsonReader;
	}

	public static XmlDictionaryReader CreateJsonReader(byte[] buffer, int offset, int count, XmlDictionaryReaderQuotas quotas)
	{
		return CreateJsonReader(buffer, offset, count, null, quotas, null);
	}

	public static XmlDictionaryReader CreateJsonReader(byte[] buffer, int offset, int count, Encoding? encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose? onClose)
	{
		XmlJsonReader xmlJsonReader = new XmlJsonReader();
		xmlJsonReader.SetInput(buffer, offset, count, encoding, quotas, onClose);
		return xmlJsonReader;
	}

	public static XmlDictionaryWriter CreateJsonWriter(Stream stream)
	{
		return CreateJsonWriter(stream, Encoding.UTF8, ownsStream: true);
	}

	public static XmlDictionaryWriter CreateJsonWriter(Stream stream, Encoding encoding)
	{
		return CreateJsonWriter(stream, encoding, ownsStream: true);
	}

	public static XmlDictionaryWriter CreateJsonWriter(Stream stream, Encoding encoding, bool ownsStream)
	{
		return CreateJsonWriter(stream, encoding, ownsStream, indent: false);
	}

	public static XmlDictionaryWriter CreateJsonWriter(Stream stream, Encoding encoding, bool ownsStream, bool indent)
	{
		return CreateJsonWriter(stream, encoding, ownsStream, indent, "  ");
	}

	public static XmlDictionaryWriter CreateJsonWriter(Stream stream, Encoding encoding, bool ownsStream, bool indent, string? indentChars)
	{
		XmlJsonWriter xmlJsonWriter = new XmlJsonWriter(indent, indentChars);
		xmlJsonWriter.SetOutput(stream, encoding, ownsStream);
		return xmlJsonWriter;
	}
}
