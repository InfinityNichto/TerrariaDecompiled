using System.Text;

namespace System.Net.Mime;

internal sealed class EncodedStreamFactory
{
	private static readonly byte[] s_footer = new byte[2] { 63, 61 };

	internal IEncodableStream GetEncoderForHeader(Encoding encoding, bool useBase64Encoding, int headerTextLength)
	{
		byte[] header = CreateHeader(encoding, useBase64Encoding);
		byte[] footer = s_footer;
		WriteStateInfoBase writeStateInfoBase;
		if (useBase64Encoding)
		{
			writeStateInfoBase = new Base64WriteStateInfo(1024, header, footer, 70, headerTextLength);
			return new Base64Stream((Base64WriteStateInfo)writeStateInfoBase);
		}
		writeStateInfoBase = new WriteStateInfoBase(1024, header, footer, 70, headerTextLength);
		return new QEncodedStream(writeStateInfoBase);
	}

	private byte[] CreateHeader(Encoding encoding, bool useBase64Encoding)
	{
		return Encoding.ASCII.GetBytes("=?" + encoding.HeaderName + "?" + (useBase64Encoding ? "B?" : "Q?"));
	}
}
