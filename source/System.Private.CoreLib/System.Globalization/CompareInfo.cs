using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace System.Globalization;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class CompareInfo : IDeserializationCallback
{
	private static class SortHandleCache
	{
		private static readonly Dictionary<string, IntPtr> s_sortNameToSortHandleCache = new Dictionary<string, IntPtr>();

		internal static IntPtr GetCachedSortHandle(string sortName)
		{
			lock (s_sortNameToSortHandleCache)
			{
				if (!s_sortNameToSortHandleCache.TryGetValue(sortName, out var value))
				{
					switch (Interop.Globalization.GetSortHandle(sortName, out value))
					{
					case Interop.Globalization.ResultCode.OutOfMemory:
						throw new OutOfMemoryException();
					default:
						throw new ExternalException(SR.Arg_ExternalException);
					case Interop.Globalization.ResultCode.Success:
						break;
					}
					try
					{
						s_sortNameToSortHandleCache.Add(sortName, value);
					}
					catch
					{
						Interop.Globalization.CloseSortHandle(value);
						throw;
					}
				}
				return value;
			}
		}
	}

	internal static readonly CompareInfo Invariant = CultureInfo.InvariantCulture.CompareInfo;

	[OptionalField(VersionAdded = 2)]
	private string m_name;

	[NonSerialized]
	private IntPtr _sortHandle;

	[NonSerialized]
	private string _sortName;

	[OptionalField(VersionAdded = 3)]
	private SortVersion m_SortVersion;

	private int culture;

	[NonSerialized]
	private bool _isAsciiEqualityOrdinal;

	public string Name
	{
		get
		{
			if (m_name == "zh-CHT" || m_name == "zh-CHS")
			{
				return m_name;
			}
			return _sortName;
		}
	}

	public SortVersion Version
	{
		get
		{
			if (m_SortVersion == null)
			{
				if (GlobalizationMode.Invariant)
				{
					m_SortVersion = new SortVersion(0, 127, new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 127));
				}
				else
				{
					m_SortVersion = (GlobalizationMode.UseNls ? NlsGetSortVersion() : IcuGetSortVersion());
				}
			}
			return m_SortVersion;
		}
	}

	public int LCID => CultureInfo.GetCultureInfo(Name).LCID;

	private static ReadOnlySpan<bool> HighCharTable => new bool[128]
	{
		true, true, true, true, true, true, true, true, true, false,
		true, false, false, true, true, true, true, true, true, true,
		true, true, true, true, true, true, true, true, true, true,
		true, true, false, false, false, false, false, false, false, true,
		false, false, false, false, false, true, false, false, false, false,
		false, false, false, false, false, false, false, false, false, false,
		false, false, false, false, false, false, false, false, false, false,
		false, false, false, false, false, false, false, false, false, false,
		false, false, false, false, false, false, false, false, false, false,
		false, false, false, false, false, false, false, false, false, false,
		false, false, false, false, false, false, false, false, false, false,
		false, false, false, false, false, false, false, false, false, false,
		false, false, false, false, false, false, false, true
	};

	internal CompareInfo(CultureInfo culture)
	{
		m_name = culture._name;
		InitSort(culture);
	}

	public static CompareInfo GetCompareInfo(int culture, Assembly assembly)
	{
		if (assembly == null)
		{
			throw new ArgumentNullException("assembly");
		}
		if (assembly != typeof(object).Module.Assembly)
		{
			throw new ArgumentException(SR.Argument_OnlyMscorlib, "assembly");
		}
		return GetCompareInfo(culture);
	}

	public static CompareInfo GetCompareInfo(string name, Assembly assembly)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (assembly == null)
		{
			throw new ArgumentNullException("assembly");
		}
		if (assembly != typeof(object).Module.Assembly)
		{
			throw new ArgumentException(SR.Argument_OnlyMscorlib, "assembly");
		}
		return GetCompareInfo(name);
	}

	public static CompareInfo GetCompareInfo(int culture)
	{
		if (CultureData.IsCustomCultureId(culture))
		{
			throw new ArgumentException(SR.Argument_CustomCultureCannotBePassedByNumber, "culture");
		}
		return CultureInfo.GetCultureInfo(culture).CompareInfo;
	}

	public static CompareInfo GetCompareInfo(string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		return CultureInfo.GetCultureInfo(name).CompareInfo;
	}

	public static bool IsSortable(char ch)
	{
		return IsSortable(MemoryMarshal.CreateReadOnlySpan(ref ch, 1));
	}

	public static bool IsSortable(string text)
	{
		if (text == null)
		{
			throw new ArgumentNullException("text");
		}
		return IsSortable(text.AsSpan());
	}

	public static bool IsSortable(ReadOnlySpan<char> text)
	{
		if (text.Length == 0)
		{
			return false;
		}
		if (GlobalizationMode.Invariant)
		{
			return true;
		}
		if (!GlobalizationMode.UseNls)
		{
			return IcuIsSortable(text);
		}
		return NlsIsSortable(text);
	}

	public static bool IsSortable(Rune value)
	{
		Span<char> destination = stackalloc char[2];
		return IsSortable(destination[..value.EncodeToUtf16(destination)]);
	}

	[MemberNotNull("_sortName")]
	private void InitSort(CultureInfo culture)
	{
		_sortName = culture.SortName;
		if (GlobalizationMode.UseNls)
		{
			NlsInitSortHandle();
		}
		else
		{
			IcuInitSortHandle();
		}
	}

	[OnDeserializing]
	private void OnDeserializing(StreamingContext ctx)
	{
		m_name = null;
	}

	void IDeserializationCallback.OnDeserialization(object sender)
	{
		OnDeserialized();
	}

	[OnDeserialized]
	private void OnDeserialized(StreamingContext ctx)
	{
		OnDeserialized();
	}

	private void OnDeserialized()
	{
		if (m_name == null)
		{
			m_name = CultureInfo.GetCultureInfo(culture)._name;
		}
		else
		{
			InitSort(CultureInfo.GetCultureInfo(m_name));
		}
	}

	[OnSerializing]
	private void OnSerializing(StreamingContext ctx)
	{
		culture = CultureInfo.GetCultureInfo(Name).LCID;
	}

	public int Compare(string? string1, string? string2)
	{
		return Compare(string1, string2, CompareOptions.None);
	}

	public int Compare(string? string1, string? string2, CompareOptions options)
	{
		int result;
		if (string1 == null)
		{
			result = ((string2 != null) ? (-1) : 0);
		}
		else
		{
			if (string2 != null)
			{
				return Compare(string1.AsSpan(), string2.AsSpan(), options);
			}
			result = 1;
		}
		CheckCompareOptionsForCompare(options);
		return result;
	}

	internal int CompareOptionIgnoreCase(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2)
	{
		if (!GlobalizationMode.Invariant)
		{
			return CompareStringCore(string1, string2, CompareOptions.IgnoreCase);
		}
		return InvariantModeCasing.CompareStringIgnoreCase(ref MemoryMarshal.GetReference(string1), string1.Length, ref MemoryMarshal.GetReference(string2), string2.Length);
	}

	public int Compare(string? string1, int offset1, int length1, string? string2, int offset2, int length2)
	{
		return Compare(string1, offset1, length1, string2, offset2, length2, CompareOptions.None);
	}

	public int Compare(string? string1, int offset1, string? string2, int offset2, CompareOptions options)
	{
		return Compare(string1, offset1, (string1 != null) ? (string1.Length - offset1) : 0, string2, offset2, (string2 != null) ? (string2.Length - offset2) : 0, options);
	}

	public int Compare(string? string1, int offset1, string? string2, int offset2)
	{
		return Compare(string1, offset1, string2, offset2, CompareOptions.None);
	}

	public int Compare(string? string1, int offset1, int length1, string? string2, int offset2, int length2, CompareOptions options)
	{
		ReadOnlySpan<char> slice = default(ReadOnlySpan<char>);
		ReadOnlySpan<char> slice2 = default(ReadOnlySpan<char>);
		if (string1 == null)
		{
			if (offset1 == 0 && length1 == 0)
			{
				goto IL_0027;
			}
		}
		else if (string1.TryGetSpan(offset1, length1, out slice))
		{
			goto IL_0027;
		}
		goto IL_006e;
		IL_0044:
		int result;
		if (string1 == null)
		{
			result = ((string2 != null) ? (-1) : 0);
		}
		else
		{
			if (string2 != null)
			{
				return Compare(slice, slice2, options);
			}
			result = 1;
		}
		CheckCompareOptionsForCompare(options);
		return result;
		IL_0027:
		if (string2 == null)
		{
			if (offset2 == 0 && length2 == 0)
			{
				goto IL_0044;
			}
		}
		else if (string2.TryGetSpan(offset2, length2, out slice2))
		{
			goto IL_0044;
		}
		goto IL_006e;
		IL_006e:
		if (length1 < 0 || length2 < 0)
		{
			throw new ArgumentOutOfRangeException((length1 < 0) ? "length1" : "length2", SR.ArgumentOutOfRange_NeedPosNum);
		}
		if (offset1 < 0 || offset2 < 0)
		{
			throw new ArgumentOutOfRangeException((offset1 < 0) ? "offset1" : "offset2", SR.ArgumentOutOfRange_NeedPosNum);
		}
		if (offset1 > (string1?.Length ?? 0) - length1)
		{
			throw new ArgumentOutOfRangeException("string1", SR.ArgumentOutOfRange_OffsetLength);
		}
		throw new ArgumentOutOfRangeException("string2", SR.ArgumentOutOfRange_OffsetLength);
	}

	public int Compare(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, CompareOptions options = CompareOptions.None)
	{
		if (string1 == string2)
		{
			CheckCompareOptionsForCompare(options);
			return 0;
		}
		if ((options & ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.StringSort)) == 0)
		{
			if (!GlobalizationMode.Invariant)
			{
				return CompareStringCore(string1, string2, options);
			}
			if ((options & CompareOptions.IgnoreCase) == 0)
			{
				return string1.SequenceCompareTo(string2);
			}
			return Ordinal.CompareStringIgnoreCase(ref MemoryMarshal.GetReference(string1), string1.Length, ref MemoryMarshal.GetReference(string2), string2.Length);
		}
		switch (options)
		{
		case CompareOptions.Ordinal:
			return string1.SequenceCompareTo(string2);
		case CompareOptions.OrdinalIgnoreCase:
			return Ordinal.CompareStringIgnoreCase(ref MemoryMarshal.GetReference(string1), string1.Length, ref MemoryMarshal.GetReference(string2), string2.Length);
		default:
			ThrowCompareOptionsCheckFailed(options);
			return -1;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[StackTraceHidden]
	private static void CheckCompareOptionsForCompare(CompareOptions options)
	{
		if (((uint)options & 0xDFFFFFE0u) != 0 && options != CompareOptions.Ordinal && options != CompareOptions.OrdinalIgnoreCase)
		{
			ThrowCompareOptionsCheckFailed(options);
		}
	}

	[DoesNotReturn]
	[StackTraceHidden]
	private static void ThrowCompareOptionsCheckFailed(CompareOptions options)
	{
		throw new ArgumentException(((options & CompareOptions.Ordinal) != 0) ? SR.Argument_CompareOptionOrdinal : SR.Argument_InvalidFlag, "options");
	}

	private int CompareStringCore(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, CompareOptions options)
	{
		if (!GlobalizationMode.UseNls)
		{
			return IcuCompareString(string1, string2, options);
		}
		return NlsCompareString(string1, string2, options);
	}

	public bool IsPrefix(string source, string prefix, CompareOptions options)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (prefix == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.prefix);
		}
		return IsPrefix(source.AsSpan(), prefix.AsSpan(), options);
	}

	public unsafe bool IsPrefix(ReadOnlySpan<char> source, ReadOnlySpan<char> prefix, CompareOptions options = CompareOptions.None)
	{
		if (prefix.IsEmpty)
		{
			return true;
		}
		if ((options & ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth)) == 0)
		{
			if (!GlobalizationMode.Invariant)
			{
				return StartsWithCore(source, prefix, options, null);
			}
			if ((options & CompareOptions.IgnoreCase) == 0)
			{
				return source.StartsWith(prefix);
			}
			return source.StartsWithOrdinalIgnoreCase(prefix);
		}
		switch (options)
		{
		case CompareOptions.Ordinal:
			return source.StartsWith(prefix);
		case CompareOptions.OrdinalIgnoreCase:
			return source.StartsWithOrdinalIgnoreCase(prefix);
		default:
			ThrowCompareOptionsCheckFailed(options);
			return false;
		}
	}

	public unsafe bool IsPrefix(ReadOnlySpan<char> source, ReadOnlySpan<char> prefix, CompareOptions options, out int matchLength)
	{
		bool flag;
		if (GlobalizationMode.Invariant || prefix.IsEmpty || ((uint)options & 0xFFFFFFE0u) != 0)
		{
			flag = IsPrefix(source, prefix, options);
			matchLength = (flag ? prefix.Length : 0);
		}
		else
		{
			int num = 0;
			flag = StartsWithCore(source, prefix, options, &num);
			matchLength = num;
		}
		return flag;
	}

	private unsafe bool StartsWithCore(ReadOnlySpan<char> source, ReadOnlySpan<char> prefix, CompareOptions options, int* matchLengthPtr)
	{
		if (!GlobalizationMode.UseNls)
		{
			return IcuStartsWith(source, prefix, options, matchLengthPtr);
		}
		return NlsStartsWith(source, prefix, options, matchLengthPtr);
	}

	public bool IsPrefix(string source, string prefix)
	{
		return IsPrefix(source, prefix, CompareOptions.None);
	}

	public bool IsSuffix(string source, string suffix, CompareOptions options)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (suffix == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.suffix);
		}
		return IsSuffix(source.AsSpan(), suffix.AsSpan(), options);
	}

	public unsafe bool IsSuffix(ReadOnlySpan<char> source, ReadOnlySpan<char> suffix, CompareOptions options = CompareOptions.None)
	{
		if (suffix.IsEmpty)
		{
			return true;
		}
		if ((options & ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth)) == 0)
		{
			if (!GlobalizationMode.Invariant)
			{
				return EndsWithCore(source, suffix, options, null);
			}
			if ((options & CompareOptions.IgnoreCase) == 0)
			{
				return source.EndsWith(suffix);
			}
			return source.EndsWithOrdinalIgnoreCase(suffix);
		}
		switch (options)
		{
		case CompareOptions.Ordinal:
			return source.EndsWith(suffix);
		case CompareOptions.OrdinalIgnoreCase:
			return source.EndsWithOrdinalIgnoreCase(suffix);
		default:
			ThrowCompareOptionsCheckFailed(options);
			return false;
		}
	}

	public unsafe bool IsSuffix(ReadOnlySpan<char> source, ReadOnlySpan<char> suffix, CompareOptions options, out int matchLength)
	{
		bool flag;
		if (GlobalizationMode.Invariant || suffix.IsEmpty || ((uint)options & 0xFFFFFFE0u) != 0)
		{
			flag = IsSuffix(source, suffix, options);
			matchLength = (flag ? suffix.Length : 0);
		}
		else
		{
			int num = 0;
			flag = EndsWithCore(source, suffix, options, &num);
			matchLength = num;
		}
		return flag;
	}

	public bool IsSuffix(string source, string suffix)
	{
		return IsSuffix(source, suffix, CompareOptions.None);
	}

	private unsafe bool EndsWithCore(ReadOnlySpan<char> source, ReadOnlySpan<char> suffix, CompareOptions options, int* matchLengthPtr)
	{
		if (!GlobalizationMode.UseNls)
		{
			return IcuEndsWith(source, suffix, options, matchLengthPtr);
		}
		return NlsEndsWith(source, suffix, options, matchLengthPtr);
	}

	public int IndexOf(string source, char value)
	{
		return IndexOf(source, value, CompareOptions.None);
	}

	public int IndexOf(string source, string value)
	{
		return IndexOf(source, value, CompareOptions.None);
	}

	public int IndexOf(string source, char value, CompareOptions options)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		return IndexOf(source, MemoryMarshal.CreateReadOnlySpan(ref value, 1), options);
	}

	public int IndexOf(string source, string value, CompareOptions options)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (value == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
		}
		return IndexOf(source.AsSpan(), value.AsSpan(), options);
	}

	public int IndexOf(string source, char value, int startIndex)
	{
		return IndexOf(source, value, startIndex, CompareOptions.None);
	}

	public int IndexOf(string source, string value, int startIndex)
	{
		return IndexOf(source, value, startIndex, CompareOptions.None);
	}

	public int IndexOf(string source, char value, int startIndex, CompareOptions options)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		return IndexOf(source, value, startIndex, source.Length - startIndex, options);
	}

	public int IndexOf(string source, string value, int startIndex, CompareOptions options)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		return IndexOf(source, value, startIndex, source.Length - startIndex, options);
	}

	public int IndexOf(string source, char value, int startIndex, int count)
	{
		return IndexOf(source, value, startIndex, count, CompareOptions.None);
	}

	public int IndexOf(string source, string value, int startIndex, int count)
	{
		return IndexOf(source, value, startIndex, count, CompareOptions.None);
	}

	public int IndexOf(string source, char value, int startIndex, int count, CompareOptions options)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (!source.TryGetSpan(startIndex, count, out var slice))
		{
			if ((uint)startIndex > (uint)source.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
			}
			else
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_Count);
			}
		}
		int num = IndexOf(slice, MemoryMarshal.CreateReadOnlySpan(ref value, 1), options);
		if (num >= 0)
		{
			num += startIndex;
		}
		return num;
	}

	public int IndexOf(string source, string value, int startIndex, int count, CompareOptions options)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (value == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
		}
		if (!source.TryGetSpan(startIndex, count, out var slice))
		{
			if ((uint)startIndex > (uint)source.Length)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
			}
			else
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_Count);
			}
		}
		int num = IndexOf(slice, value, options);
		if (num >= 0)
		{
			num += startIndex;
		}
		return num;
	}

	public unsafe int IndexOf(ReadOnlySpan<char> source, ReadOnlySpan<char> value, CompareOptions options = CompareOptions.None)
	{
		if ((options & ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth)) == 0)
		{
			if (!GlobalizationMode.Invariant)
			{
				if (value.IsEmpty)
				{
					return 0;
				}
				return IndexOfCore(source, value, options, null, fromBeginning: true);
			}
			if ((options & CompareOptions.IgnoreCase) == 0)
			{
				return source.IndexOf(value);
			}
			return Ordinal.IndexOfOrdinalIgnoreCase(source, value);
		}
		switch (options)
		{
		case CompareOptions.Ordinal:
			return source.IndexOf(value);
		case CompareOptions.OrdinalIgnoreCase:
			return Ordinal.IndexOfOrdinalIgnoreCase(source, value);
		default:
			ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidFlag, ExceptionArgument.options);
			return -1;
		}
	}

	public unsafe int IndexOf(ReadOnlySpan<char> source, ReadOnlySpan<char> value, CompareOptions options, out int matchLength)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out int num);
		int result = IndexOf(source, value, &num, options, fromBeginning: true);
		matchLength = num;
		return result;
	}

	public int IndexOf(ReadOnlySpan<char> source, Rune value, CompareOptions options = CompareOptions.None)
	{
		Span<char> destination = stackalloc char[2];
		return IndexOf(source, destination[..value.EncodeToUtf16(destination)], options);
	}

	private unsafe int IndexOf(ReadOnlySpan<char> source, ReadOnlySpan<char> value, int* matchLengthPtr, CompareOptions options, bool fromBeginning)
	{
		*matchLengthPtr = 0;
		int num = 0;
		if ((options & ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth)) == 0)
		{
			if (!GlobalizationMode.Invariant)
			{
				if (value.IsEmpty)
				{
					if (!fromBeginning)
					{
						return source.Length;
					}
					return 0;
				}
				return IndexOfCore(source, value, options, matchLengthPtr, fromBeginning);
			}
			num = (((options & CompareOptions.IgnoreCase) != 0) ? (fromBeginning ? Ordinal.IndexOfOrdinalIgnoreCase(source, value) : Ordinal.LastIndexOfOrdinalIgnoreCase(source, value)) : (fromBeginning ? source.IndexOf(value) : source.LastIndexOf(value)));
		}
		else
		{
			switch (options)
			{
			case CompareOptions.Ordinal:
				num = (fromBeginning ? source.IndexOf(value) : source.LastIndexOf(value));
				break;
			case CompareOptions.OrdinalIgnoreCase:
				num = (fromBeginning ? Ordinal.IndexOfOrdinalIgnoreCase(source, value) : Ordinal.LastIndexOfOrdinalIgnoreCase(source, value));
				break;
			default:
				ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidFlag, ExceptionArgument.options);
				break;
			}
		}
		if (num >= 0)
		{
			*matchLengthPtr = value.Length;
		}
		return num;
	}

	private unsafe int IndexOfCore(ReadOnlySpan<char> source, ReadOnlySpan<char> target, CompareOptions options, int* matchLengthPtr, bool fromBeginning)
	{
		if (!GlobalizationMode.UseNls)
		{
			return IcuIndexOfCore(source, target, options, matchLengthPtr, fromBeginning);
		}
		return NlsIndexOfCore(source, target, options, matchLengthPtr, fromBeginning);
	}

	public int LastIndexOf(string source, char value)
	{
		return LastIndexOf(source, value, CompareOptions.None);
	}

	public int LastIndexOf(string source, string value)
	{
		return LastIndexOf(source, value, CompareOptions.None);
	}

	public int LastIndexOf(string source, char value, CompareOptions options)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		return LastIndexOf(source, MemoryMarshal.CreateReadOnlySpan(ref value, 1), options);
	}

	public int LastIndexOf(string source, string value, CompareOptions options)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (value == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
		}
		return LastIndexOf(source.AsSpan(), value.AsSpan(), options);
	}

	public int LastIndexOf(string source, char value, int startIndex)
	{
		return LastIndexOf(source, value, startIndex, startIndex + 1, CompareOptions.None);
	}

	public int LastIndexOf(string source, string value, int startIndex)
	{
		return LastIndexOf(source, value, startIndex, startIndex + 1, CompareOptions.None);
	}

	public int LastIndexOf(string source, char value, int startIndex, CompareOptions options)
	{
		return LastIndexOf(source, value, startIndex, startIndex + 1, options);
	}

	public int LastIndexOf(string source, string value, int startIndex, CompareOptions options)
	{
		return LastIndexOf(source, value, startIndex, startIndex + 1, options);
	}

	public int LastIndexOf(string source, char value, int startIndex, int count)
	{
		return LastIndexOf(source, value, startIndex, count, CompareOptions.None);
	}

	public int LastIndexOf(string source, string value, int startIndex, int count)
	{
		return LastIndexOf(source, value, startIndex, count, CompareOptions.None);
	}

	public int LastIndexOf(string source, char value, int startIndex, int count, CompareOptions options)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		while ((uint)startIndex >= (uint)source.Length)
		{
			if (startIndex == -1 && source.Length == 0)
			{
				count = 0;
				break;
			}
			if (startIndex == source.Length)
			{
				startIndex--;
				if (count > 0)
				{
					count--;
				}
				continue;
			}
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
			break;
		}
		startIndex = startIndex - count + 1;
		if (!source.TryGetSpan(startIndex, count, out var slice))
		{
			ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
		}
		int num = LastIndexOf(slice, MemoryMarshal.CreateReadOnlySpan(ref value, 1), options);
		if (num >= 0)
		{
			num += startIndex;
		}
		return num;
	}

	public int LastIndexOf(string source, string value, int startIndex, int count, CompareOptions options)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (value == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
		}
		while ((uint)startIndex >= (uint)source.Length)
		{
			if (startIndex == -1 && source.Length == 0)
			{
				count = 0;
				break;
			}
			if (startIndex == source.Length)
			{
				startIndex--;
				if (count > 0)
				{
					count--;
				}
				continue;
			}
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
			break;
		}
		startIndex = startIndex - count + 1;
		if (!source.TryGetSpan(startIndex, count, out var slice))
		{
			ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
		}
		int num = LastIndexOf(slice, value, options);
		if (num >= 0)
		{
			num += startIndex;
		}
		return num;
	}

	public unsafe int LastIndexOf(ReadOnlySpan<char> source, ReadOnlySpan<char> value, CompareOptions options = CompareOptions.None)
	{
		if ((options & ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth)) == 0)
		{
			if (!GlobalizationMode.Invariant)
			{
				if (value.IsEmpty)
				{
					return source.Length;
				}
				return IndexOfCore(source, value, options, null, fromBeginning: false);
			}
			if ((options & CompareOptions.IgnoreCase) == 0)
			{
				return source.LastIndexOf(value);
			}
			return Ordinal.LastIndexOfOrdinalIgnoreCase(source, value);
		}
		return options switch
		{
			CompareOptions.Ordinal => source.LastIndexOf(value), 
			CompareOptions.OrdinalIgnoreCase => Ordinal.LastIndexOfOrdinalIgnoreCase(source, value), 
			_ => throw new ArgumentException(SR.Argument_InvalidFlag, "options"), 
		};
	}

	public unsafe int LastIndexOf(ReadOnlySpan<char> source, ReadOnlySpan<char> value, CompareOptions options, out int matchLength)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out int num);
		int result = IndexOf(source, value, &num, options, fromBeginning: false);
		matchLength = num;
		return result;
	}

	public int LastIndexOf(ReadOnlySpan<char> source, Rune value, CompareOptions options = CompareOptions.None)
	{
		Span<char> destination = stackalloc char[2];
		return LastIndexOf(source, destination[..value.EncodeToUtf16(destination)], options);
	}

	public SortKey GetSortKey(string source, CompareOptions options)
	{
		if (GlobalizationMode.Invariant)
		{
			return InvariantCreateSortKey(source, options);
		}
		return CreateSortKeyCore(source, options);
	}

	public SortKey GetSortKey(string source)
	{
		if (GlobalizationMode.Invariant)
		{
			return InvariantCreateSortKey(source, CompareOptions.None);
		}
		return CreateSortKeyCore(source, CompareOptions.None);
	}

	private SortKey CreateSortKeyCore(string source, CompareOptions options)
	{
		if (!GlobalizationMode.UseNls)
		{
			return IcuCreateSortKey(source, options);
		}
		return NlsCreateSortKey(source, options);
	}

	public int GetSortKey(ReadOnlySpan<char> source, Span<byte> destination, CompareOptions options = CompareOptions.None)
	{
		if (((uint)options & 0xDFFFFFE0u) != 0)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidFlag, ExceptionArgument.options);
		}
		if (GlobalizationMode.Invariant)
		{
			return InvariantGetSortKey(source, destination, options);
		}
		return GetSortKeyCore(source, destination, options);
	}

	private int GetSortKeyCore(ReadOnlySpan<char> source, Span<byte> destination, CompareOptions options)
	{
		if (!GlobalizationMode.UseNls)
		{
			return IcuGetSortKey(source, destination, options);
		}
		return NlsGetSortKey(source, destination, options);
	}

	public int GetSortKeyLength(ReadOnlySpan<char> source, CompareOptions options = CompareOptions.None)
	{
		if (((uint)options & 0xDFFFFFE0u) != 0)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidFlag, ExceptionArgument.options);
		}
		if (GlobalizationMode.Invariant)
		{
			return InvariantGetSortKeyLength(source, options);
		}
		return GetSortKeyLengthCore(source, options);
	}

	private int GetSortKeyLengthCore(ReadOnlySpan<char> source, CompareOptions options)
	{
		if (!GlobalizationMode.UseNls)
		{
			return IcuGetSortKeyLength(source, options);
		}
		return NlsGetSortKeyLength(source, options);
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value is CompareInfo compareInfo)
		{
			return Name == compareInfo.Name;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Name.GetHashCode();
	}

	public int GetHashCode(string source, CompareOptions options)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		return GetHashCode(source.AsSpan(), options);
	}

	public int GetHashCode(ReadOnlySpan<char> source, CompareOptions options)
	{
		if ((options & ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.StringSort)) == 0)
		{
			if (!GlobalizationMode.Invariant)
			{
				return GetHashCodeOfStringCore(source, options);
			}
			if ((options & CompareOptions.IgnoreCase) == 0)
			{
				return string.GetHashCode(source);
			}
			return string.GetHashCodeOrdinalIgnoreCase(source);
		}
		switch (options)
		{
		case CompareOptions.Ordinal:
			return string.GetHashCode(source);
		case CompareOptions.OrdinalIgnoreCase:
			return string.GetHashCodeOrdinalIgnoreCase(source);
		default:
			ThrowCompareOptionsCheckFailed(options);
			return -1;
		}
	}

	private int GetHashCodeOfStringCore(ReadOnlySpan<char> source, CompareOptions options)
	{
		if (!GlobalizationMode.UseNls)
		{
			return IcuGetHashCodeOfString(source, options);
		}
		return NlsGetHashCodeOfString(source, options);
	}

	public override string ToString()
	{
		return "CompareInfo - " + Name;
	}

	private void IcuInitSortHandle()
	{
		if (GlobalizationMode.Invariant)
		{
			_isAsciiEqualityOrdinal = true;
			return;
		}
		_isAsciiEqualityOrdinal = _sortName.Length == 0 || (_sortName.Length >= 2 && _sortName[0] == 'e' && _sortName[1] == 'n' && (_sortName.Length == 2 || _sortName[2] == '-'));
		_sortHandle = SortHandleCache.GetCachedSortHandle(_sortName);
	}

	private unsafe int IcuCompareString(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, CompareOptions options)
	{
		fixed (char* lpStr = &MemoryMarshal.GetReference(string1))
		{
			fixed (char* lpStr2 = &MemoryMarshal.GetReference(string2))
			{
				return Interop.Globalization.CompareString(_sortHandle, lpStr, string1.Length, lpStr2, string2.Length, options);
			}
		}
	}

	private unsafe int IcuIndexOfCore(ReadOnlySpan<char> source, ReadOnlySpan<char> target, CompareOptions options, int* matchLengthPtr, bool fromBeginning)
	{
		if (_isAsciiEqualityOrdinal && CanUseAsciiOrdinalForOptions(options))
		{
			if ((options & CompareOptions.IgnoreCase) != 0)
			{
				return IndexOfOrdinalIgnoreCaseHelper(source, target, options, matchLengthPtr, fromBeginning);
			}
			return IndexOfOrdinalHelper(source, target, options, matchLengthPtr, fromBeginning);
		}
		fixed (char* pSource = &MemoryMarshal.GetReference(source))
		{
			fixed (char* target2 = &MemoryMarshal.GetReference(target))
			{
				if (fromBeginning)
				{
					return Interop.Globalization.IndexOf(_sortHandle, target2, target.Length, pSource, source.Length, options, matchLengthPtr);
				}
				return Interop.Globalization.LastIndexOf(_sortHandle, target2, target.Length, pSource, source.Length, options, matchLengthPtr);
			}
		}
	}

	private unsafe int IndexOfOrdinalIgnoreCaseHelper(ReadOnlySpan<char> source, ReadOnlySpan<char> target, CompareOptions options, int* matchLengthPtr, bool fromBeginning)
	{
		fixed (char* ptr = &MemoryMarshal.GetReference(source))
		{
			fixed (char* ptr3 = &MemoryMarshal.GetReference(target))
			{
				char* ptr2 = ptr;
				char* ptr4 = ptr3;
				int num = 0;
				while (true)
				{
					if (num < target.Length)
					{
						char c = ptr4[num];
						if (c >= '\u0080' || HighCharTable[c])
						{
							break;
						}
						num++;
						continue;
					}
					if (target.Length > source.Length)
					{
						int num2 = 0;
						while (true)
						{
							if (num2 < source.Length)
							{
								char c2 = ptr2[num2];
								if (c2 >= '\u0080' || HighCharTable[c2])
								{
									break;
								}
								num2++;
								continue;
							}
							return -1;
						}
						break;
					}
					int num3;
					int num4;
					int num5;
					if (fromBeginning)
					{
						num3 = 0;
						num4 = source.Length - target.Length + 1;
						num5 = 1;
					}
					else
					{
						num3 = source.Length - target.Length;
						num4 = -1;
						num5 = -1;
					}
					int num6 = num3;
					while (true)
					{
						int num8;
						if (num6 != num4)
						{
							int num7 = 0;
							num8 = num6;
							while (true)
							{
								if (num7 < target.Length)
								{
									char c3 = ptr2[num8];
									char c4 = ptr4[num7];
									if (c3 >= '\u0080' || HighCharTable[c3])
									{
										break;
									}
									if (c3 == c4)
									{
										goto IL_0184;
									}
									if ((uint)(c3 - 97) <= 25u)
									{
										c3 = (char)(c3 - 32);
									}
									if ((uint)(c4 - 97) <= 25u)
									{
										c4 = (char)(c4 - 32);
									}
									if (c3 == c4)
									{
										goto IL_0184;
									}
									goto IL_0163;
								}
								if (num8 < source.Length && ptr2[num8] >= '\u0080')
								{
									break;
								}
								if (matchLengthPtr != null)
								{
									*matchLengthPtr = target.Length;
								}
								return num6;
								IL_0184:
								num7++;
								num8++;
							}
							break;
						}
						return -1;
						IL_0163:
						if (num8 < source.Length - 1 && (ptr2 + num8)[1] >= '\u0080')
						{
							break;
						}
						num6 += num5;
					}
					break;
				}
				if (fromBeginning)
				{
					return Interop.Globalization.IndexOf(_sortHandle, ptr4, target.Length, ptr2, source.Length, options, matchLengthPtr);
				}
				return Interop.Globalization.LastIndexOf(_sortHandle, ptr4, target.Length, ptr2, source.Length, options, matchLengthPtr);
			}
		}
	}

	private unsafe int IndexOfOrdinalHelper(ReadOnlySpan<char> source, ReadOnlySpan<char> target, CompareOptions options, int* matchLengthPtr, bool fromBeginning)
	{
		fixed (char* ptr = &MemoryMarshal.GetReference(source))
		{
			fixed (char* ptr3 = &MemoryMarshal.GetReference(target))
			{
				char* ptr2 = ptr;
				char* ptr4 = ptr3;
				int num = 0;
				while (true)
				{
					if (num < target.Length)
					{
						char c = ptr4[num];
						if (c >= '\u0080' || HighCharTable[c])
						{
							break;
						}
						num++;
						continue;
					}
					if (target.Length > source.Length)
					{
						int num2 = 0;
						while (true)
						{
							if (num2 < source.Length)
							{
								char c2 = ptr2[num2];
								if (c2 >= '\u0080' || HighCharTable[c2])
								{
									break;
								}
								num2++;
								continue;
							}
							return -1;
						}
						break;
					}
					int num3;
					int num4;
					int num5;
					if (fromBeginning)
					{
						num3 = 0;
						num4 = source.Length - target.Length + 1;
						num5 = 1;
					}
					else
					{
						num3 = source.Length - target.Length;
						num4 = -1;
						num5 = -1;
					}
					int num6 = num3;
					while (true)
					{
						int num8;
						if (num6 != num4)
						{
							int num7 = 0;
							num8 = num6;
							while (true)
							{
								if (num7 < target.Length)
								{
									char c3 = ptr2[num8];
									char c4 = ptr4[num7];
									if (c3 >= '\u0080' || HighCharTable[c3])
									{
										break;
									}
									if (c3 == c4)
									{
										num7++;
										num8++;
										continue;
									}
									goto IL_0135;
								}
								if (num8 < source.Length && ptr2[num8] >= '\u0080')
								{
									break;
								}
								if (matchLengthPtr != null)
								{
									*matchLengthPtr = target.Length;
								}
								return num6;
							}
							break;
						}
						return -1;
						IL_0135:
						if (num8 < source.Length - 1 && (ptr2 + num8)[1] >= '\u0080')
						{
							break;
						}
						num6 += num5;
					}
					break;
				}
				if (fromBeginning)
				{
					return Interop.Globalization.IndexOf(_sortHandle, ptr4, target.Length, ptr2, source.Length, options, matchLengthPtr);
				}
				return Interop.Globalization.LastIndexOf(_sortHandle, ptr4, target.Length, ptr2, source.Length, options, matchLengthPtr);
			}
		}
	}

	private unsafe bool IcuStartsWith(ReadOnlySpan<char> source, ReadOnlySpan<char> prefix, CompareOptions options, int* matchLengthPtr)
	{
		if (_isAsciiEqualityOrdinal && CanUseAsciiOrdinalForOptions(options))
		{
			if ((options & CompareOptions.IgnoreCase) != 0)
			{
				return StartsWithOrdinalIgnoreCaseHelper(source, prefix, options, matchLengthPtr);
			}
			return StartsWithOrdinalHelper(source, prefix, options, matchLengthPtr);
		}
		fixed (char* source2 = &MemoryMarshal.GetReference(source))
		{
			fixed (char* target = &MemoryMarshal.GetReference(prefix))
			{
				return Interop.Globalization.StartsWith(_sortHandle, target, prefix.Length, source2, source.Length, options, matchLengthPtr);
			}
		}
	}

	private unsafe bool StartsWithOrdinalIgnoreCaseHelper(ReadOnlySpan<char> source, ReadOnlySpan<char> prefix, CompareOptions options, int* matchLengthPtr)
	{
		int num = Math.Min(source.Length, prefix.Length);
		fixed (char* ptr = &MemoryMarshal.GetReference(source))
		{
			fixed (char* ptr3 = &MemoryMarshal.GetReference(prefix))
			{
				char* ptr2 = ptr;
				char* ptr4 = ptr3;
				while (true)
				{
					if (num != 0)
					{
						int num2 = *ptr2;
						int num3 = *ptr4;
						if (num2 >= 128 || num3 >= 128 || HighCharTable[num2] || HighCharTable[num3])
						{
							break;
						}
						if (num2 == num3)
						{
							ptr2++;
							ptr4++;
							num--;
							continue;
						}
						if ((uint)(num2 - 97) <= 25u)
						{
							num2 -= 32;
						}
						if ((uint)(num3 - 97) <= 25u)
						{
							num3 -= 32;
						}
						if (num2 == num3)
						{
							ptr2++;
							ptr4++;
							num--;
							continue;
						}
						if ((ptr2 < ptr + source.Length - 1 && ptr2[1] >= '\u0080') || (ptr4 < ptr3 + prefix.Length - 1 && ptr4[1] >= '\u0080'))
						{
							break;
						}
						return false;
					}
					if (source.Length < prefix.Length)
					{
						if (*ptr4 >= '\u0080')
						{
							break;
						}
						return false;
					}
					if (source.Length > prefix.Length && *ptr2 >= '\u0080')
					{
						break;
					}
					if (matchLengthPtr != null)
					{
						*matchLengthPtr = prefix.Length;
					}
					return true;
				}
				return Interop.Globalization.StartsWith(_sortHandle, ptr3, prefix.Length, ptr, source.Length, options, matchLengthPtr);
			}
		}
	}

	private unsafe bool StartsWithOrdinalHelper(ReadOnlySpan<char> source, ReadOnlySpan<char> prefix, CompareOptions options, int* matchLengthPtr)
	{
		int num = Math.Min(source.Length, prefix.Length);
		fixed (char* ptr = &MemoryMarshal.GetReference(source))
		{
			fixed (char* ptr3 = &MemoryMarshal.GetReference(prefix))
			{
				char* ptr2 = ptr;
				char* ptr4 = ptr3;
				while (true)
				{
					if (num != 0)
					{
						int num2 = *ptr2;
						int num3 = *ptr4;
						if (num2 >= 128 || num3 >= 128 || HighCharTable[num2] || HighCharTable[num3])
						{
							break;
						}
						if (num2 == num3)
						{
							ptr2++;
							ptr4++;
							num--;
							continue;
						}
						if ((ptr2 < ptr + source.Length - 1 && ptr2[1] >= '\u0080') || (ptr4 < ptr3 + prefix.Length - 1 && ptr4[1] >= '\u0080'))
						{
							break;
						}
						return false;
					}
					if (source.Length < prefix.Length)
					{
						if (*ptr4 >= '\u0080')
						{
							break;
						}
						return false;
					}
					if (source.Length > prefix.Length && *ptr2 >= '\u0080')
					{
						break;
					}
					if (matchLengthPtr != null)
					{
						*matchLengthPtr = prefix.Length;
					}
					return true;
				}
				return Interop.Globalization.StartsWith(_sortHandle, ptr3, prefix.Length, ptr, source.Length, options, matchLengthPtr);
			}
		}
	}

	private unsafe bool IcuEndsWith(ReadOnlySpan<char> source, ReadOnlySpan<char> suffix, CompareOptions options, int* matchLengthPtr)
	{
		if (_isAsciiEqualityOrdinal && CanUseAsciiOrdinalForOptions(options))
		{
			if ((options & CompareOptions.IgnoreCase) != 0)
			{
				return EndsWithOrdinalIgnoreCaseHelper(source, suffix, options, matchLengthPtr);
			}
			return EndsWithOrdinalHelper(source, suffix, options, matchLengthPtr);
		}
		fixed (char* source2 = &MemoryMarshal.GetReference(source))
		{
			fixed (char* target = &MemoryMarshal.GetReference(suffix))
			{
				return Interop.Globalization.EndsWith(_sortHandle, target, suffix.Length, source2, source.Length, options, matchLengthPtr);
			}
		}
	}

	private unsafe bool EndsWithOrdinalIgnoreCaseHelper(ReadOnlySpan<char> source, ReadOnlySpan<char> suffix, CompareOptions options, int* matchLengthPtr)
	{
		int num = Math.Min(source.Length, suffix.Length);
		fixed (char* ptr = &MemoryMarshal.GetReference(source))
		{
			fixed (char* ptr3 = &MemoryMarshal.GetReference(suffix))
			{
				char* ptr2 = ptr + source.Length - 1;
				char* ptr4 = ptr3 + suffix.Length - 1;
				while (true)
				{
					if (num != 0)
					{
						int num2 = *ptr2;
						int num3 = *ptr4;
						if (num2 >= 128 || num3 >= 128 || HighCharTable[num2] || HighCharTable[num3])
						{
							break;
						}
						if (num2 == num3)
						{
							ptr2--;
							ptr4--;
							num--;
							continue;
						}
						if ((uint)(num2 - 97) <= 25u)
						{
							num2 -= 32;
						}
						if ((uint)(num3 - 97) <= 25u)
						{
							num3 -= 32;
						}
						if (num2 == num3)
						{
							ptr2--;
							ptr4--;
							num--;
							continue;
						}
						if ((ptr2 > ptr && *(ptr2 - 1) >= '\u0080') || (ptr4 > ptr3 && *(ptr4 - 1) >= '\u0080'))
						{
							break;
						}
						return false;
					}
					if (source.Length < suffix.Length)
					{
						if (*ptr4 >= '\u0080')
						{
							break;
						}
						return false;
					}
					if (source.Length > suffix.Length && *ptr2 >= '\u0080')
					{
						break;
					}
					if (matchLengthPtr != null)
					{
						*matchLengthPtr = suffix.Length;
					}
					return true;
				}
				return Interop.Globalization.EndsWith(_sortHandle, ptr3, suffix.Length, ptr, source.Length, options, matchLengthPtr);
			}
		}
	}

	private unsafe bool EndsWithOrdinalHelper(ReadOnlySpan<char> source, ReadOnlySpan<char> suffix, CompareOptions options, int* matchLengthPtr)
	{
		int num = Math.Min(source.Length, suffix.Length);
		fixed (char* ptr = &MemoryMarshal.GetReference(source))
		{
			fixed (char* ptr3 = &MemoryMarshal.GetReference(suffix))
			{
				char* ptr2 = ptr + source.Length - 1;
				char* ptr4 = ptr3 + suffix.Length - 1;
				while (true)
				{
					if (num != 0)
					{
						int num2 = *ptr2;
						int num3 = *ptr4;
						if (num2 >= 128 || num3 >= 128 || HighCharTable[num2] || HighCharTable[num3])
						{
							break;
						}
						if (num2 == num3)
						{
							ptr2--;
							ptr4--;
							num--;
							continue;
						}
						if ((ptr2 > ptr && *(ptr2 - 1) >= '\u0080') || (ptr4 > ptr3 && *(ptr4 - 1) >= '\u0080'))
						{
							break;
						}
						return false;
					}
					if (source.Length < suffix.Length)
					{
						if (*ptr4 >= '\u0080')
						{
							break;
						}
						return false;
					}
					if (source.Length > suffix.Length && *ptr2 >= '\u0080')
					{
						break;
					}
					if (matchLengthPtr != null)
					{
						*matchLengthPtr = suffix.Length;
					}
					return true;
				}
				return Interop.Globalization.EndsWith(_sortHandle, ptr3, suffix.Length, ptr, source.Length, options, matchLengthPtr);
			}
		}
	}

	private unsafe SortKey IcuCreateSortKey(string source, CompareOptions options)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (((uint)options & 0xDFFFFFE0u) != 0)
		{
			throw new ArgumentException(SR.Argument_InvalidFlag, "options");
		}
		byte[] array;
		fixed (char* str = source)
		{
			int sortKey = Interop.Globalization.GetSortKey(_sortHandle, str, source.Length, null, 0, options);
			array = new byte[sortKey];
			fixed (byte[] array2 = array)
			{
				if (Interop.Globalization.GetSortKey(sortKey: (byte*)((array != null && array2.Length != 0) ? System.Runtime.CompilerServices.Unsafe.AsPointer(ref array2[0]) : null), sortHandle: _sortHandle, str: str, strLength: source.Length, sortKeyLength: sortKey, options: options) != sortKey)
				{
					throw new ArgumentException(SR.Arg_ExternalException);
				}
			}
			array2 = null;
		}
		return new SortKey(this, source, options, array);
	}

	private unsafe int IcuGetSortKey(ReadOnlySpan<char> source, Span<byte> destination, CompareOptions options)
	{
		int sortKey2;
		fixed (char* str = &MemoryMarshal.GetReference(source))
		{
			fixed (byte* sortKey = &MemoryMarshal.GetReference(destination))
			{
				sortKey2 = Interop.Globalization.GetSortKey(_sortHandle, str, source.Length, sortKey, destination.Length, options);
			}
		}
		if ((uint)sortKey2 > (uint)destination.Length)
		{
			if (sortKey2 <= destination.Length)
			{
				throw new ArgumentException(SR.Arg_ExternalException);
			}
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		return sortKey2;
	}

	private unsafe int IcuGetSortKeyLength(ReadOnlySpan<char> source, CompareOptions options)
	{
		fixed (char* str = &MemoryMarshal.GetReference(source))
		{
			return Interop.Globalization.GetSortKey(_sortHandle, str, source.Length, null, 0, options);
		}
	}

	private static bool IcuIsSortable(ReadOnlySpan<char> text)
	{
		do
		{
			if (Rune.DecodeFromUtf16(text, out var result, out var charsConsumed) != 0)
			{
				return false;
			}
			UnicodeCategory unicodeCategory = Rune.GetUnicodeCategory(result);
			if (unicodeCategory == UnicodeCategory.PrivateUse || unicodeCategory == UnicodeCategory.OtherNotAssigned)
			{
				return false;
			}
			text = text.Slice(charsConsumed);
		}
		while (!text.IsEmpty);
		return true;
	}

	private unsafe int IcuGetHashCodeOfString(ReadOnlySpan<char> source, CompareOptions options)
	{
		int num = ((source.Length <= 262144) ? (4 * source.Length) : 0);
		byte[] array = null;
		Span<byte> span = ((num > 1024) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(num))) : stackalloc byte[1024]);
		Span<byte> span2 = span;
		fixed (char* str = &MemoryMarshal.GetNonNullPinnableReference(source))
		{
			fixed (byte* sortKey = &MemoryMarshal.GetReference(span2))
			{
				num = Interop.Globalization.GetSortKey(_sortHandle, str, source.Length, sortKey, span2.Length, options);
			}
			if (num > span2.Length)
			{
				if (array != null)
				{
					ArrayPool<byte>.Shared.Return(array);
				}
				span2 = (array = ArrayPool<byte>.Shared.Rent(num));
				fixed (byte* sortKey2 = &MemoryMarshal.GetReference(span2))
				{
					num = Interop.Globalization.GetSortKey(_sortHandle, str, source.Length, sortKey2, span2.Length, options);
				}
			}
		}
		if (num == 0 || num > span2.Length)
		{
			throw new ArgumentException(SR.Arg_ExternalException);
		}
		int result = Marvin.ComputeHash32(span2.Slice(0, num), Marvin.DefaultSeed);
		if (array != null)
		{
			ArrayPool<byte>.Shared.Return(array);
		}
		return result;
	}

	private static bool CanUseAsciiOrdinalForOptions(CompareOptions options)
	{
		return (options & CompareOptions.IgnoreSymbols) == 0;
	}

	private SortVersion IcuGetSortVersion()
	{
		int sortVersion = Interop.Globalization.GetSortVersion(_sortHandle);
		return new SortVersion(sortVersion, LCID, new Guid(sortVersion, 0, 0, 0, 0, 0, 0, (byte)(LCID >> 24), (byte)((LCID & 0xFF0000) >> 16), (byte)((LCID & 0xFF00) >> 8), (byte)((uint)LCID & 0xFFu)));
	}

	private SortKey InvariantCreateSortKey(string source, CompareOptions options)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (((uint)options & 0xDFFFFFE0u) != 0)
		{
			throw new ArgumentException(SR.Argument_InvalidFlag, "options");
		}
		byte[] array;
		if (source.Length == 0)
		{
			array = Array.Empty<byte>();
		}
		else
		{
			array = new byte[source.Length * 2];
			if ((options & (CompareOptions.IgnoreCase | CompareOptions.OrdinalIgnoreCase)) != 0)
			{
				InvariantCreateSortKeyOrdinalIgnoreCase(source, array);
			}
			else
			{
				InvariantCreateSortKeyOrdinal(source, array);
			}
		}
		return new SortKey(this, source, options, array);
	}

	private static void InvariantCreateSortKeyOrdinal(ReadOnlySpan<char> source, Span<byte> sortKey)
	{
		for (int i = 0; i < source.Length; i++)
		{
			BinaryPrimitives.WriteUInt16BigEndian(sortKey, source[i]);
			sortKey = sortKey.Slice(2);
		}
	}

	private static void InvariantCreateSortKeyOrdinalIgnoreCase(ReadOnlySpan<char> source, Span<byte> sortKey)
	{
		for (int i = 0; i < source.Length; i++)
		{
			char c = source[i];
			if (char.IsHighSurrogate(c) && i < source.Length - 1)
			{
				char c2 = source[i + 1];
				if (char.IsLowSurrogate(c2))
				{
					SurrogateCasing.ToUpper(c, c2, out var hr, out var lr);
					BinaryPrimitives.WriteUInt16BigEndian(sortKey, hr);
					BinaryPrimitives.WriteUInt16BigEndian(sortKey, lr);
					i++;
					sortKey = sortKey.Slice(4);
					continue;
				}
			}
			BinaryPrimitives.WriteUInt16BigEndian(sortKey, InvariantModeCasing.ToUpper(c));
			sortKey = sortKey.Slice(2);
		}
	}

	private static int InvariantGetSortKey(ReadOnlySpan<char> source, Span<byte> destination, CompareOptions options)
	{
		if ((uint)destination.Length < (uint)(source.Length * 2))
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		if ((options & CompareOptions.IgnoreCase) == 0)
		{
			InvariantCreateSortKeyOrdinal(source, destination);
		}
		else
		{
			InvariantCreateSortKeyOrdinalIgnoreCase(source, destination);
		}
		return source.Length * 2;
	}

	private static int InvariantGetSortKeyLength(ReadOnlySpan<char> source, CompareOptions options)
	{
		int num = source.Length * 2;
		if (num < 0)
		{
			throw new ArgumentException(SR.ArgumentOutOfRange_GetByteCountOverflow, "source");
		}
		return num;
	}

	private void NlsInitSortHandle()
	{
		_sortHandle = NlsGetSortHandle(_sortName);
	}

	internal unsafe static IntPtr NlsGetSortHandle(string cultureName)
	{
		if (GlobalizationMode.Invariant)
		{
			return IntPtr.Zero;
		}
		System.Runtime.CompilerServices.Unsafe.SkipInit(out IntPtr intPtr);
		int num = Interop.Kernel32.LCMapStringEx(cultureName, 536870912u, null, 0, &intPtr, IntPtr.Size, null, null, IntPtr.Zero);
		if (num > 0)
		{
			int num2 = 0;
			char c = 'a';
			num = Interop.Kernel32.LCMapStringEx(null, 262144u, &c, 1, &num2, 4, null, null, intPtr);
			if (num > 1)
			{
				return intPtr;
			}
		}
		return IntPtr.Zero;
	}

	private unsafe static int FindStringOrdinal(uint dwFindStringOrdinalFlags, ReadOnlySpan<char> source, ReadOnlySpan<char> value, bool bIgnoreCase)
	{
		fixed (char* lpStringSource = &MemoryMarshal.GetReference(source))
		{
			fixed (char* lpStringValue = &MemoryMarshal.GetReference(value))
			{
				return Interop.Kernel32.FindStringOrdinal(dwFindStringOrdinalFlags, lpStringSource, source.Length, lpStringValue, value.Length, bIgnoreCase ? Interop.BOOL.TRUE : Interop.BOOL.FALSE);
			}
		}
	}

	internal static int NlsIndexOfOrdinalCore(ReadOnlySpan<char> source, ReadOnlySpan<char> value, bool ignoreCase, bool fromBeginning)
	{
		uint dwFindStringOrdinalFlags = (fromBeginning ? 4194304u : 8388608u);
		return FindStringOrdinal(dwFindStringOrdinalFlags, source, value, ignoreCase);
	}

	private unsafe int NlsGetHashCodeOfString(ReadOnlySpan<char> source, CompareOptions options)
	{
		if (!Environment.IsWindows8OrAbove)
		{
			source = source.ToString();
		}
		int num = source.Length;
		if (num == 0)
		{
			source = string.Empty;
			num = -1;
		}
		uint dwMapFlags = 0x400u | (uint)GetNativeCompareFlags(options);
		fixed (char* lpSrcStr = &MemoryMarshal.GetReference(source))
		{
			int num2 = Interop.Kernel32.LCMapStringEx((_sortHandle != IntPtr.Zero) ? null : _sortName, dwMapFlags, lpSrcStr, num, null, 0, null, null, _sortHandle);
			if (num2 == 0)
			{
				throw new ArgumentException(SR.Arg_ExternalException);
			}
			byte[] array = null;
			Span<byte> span = ((num2 > 512) ? ((Span<byte>)(array = ArrayPool<byte>.Shared.Rent(num2))) : stackalloc byte[512]);
			Span<byte> span2 = span;
			fixed (byte* lpDestStr = &MemoryMarshal.GetReference(span2))
			{
				if (Interop.Kernel32.LCMapStringEx((_sortHandle != IntPtr.Zero) ? null : _sortName, dwMapFlags, lpSrcStr, num, lpDestStr, num2, null, null, _sortHandle) != num2)
				{
					throw new ArgumentException(SR.Arg_ExternalException);
				}
			}
			int result = Marvin.ComputeHash32(span2.Slice(0, num2), Marvin.DefaultSeed);
			if (array != null)
			{
				ArrayPool<byte>.Shared.Return(array);
			}
			return result;
		}
	}

	internal unsafe static int NlsCompareStringOrdinalIgnoreCase(ref char string1, int count1, ref char string2, int count2)
	{
		fixed (char* lpString = &string1)
		{
			fixed (char* lpString2 = &string2)
			{
				int num = Interop.Kernel32.CompareStringOrdinal(lpString, count1, lpString2, count2, bIgnoreCase: true);
				if (num == 0)
				{
					throw new ArgumentException(SR.Arg_ExternalException);
				}
				return num - 2;
			}
		}
	}

	private unsafe int NlsCompareString(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, CompareOptions options)
	{
		string text = ((_sortHandle != IntPtr.Zero) ? null : _sortName);
		if (string1.IsEmpty)
		{
			string1 = string.Empty;
		}
		if (string2.IsEmpty)
		{
			string2 = string.Empty;
		}
		fixed (char* lpLocaleName = text)
		{
			fixed (char* lpString = &MemoryMarshal.GetReference(string1))
			{
				fixed (char* lpString2 = &MemoryMarshal.GetReference(string2))
				{
					int num = Interop.Kernel32.CompareStringEx(lpLocaleName, (uint)GetNativeCompareFlags(options), lpString, string1.Length, lpString2, string2.Length, null, null, _sortHandle);
					if (num == 0)
					{
						throw new ArgumentException(SR.Arg_ExternalException);
					}
					return num - 2;
				}
			}
		}
	}

	private unsafe int FindString(uint dwFindNLSStringFlags, ReadOnlySpan<char> lpStringSource, ReadOnlySpan<char> lpStringValue, int* pcchFound)
	{
		string text = ((_sortHandle != IntPtr.Zero) ? null : _sortName);
		int num = lpStringSource.Length;
		if (num == 0)
		{
			lpStringSource = string.Empty;
			num = -1;
		}
		fixed (char* lpLocaleName = text)
		{
			fixed (char* lpStringSource2 = &MemoryMarshal.GetReference(lpStringSource))
			{
				fixed (char* lpStringValue2 = &MemoryMarshal.GetReference(lpStringValue))
				{
					return Interop.Kernel32.FindNLSStringEx(lpLocaleName, dwFindNLSStringFlags, lpStringSource2, num, lpStringValue2, lpStringValue.Length, pcchFound, null, null, _sortHandle);
				}
			}
		}
	}

	private unsafe int NlsIndexOfCore(ReadOnlySpan<char> source, ReadOnlySpan<char> target, CompareOptions options, int* matchLengthPtr, bool fromBeginning)
	{
		uint num = (fromBeginning ? 4194304u : 8388608u);
		return FindString(num | (uint)GetNativeCompareFlags(options), source, target, matchLengthPtr);
	}

	private unsafe bool NlsStartsWith(ReadOnlySpan<char> source, ReadOnlySpan<char> prefix, CompareOptions options, int* matchLengthPtr)
	{
		int num = FindString(0x100000u | (uint)GetNativeCompareFlags(options), source, prefix, matchLengthPtr);
		if (num >= 0)
		{
			if (matchLengthPtr != null)
			{
				*matchLengthPtr += num;
			}
			return true;
		}
		return false;
	}

	private unsafe bool NlsEndsWith(ReadOnlySpan<char> source, ReadOnlySpan<char> suffix, CompareOptions options, int* matchLengthPtr)
	{
		int num = FindString(0x200000u | (uint)GetNativeCompareFlags(options), source, suffix, null);
		if (num >= 0)
		{
			if (matchLengthPtr != null)
			{
				*matchLengthPtr = source.Length - num;
			}
			return true;
		}
		return false;
	}

	private unsafe SortKey NlsCreateSortKey(string source, CompareOptions options)
	{
		if (source == null)
		{
			throw new ArgumentNullException("source");
		}
		if (((uint)options & 0xDFFFFFE0u) != 0)
		{
			throw new ArgumentException(SR.Argument_InvalidFlag, "options");
		}
		uint dwMapFlags = 0x400u | (uint)GetNativeCompareFlags(options);
		int num = source.Length;
		if (num == 0)
		{
			num = -1;
		}
		byte[] array;
		fixed (char* lpSrcStr = source)
		{
			int num2 = Interop.Kernel32.LCMapStringEx((_sortHandle != IntPtr.Zero) ? null : _sortName, dwMapFlags, lpSrcStr, num, null, 0, null, null, _sortHandle);
			if (num2 == 0)
			{
				throw new ArgumentException(SR.Arg_ExternalException);
			}
			array = new byte[num2];
			fixed (byte* lpDestStr = array)
			{
				if (Interop.Kernel32.LCMapStringEx((_sortHandle != IntPtr.Zero) ? null : _sortName, dwMapFlags, lpSrcStr, num, lpDestStr, array.Length, null, null, _sortHandle) != num2)
				{
					throw new ArgumentException(SR.Arg_ExternalException);
				}
			}
		}
		return new SortKey(this, source, options, array);
	}

	private unsafe int NlsGetSortKey(ReadOnlySpan<char> source, Span<byte> destination, CompareOptions options)
	{
		if (destination.IsEmpty)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		if (!Environment.IsWindows8OrAbove)
		{
			source = source.ToString();
		}
		uint dwMapFlags = 0x400u | (uint)GetNativeCompareFlags(options);
		int num = source.Length;
		if (num == 0)
		{
			source = string.Empty;
			num = -1;
		}
		int num3;
		fixed (char* lpSrcStr = &MemoryMarshal.GetReference(source))
		{
			fixed (byte* lpDestStr = &MemoryMarshal.GetReference(destination))
			{
				if (!Environment.IsWindows8OrAbove)
				{
					int num2 = Interop.Kernel32.LCMapStringEx((_sortHandle != IntPtr.Zero) ? null : _sortName, dwMapFlags, lpSrcStr, num, null, 0, null, null, _sortHandle);
					if (num2 > destination.Length)
					{
						ThrowHelper.ThrowArgumentException_DestinationTooShort();
					}
					if (num2 <= 0)
					{
						throw new ArgumentException(SR.Arg_ExternalException);
					}
				}
				num3 = Interop.Kernel32.LCMapStringEx((_sortHandle != IntPtr.Zero) ? null : _sortName, dwMapFlags, lpSrcStr, num, lpDestStr, destination.Length, null, null, _sortHandle);
			}
		}
		if (num3 <= 0)
		{
			if (Marshal.GetLastPInvokeError() != 122)
			{
				throw new ArgumentException(SR.Arg_ExternalException);
			}
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		return num3;
	}

	private unsafe int NlsGetSortKeyLength(ReadOnlySpan<char> source, CompareOptions options)
	{
		uint dwMapFlags = 0x400u | (uint)GetNativeCompareFlags(options);
		int num = source.Length;
		if (num == 0)
		{
			source = string.Empty;
			num = -1;
		}
		int num2;
		fixed (char* lpSrcStr = &MemoryMarshal.GetReference(source))
		{
			num2 = Interop.Kernel32.LCMapStringEx((_sortHandle != IntPtr.Zero) ? null : _sortName, dwMapFlags, lpSrcStr, num, null, 0, null, null, _sortHandle);
		}
		if (num2 <= 0)
		{
			throw new ArgumentException(SR.Arg_ExternalException);
		}
		return num2;
	}

	private unsafe static bool NlsIsSortable(ReadOnlySpan<char> text)
	{
		fixed (char* lpString = &MemoryMarshal.GetReference(text))
		{
			return Interop.Kernel32.IsNLSDefinedString(1, 0u, IntPtr.Zero, lpString, text.Length);
		}
	}

	private static int GetNativeCompareFlags(CompareOptions options)
	{
		int num = 134217728;
		if ((options & CompareOptions.IgnoreCase) != 0)
		{
			num |= 1;
		}
		if ((options & CompareOptions.IgnoreKanaType) != 0)
		{
			num |= 0x10000;
		}
		if ((options & CompareOptions.IgnoreNonSpace) != 0)
		{
			num |= 2;
		}
		if ((options & CompareOptions.IgnoreSymbols) != 0)
		{
			num |= 4;
		}
		if ((options & CompareOptions.IgnoreWidth) != 0)
		{
			num |= 0x20000;
		}
		if ((options & CompareOptions.StringSort) != 0)
		{
			num |= 0x1000;
		}
		if (options == CompareOptions.Ordinal)
		{
			num = 1073741824;
		}
		return num;
	}

	private unsafe SortVersion NlsGetSortVersion()
	{
		Interop.Kernel32.NlsVersionInfoEx nlsVersionInfoEx = default(Interop.Kernel32.NlsVersionInfoEx);
		nlsVersionInfoEx.dwNLSVersionInfoSize = sizeof(Interop.Kernel32.NlsVersionInfoEx);
		Interop.Kernel32.GetNLSVersionEx(1, _sortName, &nlsVersionInfoEx);
		return new SortVersion(nlsVersionInfoEx.dwNLSVersion, (nlsVersionInfoEx.dwEffectiveId == 0) ? LCID : nlsVersionInfoEx.dwEffectiveId, nlsVersionInfoEx.guidCustomVersion);
	}
}
