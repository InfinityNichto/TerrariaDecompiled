namespace System.Security.Cryptography;

internal static class KeySizeHelpers
{
	public static bool IsLegalSize(this int size, KeySizes[] legalSizes)
	{
		bool validatedByZeroSkipSizeKeySizes;
		return size.IsLegalSize(legalSizes, out validatedByZeroSkipSizeKeySizes);
	}

	public static bool IsLegalSize(this int size, KeySizes legalSizes, out bool validatedByZeroSkipSizeKeySizes)
	{
		validatedByZeroSkipSizeKeySizes = false;
		if (legalSizes.SkipSize == 0)
		{
			if (legalSizes.MinSize == size)
			{
				validatedByZeroSkipSizeKeySizes = true;
				return true;
			}
		}
		else if (size >= legalSizes.MinSize && size <= legalSizes.MaxSize)
		{
			int num = size - legalSizes.MinSize;
			if (num % legalSizes.SkipSize == 0)
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsLegalSize(this int size, KeySizes[] legalSizes, out bool validatedByZeroSkipSizeKeySizes)
	{
		for (int i = 0; i < legalSizes.Length; i++)
		{
			if (size.IsLegalSize(legalSizes[i], out validatedByZeroSkipSizeKeySizes))
			{
				return true;
			}
		}
		validatedByZeroSkipSizeKeySizes = false;
		return false;
	}
}
