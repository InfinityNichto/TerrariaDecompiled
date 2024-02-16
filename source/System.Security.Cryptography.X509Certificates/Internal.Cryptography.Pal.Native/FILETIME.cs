using System;

namespace Internal.Cryptography.Pal.Native;

internal struct FILETIME
{
	private uint ftTimeLow;

	private uint ftTimeHigh;

	public DateTime ToDateTime()
	{
		long fileTime = (long)(((ulong)ftTimeHigh << 32) + ftTimeLow);
		return DateTime.FromFileTime(fileTime);
	}

	public static FILETIME FromDateTime(DateTime dt)
	{
		long num = dt.ToFileTime();
		FILETIME result = default(FILETIME);
		result.ftTimeLow = (uint)num;
		result.ftTimeHigh = (uint)(num >> 32);
		return result;
	}
}
