using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace System.Reflection.Internal;

internal static class MemoryMapLightUp
{
	internal static bool IsAvailable => true;

	internal static IDisposable CreateMemoryMap(Stream stream)
	{
		return MemoryMappedFile.CreateFromFile((FileStream)stream, null, 0L, MemoryMappedFileAccess.Read, HandleInheritability.None, leaveOpen: true);
	}

	internal static IDisposable CreateViewAccessor(object memoryMap, long start, int size)
	{
		try
		{
			return ((MemoryMappedFile)memoryMap).CreateViewAccessor(start, size, MemoryMappedFileAccess.Read);
		}
		catch (UnauthorizedAccessException ex)
		{
			throw new IOException(ex.Message, ex);
		}
	}

	internal static bool TryGetSafeBufferAndPointerOffset(object accessor, out SafeBuffer safeBuffer, out long offset)
	{
		MemoryMappedViewAccessor memoryMappedViewAccessor = (MemoryMappedViewAccessor)accessor;
		safeBuffer = memoryMappedViewAccessor.SafeMemoryMappedViewHandle;
		offset = memoryMappedViewAccessor.PointerOffset;
		return true;
	}
}
