using System.Text;

namespace System.Net.Mime;

internal interface IEncodableStream
{
	int DecodeBytes(byte[] buffer, int offset, int count);

	int EncodeString(string value, Encoding encoding);

	string GetEncodedString();
}
