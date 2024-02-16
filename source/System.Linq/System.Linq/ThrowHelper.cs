using System.Diagnostics.CodeAnalysis;

namespace System.Linq;

internal static class ThrowHelper
{
	[DoesNotReturn]
	internal static void ThrowArgumentNullException(ExceptionArgument argument)
	{
		throw new ArgumentNullException(GetArgumentString(argument));
	}

	[DoesNotReturn]
	internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument)
	{
		throw new ArgumentOutOfRangeException(GetArgumentString(argument));
	}

	[DoesNotReturn]
	internal static void ThrowMoreThanOneElementException()
	{
		throw new InvalidOperationException(System.SR.MoreThanOneElement);
	}

	[DoesNotReturn]
	internal static void ThrowMoreThanOneMatchException()
	{
		throw new InvalidOperationException(System.SR.MoreThanOneMatch);
	}

	[DoesNotReturn]
	internal static void ThrowNoElementsException()
	{
		throw new InvalidOperationException(System.SR.NoElements);
	}

	[DoesNotReturn]
	internal static void ThrowNoMatchException()
	{
		throw new InvalidOperationException(System.SR.NoMatch);
	}

	[DoesNotReturn]
	internal static void ThrowNotSupportedException()
	{
		throw new NotSupportedException();
	}

	private static string GetArgumentString(ExceptionArgument argument)
	{
		return argument switch
		{
			ExceptionArgument.collectionSelector => "collectionSelector", 
			ExceptionArgument.count => "count", 
			ExceptionArgument.elementSelector => "elementSelector", 
			ExceptionArgument.enumerable => "enumerable", 
			ExceptionArgument.first => "first", 
			ExceptionArgument.func => "func", 
			ExceptionArgument.index => "index", 
			ExceptionArgument.inner => "inner", 
			ExceptionArgument.innerKeySelector => "innerKeySelector", 
			ExceptionArgument.keySelector => "keySelector", 
			ExceptionArgument.outer => "outer", 
			ExceptionArgument.outerKeySelector => "outerKeySelector", 
			ExceptionArgument.predicate => "predicate", 
			ExceptionArgument.resultSelector => "resultSelector", 
			ExceptionArgument.second => "second", 
			ExceptionArgument.selector => "selector", 
			ExceptionArgument.source => "source", 
			ExceptionArgument.third => "third", 
			ExceptionArgument.size => "size", 
			_ => string.Empty, 
		};
	}
}
