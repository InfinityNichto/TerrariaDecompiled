using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Security.AccessControl;

internal sealed class Privilege
{
	private sealed class TlsContents : IDisposable
	{
		private bool disposed;

		private int referenceCount = 1;

		private SafeTokenHandle threadHandle = new SafeTokenHandle(IntPtr.Zero);

		private readonly bool isImpersonating;

		private static volatile SafeTokenHandle processHandle = new SafeTokenHandle(IntPtr.Zero);

		private static readonly object syncRoot = new object();

		public int ReferenceCountValue => referenceCount;

		public SafeTokenHandle ThreadHandle => threadHandle;

		public bool IsImpersonating => isImpersonating;

		public TlsContents()
		{
			int num = 0;
			int num2 = 0;
			bool flag = true;
			if (processHandle.IsInvalid)
			{
				lock (syncRoot)
				{
					if (processHandle.IsInvalid)
					{
						if (!global::Interop.Advapi32.OpenProcessToken(global::Interop.Kernel32.GetCurrentProcess(), TokenAccessLevels.Duplicate, out var TokenHandle))
						{
							num2 = Marshal.GetLastWin32Error();
							flag = false;
						}
						processHandle = TokenHandle;
					}
				}
			}
			try
			{
				SafeTokenHandle safeTokenHandle = threadHandle;
				num = System.Security.Principal.Win32.OpenThreadToken(TokenAccessLevels.Query | TokenAccessLevels.AdjustPrivileges, System.Security.Principal.WinSecurityContext.Process, out threadHandle);
				num &= 0x7FF8FFFF;
				if (num != 0)
				{
					if (flag)
					{
						threadHandle = safeTokenHandle;
						if (num != 1008)
						{
							flag = false;
						}
						if (flag)
						{
							num = 0;
							if (!global::Interop.Advapi32.DuplicateTokenEx(processHandle, TokenAccessLevels.Impersonate | TokenAccessLevels.Query | TokenAccessLevels.AdjustPrivileges, IntPtr.Zero, global::Interop.Advapi32.SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, System.Security.Principal.TokenType.TokenImpersonation, ref threadHandle))
							{
								num = Marshal.GetLastWin32Error();
								flag = false;
							}
						}
						if (flag)
						{
							num = System.Security.Principal.Win32.SetThreadToken(threadHandle);
							num &= 0x7FF8FFFF;
							if (num != 0)
							{
								flag = false;
							}
						}
						if (flag)
						{
							isImpersonating = true;
						}
					}
					else
					{
						num = num2;
					}
				}
				else
				{
					flag = true;
				}
			}
			finally
			{
				if (!flag)
				{
					Dispose();
				}
			}
			switch (num)
			{
			case 8:
				throw new OutOfMemoryException();
			case 5:
			case 1347:
				throw new UnauthorizedAccessException();
			default:
				throw new InvalidOperationException();
			case 0:
				break;
			}
		}

		~TlsContents()
		{
			if (!disposed)
			{
				Dispose(disposing: false);
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing && threadHandle != null)
				{
					threadHandle.Dispose();
					threadHandle = null;
				}
				if (isImpersonating)
				{
					global::Interop.Advapi32.RevertToSelf();
				}
				disposed = true;
			}
		}

		public void IncrementReferenceCount()
		{
			referenceCount++;
		}

		public int DecrementReferenceCount()
		{
			int num = --referenceCount;
			if (num == 0)
			{
				Dispose();
			}
			return num;
		}
	}

	[ThreadStatic]
	private static TlsContents t_tlsSlotData;

	private static readonly Dictionary<global::Interop.Advapi32.LUID, string> privileges = new Dictionary<global::Interop.Advapi32.LUID, string>();

	private static readonly Dictionary<string, global::Interop.Advapi32.LUID> luids = new Dictionary<string, global::Interop.Advapi32.LUID>();

	private static readonly ReaderWriterLockSlim privilegeLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

	private bool needToRevert;

	private bool initialState;

	private bool stateWasChanged;

	private global::Interop.Advapi32.LUID luid;

	private readonly Thread currentThread = Thread.CurrentThread;

	private TlsContents tlsContents;

	public bool NeedToRevert => needToRevert;

	private static global::Interop.Advapi32.LUID LuidFromPrivilege(string privilege)
	{
		Unsafe.SkipInit(out global::Interop.Advapi32.LUID lpLuid);
		lpLuid.LowPart = 0;
		lpLuid.HighPart = 0;
		try
		{
			privilegeLock.EnterReadLock();
			if (luids.ContainsKey(privilege))
			{
				lpLuid = luids[privilege];
				privilegeLock.ExitReadLock();
			}
			else
			{
				privilegeLock.ExitReadLock();
				if (!global::Interop.Advapi32.LookupPrivilegeValue(null, privilege, out lpLuid))
				{
					switch (Marshal.GetLastWin32Error())
					{
					case 8:
						throw new OutOfMemoryException();
					case 5:
						throw new UnauthorizedAccessException();
					case 1313:
						throw new ArgumentException(System.SR.Format(System.SR.Argument_InvalidPrivilegeName, privilege));
					default:
						throw new InvalidOperationException();
					}
				}
				privilegeLock.EnterWriteLock();
			}
		}
		finally
		{
			if (privilegeLock.IsReadLockHeld)
			{
				privilegeLock.ExitReadLock();
			}
			if (privilegeLock.IsWriteLockHeld)
			{
				if (!luids.ContainsKey(privilege))
				{
					luids[privilege] = lpLuid;
					privileges[lpLuid] = privilege;
				}
				privilegeLock.ExitWriteLock();
			}
		}
		return lpLuid;
	}

	public Privilege(string privilegeName)
	{
		if (privilegeName == null)
		{
			throw new ArgumentNullException("privilegeName");
		}
		luid = LuidFromPrivilege(privilegeName);
	}

	~Privilege()
	{
		if (needToRevert)
		{
			Revert();
		}
	}

	public void Enable()
	{
		ToggleState(enable: true);
	}

	private unsafe void ToggleState(bool enable)
	{
		int num = 0;
		if (!currentThread.Equals(Thread.CurrentThread))
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_MustBeSameThread);
		}
		if (needToRevert)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_MustRevertPrivilege);
		}
		try
		{
			tlsContents = t_tlsSlotData;
			if (tlsContents == null)
			{
				tlsContents = new TlsContents();
				t_tlsSlotData = tlsContents;
			}
			else
			{
				tlsContents.IncrementReferenceCount();
			}
			Unsafe.SkipInit(out global::Interop.Advapi32.TOKEN_PRIVILEGE tOKEN_PRIVILEGE);
			tOKEN_PRIVILEGE.PrivilegeCount = 1u;
			tOKEN_PRIVILEGE.Privileges.Luid = luid;
			tOKEN_PRIVILEGE.Privileges.Attributes = (enable ? 2u : 0u);
			global::Interop.Advapi32.TOKEN_PRIVILEGE tOKEN_PRIVILEGE2 = default(global::Interop.Advapi32.TOKEN_PRIVILEGE);
			uint num2 = 0u;
			if (!global::Interop.Advapi32.AdjustTokenPrivileges(tlsContents.ThreadHandle, DisableAllPrivileges: false, &tOKEN_PRIVILEGE, (uint)sizeof(global::Interop.Advapi32.TOKEN_PRIVILEGE), &tOKEN_PRIVILEGE2, &num2))
			{
				num = Marshal.GetLastWin32Error();
			}
			else if (1300 == Marshal.GetLastWin32Error())
			{
				num = 1300;
			}
			else
			{
				initialState = (tOKEN_PRIVILEGE2.Privileges.Attributes & 2) != 0;
				stateWasChanged = initialState != enable;
				needToRevert = tlsContents.IsImpersonating || stateWasChanged;
			}
		}
		finally
		{
			if (!needToRevert)
			{
				Reset();
			}
		}
		switch (num)
		{
		case 1300:
			throw new PrivilegeNotHeldException(privileges[luid]);
		case 8:
			throw new OutOfMemoryException();
		case 5:
		case 1347:
			throw new UnauthorizedAccessException();
		default:
			throw new InvalidOperationException();
		case 0:
			break;
		}
	}

	public unsafe void Revert()
	{
		int num = 0;
		if (!currentThread.Equals(Thread.CurrentThread))
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_MustBeSameThread);
		}
		if (!NeedToRevert)
		{
			return;
		}
		bool flag = true;
		try
		{
			if (stateWasChanged && (tlsContents.ReferenceCountValue > 1 || !tlsContents.IsImpersonating))
			{
				Unsafe.SkipInit(out global::Interop.Advapi32.TOKEN_PRIVILEGE tOKEN_PRIVILEGE);
				tOKEN_PRIVILEGE.PrivilegeCount = 1u;
				tOKEN_PRIVILEGE.Privileges.Luid = luid;
				tOKEN_PRIVILEGE.Privileges.Attributes = (initialState ? 2u : 0u);
				if (!global::Interop.Advapi32.AdjustTokenPrivileges(tlsContents.ThreadHandle, DisableAllPrivileges: false, &tOKEN_PRIVILEGE, 0u, null, null))
				{
					num = Marshal.GetLastWin32Error();
					flag = false;
				}
			}
		}
		finally
		{
			if (flag)
			{
				Reset();
			}
		}
		switch (num)
		{
		case 8:
			throw new OutOfMemoryException();
		case 5:
			throw new UnauthorizedAccessException();
		default:
			throw new InvalidOperationException();
		case 0:
			break;
		}
	}

	private void Reset()
	{
		stateWasChanged = false;
		initialState = false;
		needToRevert = false;
		if (tlsContents != null && tlsContents.DecrementReferenceCount() == 0)
		{
			tlsContents = null;
			t_tlsSlotData = null;
		}
	}
}
