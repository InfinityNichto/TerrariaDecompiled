using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static class Interop
{
	internal enum BOOL
	{
		FALSE,
		TRUE
	}

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

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		private unsafe static extern BOOL GetCPInfoExW(uint CodePage, uint dwFlags, CPINFOEXW* lpCPInfoEx);

		internal unsafe static bool TryGetACPCodePage(out int codePage)
		{
			Unsafe.SkipInit(out CPINFOEXW cPINFOEXW);
			if (GetCPInfoExW(0u, 0u, &cPINFOEXW) != 0)
			{
				codePage = (int)cPINFOEXW.CodePage;
				return true;
			}
			codePage = 0;
			return false;
		}
	}
}
