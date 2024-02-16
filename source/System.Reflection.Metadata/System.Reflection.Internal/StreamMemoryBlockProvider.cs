using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Reflection.Internal;

internal sealed class StreamMemoryBlockProvider : MemoryBlockProvider
{
	internal const int MemoryMapThreshold = 16384;

	private Stream _stream;

	private readonly object _streamGuard;

	private readonly bool _leaveOpen;

	private bool _useMemoryMap;

	private readonly bool _isFileStream;

	private readonly long _imageStart;

	private readonly int _imageSize;

	private IDisposable _lazyMemoryMap;

	public override int Size => _imageSize;

	public StreamMemoryBlockProvider(Stream stream, long imageStart, int imageSize, bool isFileStream, bool leaveOpen)
	{
		_stream = stream;
		_streamGuard = new object();
		_imageStart = imageStart;
		_imageSize = imageSize;
		_leaveOpen = leaveOpen;
		_isFileStream = isFileStream;
		_useMemoryMap = isFileStream && MemoryMapLightUp.IsAvailable;
	}

	protected override void Dispose(bool disposing)
	{
		if (!_leaveOpen)
		{
			Interlocked.Exchange(ref _stream, null)?.Dispose();
		}
		Interlocked.Exchange(ref _lazyMemoryMap, null)?.Dispose();
	}

	internal unsafe static NativeHeapMemoryBlock ReadMemoryBlockNoLock(Stream stream, bool isFileStream, long start, int size)
	{
		NativeHeapMemoryBlock nativeHeapMemoryBlock = new NativeHeapMemoryBlock(size);
		bool flag = true;
		try
		{
			stream.Seek(start, SeekOrigin.Begin);
			int num = 0;
			if (!isFileStream || (num = FileStreamReadLightUp.ReadFile(stream, nativeHeapMemoryBlock.Pointer, size)) != size)
			{
				stream.CopyTo(nativeHeapMemoryBlock.Pointer + num, size - num);
			}
			flag = false;
		}
		finally
		{
			if (flag)
			{
				nativeHeapMemoryBlock.Dispose();
			}
		}
		return nativeHeapMemoryBlock;
	}

	protected override AbstractMemoryBlock GetMemoryBlockImpl(int start, int size)
	{
		long start2 = _imageStart + start;
		if (_useMemoryMap && size > 16384)
		{
			if (TryCreateMemoryMappedFileBlock(start2, size, out var block))
			{
				return block;
			}
			_useMemoryMap = false;
		}
		lock (_streamGuard)
		{
			return ReadMemoryBlockNoLock(_stream, _isFileStream, start2, size);
		}
	}

	public override Stream GetStream(out StreamConstraints constraints)
	{
		constraints = new StreamConstraints(_streamGuard, _imageStart, _imageSize);
		return _stream;
	}

	private bool TryCreateMemoryMappedFileBlock(long start, int size, [NotNullWhen(true)] out MemoryMappedFileBlock block)
	{
		if (_lazyMemoryMap == null)
		{
			IDisposable disposable;
			lock (_streamGuard)
			{
				disposable = MemoryMapLightUp.CreateMemoryMap(_stream);
			}
			if (disposable == null)
			{
				block = null;
				return false;
			}
			if (Interlocked.CompareExchange(ref _lazyMemoryMap, disposable, null) != null)
			{
				disposable.Dispose();
			}
		}
		IDisposable disposable2 = MemoryMapLightUp.CreateViewAccessor(_lazyMemoryMap, start, size);
		if (disposable2 == null)
		{
			block = null;
			return false;
		}
		if (!MemoryMapLightUp.TryGetSafeBufferAndPointerOffset(disposable2, out SafeBuffer safeBuffer, out long offset))
		{
			block = null;
			return false;
		}
		block = new MemoryMappedFileBlock(disposable2, safeBuffer, offset, size);
		return true;
	}
}
