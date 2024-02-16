using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Unicode;

namespace System.Globalization;

public class StringInfo
{
	private string _str;

	private int[] _indexes;

	private int[]? Indexes
	{
		get
		{
			if (_indexes == null && String.Length > 0)
			{
				_indexes = ParseCombiningCharacters(String);
			}
			return _indexes;
		}
	}

	public string String
	{
		get
		{
			return _str;
		}
		[MemberNotNull("_str")]
		set
		{
			_str = value ?? throw new ArgumentNullException("value");
			_indexes = null;
		}
	}

	public int LengthInTextElements
	{
		get
		{
			int[]? indexes = Indexes;
			if (indexes == null)
			{
				return 0;
			}
			return indexes.Length;
		}
	}

	public StringInfo()
		: this(string.Empty)
	{
	}

	public StringInfo(string value)
	{
		String = value;
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value is StringInfo stringInfo)
		{
			return _str.Equals(stringInfo._str);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _str.GetHashCode();
	}

	public string SubstringByTextElements(int startingTextElement)
	{
		int[]? indexes = Indexes;
		return SubstringByTextElements(startingTextElement, ((indexes != null) ? indexes.Length : 0) - startingTextElement);
	}

	public string SubstringByTextElements(int startingTextElement, int lengthInTextElements)
	{
		int[] array = Indexes ?? Array.Empty<int>();
		if ((uint)startingTextElement >= (uint)array.Length)
		{
			throw new ArgumentOutOfRangeException("startingTextElement", startingTextElement, SR.Arg_ArgumentOutOfRangeException);
		}
		if ((uint)lengthInTextElements > (uint)(array.Length - startingTextElement))
		{
			throw new ArgumentOutOfRangeException("lengthInTextElements", lengthInTextElements, SR.Arg_ArgumentOutOfRangeException);
		}
		int num = array[startingTextElement];
		Index end = ^0;
		if ((uint)(startingTextElement + lengthInTextElements) < (uint)array.Length)
		{
			end = array[startingTextElement + lengthInTextElements];
		}
		return String[num..end];
	}

	public static string GetNextTextElement(string str)
	{
		return GetNextTextElement(str, 0);
	}

	public static string GetNextTextElement(string str, int index)
	{
		int nextTextElementLength = GetNextTextElementLength(str, index);
		return str.Substring(index, nextTextElementLength);
	}

	public static int GetNextTextElementLength(string str)
	{
		return GetNextTextElementLength(str, 0);
	}

	public static int GetNextTextElementLength(string str, int index)
	{
		if (str == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.str);
		}
		if ((uint)index > (uint)str.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRange_IndexException();
		}
		return GetNextTextElementLength(str.AsSpan(index));
	}

	public static int GetNextTextElementLength(ReadOnlySpan<char> str)
	{
		return TextSegmentationUtility.GetLengthOfFirstUtf16ExtendedGraphemeCluster(str);
	}

	public static TextElementEnumerator GetTextElementEnumerator(string str)
	{
		return GetTextElementEnumerator(str, 0);
	}

	public static TextElementEnumerator GetTextElementEnumerator(string str, int index)
	{
		if (str == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.str);
		}
		if ((uint)index > (uint)str.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRange_IndexException();
		}
		return new TextElementEnumerator(str, index);
	}

	public static int[] ParseCombiningCharacters(string str)
	{
		if (str == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.str);
		}
		Span<int> initialSpan = stackalloc int[64];
		ValueListBuilder<int> valueListBuilder = new ValueListBuilder<int>(initialSpan);
		ReadOnlySpan<char> str2 = str;
		while (!str2.IsEmpty)
		{
			valueListBuilder.Append(str.Length - str2.Length);
			str2 = str2.Slice(GetNextTextElementLength(str2));
		}
		int[] result = valueListBuilder.AsSpan().ToArray();
		valueListBuilder.Dispose();
		return result;
	}
}
