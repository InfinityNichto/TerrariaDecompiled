namespace System.Formats.Asn1;

public struct AsnReaderOptions
{
	private ushort _twoDigitYearMax;

	public int UtcTimeTwoDigitYearMax
	{
		get
		{
			if (_twoDigitYearMax == 0)
			{
				return 2049;
			}
			return _twoDigitYearMax;
		}
		set
		{
			if (value < 1 || value > 9999)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_twoDigitYearMax = (ushort)value;
		}
	}

	public bool SkipSetSortOrderVerification { get; set; }
}
