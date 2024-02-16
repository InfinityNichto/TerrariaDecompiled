using System.Threading.Tasks;

namespace System.Xml;

internal sealed class XmlTextWriterBase64Encoder : Base64Encoder
{
	private readonly XmlTextEncoder _xmlTextEncoder;

	internal XmlTextWriterBase64Encoder(XmlTextEncoder xmlTextEncoder)
	{
		_xmlTextEncoder = xmlTextEncoder;
	}

	internal override void WriteChars(char[] chars, int index, int count)
	{
		_xmlTextEncoder.WriteRaw(chars, index, count);
	}

	internal override Task WriteCharsAsync(char[] chars, int index, int count)
	{
		throw new NotImplementedException();
	}
}
