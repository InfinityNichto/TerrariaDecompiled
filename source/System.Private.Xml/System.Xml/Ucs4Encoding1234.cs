namespace System.Xml;

internal sealed class Ucs4Encoding1234 : Ucs4Encoding
{
	public override string EncodingName => "ucs-4 (Bigendian)";

	public override ReadOnlySpan<byte> Preamble => new byte[4] { 0, 0, 254, 255 };

	public Ucs4Encoding1234()
	{
		ucs4Decoder = new Ucs4Decoder1234();
	}

	public override byte[] GetPreamble()
	{
		return new byte[4] { 0, 0, 254, 255 };
	}
}
