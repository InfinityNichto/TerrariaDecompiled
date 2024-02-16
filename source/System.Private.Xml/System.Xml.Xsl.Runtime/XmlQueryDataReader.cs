using System.IO;

namespace System.Xml.Xsl.Runtime;

internal sealed class XmlQueryDataReader : BinaryReader
{
	public XmlQueryDataReader(Stream input)
		: base(input)
	{
	}

	public string ReadStringQ()
	{
		if (!ReadBoolean())
		{
			return null;
		}
		return ReadString();
	}

	public sbyte ReadSByte(sbyte minValue, sbyte maxValue)
	{
		sbyte b = ReadSByte();
		if (b < minValue)
		{
			throw new ArgumentOutOfRangeException("minValue");
		}
		if (maxValue < b)
		{
			throw new ArgumentOutOfRangeException("maxValue");
		}
		return b;
	}
}
