using System.IO;
using System.Text;

namespace System.Xml;

internal sealed class XmlUTF8TextWriter : XmlBaseWriter, IXmlTextWriterInitializer
{
	private XmlUTF8NodeWriter _writer;

	public void SetOutput(Stream stream, Encoding encoding, bool ownsStream)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (encoding.WebName != Encoding.UTF8.WebName)
		{
			stream = new EncodingStreamWrapper(stream, encoding, emitBOM: true);
		}
		if (_writer == null)
		{
			_writer = new XmlUTF8NodeWriter();
		}
		_writer.SetOutput(stream, ownsStream, encoding);
		SetOutput(_writer);
	}

	protected override XmlSigningNodeWriter CreateSigningNodeWriter()
	{
		return new XmlSigningNodeWriter(text: true);
	}
}
