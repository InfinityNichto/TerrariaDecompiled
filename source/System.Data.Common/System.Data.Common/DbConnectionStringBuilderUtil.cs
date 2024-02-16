using System.Globalization;

namespace System.Data.Common;

internal static class DbConnectionStringBuilderUtil
{
	internal static string ConvertToString(object value)
	{
		try
		{
			return ((IConvertible)value).ToString(CultureInfo.InvariantCulture);
		}
		catch (InvalidCastException innerException)
		{
			throw ADP.ConvertFailed(value.GetType(), typeof(string), innerException);
		}
	}
}
