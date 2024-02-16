namespace System.Linq;

internal static class Strings
{
	internal static string ArgumentNotIEnumerableGeneric(string message)
	{
		return System.SR.Format(System.SR.ArgumentNotIEnumerableGeneric, message);
	}

	internal static string ArgumentNotValid(string message)
	{
		return System.SR.Format(System.SR.ArgumentNotValid, message);
	}

	internal static string NoMethodOnType(string name, object type)
	{
		return System.SR.Format(System.SR.NoMethodOnType, name, type);
	}

	internal static string NoMethodOnTypeMatchingArguments(string name, object type)
	{
		return System.SR.Format(System.SR.NoMethodOnTypeMatchingArguments, name, type);
	}

	internal static string EnumeratingNullEnumerableExpression()
	{
		return System.SR.EnumeratingNullEnumerableExpression;
	}
}
