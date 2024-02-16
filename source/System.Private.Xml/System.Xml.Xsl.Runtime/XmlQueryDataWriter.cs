using System.IO;

namespace System.Xml.Xsl.Runtime;

internal sealed class XmlQueryDataWriter : BinaryWriter
{
	public XmlQueryDataWriter(Stream output)
		: base(output)
	{
	}

	public void WriteStringQ(string value)
	{
		Write(value != null);
		if (value != null)
		{
			Write(value);
		}
	}
}
