using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Diagnostics;

public sealed class FileVersionInfo
{
	private readonly string _fileName;

	private string _companyName;

	private string _fileDescription;

	private string _fileVersion;

	private string _internalName;

	private string _legalCopyright;

	private string _originalFilename;

	private string _productName;

	private string _productVersion;

	private string _comments;

	private string _legalTrademarks;

	private string _privateBuild;

	private string _specialBuild;

	private string _language;

	private int _fileMajor;

	private int _fileMinor;

	private int _fileBuild;

	private int _filePrivate;

	private int _productMajor;

	private int _productMinor;

	private int _productBuild;

	private int _productPrivate;

	private bool _isDebug;

	private bool _isPatched;

	private bool _isPrivateBuild;

	private bool _isPreRelease;

	private bool _isSpecialBuild;

	private static readonly uint[] s_fallbackLanguageCodePages = new uint[3] { 67699888u, 67699940u, 67698688u };

	public string? Comments => _comments;

	public string? CompanyName => _companyName;

	public int FileBuildPart => _fileBuild;

	public string? FileDescription => _fileDescription;

	public int FileMajorPart => _fileMajor;

	public int FileMinorPart => _fileMinor;

	public string FileName => _fileName;

	public int FilePrivatePart => _filePrivate;

	public string? FileVersion => _fileVersion;

	public string? InternalName => _internalName;

	public bool IsDebug => _isDebug;

	public bool IsPatched => _isPatched;

	public bool IsPrivateBuild => _isPrivateBuild;

	public bool IsPreRelease => _isPreRelease;

	public bool IsSpecialBuild => _isSpecialBuild;

	public string? Language => _language;

	public string? LegalCopyright => _legalCopyright;

	public string? LegalTrademarks => _legalTrademarks;

	public string? OriginalFilename => _originalFilename;

	public string? PrivateBuild => _privateBuild;

	public int ProductBuildPart => _productBuild;

	public int ProductMajorPart => _productMajor;

	public int ProductMinorPart => _productMinor;

	public string? ProductName => _productName;

	public int ProductPrivatePart => _productPrivate;

	public string? ProductVersion => _productVersion;

	public string? SpecialBuild => _specialBuild;

	public static FileVersionInfo GetVersionInfo(string fileName)
	{
		if (!Path.IsPathFullyQualified(fileName))
		{
			fileName = Path.GetFullPath(fileName);
		}
		if (!File.Exists(fileName))
		{
			throw new FileNotFoundException(fileName);
		}
		return new FileVersionInfo(fileName);
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder(512);
		stringBuilder.Append("File:             ").AppendLine(FileName);
		stringBuilder.Append("InternalName:     ").AppendLine(InternalName);
		stringBuilder.Append("OriginalFilename: ").AppendLine(OriginalFilename);
		stringBuilder.Append("FileVersion:      ").AppendLine(FileVersion);
		stringBuilder.Append("FileDescription:  ").AppendLine(FileDescription);
		stringBuilder.Append("Product:          ").AppendLine(ProductName);
		stringBuilder.Append("ProductVersion:   ").AppendLine(ProductVersion);
		stringBuilder.Append("Debug:            ").AppendLine(IsDebug.ToString());
		stringBuilder.Append("Patched:          ").AppendLine(IsPatched.ToString());
		stringBuilder.Append("PreRelease:       ").AppendLine(IsPreRelease.ToString());
		stringBuilder.Append("PrivateBuild:     ").AppendLine(IsPrivateBuild.ToString());
		stringBuilder.Append("SpecialBuild:     ").AppendLine(IsSpecialBuild.ToString());
		stringBuilder.Append("Language:         ").AppendLine(Language);
		return stringBuilder.ToString();
	}

	private unsafe FileVersionInfo(string fileName)
	{
		_fileName = fileName;
		uint lpdwHandle;
		uint fileVersionInfoSizeEx = global::Interop.Version.GetFileVersionInfoSizeEx(1u, _fileName, out lpdwHandle);
		if (fileVersionInfoSizeEx == 0)
		{
			return;
		}
		byte[] array = new byte[fileVersionInfoSizeEx];
		fixed (byte* value = &array[0])
		{
			IntPtr intPtr = new IntPtr(value);
			if (!global::Interop.Version.GetFileVersionInfoEx(3u, _fileName, 0u, fileVersionInfoSizeEx, intPtr))
			{
				return;
			}
			uint varEntry = GetVarEntry(intPtr);
			if (GetVersionInfoForCodePage(intPtr, ConvertTo8DigitHex(varEntry)))
			{
				return;
			}
			uint[] array2 = s_fallbackLanguageCodePages;
			foreach (uint num in array2)
			{
				if (num != varEntry && GetVersionInfoForCodePage(intPtr, ConvertTo8DigitHex(num)))
				{
					break;
				}
			}
		}
	}

	private static string ConvertTo8DigitHex(uint value)
	{
		return value.ToString("X8");
	}

	private static global::Interop.Version.VS_FIXEDFILEINFO GetFixedFileInfo(IntPtr memPtr)
	{
		IntPtr lplpBuffer = IntPtr.Zero;
		if (global::Interop.Version.VerQueryValue(memPtr, "\\", out lplpBuffer, out var _))
		{
			return Marshal.PtrToStructure<global::Interop.Version.VS_FIXEDFILEINFO>(lplpBuffer);
		}
		return default(global::Interop.Version.VS_FIXEDFILEINFO);
	}

	private unsafe static string GetFileVersionLanguage(IntPtr memPtr)
	{
		uint wLang = GetVarEntry(memPtr) >> 16;
		char* ptr = stackalloc char[256];
		int length = global::Interop.Kernel32.VerLanguageName(wLang, ptr, 256u);
		return new string(ptr, 0, length);
	}

	private static string GetFileVersionString(IntPtr memPtr, string name)
	{
		IntPtr lplpBuffer = IntPtr.Zero;
		if (global::Interop.Version.VerQueryValue(memPtr, name, out lplpBuffer, out var _) && lplpBuffer != IntPtr.Zero)
		{
			return Marshal.PtrToStringUni(lplpBuffer);
		}
		return string.Empty;
	}

	private static uint GetVarEntry(IntPtr memPtr)
	{
		IntPtr lplpBuffer = IntPtr.Zero;
		if (global::Interop.Version.VerQueryValue(memPtr, "\\VarFileInfo\\Translation", out lplpBuffer, out var _))
		{
			return (uint)((Marshal.ReadInt16(lplpBuffer) << 16) + Marshal.ReadInt16((IntPtr)((long)lplpBuffer + 2)));
		}
		return 67699940u;
	}

	private bool GetVersionInfoForCodePage(IntPtr memIntPtr, string codepage)
	{
		Span<char> span = stackalloc char[256];
		IFormatProvider formatProvider = null;
		IFormatProvider provider = formatProvider;
		Span<char> span2 = span;
		Span<char> initialBuffer = span2;
		DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(31, 1, formatProvider, span2);
		handler.AppendLiteral("\\\\StringFileInfo\\\\");
		handler.AppendFormatted(codepage);
		handler.AppendLiteral("\\\\CompanyName");
		_companyName = GetFileVersionString(memIntPtr, string.Create(provider, initialBuffer, ref handler));
		formatProvider = null;
		IFormatProvider provider2 = formatProvider;
		span2 = span;
		Span<char> initialBuffer2 = span2;
		DefaultInterpolatedStringHandler handler2 = new DefaultInterpolatedStringHandler(35, 1, formatProvider, span2);
		handler2.AppendLiteral("\\\\StringFileInfo\\\\");
		handler2.AppendFormatted(codepage);
		handler2.AppendLiteral("\\\\FileDescription");
		_fileDescription = GetFileVersionString(memIntPtr, string.Create(provider2, initialBuffer2, ref handler2));
		formatProvider = null;
		IFormatProvider provider3 = formatProvider;
		span2 = span;
		Span<char> initialBuffer3 = span2;
		DefaultInterpolatedStringHandler handler3 = new DefaultInterpolatedStringHandler(31, 1, formatProvider, span2);
		handler3.AppendLiteral("\\\\StringFileInfo\\\\");
		handler3.AppendFormatted(codepage);
		handler3.AppendLiteral("\\\\FileVersion");
		_fileVersion = GetFileVersionString(memIntPtr, string.Create(provider3, initialBuffer3, ref handler3));
		formatProvider = null;
		IFormatProvider provider4 = formatProvider;
		span2 = span;
		Span<char> initialBuffer4 = span2;
		DefaultInterpolatedStringHandler handler4 = new DefaultInterpolatedStringHandler(32, 1, formatProvider, span2);
		handler4.AppendLiteral("\\\\StringFileInfo\\\\");
		handler4.AppendFormatted(codepage);
		handler4.AppendLiteral("\\\\InternalName");
		_internalName = GetFileVersionString(memIntPtr, string.Create(provider4, initialBuffer4, ref handler4));
		formatProvider = null;
		IFormatProvider provider5 = formatProvider;
		span2 = span;
		Span<char> initialBuffer5 = span2;
		DefaultInterpolatedStringHandler handler5 = new DefaultInterpolatedStringHandler(34, 1, formatProvider, span2);
		handler5.AppendLiteral("\\\\StringFileInfo\\\\");
		handler5.AppendFormatted(codepage);
		handler5.AppendLiteral("\\\\LegalCopyright");
		_legalCopyright = GetFileVersionString(memIntPtr, string.Create(provider5, initialBuffer5, ref handler5));
		formatProvider = null;
		IFormatProvider provider6 = formatProvider;
		span2 = span;
		Span<char> initialBuffer6 = span2;
		DefaultInterpolatedStringHandler handler6 = new DefaultInterpolatedStringHandler(36, 1, formatProvider, span2);
		handler6.AppendLiteral("\\\\StringFileInfo\\\\");
		handler6.AppendFormatted(codepage);
		handler6.AppendLiteral("\\\\OriginalFilename");
		_originalFilename = GetFileVersionString(memIntPtr, string.Create(provider6, initialBuffer6, ref handler6));
		formatProvider = null;
		IFormatProvider provider7 = formatProvider;
		span2 = span;
		Span<char> initialBuffer7 = span2;
		DefaultInterpolatedStringHandler handler7 = new DefaultInterpolatedStringHandler(31, 1, formatProvider, span2);
		handler7.AppendLiteral("\\\\StringFileInfo\\\\");
		handler7.AppendFormatted(codepage);
		handler7.AppendLiteral("\\\\ProductName");
		_productName = GetFileVersionString(memIntPtr, string.Create(provider7, initialBuffer7, ref handler7));
		formatProvider = null;
		IFormatProvider provider8 = formatProvider;
		span2 = span;
		Span<char> initialBuffer8 = span2;
		DefaultInterpolatedStringHandler handler8 = new DefaultInterpolatedStringHandler(34, 1, formatProvider, span2);
		handler8.AppendLiteral("\\\\StringFileInfo\\\\");
		handler8.AppendFormatted(codepage);
		handler8.AppendLiteral("\\\\ProductVersion");
		_productVersion = GetFileVersionString(memIntPtr, string.Create(provider8, initialBuffer8, ref handler8));
		formatProvider = null;
		IFormatProvider provider9 = formatProvider;
		span2 = span;
		Span<char> initialBuffer9 = span2;
		DefaultInterpolatedStringHandler handler9 = new DefaultInterpolatedStringHandler(28, 1, formatProvider, span2);
		handler9.AppendLiteral("\\\\StringFileInfo\\\\");
		handler9.AppendFormatted(codepage);
		handler9.AppendLiteral("\\\\Comments");
		_comments = GetFileVersionString(memIntPtr, string.Create(provider9, initialBuffer9, ref handler9));
		formatProvider = null;
		IFormatProvider provider10 = formatProvider;
		span2 = span;
		Span<char> initialBuffer10 = span2;
		DefaultInterpolatedStringHandler handler10 = new DefaultInterpolatedStringHandler(35, 1, formatProvider, span2);
		handler10.AppendLiteral("\\\\StringFileInfo\\\\");
		handler10.AppendFormatted(codepage);
		handler10.AppendLiteral("\\\\LegalTrademarks");
		_legalTrademarks = GetFileVersionString(memIntPtr, string.Create(provider10, initialBuffer10, ref handler10));
		formatProvider = null;
		IFormatProvider provider11 = formatProvider;
		span2 = span;
		Span<char> initialBuffer11 = span2;
		DefaultInterpolatedStringHandler handler11 = new DefaultInterpolatedStringHandler(32, 1, formatProvider, span2);
		handler11.AppendLiteral("\\\\StringFileInfo\\\\");
		handler11.AppendFormatted(codepage);
		handler11.AppendLiteral("\\\\PrivateBuild");
		_privateBuild = GetFileVersionString(memIntPtr, string.Create(provider11, initialBuffer11, ref handler11));
		formatProvider = null;
		IFormatProvider provider12 = formatProvider;
		span2 = span;
		Span<char> initialBuffer12 = span2;
		DefaultInterpolatedStringHandler handler12 = new DefaultInterpolatedStringHandler(32, 1, formatProvider, span2);
		handler12.AppendLiteral("\\\\StringFileInfo\\\\");
		handler12.AppendFormatted(codepage);
		handler12.AppendLiteral("\\\\SpecialBuild");
		_specialBuild = GetFileVersionString(memIntPtr, string.Create(provider12, initialBuffer12, ref handler12));
		_language = GetFileVersionLanguage(memIntPtr);
		global::Interop.Version.VS_FIXEDFILEINFO fixedFileInfo = GetFixedFileInfo(memIntPtr);
		_fileMajor = (int)HIWORD(fixedFileInfo.dwFileVersionMS);
		_fileMinor = (int)LOWORD(fixedFileInfo.dwFileVersionMS);
		_fileBuild = (int)HIWORD(fixedFileInfo.dwFileVersionLS);
		_filePrivate = (int)LOWORD(fixedFileInfo.dwFileVersionLS);
		_productMajor = (int)HIWORD(fixedFileInfo.dwProductVersionMS);
		_productMinor = (int)LOWORD(fixedFileInfo.dwProductVersionMS);
		_productBuild = (int)HIWORD(fixedFileInfo.dwProductVersionLS);
		_productPrivate = (int)LOWORD(fixedFileInfo.dwProductVersionLS);
		_isDebug = (fixedFileInfo.dwFileFlags & 1) != 0;
		_isPatched = (fixedFileInfo.dwFileFlags & 4) != 0;
		_isPrivateBuild = (fixedFileInfo.dwFileFlags & 8) != 0;
		_isPreRelease = (fixedFileInfo.dwFileFlags & 2) != 0;
		_isSpecialBuild = (fixedFileInfo.dwFileFlags & 0x20) != 0;
		return _fileVersion != string.Empty;
	}

	private static uint HIWORD(uint dword)
	{
		return (dword >> 16) & 0xFFFFu;
	}

	private static uint LOWORD(uint dword)
	{
		return dword & 0xFFFFu;
	}
}
