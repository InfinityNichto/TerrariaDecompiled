using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static class Interop
{
	internal static class Kernel32
	{
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct CPINFOEXW
		{
			internal uint MaxCharSize;

			internal unsafe fixed byte DefaultChar[2];

			internal unsafe fixed byte LeadByte[12];

			internal char UnicodeDefaultChar;

			internal uint CodePage;

			internal unsafe fixed char CodePageName[260];
		}

		internal struct CONSOLE_CURSOR_INFO
		{
			internal int dwSize;

			internal bool bVisible;
		}

		internal struct CONSOLE_SCREEN_BUFFER_INFO
		{
			internal COORD dwSize;

			internal COORD dwCursorPosition;

			internal short wAttributes;

			internal SMALL_RECT srWindow;

			internal COORD dwMaximumWindowSize;
		}

		internal struct COORD
		{
			internal short X;

			internal short Y;
		}

		internal struct SMALL_RECT
		{
			internal short Left;

			internal short Top;

			internal short Right;

			internal short Bottom;
		}

		internal enum Color : short
		{
			Black = 0,
			ForegroundBlue = 1,
			ForegroundGreen = 2,
			ForegroundRed = 4,
			ForegroundYellow = 6,
			ForegroundIntensity = 8,
			BackgroundBlue = 16,
			BackgroundGreen = 32,
			BackgroundRed = 64,
			BackgroundYellow = 96,
			BackgroundIntensity = 128,
			ForegroundMask = 15,
			BackgroundMask = 240,
			ColorMask = 255
		}

		internal struct CHAR_INFO
		{
			private ushort charData;

			private short attributes;
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		private unsafe static extern BOOL GetCPInfoExW(uint CodePage, uint dwFlags, CPINFOEXW* lpCPInfoEx);

		internal unsafe static int GetLeadByteRanges(int codePage, byte[] leadByteRanges)
		{
			int num = 0;
			Unsafe.SkipInit(out CPINFOEXW cPINFOEXW);
			if (GetCPInfoExW((uint)codePage, 0u, &cPINFOEXW) != 0)
			{
				for (int i = 0; i < 10 && leadByteRanges[i] != 0; i += 2)
				{
					leadByteRanges[i] = cPINFOEXW.LeadByte[i];
					leadByteRanges[i + 1] = cPINFOEXW.LeadByte[i + 1];
					num++;
				}
			}
			return num;
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool Beep(int frequency, int duration);

		[DllImport("kernel32.dll", BestFitMapping = true, CharSet = CharSet.Unicode, EntryPoint = "FormatMessageW", ExactSpelling = true, SetLastError = true)]
		private unsafe static extern int FormatMessage(int dwFlags, IntPtr lpSource, uint dwMessageId, int dwLanguageId, void* lpBuffer, int nSize, IntPtr arguments);

		internal static string GetMessage(int errorCode)
		{
			return GetMessage(errorCode, IntPtr.Zero);
		}

		internal unsafe static string GetMessage(int errorCode, IntPtr moduleHandle)
		{
			int num = 12800;
			if (moduleHandle != IntPtr.Zero)
			{
				num |= 0x800;
			}
			Span<char> span = stackalloc char[256];
			fixed (char* lpBuffer = span)
			{
				int num2 = FormatMessage(num, moduleHandle, (uint)errorCode, 0, lpBuffer, span.Length, IntPtr.Zero);
				if (num2 > 0)
				{
					return GetAndTrimString(span.Slice(0, num2));
				}
			}
			if (Marshal.GetLastWin32Error() == 122)
			{
				IntPtr intPtr = default(IntPtr);
				try
				{
					int num3 = FormatMessage(num | 0x100, moduleHandle, (uint)errorCode, 0, &intPtr, 0, IntPtr.Zero);
					if (num3 > 0)
					{
						return GetAndTrimString(new Span<char>((void*)intPtr, num3));
					}
				}
				finally
				{
					Marshal.FreeHGlobal(intPtr);
				}
			}
			return $"Unknown error (0x{errorCode:x})";
		}

		private static string GetAndTrimString(Span<char> buffer)
		{
			int num = buffer.Length;
			while (num > 0 && buffer[num - 1] <= ' ')
			{
				num--;
			}
			return buffer.Slice(0, num).ToString();
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool GetConsoleCursorInfo(IntPtr hConsoleOutput, out CONSOLE_CURSOR_INFO cci);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool SetConsoleCursorInfo(IntPtr hConsoleOutput, ref CONSOLE_CURSOR_INFO cci);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool FillConsoleOutputAttribute(IntPtr hConsoleOutput, short wColorAttribute, int numCells, COORD startCoord, out int pNumBytesWritten);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "FillConsoleOutputCharacterW", SetLastError = true)]
		internal static extern bool FillConsoleOutputCharacter(IntPtr hConsoleOutput, char character, int nLength, COORD dwWriteCoord, out int pNumCharsWritten);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool GetConsoleScreenBufferInfo(IntPtr hConsoleOutput, out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

		[DllImport("kernel32.dll")]
		internal static extern uint GetConsoleCP();

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern uint GetConsoleTitleW(char* title, uint nSize);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool GetConsoleMode(IntPtr handle, out int mode);

		internal static bool IsGetConsoleModeCallSuccessful(IntPtr handle)
		{
			int mode;
			return GetConsoleMode(handle, out mode);
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool SetConsoleMode(IntPtr handle, int mode);

		[DllImport("kernel32.dll")]
		internal static extern uint GetConsoleOutputCP();

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern COORD GetLargestConsoleWindowSize(IntPtr hConsoleOutput);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern uint GetFileType(IntPtr hFile);

		[DllImport("kernel32.dll")]
		[SuppressGCTransition]
		internal static extern IntPtr GetStdHandle(int nStdHandle);

		[DllImport("kernel32.dll")]
		internal unsafe static extern int MultiByteToWideChar(uint CodePage, uint dwFlags, byte* lpMultiByteStr, int cbMultiByte, char* lpWideCharStr, int cchWideChar);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "PeekConsoleInputW", SetLastError = true)]
		internal static extern bool PeekConsoleInput(IntPtr hConsoleInput, out InputRecord buffer, int numInputRecords_UseOne, out int numEventsRead);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal unsafe static extern int ReadFile(IntPtr handle, byte* bytes, int numBytesToRead, out int numBytesRead, IntPtr mustBeZero);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "ReadConsoleW", SetLastError = true)]
		internal unsafe static extern bool ReadConsole(IntPtr hConsoleInput, byte* lpBuffer, int nNumberOfCharsToRead, out int lpNumberOfCharsRead, IntPtr pInputControl);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "ReadConsoleInputW", SetLastError = true)]
		internal static extern bool ReadConsoleInput(IntPtr hConsoleInput, out InputRecord buffer, int numInputRecords_UseOne, out int numEventsRead);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "ReadConsoleOutputW", SetLastError = true)]
		internal unsafe static extern bool ReadConsoleOutput(IntPtr hConsoleOutput, CHAR_INFO* pBuffer, COORD bufferSize, COORD bufferCoord, ref SMALL_RECT readRegion);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool SetConsoleCP(int codePage);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool SetConsoleCursorPosition(IntPtr hConsoleOutput, COORD cursorPosition);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool SetConsoleOutputCP(int codePage);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool SetConsoleScreenBufferSize(IntPtr hConsoleOutput, COORD size);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern int SetConsoleTextAttribute(IntPtr hConsoleOutput, short wAttributes);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "SetConsoleTitleW", SetLastError = true)]
		internal static extern bool SetConsoleTitle(string title);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal unsafe static extern bool SetConsoleWindowInfo(IntPtr hConsoleOutput, bool absolute, SMALL_RECT* consoleWindow);

		[DllImport("kernel32.dll")]
		internal unsafe static extern int WideCharToMultiByte(uint CodePage, uint dwFlags, char* lpWideCharStr, int cchWideChar, byte* lpMultiByteStr, int cbMultiByte, IntPtr lpDefaultChar, IntPtr lpUsedDefaultChar);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal unsafe static extern int WriteFile(IntPtr handle, byte* bytes, int numBytesToWrite, out int numBytesWritten, IntPtr mustBeZero);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "WriteConsoleW", SetLastError = true)]
		internal unsafe static extern bool WriteConsole(IntPtr hConsoleOutput, byte* lpBuffer, int nNumberOfCharsToWrite, out int lpNumberOfCharsWritten, IntPtr lpReservedMustBeNull);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "WriteConsoleOutputW", SetLastError = true)]
		internal unsafe static extern bool WriteConsoleOutput(IntPtr hConsoleOutput, CHAR_INFO* buffer, COORD bufferSize, COORD bufferCoord, ref SMALL_RECT writeRegion);
	}

	internal enum BOOL
	{
		FALSE,
		TRUE
	}

	internal static class User32
	{
		[DllImport("user32.dll")]
		internal static extern short GetKeyState(int virtualKeyCode);
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	internal struct KeyEventRecord
	{
		internal BOOL keyDown;

		internal short repeatCount;

		internal short virtualKeyCode;

		internal short virtualScanCode;

		internal char uChar;

		internal int controlKeyState;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	internal struct InputRecord
	{
		internal short eventType;

		internal KeyEventRecord keyEvent;
	}
}
