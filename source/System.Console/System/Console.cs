using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

namespace System;

public static class Console
{
	private static readonly object s_syncObject = new object();

	private static TextReader s_in;

	private static TextWriter s_out;

	private static TextWriter s_error;

	private static Encoding s_inputEncoding;

	private static Encoding s_outputEncoding;

	private static bool s_isOutTextWriterRedirected;

	private static bool s_isErrorTextWriterRedirected;

	private static ConsoleCancelEventHandler s_cancelCallbacks;

	private static PosixSignalRegistration s_sigIntRegistration;

	private static PosixSignalRegistration s_sigQuitRegistration;

	private static StrongBox<bool> _isStdInRedirected;

	private static StrongBox<bool> _isStdOutRedirected;

	private static StrongBox<bool> _isStdErrRedirected;

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static TextReader In
	{
		get
		{
			return Volatile.Read(ref s_in) ?? EnsureInitialized();
			static TextReader EnsureInitialized()
			{
				ConsolePal.EnsureConsoleInitialized();
				lock (s_syncObject)
				{
					if (s_in == null)
					{
						Volatile.Write(ref s_in, ConsolePal.GetOrCreateReader());
					}
					return s_in;
				}
			}
		}
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static Encoding InputEncoding
	{
		get
		{
			Encoding encoding = Volatile.Read(ref s_inputEncoding);
			if (encoding == null)
			{
				lock (s_syncObject)
				{
					if (s_inputEncoding == null)
					{
						Volatile.Write(ref s_inputEncoding, ConsolePal.InputEncoding);
					}
					encoding = s_inputEncoding;
				}
			}
			return encoding;
		}
		set
		{
			CheckNonNull(value, "value");
			lock (s_syncObject)
			{
				ConsolePal.SetConsoleInputEncoding(value);
				Volatile.Write(ref s_inputEncoding, (Encoding)value.Clone());
				Volatile.Write(ref s_in, null);
			}
		}
	}

	public static Encoding OutputEncoding
	{
		get
		{
			Encoding encoding = Volatile.Read(ref s_outputEncoding);
			if (encoding == null)
			{
				lock (s_syncObject)
				{
					if (s_outputEncoding == null)
					{
						Volatile.Write(ref s_outputEncoding, ConsolePal.OutputEncoding);
					}
					encoding = s_outputEncoding;
				}
			}
			return encoding;
		}
		[UnsupportedOSPlatform("android")]
		[UnsupportedOSPlatform("ios")]
		[UnsupportedOSPlatform("tvos")]
		set
		{
			CheckNonNull(value, "value");
			lock (s_syncObject)
			{
				ConsolePal.SetConsoleOutputEncoding(value);
				if (s_out != null && !s_isOutTextWriterRedirected)
				{
					s_out.Flush();
					Volatile.Write(ref s_out, null);
				}
				if (s_error != null && !s_isErrorTextWriterRedirected)
				{
					s_error.Flush();
					Volatile.Write(ref s_error, null);
				}
				Volatile.Write(ref s_outputEncoding, (Encoding)value.Clone());
			}
		}
	}

	public static bool KeyAvailable
	{
		get
		{
			if (IsInputRedirected)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_ConsoleKeyAvailableOnFile);
			}
			return ConsolePal.KeyAvailable;
		}
	}

	public static TextWriter Out
	{
		get
		{
			return Volatile.Read(ref s_out) ?? EnsureInitialized();
			static TextWriter EnsureInitialized()
			{
				lock (s_syncObject)
				{
					if (s_out == null)
					{
						Volatile.Write(ref s_out, CreateOutputWriter(ConsolePal.OpenStandardOutput()));
					}
					return s_out;
				}
			}
		}
	}

	public static TextWriter Error
	{
		get
		{
			return Volatile.Read(ref s_error) ?? EnsureInitialized();
			static TextWriter EnsureInitialized()
			{
				lock (s_syncObject)
				{
					if (s_error == null)
					{
						Volatile.Write(ref s_error, CreateOutputWriter(ConsolePal.OpenStandardError()));
					}
					return s_error;
				}
			}
		}
	}

	public static bool IsInputRedirected
	{
		get
		{
			StrongBox<bool> strongBox = Volatile.Read(ref _isStdInRedirected) ?? EnsureInitialized();
			return strongBox.Value;
			static StrongBox<bool> EnsureInitialized()
			{
				Volatile.Write(ref _isStdInRedirected, new StrongBox<bool>(ConsolePal.IsInputRedirectedCore()));
				return _isStdInRedirected;
			}
		}
	}

	public static bool IsOutputRedirected
	{
		get
		{
			StrongBox<bool> strongBox = Volatile.Read(ref _isStdOutRedirected) ?? EnsureInitialized();
			return strongBox.Value;
			static StrongBox<bool> EnsureInitialized()
			{
				Volatile.Write(ref _isStdOutRedirected, new StrongBox<bool>(ConsolePal.IsOutputRedirectedCore()));
				return _isStdOutRedirected;
			}
		}
	}

	public static bool IsErrorRedirected
	{
		get
		{
			StrongBox<bool> strongBox = Volatile.Read(ref _isStdErrRedirected) ?? EnsureInitialized();
			return strongBox.Value;
			static StrongBox<bool> EnsureInitialized()
			{
				Volatile.Write(ref _isStdErrRedirected, new StrongBox<bool>(ConsolePal.IsErrorRedirectedCore()));
				return _isStdErrRedirected;
			}
		}
	}

	public static int CursorSize
	{
		[UnsupportedOSPlatform("android")]
		[UnsupportedOSPlatform("browser")]
		[UnsupportedOSPlatform("ios")]
		[UnsupportedOSPlatform("tvos")]
		get
		{
			return ConsolePal.CursorSize;
		}
		[SupportedOSPlatform("windows")]
		set
		{
			ConsolePal.CursorSize = value;
		}
	}

	[SupportedOSPlatform("windows")]
	public static bool NumberLock => ConsolePal.NumberLock;

	[SupportedOSPlatform("windows")]
	public static bool CapsLock => ConsolePal.CapsLock;

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static ConsoleColor BackgroundColor
	{
		get
		{
			return ConsolePal.BackgroundColor;
		}
		set
		{
			ConsolePal.BackgroundColor = value;
		}
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static ConsoleColor ForegroundColor
	{
		get
		{
			return ConsolePal.ForegroundColor;
		}
		set
		{
			ConsolePal.ForegroundColor = value;
		}
	}

	public static int BufferWidth
	{
		[UnsupportedOSPlatform("android")]
		[UnsupportedOSPlatform("browser")]
		[UnsupportedOSPlatform("ios")]
		[UnsupportedOSPlatform("tvos")]
		get
		{
			return ConsolePal.BufferWidth;
		}
		[SupportedOSPlatform("windows")]
		set
		{
			ConsolePal.BufferWidth = value;
		}
	}

	public static int BufferHeight
	{
		[UnsupportedOSPlatform("android")]
		[UnsupportedOSPlatform("browser")]
		[UnsupportedOSPlatform("ios")]
		[UnsupportedOSPlatform("tvos")]
		get
		{
			return ConsolePal.BufferHeight;
		}
		[SupportedOSPlatform("windows")]
		set
		{
			ConsolePal.BufferHeight = value;
		}
	}

	public static int WindowLeft
	{
		get
		{
			return ConsolePal.WindowLeft;
		}
		[SupportedOSPlatform("windows")]
		set
		{
			ConsolePal.WindowLeft = value;
		}
	}

	public static int WindowTop
	{
		get
		{
			return ConsolePal.WindowTop;
		}
		[SupportedOSPlatform("windows")]
		set
		{
			ConsolePal.WindowTop = value;
		}
	}

	public static int WindowWidth
	{
		[UnsupportedOSPlatform("android")]
		[UnsupportedOSPlatform("browser")]
		[UnsupportedOSPlatform("ios")]
		[UnsupportedOSPlatform("tvos")]
		get
		{
			return ConsolePal.WindowWidth;
		}
		[SupportedOSPlatform("windows")]
		set
		{
			ConsolePal.WindowWidth = value;
		}
	}

	public static int WindowHeight
	{
		[UnsupportedOSPlatform("android")]
		[UnsupportedOSPlatform("browser")]
		[UnsupportedOSPlatform("ios")]
		[UnsupportedOSPlatform("tvos")]
		get
		{
			return ConsolePal.WindowHeight;
		}
		[SupportedOSPlatform("windows")]
		set
		{
			ConsolePal.WindowHeight = value;
		}
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static int LargestWindowWidth => ConsolePal.LargestWindowWidth;

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static int LargestWindowHeight => ConsolePal.LargestWindowHeight;

	public static bool CursorVisible
	{
		[SupportedOSPlatform("windows")]
		get
		{
			return ConsolePal.CursorVisible;
		}
		[UnsupportedOSPlatform("android")]
		[UnsupportedOSPlatform("browser")]
		[UnsupportedOSPlatform("ios")]
		[UnsupportedOSPlatform("tvos")]
		set
		{
			ConsolePal.CursorVisible = value;
		}
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static int CursorLeft
	{
		get
		{
			return ConsolePal.GetCursorPosition().Left;
		}
		set
		{
			SetCursorPosition(value, CursorTop);
		}
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static int CursorTop
	{
		get
		{
			return ConsolePal.GetCursorPosition().Top;
		}
		set
		{
			SetCursorPosition(CursorLeft, value);
		}
	}

	public static string Title
	{
		[SupportedOSPlatform("windows")]
		get
		{
			return ConsolePal.Title;
		}
		[UnsupportedOSPlatform("android")]
		[UnsupportedOSPlatform("browser")]
		[UnsupportedOSPlatform("ios")]
		[UnsupportedOSPlatform("tvos")]
		set
		{
			ConsolePal.Title = value ?? throw new ArgumentNullException("value");
		}
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static bool TreatControlCAsInput
	{
		get
		{
			return ConsolePal.TreatControlCAsInput;
		}
		set
		{
			ConsolePal.TreatControlCAsInput = value;
		}
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static event ConsoleCancelEventHandler? CancelKeyPress
	{
		add
		{
			ConsolePal.EnsureConsoleInitialized();
			lock (s_syncObject)
			{
				s_cancelCallbacks = (ConsoleCancelEventHandler)Delegate.Combine(s_cancelCallbacks, value);
				if (s_sigIntRegistration == null)
				{
					Action<PosixSignalContext> handler = HandlePosixSignal;
					s_sigIntRegistration = PosixSignalRegistration.Create(PosixSignal.SIGINT, handler);
					s_sigQuitRegistration = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, handler);
				}
			}
		}
		remove
		{
			lock (s_syncObject)
			{
				s_cancelCallbacks = (ConsoleCancelEventHandler)Delegate.Remove(s_cancelCallbacks, value);
				if (s_cancelCallbacks == null)
				{
					s_sigIntRegistration?.Dispose();
					s_sigQuitRegistration?.Dispose();
					s_sigIntRegistration = (s_sigQuitRegistration = null);
				}
			}
		}
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static ConsoleKeyInfo ReadKey()
	{
		return ConsolePal.ReadKey(intercept: false);
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static ConsoleKeyInfo ReadKey(bool intercept)
	{
		return ConsolePal.ReadKey(intercept);
	}

	private static TextWriter CreateOutputWriter(Stream outputStream)
	{
		return TextWriter.Synchronized((outputStream == Stream.Null) ? StreamWriter.Null : new StreamWriter(outputStream, OutputEncoding.RemovePreamble(), 256, leaveOpen: true)
		{
			AutoFlush = true
		});
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static void ResetColor()
	{
		ConsolePal.ResetColor();
	}

	[SupportedOSPlatform("windows")]
	public static void SetBufferSize(int width, int height)
	{
		ConsolePal.SetBufferSize(width, height);
	}

	[SupportedOSPlatform("windows")]
	public static void SetWindowPosition(int left, int top)
	{
		ConsolePal.SetWindowPosition(left, top);
	}

	[SupportedOSPlatform("windows")]
	public static void SetWindowSize(int width, int height)
	{
		ConsolePal.SetWindowSize(width, height);
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static (int Left, int Top) GetCursorPosition()
	{
		return ConsolePal.GetCursorPosition();
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static void Beep()
	{
		ConsolePal.Beep();
	}

	[SupportedOSPlatform("windows")]
	public static void Beep(int frequency, int duration)
	{
		ConsolePal.Beep(frequency, duration);
	}

	[SupportedOSPlatform("windows")]
	public static void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop)
	{
		ConsolePal.MoveBufferArea(sourceLeft, sourceTop, sourceWidth, sourceHeight, targetLeft, targetTop, ' ', ConsoleColor.Black, BackgroundColor);
	}

	[SupportedOSPlatform("windows")]
	public static void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop, char sourceChar, ConsoleColor sourceForeColor, ConsoleColor sourceBackColor)
	{
		ConsolePal.MoveBufferArea(sourceLeft, sourceTop, sourceWidth, sourceHeight, targetLeft, targetTop, sourceChar, sourceForeColor, sourceBackColor);
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static void Clear()
	{
		ConsolePal.Clear();
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static void SetCursorPosition(int left, int top)
	{
		if (left < 0 || left >= 32767)
		{
			throw new ArgumentOutOfRangeException("left", left, System.SR.ArgumentOutOfRange_ConsoleBufferBoundaries);
		}
		if (top < 0 || top >= 32767)
		{
			throw new ArgumentOutOfRangeException("top", top, System.SR.ArgumentOutOfRange_ConsoleBufferBoundaries);
		}
		ConsolePal.SetCursorPosition(left, top);
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static Stream OpenStandardInput()
	{
		return ConsolePal.OpenStandardInput();
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	public static Stream OpenStandardInput(int bufferSize)
	{
		if (bufferSize < 0)
		{
			throw new ArgumentOutOfRangeException("bufferSize", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		return ConsolePal.OpenStandardInput();
	}

	public static Stream OpenStandardOutput()
	{
		return ConsolePal.OpenStandardOutput();
	}

	public static Stream OpenStandardOutput(int bufferSize)
	{
		if (bufferSize < 0)
		{
			throw new ArgumentOutOfRangeException("bufferSize", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		return ConsolePal.OpenStandardOutput();
	}

	public static Stream OpenStandardError()
	{
		return ConsolePal.OpenStandardError();
	}

	public static Stream OpenStandardError(int bufferSize)
	{
		if (bufferSize < 0)
		{
			throw new ArgumentOutOfRangeException("bufferSize", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		return ConsolePal.OpenStandardError();
	}

	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public static void SetIn(TextReader newIn)
	{
		CheckNonNull(newIn, "newIn");
		newIn = SyncTextReader.GetSynchronizedTextReader(newIn);
		lock (s_syncObject)
		{
			Volatile.Write(ref s_in, newIn);
		}
	}

	public static void SetOut(TextWriter newOut)
	{
		CheckNonNull(newOut, "newOut");
		newOut = TextWriter.Synchronized(newOut);
		lock (s_syncObject)
		{
			s_isOutTextWriterRedirected = true;
			Volatile.Write(ref s_out, newOut);
		}
	}

	public static void SetError(TextWriter newError)
	{
		CheckNonNull(newError, "newError");
		newError = TextWriter.Synchronized(newError);
		lock (s_syncObject)
		{
			s_isErrorTextWriterRedirected = true;
			Volatile.Write(ref s_error, newError);
		}
	}

	private static void CheckNonNull(object obj, string paramName)
	{
		if (obj == null)
		{
			throw new ArgumentNullException(paramName);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	public static int Read()
	{
		return In.Read();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[UnsupportedOSPlatform("android")]
	[UnsupportedOSPlatform("browser")]
	public static string? ReadLine()
	{
		return In.ReadLine();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void WriteLine()
	{
		Out.WriteLine();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void WriteLine(bool value)
	{
		Out.WriteLine(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void WriteLine(char value)
	{
		Out.WriteLine(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void WriteLine(char[]? buffer)
	{
		Out.WriteLine(buffer);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void WriteLine(char[] buffer, int index, int count)
	{
		Out.WriteLine(buffer, index, count);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void WriteLine(decimal value)
	{
		Out.WriteLine(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void WriteLine(double value)
	{
		Out.WriteLine(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void WriteLine(float value)
	{
		Out.WriteLine(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void WriteLine(int value)
	{
		Out.WriteLine(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[CLSCompliant(false)]
	public static void WriteLine(uint value)
	{
		Out.WriteLine(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void WriteLine(long value)
	{
		Out.WriteLine(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[CLSCompliant(false)]
	public static void WriteLine(ulong value)
	{
		Out.WriteLine(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void WriteLine(object? value)
	{
		Out.WriteLine(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void WriteLine(string? value)
	{
		Out.WriteLine(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void WriteLine(string format, object? arg0)
	{
		Out.WriteLine(format, arg0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void WriteLine(string format, object? arg0, object? arg1)
	{
		Out.WriteLine(format, arg0, arg1);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void WriteLine(string format, object? arg0, object? arg1, object? arg2)
	{
		Out.WriteLine(format, arg0, arg1, arg2);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void WriteLine(string format, params object?[]? arg)
	{
		if (arg == null)
		{
			Out.WriteLine(format, null, null);
		}
		else
		{
			Out.WriteLine(format, arg);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Write(string format, object? arg0)
	{
		Out.Write(format, arg0);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Write(string format, object? arg0, object? arg1)
	{
		Out.Write(format, arg0, arg1);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Write(string format, object? arg0, object? arg1, object? arg2)
	{
		Out.Write(format, arg0, arg1, arg2);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Write(string format, params object?[]? arg)
	{
		if (arg == null)
		{
			Out.Write(format, null, null);
		}
		else
		{
			Out.Write(format, arg);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Write(bool value)
	{
		Out.Write(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Write(char value)
	{
		Out.Write(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Write(char[]? buffer)
	{
		Out.Write(buffer);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Write(char[] buffer, int index, int count)
	{
		Out.Write(buffer, index, count);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Write(double value)
	{
		Out.Write(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Write(decimal value)
	{
		Out.Write(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Write(float value)
	{
		Out.Write(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Write(int value)
	{
		Out.Write(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[CLSCompliant(false)]
	public static void Write(uint value)
	{
		Out.Write(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Write(long value)
	{
		Out.Write(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[CLSCompliant(false)]
	public static void Write(ulong value)
	{
		Out.Write(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Write(object? value)
	{
		Out.Write(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void Write(string? value)
	{
		Out.Write(value);
	}

	private static void HandlePosixSignal(PosixSignalContext ctx)
	{
		ConsoleCancelEventHandler consoleCancelEventHandler = s_cancelCallbacks;
		if (consoleCancelEventHandler != null)
		{
			ConsoleCancelEventArgs consoleCancelEventArgs = new ConsoleCancelEventArgs((ctx.Signal != PosixSignal.SIGINT) ? ConsoleSpecialKey.ControlBreak : ConsoleSpecialKey.ControlC);
			consoleCancelEventArgs.Cancel = ctx.Cancel;
			consoleCancelEventHandler(null, consoleCancelEventArgs);
			ctx.Cancel = consoleCancelEventArgs.Cancel;
		}
	}
}
