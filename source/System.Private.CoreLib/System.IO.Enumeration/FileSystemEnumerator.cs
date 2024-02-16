using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.IO.Enumeration;

public abstract class FileSystemEnumerator<TResult> : CriticalFinalizerObject, IEnumerator<TResult>, IDisposable, IEnumerator
{
	private int _remainingRecursionDepth;

	private readonly string _originalRootDirectory;

	private readonly string _rootDirectory;

	private readonly EnumerationOptions _options;

	private readonly object _lock = new object();

	private unsafe Interop.NtDll.FILE_FULL_DIR_INFORMATION* _entry;

	private TResult _current;

	private IntPtr _buffer;

	private int _bufferLength;

	private IntPtr _directoryHandle;

	private string _currentPath;

	private bool _lastEntryFound;

	private Queue<(IntPtr Handle, string Path, int RemainingDepth)> _pending;

	public TResult Current => _current;

	object? IEnumerator.Current => Current;

	public FileSystemEnumerator(string directory, EnumerationOptions? options = null)
		: this(directory, isNormalized: false, options)
	{
	}

	internal FileSystemEnumerator(string directory, bool isNormalized, EnumerationOptions options = null)
	{
		_originalRootDirectory = directory ?? throw new ArgumentNullException("directory");
		_rootDirectory = Path.TrimEndingDirectorySeparator(isNormalized ? directory : Path.GetFullPath(directory));
		_options = options ?? EnumerationOptions.Default;
		_remainingRecursionDepth = _options.MaxRecursionDepth;
		Init();
	}

	protected virtual bool ShouldIncludeEntry(ref FileSystemEntry entry)
	{
		return true;
	}

	protected virtual bool ShouldRecurseIntoEntry(ref FileSystemEntry entry)
	{
		return true;
	}

	protected abstract TResult TransformEntry(ref FileSystemEntry entry);

	protected virtual void OnDirectoryFinished(ReadOnlySpan<char> directory)
	{
	}

	protected virtual bool ContinueOnError(int error)
	{
		return false;
	}

	private unsafe void DirectoryFinished()
	{
		_entry = default(Interop.NtDll.FILE_FULL_DIR_INFORMATION*);
		CloseDirectoryHandle();
		OnDirectoryFinished(_currentPath.AsSpan());
		if (!DequeueNextDirectory())
		{
			_lastEntryFound = true;
		}
		else
		{
			FindNextEntry();
		}
	}

	public void Reset()
	{
		throw new NotSupportedException();
	}

	public void Dispose()
	{
		InternalDispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
	}

	~FileSystemEnumerator()
	{
		InternalDispose(disposing: false);
	}

	private void Init()
	{
		using (default(DisableMediaInsertionPrompt))
		{
			_directoryHandle = CreateDirectoryHandle(_rootDirectory);
			if (_directoryHandle == IntPtr.Zero)
			{
				_lastEntryFound = true;
			}
		}
		_currentPath = _rootDirectory;
		int bufferSize = _options.BufferSize;
		_bufferLength = ((bufferSize <= 0) ? 4096 : Math.Max(1024, bufferSize));
		try
		{
			_buffer = Marshal.AllocHGlobal(_bufferLength);
		}
		catch
		{
			CloseDirectoryHandle();
			throw;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe bool GetData()
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Interop.NtDll.IO_STATUS_BLOCK iO_STATUS_BLOCK);
		int num = Interop.NtDll.NtQueryDirectoryFile(_directoryHandle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, &iO_STATUS_BLOCK, _buffer, (uint)_bufferLength, Interop.NtDll.FILE_INFORMATION_CLASS.FileFullDirectoryInformation, Interop.BOOLEAN.FALSE, null, Interop.BOOLEAN.FALSE);
		switch ((uint)num)
		{
		case 2147483654u:
			DirectoryFinished();
			return false;
		case 0u:
			return true;
		case 3221225487u:
			DirectoryFinished();
			return false;
		default:
		{
			int num2 = (int)Interop.NtDll.RtlNtStatusToDosError(num);
			if ((num2 == 5 && _options.IgnoreInaccessible) || ContinueOnError(num2))
			{
				DirectoryFinished();
				return false;
			}
			throw Win32Marshal.GetExceptionForWin32Error(num2, _currentPath);
		}
		}
	}

	private unsafe IntPtr CreateRelativeDirectoryHandle(ReadOnlySpan<char> relativePath, string fullPath)
	{
		var (num, result) = Interop.NtDll.CreateFile(relativePath, _directoryHandle, Interop.NtDll.CreateDisposition.FILE_OPEN, Interop.NtDll.DesiredAccess.FILE_READ_DATA | Interop.NtDll.DesiredAccess.SYNCHRONIZE, FileShare.ReadWrite | FileShare.Delete, (FileAttributes)0, (Interop.NtDll.CreateOptions)16417u, Interop.ObjectAttributes.OBJ_CASE_INSENSITIVE, null, 0u, null, null);
		if (num == 0)
		{
			return result;
		}
		int num2 = (int)Interop.NtDll.RtlNtStatusToDosError((int)num);
		if (ContinueOnDirectoryError(num2, ignoreNotFound: true))
		{
			return IntPtr.Zero;
		}
		throw Win32Marshal.GetExceptionForWin32Error(num2, fullPath);
	}

	private void CloseDirectoryHandle()
	{
		IntPtr intPtr = Interlocked.Exchange(ref _directoryHandle, IntPtr.Zero);
		if (intPtr != IntPtr.Zero)
		{
			Interop.Kernel32.CloseHandle(intPtr);
		}
	}

	private IntPtr CreateDirectoryHandle(string path, bool ignoreNotFound = false)
	{
		IntPtr intPtr = Interop.Kernel32.CreateFile_IntPtr(path, 1, FileShare.ReadWrite | FileShare.Delete, FileMode.Open, 33554432);
		if (intPtr == IntPtr.Zero || intPtr == (IntPtr)(-1))
		{
			int num = Marshal.GetLastWin32Error();
			if (ContinueOnDirectoryError(num, ignoreNotFound))
			{
				return IntPtr.Zero;
			}
			if (num == 2)
			{
				num = 3;
			}
			throw Win32Marshal.GetExceptionForWin32Error(num, path);
		}
		return intPtr;
	}

	private bool ContinueOnDirectoryError(int error, bool ignoreNotFound)
	{
		if ((!ignoreNotFound || (error != 2 && error != 3 && error != 267)) && (error != 5 || !_options.IgnoreInaccessible))
		{
			return ContinueOnError(error);
		}
		return true;
	}

	public unsafe bool MoveNext()
	{
		if (_lastEntryFound)
		{
			return false;
		}
		FileSystemEntry entry = default(FileSystemEntry);
		lock (_lock)
		{
			if (_lastEntryFound)
			{
				return false;
			}
			while (true)
			{
				FindNextEntry();
				if (_lastEntryFound)
				{
					return false;
				}
				FileSystemEntry.Initialize(ref entry, _entry, _currentPath.AsSpan(), _rootDirectory.AsSpan(), _originalRootDirectory.AsSpan());
				if ((_entry->FileAttributes & _options.AttributesToSkip) != 0)
				{
					continue;
				}
				if ((_entry->FileAttributes & FileAttributes.Directory) != 0)
				{
					if (_entry->FileName.Length <= 2 && _entry->FileName[0] == '.' && (_entry->FileName.Length != 2 || _entry->FileName[1] == '.'))
					{
						if (!_options.ReturnSpecialDirectories)
						{
							continue;
						}
					}
					else if (_options.RecurseSubdirectories && _remainingRecursionDepth > 0 && ShouldRecurseIntoEntry(ref entry))
					{
						string text = Path.Join(_currentPath.AsSpan(), _entry->FileName);
						IntPtr intPtr = CreateRelativeDirectoryHandle(_entry->FileName, text);
						if (intPtr != IntPtr.Zero)
						{
							try
							{
								if (_pending == null)
								{
									_pending = new Queue<(IntPtr, string, int)>();
								}
								_pending.Enqueue((intPtr, text, _remainingRecursionDepth - 1));
							}
							catch
							{
								Interop.Kernel32.CloseHandle(intPtr);
								throw;
							}
						}
					}
				}
				if (ShouldIncludeEntry(ref entry))
				{
					break;
				}
			}
			_current = TransformEntry(ref entry);
			return true;
		}
	}

	private unsafe void FindNextEntry()
	{
		_entry = Interop.NtDll.FILE_FULL_DIR_INFORMATION.GetNextInfo(_entry);
		if (_entry == null && GetData())
		{
			_entry = (Interop.NtDll.FILE_FULL_DIR_INFORMATION*)(void*)_buffer;
		}
	}

	private bool DequeueNextDirectory()
	{
		if (_pending == null || _pending.Count == 0)
		{
			return false;
		}
		(_directoryHandle, _currentPath, _remainingRecursionDepth) = _pending.Dequeue();
		return true;
	}

	private void InternalDispose(bool disposing)
	{
		if (_lock != null)
		{
			lock (_lock)
			{
				_lastEntryFound = true;
				CloseDirectoryHandle();
				if (_pending != null)
				{
					while (_pending.Count > 0)
					{
						Interop.Kernel32.CloseHandle(_pending.Dequeue().Handle);
					}
					_pending = null;
				}
				if (_buffer != (IntPtr)0)
				{
					Marshal.FreeHGlobal(_buffer);
				}
				_buffer = default(IntPtr);
			}
		}
		Dispose(disposing);
	}
}
