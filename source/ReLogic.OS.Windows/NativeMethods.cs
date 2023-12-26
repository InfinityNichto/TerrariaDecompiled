using System;
using System.Runtime.InteropServices;

namespace ReLogic.OS.Windows;

internal static class NativeMethods
{
	public delegate IntPtr WndProcCallback(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

	public enum StdHandleType
	{
		Input = -10,
		Output = -11,
		Error = -12
	}

	[Flags]
	public enum ConsoleMode
	{
		ProcessedInput = 1,
		LineInput = 2,
		EchoInput = 4,
		WindowInput = 8,
		MouseInput = 0x10,
		InsertMode = 0x20,
		QuickEditMode = 0x40,
		ExtendedFlags = 0x80,
		AutoPosition = 0x100,
		VirtualTerminalInput = 0x200
	}

	[Flags]
	public enum FlashFlags : uint
	{
		Stop = 0u,
		Caption = 1u,
		Tray = 2u,
		CaptionAndTray = 3u,
		Timer = 4u,
		UntilFocused = 0xCu
	}

	public struct FlashInfo
	{
		private uint _cbSize;

		private IntPtr _hWnd;

		private FlashFlags _dwFlags;

		private uint _uCount;

		private uint _dwTimeout;

		public static FlashInfo CreateStart(IntPtr hWnd)
		{
			FlashInfo result = default(FlashInfo);
			result._cbSize = Convert.ToUInt32(Marshal.SizeOf(typeof(FlashInfo)));
			result._hWnd = hWnd;
			result._dwFlags = FlashFlags.CaptionAndTray | FlashFlags.UntilFocused;
			result._uCount = uint.MaxValue;
			result._dwTimeout = 0u;
			return result;
		}

		public static FlashInfo CreateStop(IntPtr hWnd)
		{
			FlashInfo result = default(FlashInfo);
			result._cbSize = Convert.ToUInt32(Marshal.SizeOf(typeof(FlashInfo)));
			result._hWnd = hWnd;
			result._dwFlags = FlashFlags.Stop;
			result._uCount = uint.MaxValue;
			result._dwTimeout = 0u;
			return result;
		}
	}

	public enum DeviceCap
	{
		VertRes = 10,
		DesktopVertRes = 117
	}

	[Flags]
	private enum FileOperationFlags : ushort
	{
		FOF_SILENT = 4,
		FOF_NOCONFIRMATION = 0x10,
		FOF_ALLOWUNDO = 0x40,
		FOF_SIMPLEPROGRESS = 0x100,
		FOF_NOERRORUI = 0x400,
		FOF_WANTNUKEWARNING = 0x4000
	}

	private enum FileOperationType : uint
	{
		FO_MOVE = 1u,
		FO_COPY,
		FO_DELETE,
		FO_RENAME
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	private struct SHFILEOPSTRUCT
	{
		public IntPtr hwnd;

		[MarshalAs(UnmanagedType.U4)]
		public FileOperationType wFunc;

		public string pFrom;

		public string pTo;

		public FileOperationFlags fFlags;

		[MarshalAs(UnmanagedType.Bool)]
		public bool fAnyOperationsAborted;

		public IntPtr hNameMappings;

		public string lpszProgressTitle;
	}

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	public static extern IntPtr DefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll")]
	public static extern IntPtr GetDC(IntPtr hWnd);

	[DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
	public static extern bool TranslateMessage(ref Message message);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	public static extern IntPtr GetForegroundWindow();

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	public static extern bool GetConsoleMode(IntPtr hConsoleHandle, out ConsoleMode lpMode);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	public static extern bool SetConsoleMode(IntPtr hConsoleHandle, ConsoleMode dwMode);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	public static extern IntPtr GetStdHandle(StdHandleType nStdHandle);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool FlashWindowEx(ref FlashInfo flashInfo);

	[DllImport("gdi32.dll")]
	public static extern int GetDeviceCaps(IntPtr hdc, DeviceCap nIndex);

	[DllImport("shell32.dll", CharSet = CharSet.Auto)]
	private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

	private static bool Send(string path, FileOperationFlags flags)
	{
		try
		{
			SHFILEOPSTRUCT sHFILEOPSTRUCT = default(SHFILEOPSTRUCT);
			sHFILEOPSTRUCT.wFunc = FileOperationType.FO_DELETE;
			sHFILEOPSTRUCT.pFrom = path + "\0\0";
			sHFILEOPSTRUCT.fFlags = FileOperationFlags.FOF_ALLOWUNDO | flags;
			SHFILEOPSTRUCT FileOp = sHFILEOPSTRUCT;
			SHFileOperation(ref FileOp);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private static bool Send(string path)
	{
		return Send(path, FileOperationFlags.FOF_NOCONFIRMATION | FileOperationFlags.FOF_WANTNUKEWARNING);
	}

	public static bool MoveToRecycleBin(string path)
	{
		return Send(path, FileOperationFlags.FOF_SILENT | FileOperationFlags.FOF_NOCONFIRMATION | FileOperationFlags.FOF_NOERRORUI);
	}

	private static bool DeleteFile(string path, FileOperationFlags flags)
	{
		try
		{
			SHFILEOPSTRUCT sHFILEOPSTRUCT = default(SHFILEOPSTRUCT);
			sHFILEOPSTRUCT.wFunc = FileOperationType.FO_DELETE;
			sHFILEOPSTRUCT.pFrom = path + "\0\0";
			sHFILEOPSTRUCT.fFlags = flags;
			SHFILEOPSTRUCT FileOp = sHFILEOPSTRUCT;
			SHFileOperation(ref FileOp);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private static bool DeleteCompletelySilent(string path)
	{
		return DeleteFile(path, FileOperationFlags.FOF_SILENT | FileOperationFlags.FOF_NOCONFIRMATION | FileOperationFlags.FOF_NOERRORUI);
	}

	[DllImport("kernel32.dll")]
	private static extern IntPtr GetConsoleWindow();

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

	public static void HideConsole()
	{
		ShowWindow(GetConsoleWindow(), 0);
	}
}
