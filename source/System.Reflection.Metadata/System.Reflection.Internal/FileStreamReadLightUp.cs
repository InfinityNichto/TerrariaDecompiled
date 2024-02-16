using System.IO;

namespace System.Reflection.Internal;

internal static class FileStreamReadLightUp
{
	internal static bool IsFileStream(Stream stream)
	{
		return stream is FileStream;
	}

	internal unsafe static int ReadFile(Stream stream, byte* buffer, int size)
	{
		return stream.Read(new Span<byte>(buffer, size));
	}
}
