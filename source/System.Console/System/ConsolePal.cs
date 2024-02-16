using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System;

internal static class ConsolePal
{
	[Flags]
	internal enum ControlKeyState
	{
		RightAltPressed = 1,
		LeftAltPressed = 2,
		RightCtrlPressed = 4,
		LeftCtrlPressed = 8,
		ShiftPressed = 0x10,
		NumLockOn = 0x20,
		ScrollLockOn = 0x40,
		CapsLockOn = 0x80,
		EnhancedKey = 0x100
	}

	private sealed class WindowsConsoleStream : ConsoleStream
	{
		private readonly bool _isPipe;

		private IntPtr _handle;

		private readonly bool _useFileAPIs;

		internal WindowsConsoleStream(IntPtr handle, FileAccess access, bool useFileAPIs)
			: base(access)
		{
			_handle = handle;
			_isPipe = global::Interop.Kernel32.GetFileType(handle) == 3;
			_useFileAPIs = useFileAPIs;
		}

		protected override void Dispose(bool disposing)
		{
			_handle = IntPtr.Zero;
			base.Dispose(disposing);
		}

		public override int Read(Span<byte> buffer)
		{
			int bytesRead;
			int num = ReadFileNative(_handle, buffer, _isPipe, out bytesRead, _useFileAPIs);
			if (num != 0)
			{
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(num);
			}
			return bytesRead;
		}

		public override void Write(ReadOnlySpan<byte> buffer)
		{
			int num = WriteFileNative(_handle, buffer, _useFileAPIs);
			if (num != 0)
			{
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(num);
			}
		}

		public override void Flush()
		{
			if (_handle == IntPtr.Zero)
			{
				throw Error.GetFileNotOpen();
			}
			base.Flush();
		}

		private unsafe static int ReadFileNative(IntPtr hFile, Span<byte> buffer, bool isPipe, out int bytesRead, bool useFileAPIs)
		{
			if (buffer.IsEmpty)
			{
				bytesRead = 0;
				return 0;
			}
			bool flag;
			fixed (byte* ptr = buffer)
			{
				if (useFileAPIs)
				{
					flag = global::Interop.Kernel32.ReadFile(hFile, ptr, buffer.Length, out bytesRead, IntPtr.Zero) != 0;
				}
				else
				{
					flag = global::Interop.Kernel32.ReadConsole(hFile, ptr, buffer.Length / 2, out var lpNumberOfCharsRead, IntPtr.Zero);
					bytesRead = lpNumberOfCharsRead * 2;
				}
			}
			if (flag)
			{
				return 0;
			}
			int lastPInvokeError = Marshal.GetLastPInvokeError();
			if (lastPInvokeError == 232 || lastPInvokeError == 109)
			{
				return 0;
			}
			return lastPInvokeError;
		}

		private unsafe static int WriteFileNative(IntPtr hFile, ReadOnlySpan<byte> bytes, bool useFileAPIs)
		{
			if (bytes.IsEmpty)
			{
				return 0;
			}
			bool flag;
			fixed (byte* ptr = bytes)
			{
				flag = ((!useFileAPIs) ? global::Interop.Kernel32.WriteConsole(hFile, ptr, bytes.Length / 2, out var _, IntPtr.Zero) : (global::Interop.Kernel32.WriteFile(hFile, ptr, bytes.Length, out var _, IntPtr.Zero) != 0));
			}
			if (flag)
			{
				return 0;
			}
			int lastPInvokeError = Marshal.GetLastPInvokeError();
			if (lastPInvokeError == 232 || lastPInvokeError == 109)
			{
				return 0;
			}
			return lastPInvokeError;
		}
	}

	private static readonly object s_readKeySyncObject = new object();

	private static global::Interop.InputRecord _cachedInputRecord;

	private static volatile bool _haveReadDefaultColors;

	private static volatile byte _defaultColors;

	private static IntPtr InvalidHandleValue => new IntPtr(-1);

	private static IntPtr InputHandle => global::Interop.Kernel32.GetStdHandle(-10);

	private static IntPtr OutputHandle => global::Interop.Kernel32.GetStdHandle(-11);

	private static IntPtr ErrorHandle => global::Interop.Kernel32.GetStdHandle(-12);

	public static Encoding InputEncoding => EncodingHelper.GetSupportedConsoleEncoding((int)global::Interop.Kernel32.GetConsoleCP());

	public static Encoding OutputEncoding => EncodingHelper.GetSupportedConsoleEncoding((int)global::Interop.Kernel32.GetConsoleOutputCP());

	public static bool NumberLock
	{
		get
		{
			try
			{
				short keyState = global::Interop.User32.GetKeyState(144);
				return (keyState & 1) == 1;
			}
			catch (Exception)
			{
				throw new PlatformNotSupportedException();
			}
		}
	}

	public static bool CapsLock
	{
		get
		{
			try
			{
				short keyState = global::Interop.User32.GetKeyState(20);
				return (keyState & 1) == 1;
			}
			catch (Exception)
			{
				throw new PlatformNotSupportedException();
			}
		}
	}

	public static bool KeyAvailable
	{
		get
		{
			if (_cachedInputRecord.eventType == 1)
			{
				return true;
			}
			global::Interop.InputRecord buffer = default(global::Interop.InputRecord);
			int numEventsRead = 0;
			while (true)
			{
				if (!global::Interop.Kernel32.PeekConsoleInput(InputHandle, out buffer, 1, out numEventsRead))
				{
					int lastPInvokeError = Marshal.GetLastPInvokeError();
					if (lastPInvokeError == 6)
					{
						throw new InvalidOperationException(System.SR.InvalidOperation_ConsoleKeyAvailableOnFile);
					}
					throw System.IO.Win32Marshal.GetExceptionForWin32Error(lastPInvokeError, "stdin");
				}
				if (numEventsRead == 0)
				{
					return false;
				}
				if (IsKeyDownEvent(buffer) && !IsModKey(buffer))
				{
					break;
				}
				if (!global::Interop.Kernel32.ReadConsoleInput(InputHandle, out buffer, 1, out numEventsRead))
				{
					throw System.IO.Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError());
				}
			}
			return true;
		}
	}

	public static bool TreatControlCAsInput
	{
		get
		{
			IntPtr inputHandle = InputHandle;
			if (inputHandle == InvalidHandleValue)
			{
				throw new IOException(System.SR.IO_NoConsole);
			}
			int mode = 0;
			if (!global::Interop.Kernel32.GetConsoleMode(inputHandle, out mode))
			{
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError());
			}
			return (mode & 1) == 0;
		}
		set
		{
			IntPtr inputHandle = InputHandle;
			if (inputHandle == InvalidHandleValue)
			{
				throw new IOException(System.SR.IO_NoConsole);
			}
			int mode = 0;
			if (!global::Interop.Kernel32.GetConsoleMode(inputHandle, out mode))
			{
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError());
			}
			mode = ((!value) ? (mode | 1) : (mode & -2));
			if (!global::Interop.Kernel32.SetConsoleMode(inputHandle, mode))
			{
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError());
			}
		}
	}

	public static ConsoleColor BackgroundColor
	{
		get
		{
			bool succeeded;
			global::Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO bufferInfo = GetBufferInfo(throwOnNoConsole: false, out succeeded);
			if (!succeeded)
			{
				return ConsoleColor.Black;
			}
			return ColorAttributeToConsoleColor((global::Interop.Kernel32.Color)(bufferInfo.wAttributes & 0xF0));
		}
		set
		{
			global::Interop.Kernel32.Color color = ConsoleColorToColorAttribute(value, isBackground: true);
			bool succeeded;
			global::Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO bufferInfo = GetBufferInfo(throwOnNoConsole: false, out succeeded);
			if (succeeded)
			{
				short wAttributes = bufferInfo.wAttributes;
				wAttributes &= -241;
				wAttributes = (short)((ushort)wAttributes | (ushort)color);
				global::Interop.Kernel32.SetConsoleTextAttribute(OutputHandle, wAttributes);
			}
		}
	}

	public static ConsoleColor ForegroundColor
	{
		get
		{
			bool succeeded;
			global::Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO bufferInfo = GetBufferInfo(throwOnNoConsole: false, out succeeded);
			if (!succeeded)
			{
				return ConsoleColor.Gray;
			}
			return ColorAttributeToConsoleColor((global::Interop.Kernel32.Color)(bufferInfo.wAttributes & 0xF));
		}
		set
		{
			global::Interop.Kernel32.Color color = ConsoleColorToColorAttribute(value, isBackground: false);
			bool succeeded;
			global::Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO bufferInfo = GetBufferInfo(throwOnNoConsole: false, out succeeded);
			if (succeeded)
			{
				short wAttributes = bufferInfo.wAttributes;
				wAttributes &= -16;
				wAttributes = (short)((ushort)wAttributes | (ushort)color);
				global::Interop.Kernel32.SetConsoleTextAttribute(OutputHandle, wAttributes);
			}
		}
	}

	public static int CursorSize
	{
		get
		{
			if (!global::Interop.Kernel32.GetConsoleCursorInfo(OutputHandle, out var cci))
			{
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError());
			}
			return cci.dwSize;
		}
		set
		{
			if (value < 1 || value > 100)
			{
				throw new ArgumentOutOfRangeException("value", value, System.SR.ArgumentOutOfRange_CursorSize);
			}
			if (!global::Interop.Kernel32.GetConsoleCursorInfo(OutputHandle, out var cci))
			{
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError());
			}
			cci.dwSize = value;
			if (!global::Interop.Kernel32.SetConsoleCursorInfo(OutputHandle, ref cci))
			{
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError());
			}
		}
	}

	public static bool CursorVisible
	{
		get
		{
			if (!global::Interop.Kernel32.GetConsoleCursorInfo(OutputHandle, out var cci))
			{
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError());
			}
			return cci.bVisible;
		}
		set
		{
			if (!global::Interop.Kernel32.GetConsoleCursorInfo(OutputHandle, out var cci))
			{
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError());
			}
			cci.bVisible = value;
			if (!global::Interop.Kernel32.SetConsoleCursorInfo(OutputHandle, ref cci))
			{
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError());
			}
		}
	}

	public unsafe static string Title
	{
		get
		{
			Span<char> initialBuffer = stackalloc char[256];
			System.Text.ValueStringBuilder valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
			uint num;
			while (true)
			{
				num = 0u;
				fixed (char* title = valueStringBuilder)
				{
					num = global::Interop.Kernel32.GetConsoleTitleW(title, (uint)valueStringBuilder.Capacity);
				}
				if (num == 0)
				{
					int lastPInvokeError = Marshal.GetLastPInvokeError();
					switch (lastPInvokeError)
					{
					case 122:
						valueStringBuilder.EnsureCapacity(valueStringBuilder.Capacity * 2);
						continue;
					default:
						throw System.IO.Win32Marshal.GetExceptionForWin32Error(lastPInvokeError, string.Empty);
					case 0:
						break;
					}
					break;
				}
				if (num < valueStringBuilder.Capacity - 1 && (!IsWindows7() || num < valueStringBuilder.Capacity / 2 - 1))
				{
					break;
				}
				valueStringBuilder.EnsureCapacity(valueStringBuilder.Capacity * 2);
			}
			valueStringBuilder.Length = (int)num;
			return valueStringBuilder.ToString();
		}
		set
		{
			if (!global::Interop.Kernel32.SetConsoleTitle(value))
			{
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError());
			}
		}
	}

	public static int BufferWidth
	{
		get
		{
			return GetBufferInfo().dwSize.X;
		}
		set
		{
			SetBufferSize(value, BufferHeight);
		}
	}

	public static int BufferHeight
	{
		get
		{
			return GetBufferInfo().dwSize.Y;
		}
		set
		{
			SetBufferSize(BufferWidth, value);
		}
	}

	public static int LargestWindowWidth => global::Interop.Kernel32.GetLargestConsoleWindowSize(OutputHandle).X;

	public static int LargestWindowHeight => global::Interop.Kernel32.GetLargestConsoleWindowSize(OutputHandle).Y;

	public static int WindowLeft
	{
		get
		{
			return GetBufferInfo().srWindow.Left;
		}
		set
		{
			SetWindowPosition(value, WindowTop);
		}
	}

	public static int WindowTop
	{
		get
		{
			return GetBufferInfo().srWindow.Top;
		}
		set
		{
			SetWindowPosition(WindowLeft, value);
		}
	}

	public static int WindowWidth
	{
		get
		{
			global::Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO bufferInfo = GetBufferInfo();
			return bufferInfo.srWindow.Right - bufferInfo.srWindow.Left + 1;
		}
		set
		{
			SetWindowSize(value, WindowHeight);
		}
	}

	public static int WindowHeight
	{
		get
		{
			global::Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO bufferInfo = GetBufferInfo();
			return bufferInfo.srWindow.Bottom - bufferInfo.srWindow.Top + 1;
		}
		set
		{
			SetWindowSize(WindowWidth, value);
		}
	}

	internal static void EnsureConsoleInitialized()
	{
	}

	private static bool IsWindows7()
	{
		Version version = Environment.OSVersion.Version;
		if (version.Major == 6)
		{
			return version.Minor == 1;
		}
		return false;
	}

	public static Stream OpenStandardInput()
	{
		return GetStandardFile(-10, FileAccess.Read, Console.InputEncoding.CodePage != 1200 || Console.IsInputRedirected);
	}

	public static Stream OpenStandardOutput()
	{
		return GetStandardFile(-11, FileAccess.Write, Console.OutputEncoding.CodePage != 1200 || Console.IsOutputRedirected);
	}

	public static Stream OpenStandardError()
	{
		return GetStandardFile(-12, FileAccess.Write, Console.OutputEncoding.CodePage != 1200 || Console.IsErrorRedirected);
	}

	private static Stream GetStandardFile(int handleType, FileAccess access, bool useFileAPIs)
	{
		IntPtr stdHandle = global::Interop.Kernel32.GetStdHandle(handleType);
		if (stdHandle == IntPtr.Zero || stdHandle == InvalidHandleValue || (access != FileAccess.Read && !ConsoleHandleIsWritable(stdHandle)))
		{
			return Stream.Null;
		}
		return new WindowsConsoleStream(stdHandle, access, useFileAPIs);
	}

	private unsafe static bool ConsoleHandleIsWritable(IntPtr outErrHandle)
	{
		byte b = 65;
		int numBytesWritten;
		int num = global::Interop.Kernel32.WriteFile(outErrHandle, &b, 0, out numBytesWritten, IntPtr.Zero);
		return num != 0;
	}

	public static void SetConsoleInputEncoding(Encoding enc)
	{
		if (enc.CodePage != 1200 && !global::Interop.Kernel32.SetConsoleCP(enc.CodePage))
		{
			throw System.IO.Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError());
		}
	}

	public static void SetConsoleOutputEncoding(Encoding enc)
	{
		if (enc.CodePage != 1200 && !global::Interop.Kernel32.SetConsoleOutputCP(enc.CodePage))
		{
			throw System.IO.Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError());
		}
	}

	public static bool IsInputRedirectedCore()
	{
		return IsHandleRedirected(InputHandle);
	}

	public static bool IsOutputRedirectedCore()
	{
		return IsHandleRedirected(OutputHandle);
	}

	public static bool IsErrorRedirectedCore()
	{
		return IsHandleRedirected(ErrorHandle);
	}

	private static bool IsHandleRedirected(IntPtr handle)
	{
		uint fileType = global::Interop.Kernel32.GetFileType(handle);
		if ((fileType & 2) != 2)
		{
			return true;
		}
		return !global::Interop.Kernel32.IsGetConsoleModeCallSuccessful(handle);
	}

	internal static TextReader GetOrCreateReader()
	{
		Stream stream = OpenStandardInput();
		return SyncTextReader.GetSynchronizedTextReader((stream == Stream.Null) ? StreamReader.Null : new StreamReader(stream, new ConsoleEncoding(Console.InputEncoding), detectEncodingFromByteOrderMarks: false, 4096, leaveOpen: true));
	}

	private static bool IsKeyDownEvent(global::Interop.InputRecord ir)
	{
		if (ir.eventType == 1)
		{
			return ir.keyEvent.keyDown != global::Interop.BOOL.FALSE;
		}
		return false;
	}

	private static bool IsModKey(global::Interop.InputRecord ir)
	{
		short virtualKeyCode = ir.keyEvent.virtualKeyCode;
		if ((virtualKeyCode < 16 || virtualKeyCode > 18) && virtualKeyCode != 20 && virtualKeyCode != 144)
		{
			return virtualKeyCode == 145;
		}
		return true;
	}

	private static bool IsAltKeyDown(global::Interop.InputRecord ir)
	{
		return (ir.keyEvent.controlKeyState & 3) != 0;
	}

	public static ConsoleKeyInfo ReadKey(bool intercept)
	{
		int numEventsRead = -1;
		global::Interop.InputRecord buffer;
		lock (s_readKeySyncObject)
		{
			if (_cachedInputRecord.eventType == 1)
			{
				buffer = _cachedInputRecord;
				if (_cachedInputRecord.keyEvent.repeatCount == 0)
				{
					_cachedInputRecord.eventType = -1;
				}
				else
				{
					_cachedInputRecord.keyEvent.repeatCount--;
				}
			}
			else
			{
				while (true)
				{
					if (!global::Interop.Kernel32.ReadConsoleInput(InputHandle, out buffer, 1, out numEventsRead) || numEventsRead == 0)
					{
						throw new InvalidOperationException(System.SR.InvalidOperation_ConsoleReadKeyOnFile);
					}
					short virtualKeyCode = buffer.keyEvent.virtualKeyCode;
					if ((!IsKeyDownEvent(buffer) && virtualKeyCode != 18) || (buffer.keyEvent.uChar == '\0' && IsModKey(buffer)))
					{
						continue;
					}
					ConsoleKey consoleKey = (ConsoleKey)virtualKeyCode;
					if (!IsAltKeyDown(buffer))
					{
						break;
					}
					if (consoleKey < ConsoleKey.NumPad0 || consoleKey > ConsoleKey.NumPad9)
					{
						switch (consoleKey)
						{
						case ConsoleKey.Clear:
						case ConsoleKey.PageUp:
						case ConsoleKey.PageDown:
						case ConsoleKey.End:
						case ConsoleKey.Home:
						case ConsoleKey.LeftArrow:
						case ConsoleKey.UpArrow:
						case ConsoleKey.RightArrow:
						case ConsoleKey.DownArrow:
						case ConsoleKey.Insert:
							continue;
						}
						break;
					}
				}
				if (buffer.keyEvent.repeatCount > 1)
				{
					buffer.keyEvent.repeatCount--;
					_cachedInputRecord = buffer;
				}
			}
		}
		ControlKeyState controlKeyState = (ControlKeyState)buffer.keyEvent.controlKeyState;
		bool shift = (controlKeyState & ControlKeyState.ShiftPressed) != 0;
		bool alt = (controlKeyState & (ControlKeyState.RightAltPressed | ControlKeyState.LeftAltPressed)) != 0;
		bool control = (controlKeyState & (ControlKeyState.RightCtrlPressed | ControlKeyState.LeftCtrlPressed)) != 0;
		ConsoleKeyInfo result = new ConsoleKeyInfo(buffer.keyEvent.uChar, (ConsoleKey)buffer.keyEvent.virtualKeyCode, shift, alt, control);
		if (!intercept)
		{
			Console.Write(buffer.keyEvent.uChar);
		}
		return result;
	}

	public static void ResetColor()
	{
		if (!_haveReadDefaultColors)
		{
			GetBufferInfo(throwOnNoConsole: false, out var succeeded);
			if (!succeeded)
			{
				return;
			}
		}
		global::Interop.Kernel32.SetConsoleTextAttribute(OutputHandle, _defaultColors);
	}

	public static (int Left, int Top) GetCursorPosition()
	{
		global::Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO bufferInfo = GetBufferInfo();
		return (Left: bufferInfo.dwCursorPosition.X, Top: bufferInfo.dwCursorPosition.Y);
	}

	public static void Beep()
	{
		global::Interop.Kernel32.Beep(800, 200);
	}

	public static void Beep(int frequency, int duration)
	{
		if (frequency < 37 || frequency > 32767)
		{
			throw new ArgumentOutOfRangeException("frequency", frequency, System.SR.Format(System.SR.ArgumentOutOfRange_BeepFrequency, 37, 32767));
		}
		if (duration <= 0)
		{
			throw new ArgumentOutOfRangeException("duration", duration, System.SR.ArgumentOutOfRange_NeedPosNum);
		}
		global::Interop.Kernel32.Beep(frequency, duration);
	}

	public unsafe static void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop, char sourceChar, ConsoleColor sourceForeColor, ConsoleColor sourceBackColor)
	{
		if (sourceForeColor < ConsoleColor.Black || sourceForeColor > ConsoleColor.White)
		{
			throw new ArgumentException(System.SR.Arg_InvalidConsoleColor, "sourceForeColor");
		}
		if (sourceBackColor < ConsoleColor.Black || sourceBackColor > ConsoleColor.White)
		{
			throw new ArgumentException(System.SR.Arg_InvalidConsoleColor, "sourceBackColor");
		}
		global::Interop.Kernel32.COORD dwSize = GetBufferInfo().dwSize;
		if (sourceLeft < 0 || sourceLeft > dwSize.X)
		{
			throw new ArgumentOutOfRangeException("sourceLeft", sourceLeft, System.SR.ArgumentOutOfRange_ConsoleBufferBoundaries);
		}
		if (sourceTop < 0 || sourceTop > dwSize.Y)
		{
			throw new ArgumentOutOfRangeException("sourceTop", sourceTop, System.SR.ArgumentOutOfRange_ConsoleBufferBoundaries);
		}
		if (sourceWidth < 0 || sourceWidth > dwSize.X - sourceLeft)
		{
			throw new ArgumentOutOfRangeException("sourceWidth", sourceWidth, System.SR.ArgumentOutOfRange_ConsoleBufferBoundaries);
		}
		if (sourceHeight < 0 || sourceTop > dwSize.Y - sourceHeight)
		{
			throw new ArgumentOutOfRangeException("sourceHeight", sourceHeight, System.SR.ArgumentOutOfRange_ConsoleBufferBoundaries);
		}
		if (targetLeft < 0 || targetLeft > dwSize.X)
		{
			throw new ArgumentOutOfRangeException("targetLeft", targetLeft, System.SR.ArgumentOutOfRange_ConsoleBufferBoundaries);
		}
		if (targetTop < 0 || targetTop > dwSize.Y)
		{
			throw new ArgumentOutOfRangeException("targetTop", targetTop, System.SR.ArgumentOutOfRange_ConsoleBufferBoundaries);
		}
		if (sourceWidth == 0 || sourceHeight == 0)
		{
			return;
		}
		global::Interop.Kernel32.CHAR_INFO[] array = new global::Interop.Kernel32.CHAR_INFO[sourceWidth * sourceHeight];
		dwSize.X = (short)sourceWidth;
		dwSize.Y = (short)sourceHeight;
		global::Interop.Kernel32.COORD bufferCoord = default(global::Interop.Kernel32.COORD);
		global::Interop.Kernel32.SMALL_RECT readRegion = default(global::Interop.Kernel32.SMALL_RECT);
		readRegion.Left = (short)sourceLeft;
		readRegion.Right = (short)(sourceLeft + sourceWidth - 1);
		readRegion.Top = (short)sourceTop;
		readRegion.Bottom = (short)(sourceTop + sourceHeight - 1);
		bool flag;
		fixed (global::Interop.Kernel32.CHAR_INFO* pBuffer = array)
		{
			flag = global::Interop.Kernel32.ReadConsoleOutput(OutputHandle, pBuffer, dwSize, bufferCoord, ref readRegion);
		}
		if (!flag)
		{
			throw System.IO.Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError());
		}
		global::Interop.Kernel32.COORD cOORD = default(global::Interop.Kernel32.COORD);
		cOORD.X = (short)sourceLeft;
		global::Interop.Kernel32.Color color = ConsoleColorToColorAttribute(sourceBackColor, isBackground: true);
		color |= ConsoleColorToColorAttribute(sourceForeColor, isBackground: false);
		short wColorAttribute = (short)color;
		for (int i = sourceTop; i < sourceTop + sourceHeight; i++)
		{
			cOORD.Y = (short)i;
			if (!global::Interop.Kernel32.FillConsoleOutputCharacter(OutputHandle, sourceChar, sourceWidth, cOORD, out var pNumCharsWritten))
			{
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError());
			}
			if (!global::Interop.Kernel32.FillConsoleOutputAttribute(OutputHandle, wColorAttribute, sourceWidth, cOORD, out pNumCharsWritten))
			{
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError());
			}
		}
		global::Interop.Kernel32.SMALL_RECT writeRegion = default(global::Interop.Kernel32.SMALL_RECT);
		writeRegion.Left = (short)targetLeft;
		writeRegion.Right = (short)(targetLeft + sourceWidth);
		writeRegion.Top = (short)targetTop;
		writeRegion.Bottom = (short)(targetTop + sourceHeight);
		fixed (global::Interop.Kernel32.CHAR_INFO* buffer = array)
		{
			global::Interop.Kernel32.WriteConsoleOutput(OutputHandle, buffer, dwSize, bufferCoord, ref writeRegion);
		}
	}

	public static void Clear()
	{
		global::Interop.Kernel32.COORD cOORD = default(global::Interop.Kernel32.COORD);
		IntPtr outputHandle = OutputHandle;
		if (outputHandle == InvalidHandleValue)
		{
			throw new IOException(System.SR.IO_NoConsole);
		}
		global::Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO bufferInfo = GetBufferInfo();
		int num = bufferInfo.dwSize.X * bufferInfo.dwSize.Y;
		int pNumCharsWritten = 0;
		if (!global::Interop.Kernel32.FillConsoleOutputCharacter(outputHandle, ' ', num, cOORD, out pNumCharsWritten))
		{
			throw System.IO.Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError());
		}
		pNumCharsWritten = 0;
		if (!global::Interop.Kernel32.FillConsoleOutputAttribute(outputHandle, bufferInfo.wAttributes, num, cOORD, out pNumCharsWritten))
		{
			throw System.IO.Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError());
		}
		if (!global::Interop.Kernel32.SetConsoleCursorPosition(outputHandle, cOORD))
		{
			throw System.IO.Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError());
		}
	}

	public static void SetCursorPosition(int left, int top)
	{
		IntPtr outputHandle = OutputHandle;
		global::Interop.Kernel32.COORD cursorPosition = default(global::Interop.Kernel32.COORD);
		cursorPosition.X = (short)left;
		cursorPosition.Y = (short)top;
		if (!global::Interop.Kernel32.SetConsoleCursorPosition(outputHandle, cursorPosition))
		{
			int lastPInvokeError = Marshal.GetLastPInvokeError();
			global::Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO bufferInfo = GetBufferInfo();
			if (left >= bufferInfo.dwSize.X)
			{
				throw new ArgumentOutOfRangeException("left", left, System.SR.ArgumentOutOfRange_ConsoleBufferBoundaries);
			}
			if (top >= bufferInfo.dwSize.Y)
			{
				throw new ArgumentOutOfRangeException("top", top, System.SR.ArgumentOutOfRange_ConsoleBufferBoundaries);
			}
			throw System.IO.Win32Marshal.GetExceptionForWin32Error(lastPInvokeError);
		}
	}

	public static void SetBufferSize(int width, int height)
	{
		global::Interop.Kernel32.SMALL_RECT srWindow = GetBufferInfo().srWindow;
		if (width < srWindow.Right + 1 || width >= 32767)
		{
			throw new ArgumentOutOfRangeException("width", width, System.SR.ArgumentOutOfRange_ConsoleBufferLessThanWindowSize);
		}
		if (height < srWindow.Bottom + 1 || height >= 32767)
		{
			throw new ArgumentOutOfRangeException("height", height, System.SR.ArgumentOutOfRange_ConsoleBufferLessThanWindowSize);
		}
		global::Interop.Kernel32.COORD size = default(global::Interop.Kernel32.COORD);
		size.X = (short)width;
		size.Y = (short)height;
		if (!global::Interop.Kernel32.SetConsoleScreenBufferSize(OutputHandle, size))
		{
			throw System.IO.Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError());
		}
	}

	public unsafe static void SetWindowPosition(int left, int top)
	{
		global::Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO bufferInfo = GetBufferInfo();
		global::Interop.Kernel32.SMALL_RECT srWindow = bufferInfo.srWindow;
		int num = left + srWindow.Right - srWindow.Left;
		if (left < 0 || num > bufferInfo.dwSize.X - 1 || num < left)
		{
			throw new ArgumentOutOfRangeException("left", left, System.SR.ArgumentOutOfRange_ConsoleWindowPos);
		}
		int num2 = top + srWindow.Bottom - srWindow.Top;
		if (top < 0 || num2 > bufferInfo.dwSize.Y - 1 || num2 < top)
		{
			throw new ArgumentOutOfRangeException("top", top, System.SR.ArgumentOutOfRange_ConsoleWindowPos);
		}
		srWindow.Bottom = (short)num2;
		srWindow.Right = (short)num;
		srWindow.Left = (short)left;
		srWindow.Top = (short)top;
		if (!global::Interop.Kernel32.SetConsoleWindowInfo(OutputHandle, absolute: true, &srWindow))
		{
			throw System.IO.Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError());
		}
	}

	public unsafe static void SetWindowSize(int width, int height)
	{
		if (width <= 0)
		{
			throw new ArgumentOutOfRangeException("width", width, System.SR.ArgumentOutOfRange_NeedPosNum);
		}
		if (height <= 0)
		{
			throw new ArgumentOutOfRangeException("height", height, System.SR.ArgumentOutOfRange_NeedPosNum);
		}
		global::Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO bufferInfo = GetBufferInfo();
		bool flag = false;
		global::Interop.Kernel32.COORD size = default(global::Interop.Kernel32.COORD);
		size.X = bufferInfo.dwSize.X;
		size.Y = bufferInfo.dwSize.Y;
		if (bufferInfo.dwSize.X < bufferInfo.srWindow.Left + width)
		{
			if (bufferInfo.srWindow.Left >= 32767 - width)
			{
				throw new ArgumentOutOfRangeException("width", System.SR.Format(System.SR.ArgumentOutOfRange_ConsoleWindowBufferSize, 32767 - width));
			}
			size.X = (short)(bufferInfo.srWindow.Left + width);
			flag = true;
		}
		if (bufferInfo.dwSize.Y < bufferInfo.srWindow.Top + height)
		{
			if (bufferInfo.srWindow.Top >= 32767 - height)
			{
				throw new ArgumentOutOfRangeException("height", System.SR.Format(System.SR.ArgumentOutOfRange_ConsoleWindowBufferSize, 32767 - height));
			}
			size.Y = (short)(bufferInfo.srWindow.Top + height);
			flag = true;
		}
		if (flag && !global::Interop.Kernel32.SetConsoleScreenBufferSize(OutputHandle, size))
		{
			throw System.IO.Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastPInvokeError());
		}
		global::Interop.Kernel32.SMALL_RECT srWindow = bufferInfo.srWindow;
		srWindow.Bottom = (short)(srWindow.Top + height - 1);
		srWindow.Right = (short)(srWindow.Left + width - 1);
		if (!global::Interop.Kernel32.SetConsoleWindowInfo(OutputHandle, absolute: true, &srWindow))
		{
			int lastPInvokeError = Marshal.GetLastPInvokeError();
			if (flag)
			{
				global::Interop.Kernel32.SetConsoleScreenBufferSize(OutputHandle, bufferInfo.dwSize);
			}
			global::Interop.Kernel32.COORD largestConsoleWindowSize = global::Interop.Kernel32.GetLargestConsoleWindowSize(OutputHandle);
			if (width > largestConsoleWindowSize.X)
			{
				throw new ArgumentOutOfRangeException("width", width, System.SR.Format(System.SR.ArgumentOutOfRange_ConsoleWindowSize_Size, largestConsoleWindowSize.X));
			}
			if (height > largestConsoleWindowSize.Y)
			{
				throw new ArgumentOutOfRangeException("height", height, System.SR.Format(System.SR.ArgumentOutOfRange_ConsoleWindowSize_Size, largestConsoleWindowSize.Y));
			}
			throw System.IO.Win32Marshal.GetExceptionForWin32Error(lastPInvokeError);
		}
	}

	private static global::Interop.Kernel32.Color ConsoleColorToColorAttribute(ConsoleColor color, bool isBackground)
	{
		if (((uint)color & 0xFFFFFFF0u) != 0)
		{
			throw new ArgumentException(System.SR.Arg_InvalidConsoleColor);
		}
		global::Interop.Kernel32.Color color2 = (global::Interop.Kernel32.Color)color;
		if (isBackground)
		{
			color2 = (global::Interop.Kernel32.Color)((int)color2 << 4);
		}
		return color2;
	}

	private static ConsoleColor ColorAttributeToConsoleColor(global::Interop.Kernel32.Color c)
	{
		if ((c & global::Interop.Kernel32.Color.BackgroundMask) != 0)
		{
			c = (global::Interop.Kernel32.Color)((int)c >> 4);
		}
		return (ConsoleColor)c;
	}

	private static global::Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO GetBufferInfo()
	{
		bool succeeded;
		return GetBufferInfo(throwOnNoConsole: true, out succeeded);
	}

	private static global::Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO GetBufferInfo(bool throwOnNoConsole, out bool succeeded)
	{
		succeeded = false;
		IntPtr outputHandle = OutputHandle;
		if (outputHandle == InvalidHandleValue)
		{
			if (throwOnNoConsole)
			{
				throw new IOException(System.SR.IO_NoConsole);
			}
			return default(global::Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO);
		}
		if (!global::Interop.Kernel32.GetConsoleScreenBufferInfo(outputHandle, out var lpConsoleScreenBufferInfo) && !global::Interop.Kernel32.GetConsoleScreenBufferInfo(ErrorHandle, out lpConsoleScreenBufferInfo) && !global::Interop.Kernel32.GetConsoleScreenBufferInfo(InputHandle, out lpConsoleScreenBufferInfo))
		{
			int lastPInvokeError = Marshal.GetLastPInvokeError();
			if (lastPInvokeError == 6 && !throwOnNoConsole)
			{
				return default(global::Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO);
			}
			throw System.IO.Win32Marshal.GetExceptionForWin32Error(lastPInvokeError);
		}
		if (!_haveReadDefaultColors)
		{
			_defaultColors = (byte)((uint)lpConsoleScreenBufferInfo.wAttributes & 0xFFu);
			_haveReadDefaultColors = true;
		}
		succeeded = true;
		return lpConsoleScreenBufferInfo;
	}
}
