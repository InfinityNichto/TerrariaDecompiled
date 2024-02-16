using System.Threading.Tasks;

namespace System.Xml;

internal sealed class XmlRawWriterBase64Encoder : Base64Encoder
{
	private readonly XmlRawWriter _rawWriter;

	internal XmlRawWriterBase64Encoder(XmlRawWriter rawWriter)
	{
		_rawWriter = rawWriter;
	}

	internal override void WriteChars(char[] chars, int index, int count)
	{
		_rawWriter.WriteRaw(chars, index, count);
	}

	internal override Task WriteCharsAsync(char[] chars, int index, int count)
	{
		return _rawWriter.WriteRawAsync(chars, index, count);
	}
}
