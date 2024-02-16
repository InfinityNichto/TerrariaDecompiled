using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization;

internal static class Fx
{
	[Conditional("DEBUG")]
	public static void Assert([DoesNotReturnIf(false)] bool condition, string message)
	{
	}

	[Conditional("DEBUG")]
	[DoesNotReturn]
	public static void Assert(string message)
	{
	}
}
