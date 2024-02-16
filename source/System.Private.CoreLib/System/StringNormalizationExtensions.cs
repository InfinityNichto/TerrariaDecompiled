using System.Text;

namespace System;

public static class StringNormalizationExtensions
{
	public static bool IsNormalized(this string strInput)
	{
		return IsNormalized(strInput, NormalizationForm.FormC);
	}

	public static bool IsNormalized(this string strInput, NormalizationForm normalizationForm)
	{
		if (strInput == null)
		{
			throw new ArgumentNullException("strInput");
		}
		return strInput.IsNormalized(normalizationForm);
	}

	public static string Normalize(this string strInput)
	{
		return Normalize(strInput, NormalizationForm.FormC);
	}

	public static string Normalize(this string strInput, NormalizationForm normalizationForm)
	{
		if (strInput == null)
		{
			throw new ArgumentNullException("strInput");
		}
		return strInput.Normalize(normalizationForm);
	}
}
