using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies;

internal static class FileStreamHelpers
{
	private sealed class AsyncCopyToAwaitable : ICriticalNotifyCompletion, INotifyCompletion
	{
		private static readonly Action s_sentinel = delegate
		{
		};

		internal unsafe static readonly IOCompletionCallback s_callback = IOCallback;

		internal readonly SafeFileHandle _fileHandle;

		internal long _position;

		internal unsafe NativeOverlapped* _nativeOverlapped;

		internal Action _continuation;

		internal uint _errorCode;

		internal uint _numBytes;

		internal object CancellationLock => this;

		public bool IsCompleted => (object)_continuation == s_sentinel;

		internal AsyncCopyToAwaitable(SafeFileHandle fileHandle)
		{
			_fileHandle = fileHandle;
		}

		internal void ResetForNextOperation()
		{
			_continuation = null;
			_errorCode = 0u;
			_numBytes = 0u;
		}

		internal unsafe static void IOCallback(uint errorCode, uint numBytes, NativeOverlapped* pOVERLAP)
		{
			AsyncCopyToAwaitable asyncCopyToAwaitable = (AsyncCopyToAwaitable)ThreadPoolBoundHandle.GetNativeOverlappedState(pOVERLAP);
			asyncCopyToAwaitable._errorCode = errorCode;
			asyncCopyToAwaitable._numBytes = numBytes;
			(asyncCopyToAwaitable._continuation ?? Interlocked.CompareExchange(ref asyncCopyToAwaitable._continuation, s_sentinel, null))?.Invoke();
		}

		internal void MarkCompleted()
		{
			_continuation = s_sentinel;
		}

		public AsyncCopyToAwaitable GetAwaiter()
		{
			return this;
		}

		public void GetResult()
		{
		}

		public void OnCompleted(Action continuation)
		{
			UnsafeOnCompleted(continuation);
		}

		public void UnsafeOnCompleted(Action continuation)
		{
			if ((object)_continuation == s_sentinel || Interlocked.CompareExchange(ref _continuation, continuation, null) != null)
			{
				Task.Run(continuation);
			}
		}
	}

	private static int s_cachedSerializationSwitch;

	internal static bool UseNet5CompatStrategy { get; } = AppContextConfigHelper.GetBooleanConfig("System.IO.UseNet5CompatFileStream", "DOTNET_SYSTEM_IO_USENET5COMPATFILESTREAM");


	internal static FileStreamStrategy ChooseStrategy(FileStream fileStream, SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync)
	{
		FileStreamStrategy strategy = (UseNet5CompatStrategy ? new Net5CompatFileStreamStrategy(handle, access, (bufferSize == 0) ? 1 : bufferSize, isAsync) : EnableBufferingIfNeeded(ChooseStrategyCore(handle, access, isAsync), bufferSize));
		return WrapIfDerivedType(fileStream, strategy);
	}

	internal static FileStreamStrategy ChooseStrategy(FileStream fileStream, string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, long preallocationSize)
	{
		FileStreamStrategy strategy = (UseNet5CompatStrategy ? new Net5CompatFileStreamStrategy(path, mode, access, share, (bufferSize == 0) ? 1 : bufferSize, options, preallocationSize) : EnableBufferingIfNeeded(ChooseStrategyCore(path, mode, access, share, options, preallocationSize), bufferSize));
		return WrapIfDerivedType(fileStream, strategy);
	}

	private static FileStreamStrategy EnableBufferingIfNeeded(FileStreamStrategy strategy, int bufferSize)
	{
		if (bufferSize <= 1)
		{
			return strategy;
		}
		return new BufferedFileStreamStrategy(strategy, bufferSize);
	}

	private static FileStreamStrategy WrapIfDerivedType(FileStream fileStream, FileStreamStrategy strategy)
	{
		if (!(fileStream.GetType() == typeof(FileStream)))
		{
			return new DerivedFileStreamStrategy(fileStream, strategy);
		}
		return strategy;
	}

	internal static bool IsIoRelatedException(Exception e)
	{
		if (!(e is IOException) && !(e is UnauthorizedAccessException) && !(e is NotSupportedException))
		{
			if (e is ArgumentException)
			{
				return !(e is ArgumentNullException);
			}
			return false;
		}
		return true;
	}

	internal static void ValidateArguments(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, long preallocationSize)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path", SR.ArgumentNull_Path);
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		FileShare fileShare = share & ~FileShare.Inheritable;
		string text = null;
		if (mode < FileMode.CreateNew || mode > FileMode.Append)
		{
			text = "mode";
		}
		else if (access < FileAccess.Read || access > FileAccess.ReadWrite)
		{
			text = "access";
		}
		else if ((fileShare < FileShare.None) || fileShare > (FileShare.ReadWrite | FileShare.Delete))
		{
			text = "share";
		}
		if (text != null)
		{
			throw new ArgumentOutOfRangeException(text, SR.ArgumentOutOfRange_Enum);
		}
		if (options != 0 && (options & (FileOptions)67092479) != 0)
		{
			throw new ArgumentOutOfRangeException("options", SR.ArgumentOutOfRange_Enum);
		}
		if (bufferSize < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException_NeedNonNegNum("bufferSize");
		}
		else if (preallocationSize < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException_NeedNonNegNum("preallocationSize");
		}
		if ((access & FileAccess.Write) == 0 && (mode == FileMode.Truncate || mode == FileMode.CreateNew || mode == FileMode.Create || mode == FileMode.Append))
		{
			throw new ArgumentException(SR.Format(SR.Argument_InvalidFileModeAndAccessCombo, mode, access), "access");
		}
		if ((access & FileAccess.Read) != 0 && mode == FileMode.Append)
		{
			throw new ArgumentException(SR.Argument_InvalidAppendMode, "access");
		}
		if (preallocationSize > 0)
		{
			ValidateArgumentsForPreallocation(mode, access);
		}
		SerializationGuard(access);
	}

	internal static void ValidateArgumentsForPreallocation(FileMode mode, FileAccess access)
	{
		if ((access & FileAccess.Write) == 0)
		{
			throw new ArgumentException(SR.Argument_InvalidPreallocateAccess, "access");
		}
		if (mode != FileMode.Create && mode != FileMode.CreateNew)
		{
			throw new ArgumentException(SR.Argument_InvalidPreallocateMode, "mode");
		}
	}

	internal static void SerializationGuard(FileAccess access)
	{
		if ((access & FileAccess.Write) == FileAccess.Write)
		{
			SerializationInfo.ThrowIfDeserializationInProgress("AllowFileWrites", ref s_cachedSerializationSwitch);
		}
	}

	private static OSFileStreamStrategy ChooseStrategyCore(SafeFileHandle handle, FileAccess access, bool isAsync)
	{
		if (!isAsync)
		{
			return new SyncWindowsFileStreamStrategy(handle, access);
		}
		return new AsyncWindowsFileStreamStrategy(handle, access);
	}

	private static FileStreamStrategy ChooseStrategyCore(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize)
	{
		if ((options & FileOptions.Asynchronous) == 0)
		{
			return new SyncWindowsFileStreamStrategy(path, mode, access, share, options, preallocationSize);
		}
		return new AsyncWindowsFileStreamStrategy(path, mode, access, share, options, preallocationSize);
	}

	internal static void FlushToDisk(SafeFileHandle handle)
	{
		if (!Interop.Kernel32.FlushFileBuffers(handle))
		{
			throw Win32Marshal.GetExceptionForLastWin32Error(handle.Path);
		}
	}

	internal static long Seek(SafeFileHandle handle, long offset, SeekOrigin origin, bool closeInvalidHandle = false)
	{
		if (!Interop.Kernel32.SetFilePointerEx(handle, offset, out var lpNewFilePointer, (uint)origin))
		{
			if (closeInvalidHandle)
			{
				throw Win32Marshal.GetExceptionForWin32Error(GetLastWin32ErrorAndDisposeHandleIfInvalid(handle), handle.Path);
			}
			throw Win32Marshal.GetExceptionForLastWin32Error(handle.Path);
		}
		return lpNewFilePointer;
	}

	internal static void ThrowInvalidArgument(SafeFileHandle handle)
	{
		throw Win32Marshal.GetExceptionForWin32Error(87, handle.Path);
	}

	internal static int GetLastWin32ErrorAndDisposeHandleIfInvalid(SafeFileHandle handle)
	{
		int lastPInvokeError = Marshal.GetLastPInvokeError();
		if (lastPInvokeError == 6)
		{
			handle.Dispose();
		}
		return lastPInvokeError;
	}

	internal static void Lock(SafeFileHandle handle, bool canWrite, long position, long length)
	{
		int offsetLow = (int)position;
		int offsetHigh = (int)(position >> 32);
		int countLow = (int)length;
		int countHigh = (int)(length >> 32);
		if (!Interop.Kernel32.LockFile(handle, offsetLow, offsetHigh, countLow, countHigh))
		{
			throw Win32Marshal.GetExceptionForLastWin32Error(handle.Path);
		}
	}

	internal static void Unlock(SafeFileHandle handle, long position, long length)
	{
		int offsetLow = (int)position;
		int offsetHigh = (int)(position >> 32);
		int countLow = (int)length;
		int countHigh = (int)(length >> 32);
		if (!Interop.Kernel32.UnlockFile(handle, offsetLow, offsetHigh, countLow, countHigh))
		{
			throw Win32Marshal.GetExceptionForLastWin32Error(handle.Path);
		}
	}

	internal static void SetFileLength(SafeFileHandle handle, long length)
	{
		if (!TrySetFileLength(handle, length, out var errorCode))
		{
			throw (errorCode == 87) ? new ArgumentOutOfRangeException("length", SR.ArgumentOutOfRange_FileLengthTooBig) : Win32Marshal.GetExceptionForWin32Error(errorCode, handle.Path);
		}
	}

	internal unsafe static bool TrySetFileLength(SafeFileHandle handle, long length, out int errorCode)
	{
		Interop.Kernel32.FILE_END_OF_FILE_INFO fILE_END_OF_FILE_INFO = default(Interop.Kernel32.FILE_END_OF_FILE_INFO);
		fILE_END_OF_FILE_INFO.EndOfFile = length;
		Interop.Kernel32.FILE_END_OF_FILE_INFO fILE_END_OF_FILE_INFO2 = fILE_END_OF_FILE_INFO;
		if (!Interop.Kernel32.SetFileInformationByHandle(handle, 6, &fILE_END_OF_FILE_INFO2, (uint)sizeof(Interop.Kernel32.FILE_END_OF_FILE_INFO)))
		{
			errorCode = Marshal.GetLastPInvokeError();
			return false;
		}
		errorCode = 0;
		return true;
	}

	internal unsafe static int ReadFileNative(SafeFileHandle handle, Span<byte> bytes, NativeOverlapped* overlapped, out int errorCode)
	{
		int numBytesRead = 0;
		int num;
		fixed (byte* bytes2 = &MemoryMarshal.GetReference(bytes))
		{
			num = ((overlapped == null) ? Interop.Kernel32.ReadFile(handle, bytes2, bytes.Length, out numBytesRead, overlapped) : Interop.Kernel32.ReadFile(handle, bytes2, bytes.Length, IntPtr.Zero, overlapped));
		}
		if (num == 0)
		{
			errorCode = GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
			return -1;
		}
		errorCode = 0;
		return numBytesRead;
	}

	internal unsafe static async Task AsyncModeCopyToAsync(SafeFileHandle handle, bool canSeek, long filePosition, Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		AsyncCopyToAwaitable readAwaitable = new AsyncCopyToAwaitable(handle);
		if (canSeek)
		{
			readAwaitable._position = filePosition;
		}
		byte[] copyBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
		PreAllocatedOverlapped awaitableOverlapped = new PreAllocatedOverlapped(AsyncCopyToAwaitable.s_callback, readAwaitable, copyBuffer);
		CancellationTokenRegistration cancellationReg = default(CancellationTokenRegistration);
		try
		{
			if (cancellationToken.CanBeCanceled)
			{
				cancellationReg = cancellationToken.UnsafeRegister(delegate(object s)
				{
					AsyncCopyToAwaitable asyncCopyToAwaitable = (AsyncCopyToAwaitable)s;
					lock (asyncCopyToAwaitable.CancellationLock)
					{
						if (asyncCopyToAwaitable._nativeOverlapped != null)
						{
							Interop.Kernel32.CancelIoEx(asyncCopyToAwaitable._fileHandle, asyncCopyToAwaitable._nativeOverlapped);
						}
					}
				}, readAwaitable);
			}
			while (true)
			{
				cancellationToken.ThrowIfCancellationRequested();
				readAwaitable.ResetForNextOperation();
				try
				{
					readAwaitable._nativeOverlapped = handle.ThreadPoolBinding.AllocateNativeOverlapped(awaitableOverlapped);
					if (canSeek)
					{
						readAwaitable._nativeOverlapped->OffsetLow = (int)readAwaitable._position;
						readAwaitable._nativeOverlapped->OffsetHigh = (int)(readAwaitable._position >> 32);
					}
					if (ReadFileNative(handle, copyBuffer, readAwaitable._nativeOverlapped, out var errorCode) < 0)
					{
						switch (errorCode)
						{
						case 38:
						case 109:
							readAwaitable.MarkCompleted();
							break;
						default:
							throw Win32Marshal.GetExceptionForWin32Error(errorCode, handle.Path);
						case 997:
							break;
						}
					}
					await readAwaitable;
					switch (readAwaitable._errorCode)
					{
					case 995u:
						throw new OperationCanceledException(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(canceled: true));
					default:
						throw Win32Marshal.GetExceptionForWin32Error((int)readAwaitable._errorCode, handle.Path);
					case 0u:
					case 38u:
					case 109u:
					{
						int numBytes = (int)readAwaitable._numBytes;
						if (numBytes == 0)
						{
							return;
						}
						if (canSeek)
						{
							readAwaitable._position += numBytes;
						}
						break;
					}
					}
				}
				finally
				{
					NativeOverlapped* nativeOverlapped;
					lock (readAwaitable.CancellationLock)
					{
						nativeOverlapped = readAwaitable._nativeOverlapped;
						readAwaitable._nativeOverlapped = null;
					}
					if (nativeOverlapped != null)
					{
						handle.ThreadPoolBinding.FreeNativeOverlapped(nativeOverlapped);
					}
				}
				await destination.WriteAsync(new ReadOnlyMemory<byte>(copyBuffer, 0, (int)readAwaitable._numBytes), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		finally
		{
			cancellationReg.Dispose();
			awaitableOverlapped.Dispose();
			ArrayPool<byte>.Shared.Return(copyBuffer);
		}
	}
}
