using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes;

public sealed class NamedPipeServerStream : PipeStream
{
	internal sealed class ExecuteHelper
	{
		internal PipeStreamImpersonationWorker _userCode;

		internal SafePipeHandle _handle;

		internal bool _mustRevert;

		internal int _impersonateErrorCode;

		internal int _revertImpersonateErrorCode;

		internal ExecuteHelper(PipeStreamImpersonationWorker userCode, SafePipeHandle handle)
		{
			_userCode = userCode;
			_handle = handle;
		}
	}

	public const int MaxAllowedServerInstances = -1;

	private ConnectionValueTaskSource _reusableConnectionValueTaskSource;

	public NamedPipeServerStream(string pipeName)
		: this(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0, HandleInheritability.None)
	{
	}

	public NamedPipeServerStream(string pipeName, PipeDirection direction)
		: this(pipeName, direction, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0, HandleInheritability.None)
	{
	}

	public NamedPipeServerStream(string pipeName, PipeDirection direction, int maxNumberOfServerInstances)
		: this(pipeName, direction, maxNumberOfServerInstances, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0, HandleInheritability.None)
	{
	}

	public NamedPipeServerStream(string pipeName, PipeDirection direction, int maxNumberOfServerInstances, PipeTransmissionMode transmissionMode)
		: this(pipeName, direction, maxNumberOfServerInstances, transmissionMode, PipeOptions.None, 0, 0, HandleInheritability.None)
	{
	}

	public NamedPipeServerStream(string pipeName, PipeDirection direction, int maxNumberOfServerInstances, PipeTransmissionMode transmissionMode, PipeOptions options)
		: this(pipeName, direction, maxNumberOfServerInstances, transmissionMode, options, 0, 0, HandleInheritability.None)
	{
	}

	public NamedPipeServerStream(string pipeName, PipeDirection direction, int maxNumberOfServerInstances, PipeTransmissionMode transmissionMode, PipeOptions options, int inBufferSize, int outBufferSize)
		: this(pipeName, direction, maxNumberOfServerInstances, transmissionMode, options, inBufferSize, outBufferSize, HandleInheritability.None)
	{
	}

	private NamedPipeServerStream(string pipeName, PipeDirection direction, int maxNumberOfServerInstances, PipeTransmissionMode transmissionMode, PipeOptions options, int inBufferSize, int outBufferSize, HandleInheritability inheritability)
		: base(direction, transmissionMode, outBufferSize)
	{
		ValidateParameters(pipeName, direction, maxNumberOfServerInstances, transmissionMode, options, inBufferSize, outBufferSize, inheritability);
		Create(pipeName, direction, maxNumberOfServerInstances, transmissionMode, options, inBufferSize, outBufferSize, inheritability);
	}

	private void ValidateParameters(string pipeName, PipeDirection direction, int maxNumberOfServerInstances, PipeTransmissionMode transmissionMode, PipeOptions options, int inBufferSize, int outBufferSize, HandleInheritability inheritability)
	{
		if (pipeName == null)
		{
			throw new ArgumentNullException("pipeName");
		}
		if (pipeName.Length == 0)
		{
			throw new ArgumentException(System.SR.Argument_NeedNonemptyPipeName);
		}
		if (direction < PipeDirection.In || direction > PipeDirection.InOut)
		{
			throw new ArgumentOutOfRangeException("direction", System.SR.ArgumentOutOfRange_DirectionModeInOutOrInOut);
		}
		if (transmissionMode < PipeTransmissionMode.Byte || transmissionMode > PipeTransmissionMode.Message)
		{
			throw new ArgumentOutOfRangeException("transmissionMode", System.SR.ArgumentOutOfRange_TransmissionModeByteOrMsg);
		}
		if ((options & (PipeOptions)536870911) != 0)
		{
			throw new ArgumentOutOfRangeException("options", System.SR.ArgumentOutOfRange_OptionsInvalid);
		}
		if (inBufferSize < 0)
		{
			throw new ArgumentOutOfRangeException("inBufferSize", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (outBufferSize < 0)
		{
			throw new ArgumentOutOfRangeException("outBufferSize", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if ((maxNumberOfServerInstances < 1 || maxNumberOfServerInstances > 254) && maxNumberOfServerInstances != -1)
		{
			throw new ArgumentOutOfRangeException("maxNumberOfServerInstances", System.SR.ArgumentOutOfRange_MaxNumServerInstances);
		}
		if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable)
		{
			throw new ArgumentOutOfRangeException("inheritability", System.SR.ArgumentOutOfRange_HandleInheritabilityNoneOrInheritable);
		}
		if ((options & PipeOptions.CurrentUserOnly) != 0)
		{
			base.IsCurrentUserOnly = true;
		}
	}

	public NamedPipeServerStream(PipeDirection direction, bool isAsync, bool isConnected, SafePipeHandle safePipeHandle)
		: base(direction, PipeTransmissionMode.Byte, 0)
	{
		if (safePipeHandle == null)
		{
			throw new ArgumentNullException("safePipeHandle");
		}
		if (safePipeHandle.IsInvalid)
		{
			throw new ArgumentException(System.SR.Argument_InvalidHandle, "safePipeHandle");
		}
		ValidateHandleIsPipe(safePipeHandle);
		InitializeHandle(safePipeHandle, isExposed: true, isAsync);
		if (isConnected)
		{
			base.State = PipeState.Connected;
		}
	}

	~NamedPipeServerStream()
	{
		Dispose(disposing: false);
	}

	public Task WaitForConnectionAsync()
	{
		return WaitForConnectionAsync(CancellationToken.None);
	}

	public IAsyncResult BeginWaitForConnection(AsyncCallback? callback, object? state)
	{
		return System.Threading.Tasks.TaskToApm.Begin(WaitForConnectionAsync(), callback, state);
	}

	public void EndWaitForConnection(IAsyncResult asyncResult)
	{
		System.Threading.Tasks.TaskToApm.End(asyncResult);
	}

	private void CheckConnectOperationsServer()
	{
		if (base.State == PipeState.Closed)
		{
			throw Error.GetPipeNotOpen();
		}
		if (base.InternalHandle != null && base.InternalHandle.IsClosed)
		{
			throw Error.GetPipeNotOpen();
		}
		if (base.State == PipeState.Broken)
		{
			throw new IOException(System.SR.IO_PipeBroken);
		}
	}

	private void CheckDisconnectOperations()
	{
		if (base.State == PipeState.WaitingToConnect)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_PipeNotYetConnected);
		}
		if (base.State == PipeState.Disconnected)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_PipeAlreadyDisconnected);
		}
		if (base.InternalHandle == null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_PipeHandleNotSet);
		}
		if (base.State == PipeState.Closed || (base.InternalHandle != null && base.InternalHandle.IsClosed))
		{
			throw Error.GetPipeNotOpen();
		}
	}

	internal NamedPipeServerStream(string pipeName, PipeDirection direction, int maxNumberOfServerInstances, PipeTransmissionMode transmissionMode, PipeOptions options, int inBufferSize, int outBufferSize, PipeSecurity pipeSecurity, HandleInheritability inheritability = HandleInheritability.None, PipeAccessRights additionalAccessRights = (PipeAccessRights)0)
		: base(direction, transmissionMode, outBufferSize)
	{
		ValidateParameters(pipeName, direction, maxNumberOfServerInstances, transmissionMode, options, inBufferSize, outBufferSize, inheritability);
		if (pipeSecurity != null && base.IsCurrentUserOnly)
		{
			throw new ArgumentException(System.SR.NotSupported_PipeSecurityIsCurrentUserOnly, "pipeSecurity");
		}
		Create(pipeName, direction, maxNumberOfServerInstances, transmissionMode, options, inBufferSize, outBufferSize, pipeSecurity, inheritability, additionalAccessRights);
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			Interlocked.Exchange(ref _reusableConnectionValueTaskSource, null)?.Dispose();
		}
		finally
		{
			base.Dispose(disposing);
		}
	}

	internal override void TryToReuse(PipeValueTaskSource source)
	{
		base.TryToReuse(source);
		if (source is ConnectionValueTaskSource value && Interlocked.CompareExchange(ref _reusableConnectionValueTaskSource, value, null) != null)
		{
			source._preallocatedOverlapped.Dispose();
		}
	}

	private void Create(string pipeName, PipeDirection direction, int maxNumberOfServerInstances, PipeTransmissionMode transmissionMode, PipeOptions options, int inBufferSize, int outBufferSize, HandleInheritability inheritability)
	{
		Create(pipeName, direction, maxNumberOfServerInstances, transmissionMode, options, inBufferSize, outBufferSize, null, inheritability, (PipeAccessRights)0);
	}

	private void Create(string pipeName, PipeDirection direction, int maxNumberOfServerInstances, PipeTransmissionMode transmissionMode, PipeOptions options, int inBufferSize, int outBufferSize, PipeSecurity pipeSecurity, HandleInheritability inheritability, PipeAccessRights additionalAccessRights)
	{
		string fullPath = Path.GetFullPath("\\\\.\\pipe\\" + pipeName);
		if (string.Equals(fullPath, "\\\\.\\pipe\\anonymous", StringComparison.OrdinalIgnoreCase))
		{
			throw new ArgumentOutOfRangeException("pipeName", System.SR.ArgumentOutOfRange_AnonymousReserved);
		}
		if (base.IsCurrentUserOnly)
		{
			using (WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent())
			{
				SecurityIdentifier owner = windowsIdentity.Owner;
				PipeAccessRule rule = new PipeAccessRule(owner, PipeAccessRights.FullControl, AccessControlType.Allow);
				pipeSecurity = new PipeSecurity();
				pipeSecurity.AddAccessRule(rule);
				pipeSecurity.SetOwner(owner);
			}
			options &= ~PipeOptions.CurrentUserOnly;
		}
		int openMode = (int)direction | ((maxNumberOfServerInstances == 1) ? 524288 : 0) | (int)options | (int)additionalAccessRights;
		int pipeMode = ((int)transmissionMode << 2) | ((int)transmissionMode << 1);
		if (maxNumberOfServerInstances == -1)
		{
			maxNumberOfServerInstances = 255;
		}
		GCHandle pinningHandle = default(GCHandle);
		try
		{
			global::Interop.Kernel32.SECURITY_ATTRIBUTES securityAttributes = PipeStream.GetSecAttrs(inheritability, pipeSecurity, ref pinningHandle);
			SafePipeHandle safePipeHandle = global::Interop.Kernel32.CreateNamedPipe(fullPath, openMode, pipeMode, maxNumberOfServerInstances, outBufferSize, inBufferSize, 0, ref securityAttributes);
			if (safePipeHandle.IsInvalid)
			{
				throw System.IO.Win32Marshal.GetExceptionForLastWin32Error();
			}
			InitializeHandle(safePipeHandle, isExposed: false, (options & PipeOptions.Asynchronous) != 0);
		}
		finally
		{
			if (pinningHandle.IsAllocated)
			{
				pinningHandle.Free();
			}
		}
	}

	public void WaitForConnection()
	{
		CheckConnectOperationsServerWithHandle();
		if (base.IsAsync)
		{
			WaitForConnectionCoreAsync(CancellationToken.None).AsTask().GetAwaiter().GetResult();
			return;
		}
		if (!global::Interop.Kernel32.ConnectNamedPipe(base.InternalHandle, IntPtr.Zero))
		{
			int lastPInvokeError = Marshal.GetLastPInvokeError();
			if (lastPInvokeError != 535)
			{
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(lastPInvokeError);
			}
			if (lastPInvokeError == 535 && base.State == PipeState.Connected)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_PipeAlreadyConnected);
			}
		}
		base.State = PipeState.Connected;
	}

	public Task WaitForConnectionAsync(CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		if (!base.IsAsync)
		{
			return Task.Factory.StartNew(delegate(object s)
			{
				((NamedPipeServerStream)s).WaitForConnection();
			}, this, cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}
		return WaitForConnectionCoreAsync(cancellationToken).AsTask();
	}

	public void Disconnect()
	{
		CheckDisconnectOperations();
		if (!global::Interop.Kernel32.DisconnectNamedPipe(base.InternalHandle))
		{
			throw System.IO.Win32Marshal.GetExceptionForLastWin32Error();
		}
		base.State = PipeState.Disconnected;
	}

	public unsafe string GetImpersonationUserName()
	{
		CheckWriteOperations();
		char* ptr = stackalloc char[514];
		if (global::Interop.Kernel32.GetNamedPipeHandleStateW(base.InternalHandle, null, null, null, null, ptr, 514u))
		{
			return new string(ptr);
		}
		return HandleGetImpersonationUserNameError(Marshal.GetLastPInvokeError(), 514u, ptr);
	}

	public void RunAsClient(PipeStreamImpersonationWorker impersonationWorker)
	{
		CheckWriteOperations();
		ExecuteHelper executeHelper = new ExecuteHelper(impersonationWorker, base.InternalHandle);
		bool exceptionThrown = true;
		try
		{
			ImpersonateAndTryCode(executeHelper);
			exceptionThrown = false;
		}
		finally
		{
			RevertImpersonationOnBackout(executeHelper, exceptionThrown);
		}
		if (executeHelper._impersonateErrorCode != 0)
		{
			throw WinIOError(executeHelper._impersonateErrorCode);
		}
		if (executeHelper._revertImpersonateErrorCode != 0)
		{
			throw WinIOError(executeHelper._revertImpersonateErrorCode);
		}
	}

	private static void ImpersonateAndTryCode(object helper)
	{
		ExecuteHelper executeHelper = (ExecuteHelper)helper;
		if (global::Interop.Advapi32.ImpersonateNamedPipeClient(executeHelper._handle))
		{
			executeHelper._mustRevert = true;
		}
		else
		{
			executeHelper._impersonateErrorCode = Marshal.GetLastPInvokeError();
		}
		if (executeHelper._mustRevert)
		{
			executeHelper._userCode();
		}
	}

	private static void RevertImpersonationOnBackout(object helper, bool exceptionThrown)
	{
		ExecuteHelper executeHelper = (ExecuteHelper)helper;
		if (executeHelper._mustRevert && !global::Interop.Advapi32.RevertToSelf())
		{
			executeHelper._revertImpersonateErrorCode = Marshal.GetLastPInvokeError();
		}
	}

	private unsafe ValueTask WaitForConnectionCoreAsync(CancellationToken cancellationToken)
	{
		CheckConnectOperationsServerWithHandle();
		ConnectionValueTaskSource connectionValueTaskSource = Interlocked.Exchange(ref _reusableConnectionValueTaskSource, null) ?? new ConnectionValueTaskSource(this);
		try
		{
			connectionValueTaskSource.PrepareForOperation();
			if (!global::Interop.Kernel32.ConnectNamedPipe(base.InternalHandle, connectionValueTaskSource._overlapped))
			{
				int lastPInvokeError = Marshal.GetLastPInvokeError();
				switch (lastPInvokeError)
				{
				case 997:
					connectionValueTaskSource.RegisterForCancellation(cancellationToken);
					break;
				case 535:
					connectionValueTaskSource.Dispose();
					if (base.State == PipeState.Connected)
					{
						return ValueTask.FromException(ExceptionDispatchInfo.SetCurrentStackTrace(new InvalidOperationException(System.SR.InvalidOperation_PipeAlreadyConnected)));
					}
					base.State = PipeState.Connected;
					return ValueTask.CompletedTask;
				default:
					connectionValueTaskSource.Dispose();
					return ValueTask.FromException(ExceptionDispatchInfo.SetCurrentStackTrace(System.IO.Win32Marshal.GetExceptionForWin32Error(lastPInvokeError)));
				}
			}
		}
		catch
		{
			connectionValueTaskSource.Dispose();
			throw;
		}
		connectionValueTaskSource.FinishedScheduling();
		return new ValueTask(connectionValueTaskSource, connectionValueTaskSource.Version);
	}

	private void CheckConnectOperationsServerWithHandle()
	{
		if (base.InternalHandle == null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_PipeHandleNotSet);
		}
		CheckConnectOperationsServer();
	}

	private unsafe string HandleGetImpersonationUserNameError(int error, uint userNameMaxLength, char* userName)
	{
		if ((error == 0 || error == 1368) && Environment.Is64BitProcess)
		{
			global::Interop.Kernel32.LoadLibraryEx("sspicli.dll", IntPtr.Zero, 2048);
			if (global::Interop.Kernel32.GetNamedPipeHandleStateW(base.InternalHandle, null, null, null, null, userName, userNameMaxLength))
			{
				return new string(userName);
			}
			error = Marshal.GetLastPInvokeError();
		}
		throw WinIOError(error);
	}
}
