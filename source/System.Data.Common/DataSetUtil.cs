using System;
using System.Data;
using System.Globalization;
using System.Security;
using System.Threading;

internal static class DataSetUtil
{
	private static readonly Type s_stackOverflowType = typeof(StackOverflowException);

	private static readonly Type s_outOfMemoryType = typeof(OutOfMemoryException);

	private static readonly Type s_threadAbortType = typeof(ThreadAbortException);

	private static readonly Type s_nullReferenceType = typeof(NullReferenceException);

	private static readonly Type s_accessViolationType = typeof(AccessViolationException);

	private static readonly Type s_securityType = typeof(SecurityException);

	internal static void CheckArgumentNull<T>(T argumentValue, string argumentName) where T : class
	{
		if (argumentValue == null)
		{
			throw ArgumentNull(argumentName);
		}
	}

	private static T TraceException<T>(string trace, T e)
	{
		return e;
	}

	private static T TraceExceptionAsReturnValue<T>(T e)
	{
		return TraceException("<comm.ADP.TraceException|ERR|THROW> '%ls'\n", e);
	}

	internal static ArgumentException Argument(string message)
	{
		return TraceExceptionAsReturnValue(new ArgumentException(message));
	}

	internal static ArgumentNullException ArgumentNull(string message)
	{
		return TraceExceptionAsReturnValue(new ArgumentNullException(message));
	}

	internal static ArgumentOutOfRangeException ArgumentOutOfRange(string message, string parameterName)
	{
		return TraceExceptionAsReturnValue(new ArgumentOutOfRangeException(parameterName, message));
	}

	internal static InvalidCastException InvalidCast(string message)
	{
		return TraceExceptionAsReturnValue(new InvalidCastException(message));
	}

	internal static InvalidOperationException InvalidOperation(string message)
	{
		return TraceExceptionAsReturnValue(new InvalidOperationException(message));
	}

	internal static NotSupportedException NotSupported(string message)
	{
		return TraceExceptionAsReturnValue(new NotSupportedException(message));
	}

	internal static ArgumentOutOfRangeException InvalidEnumerationValue(Type type, int value)
	{
		return ArgumentOutOfRange(System.SR.Format(System.SR.DataSetLinq_InvalidEnumerationValue, type.Name, value.ToString(CultureInfo.InvariantCulture)), type.Name);
	}

	internal static ArgumentOutOfRangeException InvalidDataRowState(DataRowState value)
	{
		return InvalidEnumerationValue(typeof(DataRowState), (int)value);
	}

	internal static ArgumentOutOfRangeException InvalidLoadOption(LoadOption value)
	{
		return InvalidEnumerationValue(typeof(LoadOption), (int)value);
	}

	internal static bool IsCatchableExceptionType(Exception e)
	{
		Type type = e.GetType();
		if (type != s_stackOverflowType && type != s_outOfMemoryType && type != s_threadAbortType && type != s_nullReferenceType && type != s_accessViolationType)
		{
			return !s_securityType.IsAssignableFrom(type);
		}
		return false;
	}
}
