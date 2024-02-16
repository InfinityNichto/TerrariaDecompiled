using System;
using System.Runtime.InteropServices;

internal static class Interop
{
	internal static class Version
	{
		internal struct VS_FIXEDFILEINFO
		{
			internal uint dwSignature;

			internal uint dwStrucVersion;

			internal uint dwFileVersionMS;

			internal uint dwFileVersionLS;

			internal uint dwProductVersionMS;

			internal uint dwProductVersionLS;

			internal uint dwFileFlagsMask;

			internal uint dwFileFlags;

			internal uint dwFileOS;

			internal uint dwFileType;

			internal uint dwFileSubtype;

			internal uint dwFileDateMS;

			internal uint dwFileDateLS;
		}

		[DllImport("version.dll", CharSet = CharSet.Unicode, EntryPoint = "GetFileVersionInfoExW")]
		internal static extern bool GetFileVersionInfoEx(uint dwFlags, string lpwstrFilename, uint dwHandle, uint dwLen, IntPtr lpData);

		[DllImport("version.dll", CharSet = CharSet.Unicode, EntryPoint = "GetFileVersionInfoSizeExW")]
		internal static extern uint GetFileVersionInfoSizeEx(uint dwFlags, string lpwstrFilename, out uint lpdwHandle);

		[DllImport("version.dll", CharSet = CharSet.Unicode, EntryPoint = "VerQueryValueW")]
		internal static extern bool VerQueryValue(IntPtr pBlock, string lpSubBlock, out IntPtr lplpBuffer, out uint puLen);
	}

	internal static class Kernel32
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "VerLanguageNameW")]
		internal unsafe static extern int VerLanguageName(uint wLang, char* szLang, uint cchLang);
	}
}
