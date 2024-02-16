using System.Text;

namespace System.Net.Mime;

internal interface IByteEncoder
{
	int EncodeBytes(byte[] buffer, int offset, int count, bool dontDeferFinalBytes, bool shouldAppendSpaceToCRLF);

	void AppendPadding();

	int EncodeString(string value, Encoding encoding);

	string GetEncodedString();
}
