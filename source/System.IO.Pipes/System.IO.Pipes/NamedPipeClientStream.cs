using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes;

public sealed class NamedPipeClientStream : PipeStream
{
	private readonly string _normalizedPipePath;

	private readonly TokenImpersonationLevel _impersonationLevel;

	private readonly PipeOptions _pipeOptions;

	private readonly HandleInheritability _inheritability;

	private readonly PipeDirection _direction;

	[SupportedOSPlatform("windows")]
	public unsafe int NumberOfServerInstances
	{
		get
		{
			CheckPipePropertyOperations();
			Unsafe.SkipInit(out uint result);
			if (!global::Interop.Kernel32.GetNamedPipeHandleStateW(base.InternalHandle, null, &result, null, null, null, 0u))
			{
				throw WinIOError(Marshal.GetLastPInvokeError());
			}
			return (int)result;
		}
	}

	public NamedPipeClientStream(string pipeName)
		: this(".", pipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.None, HandleInheritability.None)
	{
	}

	public NamedPipeClientStream(string serverName, string pipeName)
		: this(serverName, pipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.None, HandleInheritability.None)
	{
	}

	public NamedPipeClientStream(string serverName, string pipeName, PipeDirection direction)
		: this(serverName, pipeName, direction, PipeOptions.None, TokenImpersonationLevel.None, HandleInheritability.None)
	{
	}

	public NamedPipeClientStream(string serverName, string pipeName, PipeDirection direction, PipeOptions options)
		: this(serverName, pipeName, direction, options, TokenImpersonationLevel.None, HandleInheritability.None)
	{
	}

	public NamedPipeClientStream(string serverName, string pipeName, PipeDirection direction, PipeOptions options, TokenImpersonationLevel impersonationLevel)
		: this(serverName, pipeName, direction, options, impersonationLevel, HandleInheritability.None)
	{
	}

	public NamedPipeClientStream(string serverName, string pipeName, PipeDirection direction, PipeOptions options, TokenImpersonationLevel impersonationLevel, HandleInheritability inheritability)
		: base(direction, 0)
	{
		if (pipeName == null)
		{
			throw new ArgumentNullException("pipeName");
		}
		if (serverName == null)
		{
			throw new ArgumentNullException("serverName", System.SR.ArgumentNull_ServerName);
		}
		if (pipeName.Length == 0)
		{
			throw new ArgumentException(System.SR.Argument_NeedNonemptyPipeName);
		}
		if (serverName.Length == 0)
		{
			throw new ArgumentException(System.SR.Argument_EmptyServerName);
		}
		if ((options & (PipeOptions)536870911) != 0)
		{
			throw new ArgumentOutOfRangeException("options", System.SR.ArgumentOutOfRange_OptionsInvalid);
		}
		if (impersonationLevel < TokenImpersonationLevel.None || impersonationLevel > TokenImpersonationLevel.Delegation)
		{
			throw new ArgumentOutOfRangeException("impersonationLevel", System.SR.ArgumentOutOfRange_ImpersonationInvalid);
		}
		if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable)
		{
			throw new ArgumentOutOfRangeException("inheritability", System.SR.ArgumentOutOfRange_HandleInheritabilityNoneOrInheritable);
		}
		if ((options & PipeOptions.CurrentUserOnly) != 0)
		{
			base.IsCurrentUserOnly = true;
		}
		_normalizedPipePath = PipeStream.GetPipePath(serverName, pipeName);
		_direction = direction;
		_inheritability = inheritability;
		_impersonationLevel = impersonationLevel;
		_pipeOptions = options;
	}

	public NamedPipeClientStream(PipeDirection direction, bool isAsync, bool isConnected, SafePipeHandle safePipeHandle)
		: base(direction, 0)
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

	~NamedPipeClientStream()
	{
		Dispose(disposing: false);
	}

	public void Connect()
	{
		Connect(-1);
	}

	public void Connect(int timeout)
	{
		CheckConnectOperationsClient();
		if (timeout < 0 && timeout != -1)
		{
			throw new ArgumentOutOfRangeException("timeout", System.SR.ArgumentOutOfRange_InvalidTimeout);
		}
		ConnectInternal(timeout, CancellationToken.None, Environment.TickCount);
	}

	private void ConnectInternal(int timeout, CancellationToken cancellationToken, int startTime)
	{
		int num = 0;
		SpinWait spinWait = default(SpinWait);
		do
		{
			cancellationToken.ThrowIfCancellationRequested();
			int num2 = timeout - num;
			if (cancellationToken.CanBeCanceled && num2 > 50)
			{
				num2 = 50;
			}
			if (TryConnect(num2, cancellationToken))
			{
				return;
			}
			spinWait.SpinOnce();
		}
		while (timeout == -1 || (num = Environment.TickCount - startTime) < timeout);
		throw new TimeoutException();
	}

	public Task ConnectAsync()
	{
		return ConnectAsync(-1, CancellationToken.None);
	}

	public Task ConnectAsync(int timeout)
	{
		return ConnectAsync(timeout, CancellationToken.None);
	}

	public Task ConnectAsync(CancellationToken cancellationToken)
	{
		return ConnectAsync(-1, cancellationToken);
	}

	public Task ConnectAsync(int timeout, CancellationToken cancellationToken)
	{
		CheckConnectOperationsClient();
		if (timeout < 0 && timeout != -1)
		{
			throw new ArgumentOutOfRangeException("timeout", System.SR.ArgumentOutOfRange_InvalidTimeout);
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		int startTime = Environment.TickCount;
		return Task.Run(delegate
		{
			ConnectInternal(timeout, cancellationToken, startTime);
		}, cancellationToken);
	}

	protected internal override void CheckPipePropertyOperations()
	{
		base.CheckPipePropertyOperations();
		if (base.State == PipeState.WaitingToConnect)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_PipeNotYetConnected);
		}
		if (base.State == PipeState.Broken)
		{
			throw new IOException(System.SR.IO_PipeBroken);
		}
	}

	private void CheckConnectOperationsClient()
	{
		if (base.State == PipeState.Connected)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_PipeAlreadyConnected);
		}
		if (base.State == PipeState.Closed)
		{
			throw Error.GetPipeNotOpen();
		}
	}

	private bool TryConnect(int timeout, CancellationToken cancellationToken)
	{
		global::Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = PipeStream.GetSecAttrs(_inheritability);
		int num = (int)(_pipeOptions & ~PipeOptions.CurrentUserOnly);
		if (_impersonationLevel != 0)
		{
			num |= 0x100000;
			num |= (int)(_impersonationLevel - 1) << 16;
		}
		int num2 = 0;
		if ((PipeDirection.In & _direction) != 0)
		{
			num2 |= int.MinValue;
		}
		if ((PipeDirection.Out & _direction) != 0)
		{
			num2 |= 0x40000000;
		}
		SafePipeHandle safePipeHandle = global::Interop.Kernel32.CreateNamedPipeClient(_normalizedPipePath, num2, FileShare.None, ref secAttrs, FileMode.Open, num, IntPtr.Zero);
		if (safePipeHandle.IsInvalid)
		{
			int lastPInvokeError = Marshal.GetLastPInvokeError();
			if (lastPInvokeError != 231 && lastPInvokeError != 2)
			{
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(lastPInvokeError);
			}
			if (!global::Interop.Kernel32.WaitNamedPipe(_normalizedPipePath, timeout))
			{
				lastPInvokeError = Marshal.GetLastPInvokeError();
				if (lastPInvokeError == 2 || lastPInvokeError == 121)
				{
					return false;
				}
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(lastPInvokeError);
			}
			safePipeHandle = global::Interop.Kernel32.CreateNamedPipeClient(_normalizedPipePath, num2, FileShare.None, ref secAttrs, FileMode.Open, num, IntPtr.Zero);
			if (safePipeHandle.IsInvalid)
			{
				lastPInvokeError = Marshal.GetLastPInvokeError();
				if (lastPInvokeError == 231)
				{
					return false;
				}
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(lastPInvokeError);
			}
		}
		InitializeHandle(safePipeHandle, isExposed: false, (_pipeOptions & PipeOptions.Asynchronous) != 0);
		base.State = PipeState.Connected;
		ValidateRemotePipeUser();
		return true;
	}

	private void ValidateRemotePipeUser()
	{
		if (!base.IsCurrentUserOnly)
		{
			return;
		}
		PipeSecurity accessControl = this.GetAccessControl();
		IdentityReference owner = accessControl.GetOwner(typeof(SecurityIdentifier));
		using WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();
		SecurityIdentifier owner2 = windowsIdentity.Owner;
		if (owner != owner2)
		{
			base.State = PipeState.Closed;
			throw new UnauthorizedAccessException(System.SR.UnauthorizedAccess_NotOwnedByCurrentUser);
		}
	}
}
