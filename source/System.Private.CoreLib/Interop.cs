using System;
using System.Buffers;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Internal.Win32.SafeHandles;
using Microsoft.Win32.SafeHandles;

internal static class Interop
{
	internal static class OleAut32
	{
		[DllImport("oleaut32.dll")]
		internal static extern void VariantClear(IntPtr variant);

		[DllImport("oleaut32.dll")]
		internal static extern IntPtr SysAllocStringByteLen(byte[] str, uint len);

		[DllImport("oleaut32.dll", CharSet = CharSet.Unicode)]
		internal static extern IntPtr SysAllocStringLen(IntPtr src, uint len);

		[DllImport("oleaut32.dll")]
		internal static extern void SysFreeString(IntPtr bstr);
	}

	internal static class Globalization
	{
		internal enum ResultCode
		{
			Success,
			UnknownError,
			InsufficentBuffer,
			OutOfMemory
		}

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetCalendars")]
		internal static extern int GetCalendars(string localeName, CalendarId[] calendars, int calendarsCapacity);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetCalendarInfo")]
		internal unsafe static extern ResultCode GetCalendarInfo(string localeName, CalendarId calendarId, CalendarDataType calendarDataType, char* result, int resultCapacity);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_EnumCalendarInfo")]
		internal unsafe static extern bool EnumCalendarInfo(delegate* unmanaged<char*, IntPtr, void> callback, string localeName, CalendarId calendarId, CalendarDataType calendarDataType, IntPtr context);

		[DllImport("System.Globalization.Native", EntryPoint = "GlobalizationNative_GetLatestJapaneseEra")]
		internal static extern int GetLatestJapaneseEra();

		[DllImport("System.Globalization.Native", EntryPoint = "GlobalizationNative_GetJapaneseEraStartDate")]
		internal static extern bool GetJapaneseEraStartDate(int era, out int startYear, out int startMonth, out int startDay);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_ChangeCase")]
		internal unsafe static extern void ChangeCase(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, bool bToUpper);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_ChangeCaseInvariant")]
		internal unsafe static extern void ChangeCaseInvariant(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, bool bToUpper);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_ChangeCaseTurkish")]
		internal unsafe static extern void ChangeCaseTurkish(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, bool bToUpper);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_InitOrdinalCasingPage")]
		internal unsafe static extern void InitOrdinalCasingPage(int pageNumber, char* pTarget);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Ansi, EntryPoint = "GlobalizationNative_GetSortHandle")]
		internal static extern ResultCode GetSortHandle(string localeName, out IntPtr sortHandle);

		[DllImport("System.Globalization.Native", EntryPoint = "GlobalizationNative_CloseSortHandle")]
		internal static extern void CloseSortHandle(IntPtr handle);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_CompareString")]
		internal unsafe static extern int CompareString(IntPtr sortHandle, char* lpStr1, int cwStr1Len, char* lpStr2, int cwStr2Len, CompareOptions options);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_IndexOf")]
		internal unsafe static extern int IndexOf(IntPtr sortHandle, char* target, int cwTargetLength, char* pSource, int cwSourceLength, CompareOptions options, int* matchLengthPtr);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_LastIndexOf")]
		internal unsafe static extern int LastIndexOf(IntPtr sortHandle, char* target, int cwTargetLength, char* pSource, int cwSourceLength, CompareOptions options, int* matchLengthPtr);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_StartsWith")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal unsafe static extern bool StartsWith(IntPtr sortHandle, char* target, int cwTargetLength, char* source, int cwSourceLength, CompareOptions options, int* matchedLength);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_EndsWith")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal unsafe static extern bool EndsWith(IntPtr sortHandle, char* target, int cwTargetLength, char* source, int cwSourceLength, CompareOptions options, int* matchedLength);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetSortKey")]
		internal unsafe static extern int GetSortKey(IntPtr sortHandle, char* str, int strLength, byte* sortKey, int sortKeyLength, CompareOptions options);

		[DllImport("System.Globalization.Native", EntryPoint = "GlobalizationNative_GetSortVersion")]
		internal static extern int GetSortVersion(IntPtr sortHandle);

		[DllImport("System.Globalization.Native", EntryPoint = "GlobalizationNative_LoadICU")]
		internal static extern int LoadICU();

		internal static void InitICUFunctions(IntPtr icuuc, IntPtr icuin, ReadOnlySpan<char> version, ReadOnlySpan<char> suffix)
		{
			InitICUFunctions(icuuc, icuin, version.ToString(), (suffix.Length > 0) ? suffix.ToString() : null);
		}

		[DllImport("System.Globalization.Native", EntryPoint = "GlobalizationNative_InitICUFunctions")]
		internal static extern void InitICUFunctions(IntPtr icuuc, IntPtr icuin, string version, string suffix);

		[DllImport("System.Globalization.Native", EntryPoint = "GlobalizationNative_GetICUVersion")]
		internal static extern int GetICUVersion();

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_ToAscii")]
		internal unsafe static extern int ToAscii(uint flags, char* src, int srcLen, char* dstBuffer, int dstBufferCapacity);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_ToUnicode")]
		internal unsafe static extern int ToUnicode(uint flags, char* src, int srcLen, char* dstBuffer, int dstBufferCapacity);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetLocaleName")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal unsafe static extern bool GetLocaleName(string localeName, char* value, int valueLength);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetLocaleInfoString")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal unsafe static extern bool GetLocaleInfoString(string localeName, uint localeStringData, char* value, int valueLength, string uiLocaleName = null);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_IsPredefinedLocale")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool IsPredefinedLocale(string localeName);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetLocaleTimeFormat")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal unsafe static extern bool GetLocaleTimeFormat(string localeName, bool shortFormat, char* value, int valueLength);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetLocaleInfoInt")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetLocaleInfoInt(string localeName, uint localeNumberData, ref int value);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetLocaleInfoGroupingSizes")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetLocaleInfoGroupingSizes(string localeName, uint localeGroupingData, ref int primaryGroupSize, ref int secondaryGroupSize);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetLocales")]
		internal static extern int GetLocales([Out] char[] value, int valueLength);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_IsNormalized")]
		internal unsafe static extern int IsNormalized(NormalizationForm normalizationForm, char* src, int srcLen);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_NormalizeString")]
		internal unsafe static extern int NormalizeString(NormalizationForm normalizationForm, char* src, int srcLen, char* dstBuffer, int dstBufferCapacity);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_WindowsIdToIanaId")]
		internal unsafe static extern int WindowsIdToIanaId(string windowsId, IntPtr region, char* ianaId, int ianaIdLength);

		[DllImport("System.Globalization.Native", CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_IanaIdToWindowsId")]
		internal unsafe static extern int IanaIdToWindowsId(string ianaId, char* windowsId, int windowsIdLength);
	}

	internal enum BOOL
	{
		FALSE,
		TRUE
	}

	internal static class Kernel32
	{
		internal struct NlsVersionInfoEx
		{
			internal int dwNLSVersionInfoSize;

			internal int dwNLSVersion;

			internal int dwDefinedVersion;

			internal int dwEffectiveId;

			internal Guid guidCustomVersion;
		}

		internal struct CONDITION_VARIABLE
		{
			private IntPtr Ptr;
		}

		internal struct CRITICAL_SECTION
		{
			private IntPtr DebugInfo;

			private int LockCount;

			private int RecursionCount;

			private IntPtr OwningThread;

			private IntPtr LockSemaphore;

			private UIntPtr SpinCount;
		}

		internal struct FILE_BASIC_INFO
		{
			internal long CreationTime;

			internal long LastAccessTime;

			internal long LastWriteTime;

			internal long ChangeTime;

			internal uint FileAttributes;
		}

		internal struct FILE_ALLOCATION_INFO
		{
			internal long AllocationSize;
		}

		internal struct FILE_END_OF_FILE_INFO
		{
			internal long EndOfFile;
		}

		internal struct FILE_STANDARD_INFO
		{
			internal long AllocationSize;

			internal long EndOfFile;

			internal uint NumberOfLinks;

			internal BOOL DeletePending;

			internal BOOL Directory;
		}

		internal struct FILE_TIME
		{
			internal uint dwLowDateTime;

			internal uint dwHighDateTime;

			internal long ToTicks()
			{
				return (long)(((ulong)dwHighDateTime << 32) + dwLowDateTime);
			}

			internal DateTimeOffset ToDateTimeOffset()
			{
				return DateTimeOffset.FromFileTime(ToTicks());
			}
		}

		internal enum FINDEX_INFO_LEVELS : uint
		{
			FindExInfoStandard,
			FindExInfoBasic,
			FindExInfoMaxInfoLevel
		}

		internal enum FINDEX_SEARCH_OPS : uint
		{
			FindExSearchNameMatch,
			FindExSearchLimitToDirectories,
			FindExSearchLimitToDevices,
			FindExSearchMaxSearchOp
		}

		internal enum GET_FILEEX_INFO_LEVELS : uint
		{
			GetFileExInfoStandard,
			GetFileExMaxInfoLevel
		}

		internal struct CPINFO
		{
			internal int MaxCharSize;

			internal unsafe fixed byte DefaultChar[2];

			internal unsafe fixed byte LeadByte[12];
		}

		internal struct PROCESS_MEMORY_COUNTERS
		{
			public uint cb;

			public uint PageFaultCount;

			public UIntPtr PeakWorkingSetSize;

			public UIntPtr WorkingSetSize;

			public UIntPtr QuotaPeakPagedPoolUsage;

			public UIntPtr QuotaPagedPoolUsage;

			public UIntPtr QuotaPeakNonPagedPoolUsage;

			public UIntPtr QuotaNonPagedPoolUsage;

			public UIntPtr PagefileUsage;

			public UIntPtr PeakPagefileUsage;
		}

		internal struct MEMORY_BASIC_INFORMATION
		{
			internal unsafe void* BaseAddress;

			internal unsafe void* AllocationBase;

			internal uint AllocationProtect;

			internal UIntPtr RegionSize;

			internal uint State;

			internal uint Protect;

			internal uint Type;
		}

		internal struct MEMORYSTATUSEX
		{
			internal uint dwLength;

			internal uint dwMemoryLoad;

			internal ulong ullTotalPhys;

			internal ulong ullAvailPhys;

			internal ulong ullTotalPageFile;

			internal ulong ullAvailPageFile;

			internal ulong ullTotalVirtual;

			internal ulong ullAvailVirtual;

			internal ulong ullAvailExtendedVirtual;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct OSVERSIONINFOEX
		{
			public int dwOSVersionInfoSize;

			public int dwMajorVersion;

			public int dwMinorVersion;

			public int dwBuildNumber;

			public int dwPlatformId;

			public unsafe fixed char szCSDVersion[128];

			public ushort wServicePackMajor;

			public ushort wServicePackMinor;

			public ushort wSuiteMask;

			public byte wProductType;

			public byte wReserved;
		}

		internal struct SymbolicLinkReparseBuffer
		{
			internal uint ReparseTag;

			internal ushort ReparseDataLength;

			internal ushort Reserved;

			internal ushort SubstituteNameOffset;

			internal ushort SubstituteNameLength;

			internal ushort PrintNameOffset;

			internal ushort PrintNameLength;

			internal uint Flags;
		}

		internal struct MountPointReparseBuffer
		{
			public uint ReparseTag;

			public ushort ReparseDataLength;

			public ushort Reserved;

			public ushort SubstituteNameOffset;

			public ushort SubstituteNameLength;

			public ushort PrintNameOffset;

			public ushort PrintNameLength;
		}

		internal struct SECURITY_ATTRIBUTES
		{
			internal uint nLength;

			internal IntPtr lpSecurityDescriptor;

			internal BOOL bInheritHandle;
		}

		internal struct SYSTEM_INFO
		{
			internal ushort wProcessorArchitecture;

			internal ushort wReserved;

			internal int dwPageSize;

			internal IntPtr lpMinimumApplicationAddress;

			internal IntPtr lpMaximumApplicationAddress;

			internal IntPtr dwActiveProcessorMask;

			internal int dwNumberOfProcessors;

			internal int dwProcessorType;

			internal int dwAllocationGranularity;

			internal short wProcessorLevel;

			internal short wProcessorRevision;
		}

		internal struct SYSTEMTIME
		{
			internal ushort Year;

			internal ushort Month;

			internal ushort DayOfWeek;

			internal ushort Day;

			internal ushort Hour;

			internal ushort Minute;

			internal ushort Second;

			internal ushort Milliseconds;

			internal bool Equals(in SYSTEMTIME other)
			{
				if (Year == other.Year && Month == other.Month && DayOfWeek == other.DayOfWeek && Day == other.Day && Hour == other.Hour && Minute == other.Minute && Second == other.Second)
				{
					return Milliseconds == other.Milliseconds;
				}
				return false;
			}
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct TIME_DYNAMIC_ZONE_INFORMATION
		{
			internal int Bias;

			internal unsafe fixed char StandardName[32];

			internal SYSTEMTIME StandardDate;

			internal int StandardBias;

			internal unsafe fixed char DaylightName[32];

			internal SYSTEMTIME DaylightDate;

			internal int DaylightBias;

			internal unsafe fixed char TimeZoneKeyName[128];

			internal byte DynamicDaylightTimeDisabled;

			internal unsafe string GetTimeZoneKeyName()
			{
				fixed (char* value = TimeZoneKeyName)
				{
					return new string(value);
				}
			}
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct TIME_ZONE_INFORMATION
		{
			internal int Bias;

			internal unsafe fixed char StandardName[32];

			internal SYSTEMTIME StandardDate;

			internal int StandardBias;

			internal unsafe fixed char DaylightName[32];

			internal SYSTEMTIME DaylightDate;

			internal int DaylightBias;

			internal unsafe TIME_ZONE_INFORMATION(in TIME_DYNAMIC_ZONE_INFORMATION dtzi)
			{
				fixed (TIME_ZONE_INFORMATION* ptr = &this)
				{
					fixed (TIME_DYNAMIC_ZONE_INFORMATION* ptr2 = &dtzi)
					{
						*ptr = *(TIME_ZONE_INFORMATION*)ptr2;
					}
				}
			}

			internal unsafe string GetStandardName()
			{
				fixed (char* value = StandardName)
				{
					return new string(value);
				}
			}

			internal unsafe string GetDaylightName()
			{
				fixed (char* value = DaylightName)
				{
					return new string(value);
				}
			}
		}

		internal struct REG_TZI_FORMAT
		{
			internal int Bias;

			internal int StandardBias;

			internal int DaylightBias;

			internal SYSTEMTIME StandardDate;

			internal SYSTEMTIME DaylightDate;

			internal REG_TZI_FORMAT(in TIME_ZONE_INFORMATION tzi)
			{
				Bias = tzi.Bias;
				StandardDate = tzi.StandardDate;
				StandardBias = tzi.StandardBias;
				DaylightDate = tzi.DaylightDate;
				DaylightBias = tzi.DaylightBias;
			}
		}

		internal struct WIN32_FILE_ATTRIBUTE_DATA
		{
			internal int dwFileAttributes;

			internal FILE_TIME ftCreationTime;

			internal FILE_TIME ftLastAccessTime;

			internal FILE_TIME ftLastWriteTime;

			internal uint nFileSizeHigh;

			internal uint nFileSizeLow;

			internal void PopulateFrom(ref WIN32_FIND_DATA findData)
			{
				dwFileAttributes = (int)findData.dwFileAttributes;
				ftCreationTime = findData.ftCreationTime;
				ftLastAccessTime = findData.ftLastAccessTime;
				ftLastWriteTime = findData.ftLastWriteTime;
				nFileSizeHigh = findData.nFileSizeHigh;
				nFileSizeLow = findData.nFileSizeLow;
			}
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct WIN32_FIND_DATA
		{
			internal uint dwFileAttributes;

			internal FILE_TIME ftCreationTime;

			internal FILE_TIME ftLastAccessTime;

			internal FILE_TIME ftLastWriteTime;

			internal uint nFileSizeHigh;

			internal uint nFileSizeLow;

			internal uint dwReserved0;

			internal uint dwReserved1;

			private unsafe fixed char _cFileName[260];

			private unsafe fixed char _cAlternateFileName[14];

			internal unsafe ReadOnlySpan<char> cFileName
			{
				get
				{
					fixed (char* pointer = _cFileName)
					{
						return new ReadOnlySpan<char>(pointer, 260);
					}
				}
			}
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		internal unsafe static extern int LCIDToLocaleName(int locale, char* pLocaleName, int cchName, uint dwFlags);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		internal static extern int LocaleNameToLCID(string lpName, uint dwFlags);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal unsafe static extern int LCMapStringEx(string lpLocaleName, uint dwMapFlags, char* lpSrcStr, int cchSrc, void* lpDestStr, int cchDest, void* lpVersionInformation, void* lpReserved, IntPtr sortHandle);

		[DllImport("kernel32.dll")]
		internal unsafe static extern int FindNLSStringEx(char* lpLocaleName, uint dwFindNLSStringFlags, char* lpStringSource, int cchSource, char* lpStringValue, int cchValue, int* pcchFound, void* lpVersionInformation, void* lpReserved, IntPtr sortHandle);

		[DllImport("kernel32.dll")]
		internal unsafe static extern int CompareStringEx(char* lpLocaleName, uint dwCmpFlags, char* lpString1, int cchCount1, char* lpString2, int cchCount2, void* lpVersionInformation, void* lpReserved, IntPtr lParam);

		[DllImport("kernel32.dll")]
		internal unsafe static extern int CompareStringOrdinal(char* lpString1, int cchCount1, char* lpString2, int cchCount2, bool bIgnoreCase);

		[DllImport("kernel32.dll")]
		internal unsafe static extern int FindStringOrdinal(uint dwFindStringOrdinalFlags, char* lpStringSource, int cchSource, char* lpStringValue, int cchValue, BOOL bIgnoreCase);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		internal unsafe static extern bool IsNLSDefinedString(int Function, uint dwFlags, IntPtr lpVersionInformation, char* lpString, int cchStr);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		internal unsafe static extern BOOL GetUserPreferredUILanguages(uint dwFlags, uint* pulNumLanguages, char* pwszLanguagesBuffer, uint* pcchLanguagesBuffer);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		internal unsafe static extern int GetLocaleInfoEx(string lpLocaleName, uint LCType, void* lpLCData, int cchData);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		internal unsafe static extern bool EnumSystemLocalesEx(delegate* unmanaged<char*, uint, void*, BOOL> lpLocaleEnumProcEx, uint dwFlags, void* lParam, IntPtr reserved);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		internal unsafe static extern bool EnumTimeFormatsEx(delegate* unmanaged<char*, void*, BOOL> lpTimeFmtEnumProcEx, string lpLocaleName, uint dwFlags, void* lParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		internal static extern int GetCalendarInfoEx(string lpLocaleName, uint Calendar, IntPtr lpReserved, uint CalType, IntPtr lpCalData, int cchData, out int lpValue);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		internal static extern int GetCalendarInfoEx(string lpLocaleName, uint Calendar, IntPtr lpReserved, uint CalType, IntPtr lpCalData, int cchData, IntPtr lpValue);

		[DllImport("kernel32.dll")]
		internal static extern int GetUserGeoID(int geoClass);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		internal unsafe static extern int GetGeoInfo(int location, int geoType, char* lpGeoData, int cchData, int LangId);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		internal unsafe static extern bool EnumCalendarInfoExEx(delegate* unmanaged<char*, uint, IntPtr, void*, BOOL> pCalInfoEnumProcExEx, string lpLocaleName, uint Calendar, string lpReserved, uint CalType, void* lParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		internal unsafe static extern bool GetNLSVersionEx(int function, string localeName, NlsVersionInfoEx* lpVersionInformation);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		internal unsafe static extern int ResolveLocaleName(string lpNameToResolve, char* lpLocaleName, int cchLocaleName);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal unsafe static extern bool CancelIoEx(SafeHandle handle, NativeOverlapped* lpOverlapped);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern IntPtr CreateIoCompletionPort(IntPtr FileHandle, IntPtr ExistingCompletionPort, UIntPtr CompletionKey, int NumberOfConcurrentThreads);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool PostQueuedCompletionStatus(IntPtr CompletionPort, int dwNumberOfBytesTransferred, UIntPtr CompletionKey, IntPtr lpOverlapped);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetQueuedCompletionStatus(IntPtr CompletionPort, out int lpNumberOfBytes, out UIntPtr CompletionKey, out IntPtr lpOverlapped, int dwMilliseconds);

		[DllImport("kernel32.dll", ExactSpelling = true)]
		internal unsafe static extern void InitializeConditionVariable(CONDITION_VARIABLE* ConditionVariable);

		[DllImport("kernel32.dll", ExactSpelling = true)]
		internal unsafe static extern void WakeConditionVariable(CONDITION_VARIABLE* ConditionVariable);

		[DllImport("kernel32.dll", ExactSpelling = true)]
		internal unsafe static extern bool SleepConditionVariableCS(CONDITION_VARIABLE* ConditionVariable, CRITICAL_SECTION* CriticalSection, int dwMilliseconds);

		internal static int CopyFile(string src, string dst, bool failIfExists)
		{
			int flags = (failIfExists ? 1 : 0);
			int cancel = 0;
			if (!CopyFileEx(src, dst, IntPtr.Zero, IntPtr.Zero, ref cancel, flags))
			{
				return Marshal.GetLastWin32Error();
			}
			return 0;
		}

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "CopyFileExW", SetLastError = true)]
		private static extern bool CopyFileExPrivate(string src, string dst, IntPtr progressRoutine, IntPtr progressData, ref int cancel, int flags);

		internal static bool CopyFileEx(string src, string dst, IntPtr progressRoutine, IntPtr progressData, ref int cancel, int flags)
		{
			src = PathInternal.EnsureExtendedPrefixIfNeeded(src);
			dst = PathInternal.EnsureExtendedPrefixIfNeeded(dst);
			return CopyFileExPrivate(src, dst, progressRoutine, progressData, ref cancel, flags);
		}

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "CreateDirectoryW", SetLastError = true)]
		private static extern bool CreateDirectoryPrivate(string path, ref SECURITY_ATTRIBUTES lpSecurityAttributes);

		internal static bool CreateDirectory(string path, ref SECURITY_ATTRIBUTES lpSecurityAttributes)
		{
			path = PathInternal.EnsureExtendedPrefix(path);
			return CreateDirectoryPrivate(path, ref lpSecurityAttributes);
		}

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "CreateFileW", ExactSpelling = true, SetLastError = true)]
		private unsafe static extern SafeFileHandle CreateFilePrivate(string lpFileName, int dwDesiredAccess, FileShare dwShareMode, SECURITY_ATTRIBUTES* lpSecurityAttributes, FileMode dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile);

		internal unsafe static SafeFileHandle CreateFile(string lpFileName, int dwDesiredAccess, FileShare dwShareMode, SECURITY_ATTRIBUTES* lpSecurityAttributes, FileMode dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile)
		{
			lpFileName = PathInternal.EnsureExtendedPrefixIfNeeded(lpFileName);
			return CreateFilePrivate(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
		}

		internal unsafe static SafeFileHandle CreateFile(string lpFileName, int dwDesiredAccess, FileShare dwShareMode, FileMode dwCreationDisposition, int dwFlagsAndAttributes)
		{
			lpFileName = PathInternal.EnsureExtendedPrefixIfNeeded(lpFileName);
			return CreateFilePrivate(lpFileName, dwDesiredAccess, dwShareMode, null, dwCreationDisposition, dwFlagsAndAttributes, IntPtr.Zero);
		}

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "DeleteFileW", SetLastError = true)]
		private static extern bool DeleteFilePrivate(string path);

		internal static bool DeleteFile(string path)
		{
			path = PathInternal.EnsureExtendedPrefixIfNeeded(path);
			return DeleteFilePrivate(path);
		}

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "DeleteVolumeMountPointW", SetLastError = true)]
		internal static extern bool DeleteVolumeMountPointPrivate(string mountPoint);

		internal static bool DeleteVolumeMountPoint(string mountPoint)
		{
			mountPoint = PathInternal.EnsureExtendedPrefixIfNeeded(mountPoint);
			return DeleteVolumeMountPointPrivate(mountPoint);
		}

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "CreateFileW", ExactSpelling = true, SetLastError = true)]
		private unsafe static extern IntPtr CreateFilePrivate_IntPtr(string lpFileName, int dwDesiredAccess, FileShare dwShareMode, SECURITY_ATTRIBUTES* lpSecurityAttributes, FileMode dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile);

		internal unsafe static IntPtr CreateFile_IntPtr(string lpFileName, int dwDesiredAccess, FileShare dwShareMode, FileMode dwCreationDisposition, int dwFlagsAndAttributes)
		{
			lpFileName = PathInternal.EnsureExtendedPrefixIfNeeded(lpFileName);
			return CreateFilePrivate_IntPtr(lpFileName, dwDesiredAccess, dwShareMode, null, dwCreationDisposition, dwFlagsAndAttributes, IntPtr.Zero);
		}

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "CreateSymbolicLinkW", ExactSpelling = true, SetLastError = true)]
		private static extern bool CreateSymbolicLinkPrivate(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

		internal static void CreateSymbolicLink(string symlinkFileName, string targetFileName, bool isDirectory)
		{
			string path = symlinkFileName;
			symlinkFileName = PathInternal.EnsureExtendedPrefixIfNeeded(symlinkFileName);
			targetFileName = PathInternal.EnsureExtendedPrefixIfNeeded(targetFileName);
			int num = 0;
			bool flag = (Environment.OSVersion.Version.Major == 10 && Environment.OSVersion.Version.Build >= 14972) || Environment.OSVersion.Version.Major >= 11;
			if (flag)
			{
				num = 2;
			}
			if (isDirectory)
			{
				num |= 1;
			}
			if (!CreateSymbolicLinkPrivate(symlinkFileName, targetFileName, num))
			{
				throw Win32Marshal.GetExceptionForLastWin32Error(path);
			}
			int lastWin32Error;
			if (!flag && (lastWin32Error = Marshal.GetLastWin32Error()) != 0)
			{
				throw Win32Marshal.GetExceptionForWin32Error(lastWin32Error, path);
			}
		}

		[DllImport("kernel32.dll", ExactSpelling = true)]
		internal unsafe static extern void InitializeCriticalSection(CRITICAL_SECTION* lpCriticalSection);

		[DllImport("kernel32.dll", ExactSpelling = true)]
		internal unsafe static extern void EnterCriticalSection(CRITICAL_SECTION* lpCriticalSection);

		[DllImport("kernel32.dll", ExactSpelling = true)]
		internal unsafe static extern void LeaveCriticalSection(CRITICAL_SECTION* lpCriticalSection);

		[DllImport("kernel32.dll", ExactSpelling = true)]
		internal unsafe static extern void DeleteCriticalSection(CRITICAL_SECTION* lpCriticalSection);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal static extern bool DeviceIoControl(SafeHandle hDevice, uint dwIoControlCode, IntPtr lpInBuffer, uint nInBufferSize, byte[] lpOutBuffer, uint nOutBufferSize, out uint lpBytesReturned, IntPtr lpOverlapped);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "ExpandEnvironmentStringsW", ExactSpelling = true, SetLastError = true)]
		internal static extern uint ExpandEnvironmentStrings(string lpSrc, ref char lpDst, uint nSize);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "FindNextFileW", SetLastError = true)]
		internal static extern bool FindNextFile(SafeFindHandle hndFindFile, ref WIN32_FIND_DATA lpFindFileData);

		[DllImport("kernel32.dll")]
		[SuppressGCTransition]
		internal unsafe static extern BOOL FileTimeToSystemTime(ulong* lpFileTime, SYSTEMTIME* lpSystemTime);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool FindClose(IntPtr hFindFile);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "FindFirstFileExW", ExactSpelling = true, SetLastError = true)]
		private static extern SafeFindHandle FindFirstFileExPrivate(string lpFileName, FINDEX_INFO_LEVELS fInfoLevelId, ref WIN32_FIND_DATA lpFindFileData, FINDEX_SEARCH_OPS fSearchOp, IntPtr lpSearchFilter, int dwAdditionalFlags);

		internal static SafeFindHandle FindFirstFile(string fileName, ref WIN32_FIND_DATA data)
		{
			fileName = PathInternal.EnsureExtendedPrefixIfNeeded(fileName);
			return FindFirstFileExPrivate(fileName, FINDEX_INFO_LEVELS.FindExInfoBasic, ref data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, 0);
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool FlushFileBuffers(SafeHandle hHandle);

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern bool FreeLibrary(IntPtr hModule);

		[DllImport("kernel32.dll")]
		[SuppressGCTransition]
		internal static extern int GetLastError();

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetComputerNameW", ExactSpelling = true)]
		private static extern int GetComputerName(ref char lpBuffer, ref uint nSize);

		internal static string GetComputerName()
		{
			Span<char> span = stackalloc char[16];
			uint nSize = (uint)span.Length;
			if (GetComputerName(ref MemoryMarshal.GetReference(span), ref nSize) == 0)
			{
				return null;
			}
			return span.Slice(0, (int)nSize).ToString();
		}

		[DllImport("kernel32.dll")]
		internal unsafe static extern BOOL GetCPInfo(uint codePage, CPINFO* lpCpInfo);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "GetCurrentDirectoryW", ExactSpelling = true, SetLastError = true)]
		internal static extern uint GetCurrentDirectory(uint nBufferLength, ref char lpBuffer);

		[DllImport("kernel32.dll")]
		internal static extern IntPtr GetCurrentProcess();

		[DllImport("kernel32.dll")]
		internal static extern uint GetCurrentProcessId();

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetFileAttributesExW", ExactSpelling = true, SetLastError = true)]
		private static extern bool GetFileAttributesExPrivate(string name, GET_FILEEX_INFO_LEVELS fileInfoLevel, ref WIN32_FILE_ATTRIBUTE_DATA lpFileInformation);

		internal static bool GetFileAttributesEx(string name, GET_FILEEX_INFO_LEVELS fileInfoLevel, ref WIN32_FILE_ATTRIBUTE_DATA lpFileInformation)
		{
			name = PathInternal.EnsureExtendedPrefixIfNeeded(name);
			return GetFileAttributesExPrivate(name, fileInfoLevel, ref lpFileInformation);
		}

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern bool GetFileInformationByHandleEx(SafeFileHandle hFile, int FileInformationClass, void* lpFileInformation, uint dwBufferSize);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern int GetFileType(SafeHandle hFile);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetFinalPathNameByHandleW", ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern uint GetFinalPathNameByHandle(SafeFileHandle hFile, char* lpszFilePath, uint cchFilePath, uint dwFlags);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal static extern uint GetFullPathNameW(ref char lpFileName, uint nBufferLength, ref char lpBuffer, IntPtr lpFilePart);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern int GetLogicalDrives();

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal static extern uint GetLongPathNameW(ref char lpszShortPath, ref char lpszLongPath, uint cchBuffer);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetModuleFileNameW", ExactSpelling = true, SetLastError = true)]
		internal static extern uint GetModuleFileName(IntPtr hModule, ref char lpFilename, uint nSize);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		internal unsafe static extern bool GetOverlappedResult(SafeFileHandle hFile, NativeOverlapped* lpOverlapped, ref int lpNumberOfBytesTransferred, bool bWait);

		[DllImport("kernel32.dll", EntryPoint = "K32GetProcessMemoryInfo")]
		internal static extern bool GetProcessMemoryInfo(IntPtr Process, ref PROCESS_MEMORY_COUNTERS ppsmemCounters, uint cb);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool GetProcessTimes(IntPtr handleProcess, out long creation, out long exit, out long kernel, out long user);

		[DllImport("kernel32.dll")]
		[SuppressGCTransition]
		internal static extern IntPtr GetStdHandle(int nStdHandle);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal static extern uint GetSystemDirectoryW(ref char lpBuffer, uint uSize);

		[DllImport("kernel32.dll")]
		internal static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

		[DllImport("kernel32.dll")]
		[SuppressGCTransition]
		internal unsafe static extern void GetSystemTime(SYSTEMTIME* lpSystemTime);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool GetSystemTimes(out long idle, out long kernel, out long user);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal static extern uint GetTempFileNameW(ref char lpPathName, string lpPrefixString, uint uUnique, ref char lpTempFileName);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, ExactSpelling = true)]
		internal static extern uint GetTempPathW(int bufferLen, ref char buffer);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "GetVolumeInformationW", SetLastError = true)]
		internal unsafe static extern bool GetVolumeInformation(string drive, char* volumeName, int volumeNameBufLen, int* volSerialNumber, int* maxFileNameLen, out int fileSystemFlags, char* fileSystemName, int fileSystemNameBufLen);

		[DllImport("kernel32.dll")]
		internal unsafe static extern BOOL GlobalMemoryStatusEx(MEMORYSTATUSEX* lpBuffer);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "LoadLibraryExW", ExactSpelling = true, SetLastError = true)]
		internal static extern IntPtr LoadLibraryEx(string libFilename, IntPtr reserved, int flags);

		[DllImport("kernel32.dll")]
		internal static extern IntPtr LocalAlloc(uint uFlags, nuint uBytes);

		[DllImport("kernel32.dll")]
		internal static extern IntPtr LocalReAlloc(IntPtr hMem, nuint uBytes, uint uFlags);

		[DllImport("kernel32.dll")]
		internal static extern IntPtr LocalFree(IntPtr hMem);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool LockFile(SafeFileHandle handle, int offsetLow, int offsetHigh, int countLow, int countHigh);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool UnlockFile(SafeFileHandle handle, int offsetLow, int offsetHigh, int countLow, int countHigh);

		[DllImport("kernel32.dll")]
		internal unsafe static extern int MultiByteToWideChar(uint CodePage, uint dwFlags, byte* lpMultiByteStr, int cbMultiByte, char* lpWideCharStr, int cchWideChar);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "MoveFileExW", SetLastError = true)]
		private static extern bool MoveFileExPrivate(string src, string dst, uint flags);

		internal static bool MoveFile(string src, string dst, bool overwrite)
		{
			src = PathInternal.EnsureExtendedPrefixIfNeeded(src);
			dst = PathInternal.EnsureExtendedPrefixIfNeeded(dst);
			uint num = 2u;
			if (overwrite)
			{
				num |= 1u;
			}
			return MoveFileExPrivate(src, dst, num);
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "OutputDebugStringW", ExactSpelling = true)]
		internal static extern void OutputDebugString(string message);

		[DllImport("kernel32.dll", ExactSpelling = true)]
		[SuppressGCTransition]
		internal unsafe static extern BOOL QueryPerformanceCounter(long* lpPerformanceCount);

		[DllImport("kernel32.dll", ExactSpelling = true)]
		internal unsafe static extern BOOL QueryPerformanceFrequency(long* lpFrequency);

		[DllImport("kernel32.dll")]
		internal static extern bool QueryUnbiasedInterruptTime(out ulong UnbiasedTime);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal unsafe static extern int ReadFile(SafeHandle handle, byte* bytes, int numBytesToRead, IntPtr numBytesRead_mustBeZero, NativeOverlapped* overlapped);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal unsafe static extern int ReadFile(SafeHandle handle, byte* bytes, int numBytesToRead, out int numBytesRead, NativeOverlapped* overlapped);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal unsafe static extern int ReadFileScatter(SafeHandle hFile, long* aSegmentArray, int nNumberOfBytesToRead, IntPtr lpReserved, NativeOverlapped* lpOverlapped);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal unsafe static extern int WriteFileGather(SafeHandle hFile, long* aSegmentArray, int nNumberOfBytesToWrite, IntPtr lpReserved, NativeOverlapped* lpOverlapped);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "RemoveDirectoryW", SetLastError = true)]
		private static extern bool RemoveDirectoryPrivate(string path);

		internal static bool RemoveDirectory(string path)
		{
			path = PathInternal.EnsureExtendedPrefixIfNeeded(path);
			return RemoveDirectoryPrivate(path);
		}

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "ReplaceFileW", SetLastError = true)]
		private static extern bool ReplaceFilePrivate(string replacedFileName, string replacementFileName, string backupFileName, int dwReplaceFlags, IntPtr lpExclude, IntPtr lpReserved);

		internal static bool ReplaceFile(string replacedFileName, string replacementFileName, string backupFileName, int dwReplaceFlags, IntPtr lpExclude, IntPtr lpReserved)
		{
			replacedFileName = PathInternal.EnsureExtendedPrefixIfNeeded(replacedFileName);
			replacementFileName = PathInternal.EnsureExtendedPrefixIfNeeded(replacementFileName);
			backupFileName = PathInternal.EnsureExtendedPrefixIfNeeded(backupFileName);
			return ReplaceFilePrivate(replacedFileName, replacementFileName, backupFileName, dwReplaceFlags, lpExclude, lpReserved);
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		internal unsafe static extern bool SetConsoleCtrlHandler(delegate* unmanaged<int, BOOL> HandlerRoutine, bool Add);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "SetCurrentDirectoryW", ExactSpelling = true, SetLastError = true)]
		internal static extern bool SetCurrentDirectory(string path);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "SetFileAttributesW", SetLastError = true)]
		private static extern bool SetFileAttributesPrivate(string name, int attr);

		internal static bool SetFileAttributes(string name, int attr)
		{
			name = PathInternal.EnsureExtendedPrefixIfNeeded(name);
			return SetFileAttributesPrivate(name, attr);
		}

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern bool SetFileInformationByHandle(SafeFileHandle hFile, int FileInformationClass, void* lpFileInformation, uint dwBufferSize);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool SetFilePointerEx(SafeFileHandle hFile, long liDistanceToMove, out long lpNewFilePointer, uint dwMoveMethod);

		[DllImport("kernel32.dll")]
		[SuppressGCTransition]
		internal static extern void SetLastError(int errorCode);

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		[SuppressGCTransition]
		internal static extern bool SetThreadErrorMode(uint dwNewMode, out uint lpOldMode);

		[DllImport("kernel32.dll")]
		[SuppressGCTransition]
		internal unsafe static extern BOOL SystemTimeToFileTime(SYSTEMTIME* lpSystemTime, ulong* lpFileTime);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal static extern uint GetDynamicTimeZoneInformation(out TIME_DYNAMIC_ZONE_INFORMATION pTimeZoneInformation);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal static extern uint GetTimeZoneInformation(out TIME_ZONE_INFORMATION lpTimeZoneInformation);

		[DllImport("kernel32.dll")]
		internal unsafe static extern BOOL TzSpecificLocalTimeToSystemTime(IntPtr lpTimeZoneInformation, SYSTEMTIME* lpLocalTime, SYSTEMTIME* lpUniversalTime);

		[DllImport("kernel32.dll")]
		internal static extern bool VerifyVersionInfoW(ref OSVERSIONINFOEX lpVersionInfo, uint dwTypeMask, ulong dwlConditionMask);

		[DllImport("kernel32.dll")]
		internal static extern ulong VerSetConditionMask(ulong ConditionMask, uint TypeMask, byte Condition);

		[DllImport("kernel32.dll", ExactSpelling = true)]
		internal unsafe static extern void* VirtualAlloc(void* lpAddress, UIntPtr dwSize, int flAllocationType, int flProtect);

		[DllImport("kernel32.dll", ExactSpelling = true)]
		internal unsafe static extern bool VirtualFree(void* lpAddress, UIntPtr dwSize, int dwFreeType);

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern UIntPtr VirtualQuery(void* lpAddress, ref MEMORY_BASIC_INFORMATION lpBuffer, UIntPtr dwLength);

		[DllImport("kernel32.dll")]
		internal unsafe static extern int WideCharToMultiByte(uint CodePage, uint dwFlags, char* lpWideCharStr, int cchWideChar, byte* lpMultiByteStr, int cbMultiByte, IntPtr lpDefaultChar, IntPtr lpUsedDefaultChar);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal unsafe static extern int WriteFile(SafeHandle handle, byte* bytes, int numBytesToWrite, IntPtr numBytesWritten_mustBeZero, NativeOverlapped* lpOverlapped);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal unsafe static extern int WriteFile(SafeHandle handle, byte* bytes, int numBytesToWrite, out int numBytesWritten, NativeOverlapped* lpOverlapped);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool CloseHandle(IntPtr handle);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool SetEvent(SafeWaitHandle handle);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool ResetEvent(SafeWaitHandle handle);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "CreateEventExW", ExactSpelling = true, SetLastError = true)]
		internal static extern SafeWaitHandle CreateEventEx(IntPtr lpSecurityAttributes, string name, uint flags, uint desiredAccess);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "OpenEventW", ExactSpelling = true, SetLastError = true)]
		internal static extern SafeWaitHandle OpenEvent(uint desiredAccess, bool inheritHandle, string name);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetEnvironmentVariableW", ExactSpelling = true, SetLastError = true)]
		internal static extern uint GetEnvironmentVariable(string lpName, ref char lpBuffer, uint nSize);

		[DllImport("kernel32.dll", ExactSpelling = true)]
		internal unsafe static extern char* GetEnvironmentStringsW();

		[DllImport("kernel32.dll", ExactSpelling = true)]
		internal unsafe static extern BOOL FreeEnvironmentStringsW(char* lpszEnvironmentBlock);

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

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "OpenMutexW", ExactSpelling = true, SetLastError = true)]
		internal static extern SafeWaitHandle OpenMutex(uint desiredAccess, bool inheritHandle, string name);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "CreateMutexExW", ExactSpelling = true, SetLastError = true)]
		internal static extern SafeWaitHandle CreateMutexEx(IntPtr lpMutexAttributes, string name, uint flags, uint desiredAccess);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool ReleaseMutex(SafeWaitHandle handle);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "OpenSemaphoreW", ExactSpelling = true, SetLastError = true)]
		internal static extern SafeWaitHandle OpenSemaphore(uint desiredAccess, bool inheritHandle, string name);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "CreateSemaphoreExW", ExactSpelling = true, SetLastError = true)]
		internal static extern SafeWaitHandle CreateSemaphoreEx(IntPtr lpSecurityAttributes, int initialCount, int maximumCount, string name, uint flags, uint desiredAccess);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool ReleaseSemaphore(SafeWaitHandle handle, int releaseCount, out int previousCount);

		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "SetEnvironmentVariableW", ExactSpelling = true, SetLastError = true)]
		internal static extern bool SetEnvironmentVariable(string lpName, string lpValue);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal unsafe static extern int WriteFile(IntPtr handle, byte* bytes, int numBytesToWrite, out int numBytesWritten, IntPtr mustBeZero);
	}

	internal static class Normaliz
	{
		[DllImport("Normaliz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal unsafe static extern int IdnToAscii(uint dwFlags, char* lpUnicodeCharStr, int cchUnicodeChar, char* lpASCIICharStr, int cchASCIIChar);

		[DllImport("Normaliz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal unsafe static extern int IdnToUnicode(uint dwFlags, char* lpASCIICharStr, int cchASCIIChar, char* lpUnicodeCharStr, int cchUnicodeChar);

		[DllImport("Normaliz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal unsafe static extern BOOL IsNormalizedString(NormalizationForm normForm, char* source, int length);

		[DllImport("Normaliz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal unsafe static extern int NormalizeString(NormalizationForm normForm, char* source, int sourceLength, char* destination, int destinationLength);
	}

	internal static class HostPolicy
	{
		[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
		internal delegate void corehost_resolve_component_dependencies_result_fn(string assemblyPaths, string nativeSearchPaths, string resourceSearchPaths);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
		internal delegate void corehost_error_writer_fn(string message);

		[DllImport("hostpolicy.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
		internal static extern int corehost_resolve_component_dependencies(string componentMainAssemblyPath, corehost_resolve_component_dependencies_result_fn result);

		[DllImport("hostpolicy.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
		internal static extern IntPtr corehost_set_error_writer(IntPtr errorWriter);
	}

	internal static class Advapi32
	{
		internal enum ActivityControl : uint
		{
			EVENT_ACTIVITY_CTRL_GET_ID = 1u,
			EVENT_ACTIVITY_CTRL_SET_ID,
			EVENT_ACTIVITY_CTRL_CREATE_ID,
			EVENT_ACTIVITY_CTRL_GET_SET_ID,
			EVENT_ACTIVITY_CTRL_CREATE_SET_ID
		}

		internal struct EVENT_FILTER_DESCRIPTOR
		{
			public long Ptr;

			public int Size;

			public int Type;
		}

		internal unsafe delegate void EtwEnableCallback(in Guid sourceId, int isEnabled, byte level, long matchAnyKeywords, long matchAllKeywords, EVENT_FILTER_DESCRIPTOR* filterData, void* callbackContext);

		internal enum EVENT_INFO_CLASS
		{
			BinaryTrackInfo,
			SetEnableAllKeywords,
			SetTraits
		}

		internal enum TRACE_QUERY_INFO_CLASS
		{
			TraceGuidQueryList,
			TraceGuidQueryInfo,
			TraceGuidQueryProcess,
			TraceStackTracingInfo,
			MaxTraceSetInfoClass
		}

		internal struct TRACE_GUID_INFO
		{
			public int InstanceCount;

			public int Reserved;
		}

		internal struct TRACE_PROVIDER_INSTANCE_INFO
		{
			public int NextOffset;

			public int EnableCount;

			public int Pid;

			public int Flags;
		}

		internal struct TRACE_ENABLE_INFO
		{
			public int IsEnabled;

			public byte Level;

			public byte Reserved1;

			public ushort LoggerId;

			public int EnableProperty;

			public int Reserved2;

			public long MatchAnyKeyword;

			public long MatchAllKeyword;
		}

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "EncryptFileW", SetLastError = true)]
		private static extern bool EncryptFilePrivate(string lpFileName);

		internal static bool EncryptFile(string path)
		{
			path = PathInternal.EnsureExtendedPrefixIfNeeded(path);
			return EncryptFilePrivate(path);
		}

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "DecryptFileW", SetLastError = true)]
		private static extern bool DecryptFileFilePrivate(string lpFileName, int dwReserved);

		internal static bool DecryptFile(string path)
		{
			path = PathInternal.EnsureExtendedPrefixIfNeeded(path);
			return DecryptFileFilePrivate(path, 0);
		}

		[DllImport("advapi32.dll", ExactSpelling = true)]
		internal static extern int EventActivityIdControl(ActivityControl ControlCode, ref Guid ActivityId);

		[DllImport("advapi32.dll", ExactSpelling = true)]
		internal unsafe static extern uint EventRegister(in Guid providerId, EtwEnableCallback enableCallback, void* callbackContext, ref long registrationHandle);

		[DllImport("advapi32.dll", ExactSpelling = true)]
		internal unsafe static extern int EventSetInformation(long registrationHandle, EVENT_INFO_CLASS informationClass, void* eventInformation, uint informationLength);

		[DllImport("advapi32.dll", ExactSpelling = true)]
		internal unsafe static extern int EnumerateTraceGuidsEx(TRACE_QUERY_INFO_CLASS TraceQueryInfoClass, void* InBuffer, int InBufferSize, void* OutBuffer, int OutBufferSize, out int ReturnLength);

		[DllImport("advapi32.dll", ExactSpelling = true)]
		internal static extern uint EventUnregister(long registrationHandle);

		internal unsafe static int EventWriteTransfer(long registrationHandle, in EventDescriptor eventDescriptor, Guid* activityId, Guid* relatedActivityId, int userDataCount, EventProvider.EventData* userData)
		{
			int num = EventWriteTransfer_PInvoke(registrationHandle, in eventDescriptor, activityId, relatedActivityId, userDataCount, userData);
			if (num == 87 && relatedActivityId == null)
			{
				Guid empty = Guid.Empty;
				num = EventWriteTransfer_PInvoke(registrationHandle, in eventDescriptor, activityId, &empty, userDataCount, userData);
			}
			return num;
		}

		[DllImport("advapi32.dll", EntryPoint = "EventWriteTransfer", ExactSpelling = true)]
		private unsafe static extern int EventWriteTransfer_PInvoke(long registrationHandle, in EventDescriptor eventDescriptor, Guid* activityId, Guid* relatedActivityId, int userDataCount, EventProvider.EventData* userData);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal static extern bool LookupAccountNameW(string lpSystemName, ref char lpAccountName, ref byte Sid, ref uint cbSid, ref char ReferencedDomainName, ref uint cchReferencedDomainName, out uint peUse);

		[DllImport("advapi32.dll")]
		internal static extern int RegCloseKey(IntPtr hKey);

		[DllImport("advapi32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "RegDeleteValueW", ExactSpelling = true)]
		internal static extern int RegDeleteValue(SafeRegistryHandle hKey, string lpValueName);

		[DllImport("advapi32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "RegEnumKeyExW", ExactSpelling = true)]
		internal static extern int RegEnumKeyEx(SafeRegistryHandle hKey, int dwIndex, char[] lpName, ref int lpcbName, int[] lpReserved, [Out] char[] lpClass, int[] lpcbClass, long[] lpftLastWriteTime);

		[DllImport("advapi32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "RegEnumValueW", ExactSpelling = true)]
		internal static extern int RegEnumValue(SafeRegistryHandle hKey, int dwIndex, char[] lpValueName, ref int lpcbValueName, IntPtr lpReserved_MustBeZero, int[] lpType, byte[] lpData, int[] lpcbData);

		[DllImport("advapi32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "RegOpenKeyExW", ExactSpelling = true)]
		internal static extern int RegOpenKeyEx(SafeRegistryHandle hKey, string lpSubKey, int ulOptions, int samDesired, out SafeRegistryHandle hkResult);

		[DllImport("advapi32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "RegQueryValueExW", ExactSpelling = true)]
		internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, string lpValueName, int[] lpReserved, ref int lpType, [Out] byte[] lpData, ref int lpcbData);

		[DllImport("advapi32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "RegQueryValueExW", ExactSpelling = true)]
		internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, string lpValueName, int[] lpReserved, ref int lpType, ref int lpData, ref int lpcbData);

		[DllImport("advapi32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "RegQueryValueExW", ExactSpelling = true)]
		internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, string lpValueName, int[] lpReserved, ref int lpType, ref long lpData, ref int lpcbData);

		[DllImport("advapi32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "RegQueryValueExW", ExactSpelling = true)]
		internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, string lpValueName, int[] lpReserved, ref int lpType, [Out] char[] lpData, ref int lpcbData);

		[DllImport("advapi32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, EntryPoint = "RegSetValueExW", ExactSpelling = true)]
		internal static extern int RegSetValueEx(SafeRegistryHandle hKey, string lpValueName, int Reserved, int dwType, string lpData, int cbData);
	}

	internal static class BCrypt
	{
		internal enum NTSTATUS : uint
		{
			STATUS_SUCCESS = 0u,
			STATUS_NOT_FOUND = 3221226021u,
			STATUS_INVALID_PARAMETER = 3221225485u,
			STATUS_NO_MEMORY = 3221225495u,
			STATUS_AUTH_TAG_MISMATCH = 3221266434u
		}

		[DllImport("BCrypt.dll", CharSet = CharSet.Unicode)]
		internal unsafe static extern NTSTATUS BCryptGenRandom(IntPtr hAlgorithm, byte* pbBuffer, int cbBuffer, int dwFlags);
	}

	internal static class Crypt32
	{
		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool CryptProtectMemory(SafeBuffer pData, uint cbData, uint dwFlags);

		[DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern bool CryptUnprotectMemory(SafeBuffer pData, uint cbData, uint dwFlags);
	}

	internal enum BOOLEAN : byte
	{
		FALSE,
		TRUE
	}

	internal static class NtDll
	{
		public enum CreateDisposition : uint
		{
			FILE_SUPERSEDE,
			FILE_OPEN,
			FILE_CREATE,
			FILE_OPEN_IF,
			FILE_OVERWRITE,
			FILE_OVERWRITE_IF
		}

		public enum CreateOptions : uint
		{
			FILE_DIRECTORY_FILE = 1u,
			FILE_WRITE_THROUGH = 2u,
			FILE_SEQUENTIAL_ONLY = 4u,
			FILE_NO_INTERMEDIATE_BUFFERING = 8u,
			FILE_SYNCHRONOUS_IO_ALERT = 0x10u,
			FILE_SYNCHRONOUS_IO_NONALERT = 0x20u,
			FILE_NON_DIRECTORY_FILE = 0x40u,
			FILE_CREATE_TREE_CONNECTION = 0x80u,
			FILE_COMPLETE_IF_OPLOCKED = 0x100u,
			FILE_NO_EA_KNOWLEDGE = 0x200u,
			FILE_RANDOM_ACCESS = 0x800u,
			FILE_DELETE_ON_CLOSE = 0x1000u,
			FILE_OPEN_BY_FILE_ID = 0x2000u,
			FILE_OPEN_FOR_BACKUP_INTENT = 0x4000u,
			FILE_NO_COMPRESSION = 0x8000u,
			FILE_OPEN_REQUIRING_OPLOCK = 0x10000u,
			FILE_DISALLOW_EXCLUSIVE = 0x20000u,
			FILE_SESSION_AWARE = 0x40000u,
			FILE_RESERVE_OPFILTER = 0x100000u,
			FILE_OPEN_REPARSE_POINT = 0x200000u,
			FILE_OPEN_NO_RECALL = 0x400000u
		}

		[Flags]
		public enum DesiredAccess : uint
		{
			FILE_READ_DATA = 1u,
			FILE_LIST_DIRECTORY = 1u,
			FILE_WRITE_DATA = 2u,
			FILE_ADD_FILE = 2u,
			FILE_APPEND_DATA = 4u,
			FILE_ADD_SUBDIRECTORY = 4u,
			FILE_CREATE_PIPE_INSTANCE = 4u,
			FILE_READ_EA = 8u,
			FILE_WRITE_EA = 0x10u,
			FILE_EXECUTE = 0x20u,
			FILE_TRAVERSE = 0x20u,
			FILE_DELETE_CHILD = 0x40u,
			FILE_READ_ATTRIBUTES = 0x80u,
			FILE_WRITE_ATTRIBUTES = 0x100u,
			FILE_ALL_ACCESS = 0xF01FFu,
			DELETE = 0x10000u,
			READ_CONTROL = 0x20000u,
			WRITE_DAC = 0x40000u,
			WRITE_OWNER = 0x80000u,
			SYNCHRONIZE = 0x100000u,
			STANDARD_RIGHTS_READ = 0x20000u,
			STANDARD_RIGHTS_WRITE = 0x20000u,
			STANDARD_RIGHTS_EXECUTE = 0x20000u,
			FILE_GENERIC_READ = 0x80000000u,
			FILE_GENERIC_WRITE = 0x40000000u,
			FILE_GENERIC_EXECUTE = 0x20000000u
		}

		public struct IO_STATUS_BLOCK
		{
			[StructLayout(LayoutKind.Explicit)]
			public struct IO_STATUS
			{
				[FieldOffset(0)]
				public uint Status;

				[FieldOffset(0)]
				public IntPtr Pointer;
			}

			public IO_STATUS Status;

			public IntPtr Information;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct FILE_FULL_DIR_INFORMATION
		{
			public uint NextEntryOffset;

			public uint FileIndex;

			public LongFileTime CreationTime;

			public LongFileTime LastAccessTime;

			public LongFileTime LastWriteTime;

			public LongFileTime ChangeTime;

			public long EndOfFile;

			public long AllocationSize;

			public FileAttributes FileAttributes;

			public uint FileNameLength;

			public uint EaSize;

			private char _fileName;

			public unsafe ReadOnlySpan<char> FileName
			{
				get
				{
					fixed (char* pointer = &_fileName)
					{
						return new ReadOnlySpan<char>(pointer, (int)FileNameLength / 2);
					}
				}
			}

			public unsafe static FILE_FULL_DIR_INFORMATION* GetNextInfo(FILE_FULL_DIR_INFORMATION* info)
			{
				if (info == null)
				{
					return null;
				}
				uint nextEntryOffset = info->NextEntryOffset;
				if (nextEntryOffset == 0)
				{
					return null;
				}
				return (FILE_FULL_DIR_INFORMATION*)((byte*)info + nextEntryOffset);
			}
		}

		public enum FILE_INFORMATION_CLASS : uint
		{
			FileDirectoryInformation = 1u,
			FileFullDirectoryInformation,
			FileBothDirectoryInformation,
			FileBasicInformation,
			FileStandardInformation,
			FileInternalInformation,
			FileEaInformation,
			FileAccessInformation,
			FileNameInformation,
			FileRenameInformation,
			FileLinkInformation,
			FileNamesInformation,
			FileDispositionInformation,
			FilePositionInformation,
			FileFullEaInformation,
			FileModeInformation,
			FileAlignmentInformation,
			FileAllInformation,
			FileAllocationInformation,
			FileEndOfFileInformation,
			FileAlternateNameInformation,
			FileStreamInformation,
			FilePipeInformation,
			FilePipeLocalInformation,
			FilePipeRemoteInformation,
			FileMailslotQueryInformation,
			FileMailslotSetInformation,
			FileCompressionInformation,
			FileObjectIdInformation,
			FileCompletionInformation,
			FileMoveClusterInformation,
			FileQuotaInformation,
			FileReparsePointInformation,
			FileNetworkOpenInformation,
			FileAttributeTagInformation,
			FileTrackingInformation,
			FileIdBothDirectoryInformation,
			FileIdFullDirectoryInformation,
			FileValidDataLengthInformation,
			FileShortNameInformation,
			FileIoCompletionNotificationInformation,
			FileIoStatusBlockRangeInformation,
			FileIoPriorityHintInformation,
			FileSfioReserveInformation,
			FileSfioVolumeInformation,
			FileHardLinkInformation,
			FileProcessIdsUsingFileInformation,
			FileNormalizedNameInformation,
			FileNetworkPhysicalNameInformation,
			FileIdGlobalTxDirectoryInformation,
			FileIsRemoteDeviceInformation,
			FileUnusedInformation,
			FileNumaNodeInformation,
			FileStandardLinkInformation,
			FileRemoteProtocolInformation,
			FileRenameInformationBypassAccessCheck,
			FileLinkInformationBypassAccessCheck,
			FileVolumeNameInformation,
			FileIdInformation,
			FileIdExtdDirectoryInformation,
			FileReplaceCompletionInformation,
			FileHardLinkFullIdInformation,
			FileIdExtdBothDirectoryInformation,
			FileDispositionInformationEx,
			FileRenameInformationEx,
			FileRenameInformationExBypassAccessCheck,
			FileDesiredStorageClassInformation,
			FileStatInformation
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct RTL_OSVERSIONINFOEX
		{
			internal uint dwOSVersionInfoSize;

			internal uint dwMajorVersion;

			internal uint dwMinorVersion;

			internal uint dwBuildNumber;

			internal uint dwPlatformId;

			internal unsafe fixed char szCSDVersion[128];
		}

		internal struct SYSTEM_LEAP_SECOND_INFORMATION
		{
			public BOOLEAN Enabled;

			public uint Flags;
		}

		[DllImport("ntdll.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		private unsafe static extern uint NtCreateFile(IntPtr* FileHandle, DesiredAccess DesiredAccess, OBJECT_ATTRIBUTES* ObjectAttributes, IO_STATUS_BLOCK* IoStatusBlock, long* AllocationSize, FileAttributes FileAttributes, FileShare ShareAccess, CreateDisposition CreateDisposition, CreateOptions CreateOptions, void* EaBuffer, uint EaLength);

		internal unsafe static (uint status, IntPtr handle) CreateFile(ReadOnlySpan<char> path, IntPtr rootDirectory, CreateDisposition createDisposition, DesiredAccess desiredAccess = DesiredAccess.SYNCHRONIZE | DesiredAccess.FILE_GENERIC_READ, FileShare shareAccess = FileShare.ReadWrite | FileShare.Delete, FileAttributes fileAttributes = (FileAttributes)0, CreateOptions createOptions = CreateOptions.FILE_SYNCHRONOUS_IO_NONALERT, ObjectAttributes objectAttributes = ObjectAttributes.OBJ_CASE_INSENSITIVE, void* eaBuffer = null, uint eaLength = 0u, long* preallocationSize = null, SECURITY_QUALITY_OF_SERVICE* securityQualityOfService = null)
		{
			checked
			{
				fixed (char* ptr = &MemoryMarshal.GetReference(path))
				{
					UNICODE_STRING uNICODE_STRING = default(UNICODE_STRING);
					uNICODE_STRING.Length = (ushort)(path.Length * 2);
					uNICODE_STRING.MaximumLength = (ushort)(path.Length * 2);
					uNICODE_STRING.Buffer = (IntPtr)ptr;
					UNICODE_STRING uNICODE_STRING2 = uNICODE_STRING;
					OBJECT_ATTRIBUTES oBJECT_ATTRIBUTES = new OBJECT_ATTRIBUTES(&uNICODE_STRING2, objectAttributes, rootDirectory, securityQualityOfService);
					System.Runtime.CompilerServices.Unsafe.SkipInit(out IntPtr item);
					System.Runtime.CompilerServices.Unsafe.SkipInit(out IO_STATUS_BLOCK iO_STATUS_BLOCK);
					uint item2 = NtCreateFile(&item, desiredAccess, &oBJECT_ATTRIBUTES, &iO_STATUS_BLOCK, preallocationSize, fileAttributes, shareAccess, createDisposition, createOptions, eaBuffer, eaLength);
					return (status: item2, handle: item);
				}
			}
		}

		[DllImport("ntdll.dll", ExactSpelling = true)]
		public static extern uint RtlNtStatusToDosError(int Status);

		[DllImport("ntdll.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public unsafe static extern int NtQueryDirectoryFile(IntPtr FileHandle, IntPtr Event, IntPtr ApcRoutine, IntPtr ApcContext, IO_STATUS_BLOCK* IoStatusBlock, IntPtr FileInformation, uint Length, FILE_INFORMATION_CLASS FileInformationClass, BOOLEAN ReturnSingleEntry, UNICODE_STRING* FileName, BOOLEAN RestartScan);

		[DllImport("ntdll.dll", ExactSpelling = true)]
		internal unsafe static extern int NtQueryInformationFile(SafeFileHandle FileHandle, out IO_STATUS_BLOCK IoStatusBlock, void* FileInformation, uint Length, uint FileInformationClass);

		[DllImport("ntdll.dll", ExactSpelling = true)]
		internal unsafe static extern uint NtQuerySystemInformation(int SystemInformationClass, void* SystemInformation, uint SystemInformationLength, uint* ReturnLength);

		[DllImport("ntdll.dll", ExactSpelling = true)]
		private static extern int RtlGetVersion(ref RTL_OSVERSIONINFOEX lpVersionInformation);

		internal unsafe static int RtlGetVersionEx(out RTL_OSVERSIONINFOEX osvi)
		{
			osvi = default(RTL_OSVERSIONINFOEX);
			osvi.dwOSVersionInfoSize = (uint)sizeof(RTL_OSVERSIONINFOEX);
			return RtlGetVersion(ref osvi);
		}
	}

	internal struct UNICODE_STRING
	{
		internal ushort Length;

		internal ushort MaximumLength;

		internal IntPtr Buffer;
	}

	internal struct SECURITY_QUALITY_OF_SERVICE
	{
		public uint Length;

		public ImpersonationLevel ImpersonationLevel;

		public ContextTrackingMode ContextTrackingMode;

		public BOOLEAN EffectiveOnly;
	}

	public enum ImpersonationLevel : uint
	{
		Anonymous,
		Identification,
		Impersonation,
		Delegation
	}

	public enum ContextTrackingMode : byte
	{
		Static,
		Dynamic
	}

	internal struct OBJECT_ATTRIBUTES
	{
		public uint Length;

		public IntPtr RootDirectory;

		public unsafe UNICODE_STRING* ObjectName;

		public ObjectAttributes Attributes;

		public unsafe void* SecurityDescriptor;

		public unsafe SECURITY_QUALITY_OF_SERVICE* SecurityQualityOfService;

		public unsafe OBJECT_ATTRIBUTES(UNICODE_STRING* objectName, ObjectAttributes attributes, IntPtr rootDirectory, SECURITY_QUALITY_OF_SERVICE* securityQualityOfService = null)
		{
			Length = (uint)sizeof(OBJECT_ATTRIBUTES);
			RootDirectory = rootDirectory;
			ObjectName = objectName;
			Attributes = attributes;
			SecurityDescriptor = null;
			SecurityQualityOfService = securityQualityOfService;
		}
	}

	[Flags]
	public enum ObjectAttributes : uint
	{
		OBJ_INHERIT = 2u,
		OBJ_PERMANENT = 0x10u,
		OBJ_EXCLUSIVE = 0x20u,
		OBJ_CASE_INSENSITIVE = 0x40u,
		OBJ_OPENIF = 0x80u,
		OBJ_OPENLINK = 0x100u
	}

	internal static class Ole32
	{
		[DllImport("ole32.dll", CharSet = CharSet.Unicode)]
		internal static extern int CLSIDFromProgID(string lpszProgID, out Guid lpclsid);

		[DllImport("ole32.dll")]
		internal static extern int CoCreateGuid(out Guid guid);

		[DllImport("ole32.dll")]
		internal static extern int CoGetStandardMarshal(ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags, out IntPtr ppMarshal);

		[DllImport("ole32.dll")]
		internal static extern IntPtr CoTaskMemAlloc(nuint cb);

		[DllImport("ole32.dll")]
		internal static extern IntPtr CoTaskMemRealloc(IntPtr pv, nuint cb);

		[DllImport("ole32.dll")]
		internal static extern void CoTaskMemFree(IntPtr ptr);
	}

	internal static class Secur32
	{
		[DllImport("secur32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal static extern BOOLEAN GetUserNameExW(int NameFormat, ref char lpNameBuffer, ref uint lpnSize);
	}

	internal static class Shell32
	{
		[DllImport("shell32.dll", BestFitMapping = false, CharSet = CharSet.Unicode, ExactSpelling = true)]
		internal static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out string ppszPath);
	}

	internal static class Ucrtbase
	{
		[DllImport("ucrtbase.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		internal unsafe static extern void* _aligned_malloc(nuint size, nuint alignment);

		[DllImport("ucrtbase.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		internal unsafe static extern void _aligned_free(void* ptr);

		[DllImport("ucrtbase.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		internal unsafe static extern void* _aligned_realloc(void* ptr, nuint size, nuint alignment);

		[DllImport("ucrtbase.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		internal unsafe static extern void* calloc(nuint num, nuint size);

		[DllImport("ucrtbase.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		internal unsafe static extern void free(void* ptr);

		[DllImport("ucrtbase.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		internal unsafe static extern void* malloc(nuint size);

		[DllImport("ucrtbase.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		internal unsafe static extern void* realloc(void* ptr, nuint new_size);
	}

	internal static class User32
	{
		internal struct USEROBJECTFLAGS
		{
			public int fInherit;

			public int fReserved;

			public int dwFlags;
		}

		[DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "LoadStringW", ExactSpelling = true, SetLastError = true)]
		internal unsafe static extern int LoadString(IntPtr hInstance, uint uID, char* lpBuffer, int cchBufferMax);

		[DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW")]
		public static extern IntPtr SendMessageTimeout(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, int flags, int timeout, out IntPtr pdwResult);

		[DllImport("user32.dll", ExactSpelling = true)]
		internal static extern IntPtr GetProcessWindowStation();

		[DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		public unsafe static extern bool GetUserObjectInformationW(IntPtr hObj, int nIndex, void* pvBuffer, uint nLength, ref uint lpnLengthNeeded);
	}

	internal struct LongFileTime
	{
		internal long TicksSince1601;

		internal DateTimeOffset ToDateTimeOffset()
		{
			return new DateTimeOffset(DateTime.FromFileTimeUtc(TicksSince1601));
		}
	}

	internal static bool CallStringMethod<TArg1, TArg2, TArg3>(SpanFunc<char, TArg1, TArg2, TArg3, Globalization.ResultCode> interopCall, TArg1 arg1, TArg2 arg2, TArg3 arg3, out string result)
	{
		Span<char> span = stackalloc char[256];
		switch (interopCall(span, arg1, arg2, arg3))
		{
		case Globalization.ResultCode.Success:
			result = span.Slice(0, span.IndexOf('\0')).ToString();
			return true;
		case Globalization.ResultCode.InsufficentBuffer:
			span = new char[1280];
			if (interopCall(span, arg1, arg2, arg3) == Globalization.ResultCode.Success)
			{
				result = span.Slice(0, span.IndexOf('\0')).ToString();
				return true;
			}
			break;
		}
		result = null;
		return false;
	}

	internal unsafe static void GetRandomBytes(byte* buffer, int length)
	{
		switch (BCrypt.BCryptGenRandom(IntPtr.Zero, buffer, length, 2))
		{
		case BCrypt.NTSTATUS.STATUS_NO_MEMORY:
			throw new OutOfMemoryException();
		default:
			throw new InvalidOperationException();
		case BCrypt.NTSTATUS.STATUS_SUCCESS:
			break;
		}
	}
}
