using System.Text;

namespace System.IO;

internal static class EncodingCache
{
	internal static readonly Encoding UTF8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
}
