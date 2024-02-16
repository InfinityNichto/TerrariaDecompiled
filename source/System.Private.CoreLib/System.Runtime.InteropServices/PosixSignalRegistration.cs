using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices;

public sealed class PosixSignalRegistration : IDisposable
{
	private sealed class Token
	{
		public PosixSignal Signal { get; }

		public Action<PosixSignalContext> Handler { get; }

		public Token(PosixSignal signal, Action<PosixSignalContext> handler)
		{
			Signal = signal;
			Handler = handler;
		}
	}

	private Token _token;

	private static readonly HashSet<Token> s_registrations = new HashSet<Token>();

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static PosixSignalRegistration Create(PosixSignal signal, Action<PosixSignalContext> handler)
	{
		if (handler == null)
		{
			throw new ArgumentNullException("handler");
		}
		return Register(signal, handler);
	}

	private PosixSignalRegistration(Token token)
	{
		_token = token;
	}

	public void Dispose()
	{
		Unregister();
		GC.SuppressFinalize(this);
	}

	~PosixSignalRegistration()
	{
		Unregister();
	}

	private unsafe static PosixSignalRegistration Register(PosixSignal signal, Action<PosixSignalContext> handler)
	{
		if ((uint)(signal - -4) > 3u)
		{
			throw new PlatformNotSupportedException();
		}
		Token token = new Token(signal, handler);
		PosixSignalRegistration result = new PosixSignalRegistration(token);
		lock (s_registrations)
		{
			if (s_registrations.Count == 0 && !Interop.Kernel32.SetConsoleCtrlHandler((delegate* unmanaged<int, Interop.BOOL>)(delegate*<int, Interop.BOOL>)(&HandlerRoutine), Add: true))
			{
				throw Win32Marshal.GetExceptionForLastWin32Error();
			}
			s_registrations.Add(token);
			return result;
		}
	}

	private unsafe void Unregister()
	{
		lock (s_registrations)
		{
			Token token = _token;
			if (token != null)
			{
				_token = null;
				s_registrations.Remove(token);
				if (s_registrations.Count == 0 && !Interop.Kernel32.SetConsoleCtrlHandler((delegate* unmanaged<int, Interop.BOOL>)(delegate*<int, Interop.BOOL>)(&HandlerRoutine), Add: false))
				{
					throw Win32Marshal.GetExceptionForLastWin32Error();
				}
			}
		}
	}

	[UnmanagedCallersOnly]
	private static Interop.BOOL HandlerRoutine(int dwCtrlType)
	{
		PosixSignal posixSignal;
		switch (dwCtrlType)
		{
		case 0:
			posixSignal = PosixSignal.SIGINT;
			break;
		case 1:
			posixSignal = PosixSignal.SIGQUIT;
			break;
		case 6:
			posixSignal = PosixSignal.SIGTERM;
			break;
		case 2:
			posixSignal = PosixSignal.SIGHUP;
			break;
		default:
			return Interop.BOOL.FALSE;
		}
		List<Token> list = null;
		lock (s_registrations)
		{
			foreach (Token s_registration in s_registrations)
			{
				if (s_registration.Signal == posixSignal)
				{
					(list ?? (list = new List<Token>())).Add(s_registration);
				}
			}
		}
		if (list == null)
		{
			return Interop.BOOL.FALSE;
		}
		PosixSignalContext posixSignalContext = new PosixSignalContext(posixSignal);
		foreach (Token item in list)
		{
			item.Handler(posixSignalContext);
		}
		if (!posixSignalContext.Cancel)
		{
			return Interop.BOOL.FALSE;
		}
		return Interop.BOOL.TRUE;
	}
}
