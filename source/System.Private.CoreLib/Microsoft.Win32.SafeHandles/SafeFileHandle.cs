using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Strategies;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Microsoft.Win32.SafeHandles;

public sealed class SafeFileHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	internal sealed class ThreadPoolValueTaskSource : IThreadPoolWorkItem, IValueTaskSource<int>, IValueTaskSource<long>, IValueTaskSource
	{
		private enum Operation : byte
		{
			None,
			Read,
			Write,
			ReadScatter,
			WriteGather
		}

		private readonly SafeFileHandle _fileHandle;

		private ManualResetValueTaskSourceCore<long> _source;

		private Operation _operation;

		private ExecutionContext _context;

		private OSFileStreamStrategy _strategy;

		private long _fileOffset;

		private CancellationToken _cancellationToken;

		private ReadOnlyMemory<byte> _singleSegment;

		private IReadOnlyList<Memory<byte>> _readScatterBuffers;

		private IReadOnlyList<ReadOnlyMemory<byte>> _writeGatherBuffers;

		internal ThreadPoolValueTaskSource(SafeFileHandle fileHandle)
		{
			_fileHandle = fileHandle;
		}

		public ValueTaskSourceStatus GetStatus(short token)
		{
			return _source.GetStatus(token);
		}

		public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
		{
			_source.OnCompleted(continuation, state, token, flags);
		}

		void IValueTaskSource.GetResult(short token)
		{
			GetResult(token);
		}

		int IValueTaskSource<int>.GetResult(short token)
		{
			return (int)GetResult(token);
		}

		public long GetResult(short token)
		{
			try
			{
				return _source.GetResult(token);
			}
			finally
			{
				_source.Reset();
				Volatile.Write(ref _fileHandle._reusableThreadPoolValueTaskSource, this);
			}
		}

		private void ExecuteInternal()
		{
			long num = 0L;
			Exception ex = null;
			try
			{
				if (_cancellationToken.IsCancellationRequested)
				{
					ex = new OperationCanceledException(_cancellationToken);
				}
				else
				{
					switch (_operation)
					{
					case Operation.Read:
					{
						Memory<byte> memory = MemoryMarshal.AsMemory(_singleSegment);
						num = RandomAccess.ReadAtOffset(_fileHandle, memory.Span, _fileOffset);
						break;
					}
					case Operation.Write:
						RandomAccess.WriteAtOffset(_fileHandle, _singleSegment.Span, _fileOffset);
						break;
					case Operation.ReadScatter:
						num = RandomAccess.ReadScatterAtOffset(_fileHandle, _readScatterBuffers, _fileOffset);
						break;
					case Operation.WriteGather:
						RandomAccess.WriteGatherAtOffset(_fileHandle, _writeGatherBuffers, _fileOffset);
						break;
					}
				}
			}
			catch (Exception ex2)
			{
				ex = ex2;
			}
			finally
			{
				if (_strategy != null)
				{
					if (ex != null)
					{
						_strategy.OnIncompleteOperation(_singleSegment.Length, 0);
					}
					else if (_operation == Operation.Read && num != _singleSegment.Length)
					{
						_strategy.OnIncompleteOperation(_singleSegment.Length, (int)num);
					}
				}
				_operation = Operation.None;
				_context = null;
				_strategy = null;
				_cancellationToken = default(CancellationToken);
				_singleSegment = default(ReadOnlyMemory<byte>);
				_readScatterBuffers = null;
				_writeGatherBuffers = null;
			}
			if (ex == null)
			{
				_source.SetResult(num);
			}
			else
			{
				_source.SetException(ex);
			}
		}

		void IThreadPoolWorkItem.Execute()
		{
			if (_context == null || _context.IsDefault)
			{
				ExecuteInternal();
				return;
			}
			ExecutionContext.RunForThreadPoolUnsafe(_context, delegate(ThreadPoolValueTaskSource x)
			{
				x.ExecuteInternal();
			}, in this);
		}

		private void QueueToThreadPool()
		{
			_context = ExecutionContext.Capture();
			ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: true);
		}

		public ValueTask<int> QueueRead(Memory<byte> buffer, long fileOffset, CancellationToken cancellationToken, OSFileStreamStrategy strategy)
		{
			_operation = Operation.Read;
			_singleSegment = buffer;
			_fileOffset = fileOffset;
			_cancellationToken = cancellationToken;
			_strategy = strategy;
			QueueToThreadPool();
			return new ValueTask<int>(this, _source.Version);
		}

		public ValueTask QueueWrite(ReadOnlyMemory<byte> buffer, long fileOffset, CancellationToken cancellationToken, OSFileStreamStrategy strategy)
		{
			_operation = Operation.Write;
			_singleSegment = buffer;
			_fileOffset = fileOffset;
			_cancellationToken = cancellationToken;
			_strategy = strategy;
			QueueToThreadPool();
			return new ValueTask(this, _source.Version);
		}

		public ValueTask<long> QueueReadScatter(IReadOnlyList<Memory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
		{
			_operation = Operation.ReadScatter;
			_readScatterBuffers = buffers;
			_fileOffset = fileOffset;
			_cancellationToken = cancellationToken;
			QueueToThreadPool();
			return new ValueTask<long>(this, _source.Version);
		}

		public ValueTask QueueWriteGather(IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
		{
			_operation = Operation.WriteGather;
			_writeGatherBuffers = buffers;
			_fileOffset = fileOffset;
			_cancellationToken = cancellationToken;
			QueueToThreadPool();
			return new ValueTask(this, _source.Version);
		}
	}

	internal sealed class OverlappedValueTaskSource : IValueTaskSource<int>, IValueTaskSource
	{
		internal unsafe static readonly IOCompletionCallback s_ioCallback = IOCallback;

		internal readonly PreAllocatedOverlapped _preallocatedOverlapped;

		internal readonly SafeFileHandle _fileHandle;

		private OSFileStreamStrategy _strategy;

		internal MemoryHandle _memoryHandle;

		private int _bufferSize;

		internal ManualResetValueTaskSourceCore<int> _source;

		private unsafe NativeOverlapped* _overlapped;

		private CancellationTokenRegistration _cancellationRegistration;

		internal ulong _result;

		internal short Version => _source.Version;

		internal OverlappedValueTaskSource(SafeFileHandle fileHandle)
		{
			_fileHandle = fileHandle;
			_source.RunContinuationsAsynchronously = true;
			_preallocatedOverlapped = PreAllocatedOverlapped.UnsafeCreate(s_ioCallback, this, null);
		}

		internal void Dispose()
		{
			ReleaseResources();
			_preallocatedOverlapped.Dispose();
		}

		internal static Exception GetIOError(int errorCode, string path)
		{
			if (errorCode != 38)
			{
				return Win32Marshal.GetExceptionForWin32Error(errorCode, path);
			}
			return ThrowHelper.CreateEndOfFileException();
		}

		internal unsafe NativeOverlapped* PrepareForOperation(ReadOnlyMemory<byte> memory, long fileOffset, OSFileStreamStrategy strategy = null)
		{
			_result = 0uL;
			_strategy = strategy;
			_bufferSize = memory.Length;
			_memoryHandle = memory.Pin();
			_overlapped = _fileHandle.ThreadPoolBinding.AllocateNativeOverlapped(_preallocatedOverlapped);
			_overlapped->OffsetLow = (int)fileOffset;
			_overlapped->OffsetHigh = (int)(fileOffset >> 32);
			return _overlapped;
		}

		public ValueTaskSourceStatus GetStatus(short token)
		{
			return _source.GetStatus(token);
		}

		public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
		{
			_source.OnCompleted(continuation, state, token, flags);
		}

		void IValueTaskSource.GetResult(short token)
		{
			GetResult(token);
		}

		public int GetResult(short token)
		{
			try
			{
				return _source.GetResult(token);
			}
			finally
			{
				_fileHandle.TryToReuse(this);
			}
		}

		internal unsafe void RegisterForCancellation(CancellationToken cancellationToken)
		{
			if (!cancellationToken.CanBeCanceled)
			{
				return;
			}
			try
			{
				_cancellationRegistration = cancellationToken.UnsafeRegister(delegate(object s, CancellationToken token)
				{
					OverlappedValueTaskSource overlappedValueTaskSource = (OverlappedValueTaskSource)s;
					if (!overlappedValueTaskSource._fileHandle.IsInvalid)
					{
						try
						{
							Interop.Kernel32.CancelIoEx(overlappedValueTaskSource._fileHandle, overlappedValueTaskSource._overlapped);
						}
						catch (ObjectDisposedException)
						{
						}
					}
				}, this);
			}
			catch (OutOfMemoryException)
			{
			}
		}

		private unsafe void ReleaseResources()
		{
			_strategy = null;
			_cancellationRegistration.Dispose();
			_memoryHandle.Dispose();
			if (_overlapped != null)
			{
				_fileHandle.ThreadPoolBinding.FreeNativeOverlapped(_overlapped);
				_overlapped = null;
			}
		}

		internal void FinishedScheduling()
		{
			ulong num = Interlocked.Exchange(ref _result, 1uL);
			if (num != 0L)
			{
				Complete((uint)num, (uint)(int)(num >> 32) & 0x7FFFFFFFu);
			}
		}

		private unsafe static void IOCallback(uint errorCode, uint numBytes, NativeOverlapped* pOverlapped)
		{
			OverlappedValueTaskSource overlappedValueTaskSource = (OverlappedValueTaskSource)ThreadPoolBoundHandle.GetNativeOverlappedState(pOverlapped);
			if (Interlocked.Exchange(ref overlappedValueTaskSource._result, 0x8000000000000000uL | ((ulong)numBytes << 32) | errorCode) != 0L)
			{
				overlappedValueTaskSource.Complete(errorCode, numBytes);
			}
		}

		internal void Complete(uint errorCode, uint numBytes)
		{
			OSFileStreamStrategy strategy = _strategy;
			ReleaseResources();
			switch (errorCode)
			{
			case 0u:
			case 38u:
			case 109u:
			case 232u:
				if (_bufferSize != numBytes)
				{
					strategy?.OnIncompleteOperation(_bufferSize, (int)numBytes);
				}
				_source.SetResult((int)numBytes);
				break;
			case 995u:
			{
				strategy?.OnIncompleteOperation(_bufferSize, 0);
				CancellationToken token = _cancellationRegistration.Token;
				_source.SetException(token.IsCancellationRequested ? new OperationCanceledException(token) : new OperationCanceledException());
				break;
			}
			default:
				strategy?.OnIncompleteOperation(_bufferSize, 0);
				_source.SetException(Win32Marshal.GetExceptionForWin32Error((int)errorCode));
				break;
			}
		}
	}

	private string _path;

	private ThreadPoolValueTaskSource _reusableThreadPoolValueTaskSource;

	private volatile FileOptions _fileOptions = (FileOptions)(-1);

	private volatile int _fileType = -1;

	private OverlappedValueTaskSource _reusableOverlappedValueTaskSource;

	internal string? Path => _path;

	public bool IsAsync => (GetFileOptions() & FileOptions.Asynchronous) != 0;

	internal bool CanSeek
	{
		get
		{
			if (!base.IsClosed)
			{
				return GetFileType() == 1;
			}
			return false;
		}
	}

	internal ThreadPoolBoundHandle? ThreadPoolBinding { get; set; }

	public SafeFileHandle(IntPtr preexistingHandle, bool ownsHandle)
		: base(ownsHandle)
	{
		SetHandle(preexistingHandle);
	}

	internal ThreadPoolValueTaskSource GetThreadPoolValueTaskSource()
	{
		return Interlocked.Exchange(ref _reusableThreadPoolValueTaskSource, null) ?? new ThreadPoolValueTaskSource(this);
	}

	public SafeFileHandle()
		: base(ownsHandle: true)
	{
	}

	internal static SafeFileHandle Open(string fullPath, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize)
	{
		using (DisableMediaInsertionPrompt.Create())
		{
			SafeFileHandle safeFileHandle = CreateFile(fullPath, mode, access, share, options);
			if (preallocationSize > 0)
			{
				Preallocate(fullPath, preallocationSize, safeFileHandle);
			}
			if ((options & FileOptions.Asynchronous) != 0)
			{
				safeFileHandle.InitThreadPoolBinding();
			}
			return safeFileHandle;
		}
	}

	private unsafe static SafeFileHandle CreateFile(string fullPath, FileMode mode, FileAccess access, FileShare share, FileOptions options)
	{
		Interop.Kernel32.SECURITY_ATTRIBUTES sECURITY_ATTRIBUTES = default(Interop.Kernel32.SECURITY_ATTRIBUTES);
		if ((share & FileShare.Inheritable) != 0)
		{
			Interop.Kernel32.SECURITY_ATTRIBUTES sECURITY_ATTRIBUTES2 = default(Interop.Kernel32.SECURITY_ATTRIBUTES);
			sECURITY_ATTRIBUTES2.nLength = (uint)sizeof(Interop.Kernel32.SECURITY_ATTRIBUTES);
			sECURITY_ATTRIBUTES2.bInheritHandle = Interop.BOOL.TRUE;
			sECURITY_ATTRIBUTES = sECURITY_ATTRIBUTES2;
		}
		int dwDesiredAccess = (((access & FileAccess.Read) == FileAccess.Read) ? int.MinValue : 0) | (((access & FileAccess.Write) == FileAccess.Write) ? 1073741824 : 0);
		share &= ~FileShare.Inheritable;
		if (mode == FileMode.Append)
		{
			mode = FileMode.OpenOrCreate;
		}
		int num = (int)options;
		num |= 0x100000;
		SafeFileHandle safeFileHandle = Interop.Kernel32.CreateFile(fullPath, dwDesiredAccess, share, &sECURITY_ATTRIBUTES, mode, num, IntPtr.Zero);
		if (safeFileHandle.IsInvalid)
		{
			int num2 = Marshal.GetLastPInvokeError();
			if (num2 == 3 && fullPath.Length == PathInternal.GetRootLength(fullPath))
			{
				num2 = 5;
			}
			throw Win32Marshal.GetExceptionForWin32Error(num2, fullPath);
		}
		safeFileHandle._path = fullPath;
		safeFileHandle._fileOptions = options;
		return safeFileHandle;
	}

	private unsafe static void Preallocate(string fullPath, long preallocationSize, SafeFileHandle fileHandle)
	{
		Interop.Kernel32.FILE_ALLOCATION_INFO fILE_ALLOCATION_INFO = default(Interop.Kernel32.FILE_ALLOCATION_INFO);
		fILE_ALLOCATION_INFO.AllocationSize = preallocationSize;
		Interop.Kernel32.FILE_ALLOCATION_INFO fILE_ALLOCATION_INFO2 = fILE_ALLOCATION_INFO;
		if (!Interop.Kernel32.SetFileInformationByHandle(fileHandle, 5, &fILE_ALLOCATION_INFO2, (uint)sizeof(Interop.Kernel32.FILE_ALLOCATION_INFO)))
		{
			int lastPInvokeError = Marshal.GetLastPInvokeError();
			if (lastPInvokeError == 112 || lastPInvokeError == 223)
			{
				fileHandle.Dispose();
				Interop.Kernel32.DeleteFile(fullPath);
				throw new IOException(SR.Format((lastPInvokeError == 112) ? SR.IO_DiskFull_Path_AllocationSize : SR.IO_FileTooLarge_Path_AllocationSize, fullPath, preallocationSize));
			}
		}
	}

	internal void EnsureThreadPoolBindingInitialized()
	{
		if (IsAsync && ThreadPoolBinding == null)
		{
			Init();
		}
		void Init()
		{
			lock (this)
			{
				if (ThreadPoolBinding == null)
				{
					InitThreadPoolBinding();
				}
			}
		}
	}

	private void InitThreadPoolBinding()
	{
		try
		{
			ThreadPoolBinding = ThreadPoolBoundHandle.BindHandle(this);
		}
		catch (ArgumentException innerException)
		{
			if (base.OwnsHandle)
			{
				Dispose();
			}
			throw new IOException(SR.IO_BindHandleFailed, innerException);
		}
	}

	internal unsafe FileOptions GetFileOptions()
	{
		FileOptions fileOptions = _fileOptions;
		if (fileOptions != (FileOptions)(-1))
		{
			return fileOptions;
		}
		Interop.NtDll.IO_STATUS_BLOCK IoStatusBlock;
		System.Runtime.CompilerServices.Unsafe.SkipInit(out Interop.NtDll.CreateOptions createOptions);
		int num = Interop.NtDll.NtQueryInformationFile(this, out IoStatusBlock, &createOptions, 4u, 16u);
		if (num != 0L)
		{
			int errorCode = (int)Interop.NtDll.RtlNtStatusToDosError(num);
			throw Win32Marshal.GetExceptionForWin32Error(errorCode);
		}
		FileOptions fileOptions2 = FileOptions.None;
		if ((createOptions & (Interop.NtDll.CreateOptions)48u) == 0)
		{
			fileOptions2 |= FileOptions.Asynchronous;
		}
		if ((createOptions & Interop.NtDll.CreateOptions.FILE_WRITE_THROUGH) != 0)
		{
			fileOptions2 |= FileOptions.WriteThrough;
		}
		if ((createOptions & Interop.NtDll.CreateOptions.FILE_RANDOM_ACCESS) != 0)
		{
			fileOptions2 |= FileOptions.RandomAccess;
		}
		if ((createOptions & Interop.NtDll.CreateOptions.FILE_SEQUENTIAL_ONLY) != 0)
		{
			fileOptions2 |= FileOptions.SequentialScan;
		}
		if ((createOptions & Interop.NtDll.CreateOptions.FILE_DELETE_ON_CLOSE) != 0)
		{
			fileOptions2 |= FileOptions.DeleteOnClose;
		}
		if ((createOptions & Interop.NtDll.CreateOptions.FILE_NO_INTERMEDIATE_BUFFERING) != 0)
		{
			fileOptions2 |= (FileOptions)536870912;
		}
		return _fileOptions = fileOptions2;
	}

	internal int GetFileType()
	{
		int num = _fileType;
		if (num == -1)
		{
			num = (_fileType = Interop.Kernel32.GetFileType(this));
		}
		return num;
	}

	internal OverlappedValueTaskSource GetOverlappedValueTaskSource()
	{
		return Interlocked.Exchange(ref _reusableOverlappedValueTaskSource, null) ?? new OverlappedValueTaskSource(this);
	}

	protected override bool ReleaseHandle()
	{
		bool result = Interop.Kernel32.CloseHandle(handle);
		Interlocked.Exchange(ref _reusableOverlappedValueTaskSource, null)?.Dispose();
		return result;
	}

	private void TryToReuse(OverlappedValueTaskSource source)
	{
		source._source.Reset();
		if (Interlocked.CompareExchange(ref _reusableOverlappedValueTaskSource, source, null) != null)
		{
			source._preallocatedOverlapped.Dispose();
		}
	}
}
