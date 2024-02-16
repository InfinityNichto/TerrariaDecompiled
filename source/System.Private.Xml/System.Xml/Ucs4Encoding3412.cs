namespace System.Xml;

internal sealed class Ucs4Encoding3412 : Ucs4Encoding
{
	public override string EncodingName => "ucs-4 (order 3412)";

	public override ReadOnlySpan<byte> Preamble => new byte[4] { 254, 255, 0, 0 };

	public Ucs4Encoding3412()
	{
		ucs4Decoder = new Ucs4Decoder3412();
	}

	public override byte[] GetPreamble()
	{
		return new byte[4] { 254, 255, 0, 0 };
	}
}
