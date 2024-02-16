namespace System.Globalization;

public static class GlobalizationExtensions
{
	public static StringComparer GetStringComparer(this CompareInfo compareInfo, CompareOptions options)
	{
		if (compareInfo == null)
		{
			throw new ArgumentNullException("compareInfo");
		}
		return options switch
		{
			CompareOptions.Ordinal => StringComparer.Ordinal, 
			CompareOptions.OrdinalIgnoreCase => StringComparer.OrdinalIgnoreCase, 
			_ => new CultureAwareComparer(compareInfo, options), 
		};
	}
}
