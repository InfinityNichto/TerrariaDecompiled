using System.Diagnostics.CodeAnalysis;

namespace System.Text.Encodings.Web;

internal static class ThrowHelper
{
	[DoesNotReturn]
	internal static void ThrowArgumentNullException(ExceptionArgument argument)
	{
		throw new ArgumentNullException(GetArgumentName(argument));
	}

	private static string GetArgumentName(ExceptionArgument argument)
	{
		return argument.ToString();
	}
}
