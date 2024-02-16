using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.IsolatedStorage;

public class IsolatedStorageFileStream : FileStream
{
	private struct InitialiationData
	{
		public FileStream NestedStream;

		public IsolatedStorageFile StorageFile;

		public string FullPath;
	}

	private readonly FileStream _fs;

	private readonly IsolatedStorageFile _isf;

	private readonly string _givenPath;

	private readonly string _fullPath;

	public override bool CanRead => _fs.CanRead;

	public override bool CanWrite => _fs.CanWrite;

	public override bool CanSeek => _fs.CanSeek;

	public override long Length => _fs.Length;

	public override long Position
	{
		get
		{
			return _fs.Position;
		}
		set
		{
			_fs.Position = value;
		}
	}

	public override bool IsAsync => _fs.IsAsync;

	[Obsolete("IsolatedStorageFileStream.Handle has been deprecated. Use IsolatedStorageFileStream's SafeFileHandle property instead.")]
	public override IntPtr Handle => _fs.Handle;

	public override SafeFileHandle SafeFileHandle
	{
		get
		{
			throw new IsolatedStorageException(System.SR.IsolatedStorage_Operation_ISFS);
		}
	}

	public IsolatedStorageFileStream(string path, FileMode mode)
		: this(path, mode, (mode == FileMode.Append) ? FileAccess.Write : FileAccess.ReadWrite, FileShare.None, null)
	{
	}

	public IsolatedStorageFileStream(string path, FileMode mode, IsolatedStorageFile? isf)
		: this(path, mode, (mode == FileMode.Append) ? FileAccess.Write : FileAccess.ReadWrite, FileShare.None, isf)
	{
	}

	public IsolatedStorageFileStream(string path, FileMode mode, FileAccess access)
		: this(path, mode, access, (access == FileAccess.Read) ? FileShare.Read : FileShare.None, 1024, null)
	{
	}

	public IsolatedStorageFileStream(string path, FileMode mode, FileAccess access, IsolatedStorageFile? isf)
		: this(path, mode, access, (access == FileAccess.Read) ? FileShare.Read : FileShare.None, 1024, isf)
	{
	}

	public IsolatedStorageFileStream(string path, FileMode mode, FileAccess access, FileShare share)
		: this(path, mode, access, share, 1024, null)
	{
	}

	public IsolatedStorageFileStream(string path, FileMode mode, FileAccess access, FileShare share, IsolatedStorageFile? isf)
		: this(path, mode, access, share, 1024, isf)
	{
	}

	public IsolatedStorageFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize)
		: this(path, mode, access, share, bufferSize, null)
	{
	}

	public IsolatedStorageFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, IsolatedStorageFile? isf)
		: this(path, mode, access, share, bufferSize, InitializeFileStream(path, mode, access, share, bufferSize, isf))
	{
	}

	private IsolatedStorageFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, InitialiationData initializationData)
		: base(new SafeFileHandle(initializationData.NestedStream.SafeFileHandle.DangerousGetHandle(), ownsHandle: false), access, bufferSize)
	{
		_isf = initializationData.StorageFile;
		_givenPath = path;
		_fullPath = initializationData.FullPath;
		_fs = initializationData.NestedStream;
	}

	private static InitialiationData InitializeFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, IsolatedStorageFile isf)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		if (path.Length == 0 || path.Equals("\\"))
		{
			throw new ArgumentException(System.SR.IsolatedStorage_Path);
		}
		bool flag = false;
		if (isf == null)
		{
			isf = IsolatedStorageFile.GetUserStoreForDomain();
			flag = true;
		}
		if (isf.Disposed)
		{
			throw new ObjectDisposedException(null, System.SR.IsolatedStorage_StoreNotOpen);
		}
		if ((uint)(mode - 1) > 5u)
		{
			throw new ArgumentException(System.SR.IsolatedStorage_FileOpenMode);
		}
		InitialiationData initialiationData = default(InitialiationData);
		initialiationData.FullPath = isf.GetFullPath(path);
		initialiationData.StorageFile = isf;
		InitialiationData result = initialiationData;
		try
		{
			result.NestedStream = new FileStream(result.FullPath, mode, access, share, bufferSize, FileOptions.None);
			return result;
		}
		catch (Exception rootCause)
		{
			try
			{
				if (flag)
				{
					result.StorageFile?.Dispose();
				}
			}
			catch
			{
			}
			throw IsolatedStorageFile.GetIsolatedStorageException(System.SR.IsolatedStorage_Operation_ISFS, rootCause);
		}
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			if (disposing && _fs != null)
			{
				_fs.Dispose();
			}
		}
		finally
		{
			base.Dispose(disposing);
		}
	}

	public override ValueTask DisposeAsync()
	{
		if (!(GetType() != typeof(IsolatedStorageFileStream)))
		{
			if (_fs == null)
			{
				return default(ValueTask);
			}
			return _fs.DisposeAsync();
		}
		return base.DisposeAsync();
	}

	public override void Flush()
	{
		_fs.Flush();
	}

	public override void Flush(bool flushToDisk)
	{
		_fs.Flush(flushToDisk);
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		return _fs.FlushAsync(cancellationToken);
	}

	public override void SetLength(long value)
	{
		_fs.SetLength(value);
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		return _fs.Read(buffer, offset, count);
	}

	public override int Read(Span<byte> buffer)
	{
		return _fs.Read(buffer);
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		return _fs.ReadAsync(buffer, offset, count, cancellationToken);
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
	{
		return _fs.ReadAsync(buffer, cancellationToken);
	}

	public override int ReadByte()
	{
		return _fs.ReadByte();
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		return _fs.Seek(offset, origin);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		_fs.Write(buffer, offset, count);
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		_fs.Write(buffer);
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		return _fs.WriteAsync(buffer, offset, count, cancellationToken);
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
	{
		return _fs.WriteAsync(buffer, cancellationToken);
	}

	public override void WriteByte(byte value)
	{
		_fs.WriteByte(value);
	}

	public override IAsyncResult BeginRead(byte[] array, int offset, int numBytes, AsyncCallback? userCallback, object? stateObject)
	{
		return _fs.BeginRead(array, offset, numBytes, userCallback, stateObject);
	}

	public override IAsyncResult BeginWrite(byte[] array, int offset, int numBytes, AsyncCallback? userCallback, object? stateObject)
	{
		return _fs.BeginWrite(array, offset, numBytes, userCallback, stateObject);
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		return _fs.EndRead(asyncResult);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		_fs.EndWrite(asyncResult);
	}

	[UnsupportedOSPlatform("macos")]
	public override void Unlock(long position, long length)
	{
		_fs.Unlock(position, length);
	}

	[UnsupportedOSPlatform("macos")]
	public override void Lock(long position, long length)
	{
		_fs.Lock(position, length);
	}
}
