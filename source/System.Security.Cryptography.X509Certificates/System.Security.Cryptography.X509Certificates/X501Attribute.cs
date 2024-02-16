namespace System.Security.Cryptography.X509Certificates;

internal class X501Attribute : AsnEncodedData
{
	internal X501Attribute(Oid oid, byte[] rawData)
		: base(oid, rawData)
	{
	}
}
