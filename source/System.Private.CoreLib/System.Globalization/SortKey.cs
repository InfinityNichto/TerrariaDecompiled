using System.Diagnostics.CodeAnalysis;

namespace System.Globalization;

public sealed class SortKey
{
	private readonly CompareInfo _compareInfo;

	private readonly CompareOptions _options;

	private readonly string _string;

	private readonly byte[] _keyData;

	public string OriginalString => _string;

	public byte[] KeyData => (byte[])_keyData.Clone();

	internal SortKey(CompareInfo compareInfo, string str, CompareOptions options, byte[] keyData)
	{
		_keyData = keyData;
		_compareInfo = compareInfo;
		_options = options;
		_string = str;
	}

	public static int Compare(SortKey sortkey1, SortKey sortkey2)
	{
		if (sortkey1 == null)
		{
			throw new ArgumentNullException("sortkey1");
		}
		if (sortkey2 == null)
		{
			throw new ArgumentNullException("sortkey2");
		}
		byte[] keyData = sortkey1._keyData;
		byte[] keyData2 = sortkey2._keyData;
		return new ReadOnlySpan<byte>(keyData).SequenceCompareTo(keyData2);
	}

	public override bool Equals([NotNullWhen(true)] object? value)
	{
		if (value is SortKey sortKey)
		{
			return new ReadOnlySpan<byte>(_keyData).SequenceEqual(sortKey._keyData);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _compareInfo.GetHashCode(_string, _options);
	}

	public override string ToString()
	{
		return $"SortKey - {_compareInfo.Name}, {_options}, {_string}";
	}
}
