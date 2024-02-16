namespace System.IO;

public class EnumerationOptions
{
	private int _maxRecursionDepth;

	internal static EnumerationOptions Compatible { get; } = new EnumerationOptions
	{
		MatchType = MatchType.Win32,
		AttributesToSkip = (FileAttributes)0,
		IgnoreInaccessible = false
	};


	private static EnumerationOptions CompatibleRecursive { get; } = new EnumerationOptions
	{
		RecurseSubdirectories = true,
		MatchType = MatchType.Win32,
		AttributesToSkip = (FileAttributes)0,
		IgnoreInaccessible = false
	};


	internal static EnumerationOptions Default { get; } = new EnumerationOptions();


	public bool RecurseSubdirectories { get; set; }

	public bool IgnoreInaccessible { get; set; }

	public int BufferSize { get; set; }

	public FileAttributes AttributesToSkip { get; set; }

	public MatchType MatchType { get; set; }

	public MatchCasing MatchCasing { get; set; }

	public int MaxRecursionDepth
	{
		get
		{
			return _maxRecursionDepth;
		}
		set
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_NeedNonNegNum);
			}
			_maxRecursionDepth = value;
		}
	}

	public bool ReturnSpecialDirectories { get; set; }

	public EnumerationOptions()
	{
		IgnoreInaccessible = true;
		AttributesToSkip = FileAttributes.Hidden | FileAttributes.System;
		MaxRecursionDepth = int.MaxValue;
	}

	internal static EnumerationOptions FromSearchOption(SearchOption searchOption)
	{
		if (searchOption != 0 && searchOption != SearchOption.AllDirectories)
		{
			throw new ArgumentOutOfRangeException("searchOption", SR.ArgumentOutOfRange_Enum);
		}
		if (searchOption != SearchOption.AllDirectories)
		{
			return Compatible;
		}
		return CompatibleRecursive;
	}
}
