using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json;

internal static class ThrowHelper
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowOutOfMemoryException_BufferMaximumSizeExceeded(uint capacity)
	{
		throw new OutOfMemoryException(System.SR.Format(System.SR.BufferMaximumSizeExceeded, capacity));
	}

	public static ArgumentOutOfRangeException GetArgumentOutOfRangeException_MaxDepthMustBePositive(string parameterName)
	{
		return GetArgumentOutOfRangeException(parameterName, System.SR.MaxDepthMustBePositive);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static ArgumentOutOfRangeException GetArgumentOutOfRangeException(string parameterName, string message)
	{
		return new ArgumentOutOfRangeException(parameterName, message);
	}

	public static ArgumentOutOfRangeException GetArgumentOutOfRangeException_CommentEnumMustBeInRange(string parameterName)
	{
		return GetArgumentOutOfRangeException(parameterName, System.SR.CommentHandlingMustBeValid);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static ArgumentException GetArgumentException(string message)
	{
		return new ArgumentException(message);
	}

	[DoesNotReturn]
	public static void ThrowArgumentException(string message)
	{
		throw GetArgumentException(message);
	}

	[DoesNotReturn]
	public static void ThrowArgumentException_PropertyNameTooLarge(int tokenLength)
	{
		throw GetArgumentException(System.SR.Format(System.SR.PropertyNameTooLarge, tokenLength));
	}

	[DoesNotReturn]
	public static void ThrowArgumentException_ValueTooLarge(int tokenLength)
	{
		throw GetArgumentException(System.SR.Format(System.SR.ValueTooLarge, tokenLength));
	}

	[DoesNotReturn]
	public static void ThrowArgumentException_ValueNotSupported()
	{
		throw GetArgumentException(System.SR.SpecialNumberValuesNotSupported);
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_NeedLargerSpan()
	{
		throw GetInvalidOperationException(System.SR.FailedToGetLargerSpan);
	}

	[DoesNotReturn]
	public static void ThrowArgumentException(ReadOnlySpan<byte> propertyName, ReadOnlySpan<byte> value)
	{
		if (propertyName.Length > 166666666)
		{
			ThrowArgumentException(System.SR.Format(System.SR.PropertyNameTooLarge, propertyName.Length));
		}
		else
		{
			ThrowArgumentException(System.SR.Format(System.SR.ValueTooLarge, value.Length));
		}
	}

	[DoesNotReturn]
	public static void ThrowArgumentException(ReadOnlySpan<byte> propertyName, ReadOnlySpan<char> value)
	{
		if (propertyName.Length > 166666666)
		{
			ThrowArgumentException(System.SR.Format(System.SR.PropertyNameTooLarge, propertyName.Length));
		}
		else
		{
			ThrowArgumentException(System.SR.Format(System.SR.ValueTooLarge, value.Length));
		}
	}

	[DoesNotReturn]
	public static void ThrowArgumentException(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> value)
	{
		if (propertyName.Length > 166666666)
		{
			ThrowArgumentException(System.SR.Format(System.SR.PropertyNameTooLarge, propertyName.Length));
		}
		else
		{
			ThrowArgumentException(System.SR.Format(System.SR.ValueTooLarge, value.Length));
		}
	}

	[DoesNotReturn]
	public static void ThrowArgumentException(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> value)
	{
		if (propertyName.Length > 166666666)
		{
			ThrowArgumentException(System.SR.Format(System.SR.PropertyNameTooLarge, propertyName.Length));
		}
		else
		{
			ThrowArgumentException(System.SR.Format(System.SR.ValueTooLarge, value.Length));
		}
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationOrArgumentException(ReadOnlySpan<byte> propertyName, int currentDepth)
	{
		currentDepth &= 0x7FFFFFFF;
		if (currentDepth >= 1000)
		{
			ThrowInvalidOperationException(System.SR.Format(System.SR.DepthTooLarge, currentDepth, 1000));
		}
		else
		{
			ThrowArgumentException(System.SR.Format(System.SR.PropertyNameTooLarge, propertyName.Length));
		}
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException(int currentDepth)
	{
		currentDepth &= 0x7FFFFFFF;
		ThrowInvalidOperationException(System.SR.Format(System.SR.DepthTooLarge, currentDepth, 1000));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException(string message)
	{
		throw GetInvalidOperationException(message);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static InvalidOperationException GetInvalidOperationException(string message)
	{
		InvalidOperationException ex = new InvalidOperationException(message);
		ex.Source = "System.Text.Json.Rethrowable";
		return ex;
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationOrArgumentException(ReadOnlySpan<char> propertyName, int currentDepth)
	{
		currentDepth &= 0x7FFFFFFF;
		if (currentDepth >= 1000)
		{
			ThrowInvalidOperationException(System.SR.Format(System.SR.DepthTooLarge, currentDepth, 1000));
		}
		else
		{
			ThrowArgumentException(System.SR.Format(System.SR.PropertyNameTooLarge, propertyName.Length));
		}
	}

	public static InvalidOperationException GetInvalidOperationException_ExpectedArray(JsonTokenType tokenType)
	{
		return GetInvalidOperationException("array", tokenType);
	}

	public static InvalidOperationException GetInvalidOperationException_ExpectedObject(JsonTokenType tokenType)
	{
		return GetInvalidOperationException("object", tokenType);
	}

	public static InvalidOperationException GetInvalidOperationException_ExpectedNumber(JsonTokenType tokenType)
	{
		return GetInvalidOperationException("number", tokenType);
	}

	public static InvalidOperationException GetInvalidOperationException_ExpectedBoolean(JsonTokenType tokenType)
	{
		return GetInvalidOperationException("boolean", tokenType);
	}

	public static InvalidOperationException GetInvalidOperationException_ExpectedString(JsonTokenType tokenType)
	{
		return GetInvalidOperationException("string", tokenType);
	}

	public static InvalidOperationException GetInvalidOperationException_ExpectedStringComparison(JsonTokenType tokenType)
	{
		return GetInvalidOperationException(tokenType);
	}

	public static InvalidOperationException GetInvalidOperationException_ExpectedComment(JsonTokenType tokenType)
	{
		return GetInvalidOperationException("comment", tokenType);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static InvalidOperationException GetInvalidOperationException_CannotSkipOnPartial()
	{
		return GetInvalidOperationException(System.SR.CannotSkip);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static InvalidOperationException GetInvalidOperationException(string message, JsonTokenType tokenType)
	{
		return GetInvalidOperationException(System.SR.Format(System.SR.InvalidCast, tokenType, message));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static InvalidOperationException GetInvalidOperationException(JsonTokenType tokenType)
	{
		return GetInvalidOperationException(System.SR.Format(System.SR.InvalidComparison, tokenType));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static InvalidOperationException GetJsonElementWrongTypeException(JsonTokenType expectedType, JsonTokenType actualType)
	{
		return GetJsonElementWrongTypeException(expectedType.ToValueKind(), actualType.ToValueKind());
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static InvalidOperationException GetJsonElementWrongTypeException(string expectedTypeName, JsonTokenType actualType)
	{
		return GetJsonElementWrongTypeException(expectedTypeName, actualType.ToValueKind());
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static InvalidOperationException GetJsonElementWrongTypeException(JsonValueKind expectedType, JsonValueKind actualType)
	{
		return GetInvalidOperationException(System.SR.Format(System.SR.JsonElementHasWrongType, expectedType, actualType));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static InvalidOperationException GetJsonElementWrongTypeException(string expectedTypeName, JsonValueKind actualType)
	{
		return GetInvalidOperationException(System.SR.Format(System.SR.JsonElementHasWrongType, expectedTypeName, actualType));
	}

	[DoesNotReturn]
	public static void ThrowJsonReaderException(ref Utf8JsonReader json, ExceptionResource resource, byte nextByte = 0, ReadOnlySpan<byte> bytes = default(ReadOnlySpan<byte>))
	{
		throw GetJsonReaderException(ref json, resource, nextByte, bytes);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static JsonException GetJsonReaderException(ref Utf8JsonReader json, ExceptionResource resource, byte nextByte, ReadOnlySpan<byte> bytes)
	{
		string resourceString = GetResourceString(ref json, resource, nextByte, JsonHelpers.Utf8GetString(bytes));
		long lineNumber = json.CurrentState._lineNumber;
		long bytePositionInLine = json.CurrentState._bytePositionInLine;
		resourceString += $" LineNumber: {lineNumber} | BytePositionInLine: {bytePositionInLine}.";
		return new JsonReaderException(resourceString, lineNumber, bytePositionInLine);
	}

	private static bool IsPrintable(byte value)
	{
		if (value >= 32)
		{
			return value < 127;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static string GetPrintableString(byte value)
	{
		if (!IsPrintable(value))
		{
			return $"0x{value:X2}";
		}
		char c = (char)value;
		return c.ToString();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string GetResourceString(ref Utf8JsonReader json, ExceptionResource resource, byte nextByte, string characters)
	{
		string printableString = GetPrintableString(nextByte);
		string result = "";
		switch (resource)
		{
		case ExceptionResource.ArrayDepthTooLarge:
			result = System.SR.Format(System.SR.ArrayDepthTooLarge, json.CurrentState.Options.MaxDepth);
			break;
		case ExceptionResource.MismatchedObjectArray:
			result = System.SR.Format(System.SR.MismatchedObjectArray, printableString);
			break;
		case ExceptionResource.TrailingCommaNotAllowedBeforeArrayEnd:
			result = System.SR.TrailingCommaNotAllowedBeforeArrayEnd;
			break;
		case ExceptionResource.TrailingCommaNotAllowedBeforeObjectEnd:
			result = System.SR.TrailingCommaNotAllowedBeforeObjectEnd;
			break;
		case ExceptionResource.EndOfStringNotFound:
			result = System.SR.EndOfStringNotFound;
			break;
		case ExceptionResource.RequiredDigitNotFoundAfterSign:
			result = System.SR.Format(System.SR.RequiredDigitNotFoundAfterSign, printableString);
			break;
		case ExceptionResource.RequiredDigitNotFoundAfterDecimal:
			result = System.SR.Format(System.SR.RequiredDigitNotFoundAfterDecimal, printableString);
			break;
		case ExceptionResource.RequiredDigitNotFoundEndOfData:
			result = System.SR.RequiredDigitNotFoundEndOfData;
			break;
		case ExceptionResource.ExpectedEndAfterSingleJson:
			result = System.SR.Format(System.SR.ExpectedEndAfterSingleJson, printableString);
			break;
		case ExceptionResource.ExpectedEndOfDigitNotFound:
			result = System.SR.Format(System.SR.ExpectedEndOfDigitNotFound, printableString);
			break;
		case ExceptionResource.ExpectedNextDigitEValueNotFound:
			result = System.SR.Format(System.SR.ExpectedNextDigitEValueNotFound, printableString);
			break;
		case ExceptionResource.ExpectedSeparatorAfterPropertyNameNotFound:
			result = System.SR.Format(System.SR.ExpectedSeparatorAfterPropertyNameNotFound, printableString);
			break;
		case ExceptionResource.ExpectedStartOfPropertyNotFound:
			result = System.SR.Format(System.SR.ExpectedStartOfPropertyNotFound, printableString);
			break;
		case ExceptionResource.ExpectedStartOfPropertyOrValueNotFound:
			result = System.SR.ExpectedStartOfPropertyOrValueNotFound;
			break;
		case ExceptionResource.ExpectedStartOfPropertyOrValueAfterComment:
			result = System.SR.Format(System.SR.ExpectedStartOfPropertyOrValueAfterComment, printableString);
			break;
		case ExceptionResource.ExpectedStartOfValueNotFound:
			result = System.SR.Format(System.SR.ExpectedStartOfValueNotFound, printableString);
			break;
		case ExceptionResource.ExpectedValueAfterPropertyNameNotFound:
			result = System.SR.ExpectedValueAfterPropertyNameNotFound;
			break;
		case ExceptionResource.FoundInvalidCharacter:
			result = System.SR.Format(System.SR.FoundInvalidCharacter, printableString);
			break;
		case ExceptionResource.InvalidEndOfJsonNonPrimitive:
			result = System.SR.Format(System.SR.InvalidEndOfJsonNonPrimitive, json.TokenType);
			break;
		case ExceptionResource.ObjectDepthTooLarge:
			result = System.SR.Format(System.SR.ObjectDepthTooLarge, json.CurrentState.Options.MaxDepth);
			break;
		case ExceptionResource.ExpectedFalse:
			result = System.SR.Format(System.SR.ExpectedFalse, characters);
			break;
		case ExceptionResource.ExpectedNull:
			result = System.SR.Format(System.SR.ExpectedNull, characters);
			break;
		case ExceptionResource.ExpectedTrue:
			result = System.SR.Format(System.SR.ExpectedTrue, characters);
			break;
		case ExceptionResource.InvalidCharacterWithinString:
			result = System.SR.Format(System.SR.InvalidCharacterWithinString, printableString);
			break;
		case ExceptionResource.InvalidCharacterAfterEscapeWithinString:
			result = System.SR.Format(System.SR.InvalidCharacterAfterEscapeWithinString, printableString);
			break;
		case ExceptionResource.InvalidHexCharacterWithinString:
			result = System.SR.Format(System.SR.InvalidHexCharacterWithinString, printableString);
			break;
		case ExceptionResource.EndOfCommentNotFound:
			result = System.SR.EndOfCommentNotFound;
			break;
		case ExceptionResource.ZeroDepthAtEnd:
			result = System.SR.Format(System.SR.ZeroDepthAtEnd);
			break;
		case ExceptionResource.ExpectedJsonTokens:
			result = System.SR.ExpectedJsonTokens;
			break;
		case ExceptionResource.NotEnoughData:
			result = System.SR.NotEnoughData;
			break;
		case ExceptionResource.ExpectedOneCompleteToken:
			result = System.SR.ExpectedOneCompleteToken;
			break;
		case ExceptionResource.InvalidCharacterAtStartOfComment:
			result = System.SR.Format(System.SR.InvalidCharacterAtStartOfComment, printableString);
			break;
		case ExceptionResource.UnexpectedEndOfDataWhileReadingComment:
			result = System.SR.Format(System.SR.UnexpectedEndOfDataWhileReadingComment);
			break;
		case ExceptionResource.UnexpectedEndOfLineSeparator:
			result = System.SR.Format(System.SR.UnexpectedEndOfLineSeparator);
			break;
		case ExceptionResource.InvalidLeadingZeroInNumber:
			result = System.SR.Format(System.SR.InvalidLeadingZeroInNumber, printableString);
			break;
		}
		return result;
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException(ExceptionResource resource, int currentDepth, byte token, JsonTokenType tokenType)
	{
		throw GetInvalidOperationException(resource, currentDepth, token, tokenType);
	}

	[DoesNotReturn]
	public static void ThrowArgumentException_InvalidCommentValue()
	{
		throw new ArgumentException(System.SR.CannotWriteCommentWithEmbeddedDelimiter);
	}

	[DoesNotReturn]
	public static void ThrowArgumentException_InvalidUTF8(ReadOnlySpan<byte> value)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = Math.Min(value.Length, 10);
		for (int i = 0; i < num; i++)
		{
			byte value2 = value[i];
			if (IsPrintable(value2))
			{
				stringBuilder.Append((char)value2);
				continue;
			}
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(2, 1, stringBuilder2);
			handler.AppendLiteral("0x");
			handler.AppendFormatted(value2, "X2");
			stringBuilder2.Append(ref handler);
		}
		if (num < value.Length)
		{
			stringBuilder.Append("...");
		}
		throw new ArgumentException(System.SR.Format(System.SR.CannotEncodeInvalidUTF8, stringBuilder));
	}

	[DoesNotReturn]
	public static void ThrowArgumentException_InvalidUTF16(int charAsInt)
	{
		throw new ArgumentException(System.SR.Format(System.SR.CannotEncodeInvalidUTF16, $"0x{charAsInt:X2}"));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ReadInvalidUTF16(int charAsInt)
	{
		throw GetInvalidOperationException(System.SR.Format(System.SR.CannotReadInvalidUTF16, $"0x{charAsInt:X2}"));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ReadInvalidUTF16()
	{
		throw GetInvalidOperationException(System.SR.CannotReadIncompleteUTF16);
	}

	public static InvalidOperationException GetInvalidOperationException_ReadInvalidUTF8(DecoderFallbackException innerException)
	{
		return GetInvalidOperationException(System.SR.CannotTranscodeInvalidUtf8, innerException);
	}

	public static ArgumentException GetArgumentException_ReadInvalidUTF16(EncoderFallbackException innerException)
	{
		return new ArgumentException(System.SR.CannotTranscodeInvalidUtf16, innerException);
	}

	public static InvalidOperationException GetInvalidOperationException(string message, Exception innerException)
	{
		InvalidOperationException ex = new InvalidOperationException(message, innerException);
		ex.Source = "System.Text.Json.Rethrowable";
		return ex;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static InvalidOperationException GetInvalidOperationException(ExceptionResource resource, int currentDepth, byte token, JsonTokenType tokenType)
	{
		string resourceString = GetResourceString(resource, currentDepth, token, tokenType);
		InvalidOperationException invalidOperationException = GetInvalidOperationException(resourceString);
		invalidOperationException.Source = "System.Text.Json.Rethrowable";
		return invalidOperationException;
	}

	[DoesNotReturn]
	public static void ThrowOutOfMemoryException(uint capacity)
	{
		throw new OutOfMemoryException(System.SR.Format(System.SR.BufferMaximumSizeExceeded, capacity));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string GetResourceString(ExceptionResource resource, int currentDepth, byte token, JsonTokenType tokenType)
	{
		string result = "";
		switch (resource)
		{
		case ExceptionResource.MismatchedObjectArray:
			result = ((tokenType == JsonTokenType.PropertyName) ? System.SR.Format(System.SR.CannotWriteEndAfterProperty, (char)token) : System.SR.Format(System.SR.MismatchedObjectArray, (char)token));
			break;
		case ExceptionResource.DepthTooLarge:
			result = System.SR.Format(System.SR.DepthTooLarge, currentDepth & 0x7FFFFFFF, 1000);
			break;
		case ExceptionResource.CannotStartObjectArrayWithoutProperty:
			result = System.SR.Format(System.SR.CannotStartObjectArrayWithoutProperty, tokenType);
			break;
		case ExceptionResource.CannotStartObjectArrayAfterPrimitiveOrClose:
			result = System.SR.Format(System.SR.CannotStartObjectArrayAfterPrimitiveOrClose, tokenType);
			break;
		case ExceptionResource.CannotWriteValueWithinObject:
			result = System.SR.Format(System.SR.CannotWriteValueWithinObject, tokenType);
			break;
		case ExceptionResource.CannotWritePropertyWithinArray:
			result = ((tokenType == JsonTokenType.PropertyName) ? System.SR.Format(System.SR.CannotWritePropertyAfterProperty) : System.SR.Format(System.SR.CannotWritePropertyWithinArray, tokenType));
			break;
		case ExceptionResource.CannotWriteValueAfterPrimitiveOrClose:
			result = System.SR.Format(System.SR.CannotWriteValueAfterPrimitiveOrClose, tokenType);
			break;
		}
		return result;
	}

	public static FormatException GetFormatException()
	{
		FormatException ex = new FormatException();
		ex.Source = "System.Text.Json.Rethrowable";
		return ex;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static FormatException GetFormatException(NumericType numericType)
	{
		string message = "";
		switch (numericType)
		{
		case NumericType.Byte:
			message = System.SR.FormatByte;
			break;
		case NumericType.SByte:
			message = System.SR.FormatSByte;
			break;
		case NumericType.Int16:
			message = System.SR.FormatInt16;
			break;
		case NumericType.Int32:
			message = System.SR.FormatInt32;
			break;
		case NumericType.Int64:
			message = System.SR.FormatInt64;
			break;
		case NumericType.UInt16:
			message = System.SR.FormatUInt16;
			break;
		case NumericType.UInt32:
			message = System.SR.FormatUInt32;
			break;
		case NumericType.UInt64:
			message = System.SR.FormatUInt64;
			break;
		case NumericType.Single:
			message = System.SR.FormatSingle;
			break;
		case NumericType.Double:
			message = System.SR.FormatDouble;
			break;
		case NumericType.Decimal:
			message = System.SR.FormatDecimal;
			break;
		}
		FormatException ex = new FormatException(message);
		ex.Source = "System.Text.Json.Rethrowable";
		return ex;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static FormatException GetFormatException(DataType dateType)
	{
		string message = "";
		switch (dateType)
		{
		case DataType.Boolean:
			message = System.SR.FormatBoolean;
			break;
		case DataType.DateTime:
			message = System.SR.FormatDateTime;
			break;
		case DataType.DateTimeOffset:
			message = System.SR.FormatDateTimeOffset;
			break;
		case DataType.TimeSpan:
			message = System.SR.FormatTimeSpan;
			break;
		case DataType.Base64String:
			message = System.SR.CannotDecodeInvalidBase64;
			break;
		case DataType.Guid:
			message = System.SR.FormatGuid;
			break;
		}
		FormatException ex = new FormatException(message);
		ex.Source = "System.Text.Json.Rethrowable";
		return ex;
	}

	public static InvalidOperationException GetInvalidOperationException_ExpectedChar(JsonTokenType tokenType)
	{
		return GetInvalidOperationException("char", tokenType);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowArgumentException_NodeValueNotAllowed(string paramName)
	{
		throw new ArgumentException(System.SR.NodeValueNotAllowed, paramName);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowArgumentException_NodeArrayTooSmall(string paramName)
	{
		throw new ArgumentException(System.SR.NodeArrayTooSmall, paramName);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowArgumentOutOfRangeException_NodeArrayIndexNegative(string paramName)
	{
		throw new ArgumentOutOfRangeException(paramName, System.SR.NodeArrayIndexNegative);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowArgumentException_DuplicateKey(string propertyName)
	{
		throw new ArgumentException(System.SR.NodeDuplicateKey, propertyName);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_NodeAlreadyHasParent()
	{
		throw new InvalidOperationException(System.SR.NodeAlreadyHasParent);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_NodeCycleDetected()
	{
		throw new InvalidOperationException(System.SR.NodeCycleDetected);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowNotSupportedException_NodeCollectionIsReadOnly()
	{
		throw NotSupportedException_NodeCollectionIsReadOnly();
	}

	public static NotSupportedException NotSupportedException_NodeCollectionIsReadOnly()
	{
		return new NotSupportedException(System.SR.NodeCollectionIsReadOnly);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowArgumentException_DeserializeWrongType(Type type, object value)
	{
		throw new ArgumentException(System.SR.Format(System.SR.DeserializeWrongType, type, value.GetType()));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowNotSupportedException_SerializationNotSupported(Type propertyType)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.SerializationNotSupportedType, propertyType));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowNotSupportedException_TypeRequiresAsyncSerialization(Type propertyType)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.TypeRequiresAsyncSerialization, propertyType));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowNotSupportedException_ConstructorMaxOf64Parameters(Type type)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.ConstructorMaxOf64Parameters, type));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowNotSupportedException_DictionaryKeyTypeNotSupported(Type keyType, JsonConverter converter)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.DictionaryKeyTypeNotSupported, keyType, converter.GetType()));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowJsonException_DeserializeUnableToConvertValue(Type propertyType)
	{
		throw new JsonException(System.SR.Format(System.SR.DeserializeUnableToConvertValue, propertyType))
		{
			AppendPathInformation = true
		};
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidCastException_DeserializeUnableToAssignValue(Type typeOfValue, Type declaredType)
	{
		throw new InvalidCastException(System.SR.Format(System.SR.DeserializeUnableToAssignValue, typeOfValue, declaredType));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_DeserializeUnableToAssignNull(Type declaredType)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.DeserializeUnableToAssignNull, declaredType));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowJsonException_SerializationConverterRead(JsonConverter converter)
	{
		throw new JsonException(System.SR.Format(System.SR.SerializationConverterRead, converter))
		{
			AppendPathInformation = true
		};
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowJsonException_SerializationConverterWrite(JsonConverter converter)
	{
		throw new JsonException(System.SR.Format(System.SR.SerializationConverterWrite, converter))
		{
			AppendPathInformation = true
		};
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowJsonException_SerializerCycleDetected(int maxDepth)
	{
		throw new JsonException(System.SR.Format(System.SR.SerializerCycleDetected, maxDepth))
		{
			AppendPathInformation = true
		};
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowJsonException(string message = null)
	{
		throw new JsonException(message)
		{
			AppendPathInformation = true
		};
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_CannotSerializeInvalidType(Type type, Type parentClassType, MemberInfo memberInfo)
	{
		if (parentClassType == null)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.CannotSerializeInvalidType, type));
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.CannotSerializeInvalidMember, type, memberInfo.Name, parentClassType));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializationConverterNotCompatible(Type converterType, Type type)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.SerializationConverterNotCompatible, converterType, type));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializationConverterOnAttributeInvalid(Type classType, MemberInfo memberInfo)
	{
		string text = classType.ToString();
		if (memberInfo != null)
		{
			text = text + "." + memberInfo.Name;
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.SerializationConverterOnAttributeInvalid, text));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializationConverterOnAttributeNotCompatible(Type classTypeAttributeIsOn, MemberInfo memberInfo, Type typeToConvert)
	{
		string text = classTypeAttributeIsOn.ToString();
		if (memberInfo != null)
		{
			text = text + "." + memberInfo.Name;
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.SerializationConverterOnAttributeNotCompatible, text, typeToConvert));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializerOptionsImmutable(JsonSerializerContext context)
	{
		string message = ((context == null) ? System.SR.SerializerOptionsImmutable : System.SR.SerializerContextOptionsImmutable);
		throw new InvalidOperationException(message);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializerPropertyNameConflict(Type type, JsonPropertyInfo jsonPropertyInfo)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.SerializerPropertyNameConflict, type, jsonPropertyInfo.ClrName));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializerPropertyNameNull(Type parentType, JsonPropertyInfo jsonPropertyInfo)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.SerializerPropertyNameNull, parentType, jsonPropertyInfo.MemberInfo?.Name));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_NamingPolicyReturnNull(JsonNamingPolicy namingPolicy)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.NamingPolicyReturnNull, namingPolicy));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializerConverterFactoryReturnsNull(Type converterType)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.SerializerConverterFactoryReturnsNull, converterType));
	}

	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializerConverterFactoryReturnsJsonConverterFactorty(Type converterType)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.SerializerConverterFactoryReturnsJsonConverterFactory, converterType));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_MultiplePropertiesBindToConstructorParameters(Type parentType, string parameterName, string firstMatchName, string secondMatchName)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.MultipleMembersBindWithConstructorParameter, firstMatchName, secondMatchName, parentType, parameterName));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ConstructorParameterIncompleteBinding(Type parentType)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.ConstructorParamIncompleteBinding, parentType));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ExtensionDataCannotBindToCtorParam(JsonPropertyInfo jsonPropertyInfo)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.ExtensionDataCannotBindToCtorParam, jsonPropertyInfo.ClrName, jsonPropertyInfo.DeclaringType));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_JsonIncludeOnNonPublicInvalid(string memberName, Type declaringType)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.JsonIncludeOnNonPublicInvalid, memberName, declaringType));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_IgnoreConditionOnValueTypeInvalid(string clrPropertyName, Type propertyDeclaringType)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.IgnoreConditionOnValueTypeInvalid, clrPropertyName, propertyDeclaringType));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_NumberHandlingOnPropertyInvalid(JsonPropertyInfo jsonPropertyInfo)
	{
		MemberInfo memberInfo = jsonPropertyInfo.MemberInfo;
		throw new InvalidOperationException(System.SR.Format(System.SR.NumberHandlingOnPropertyInvalid, memberInfo.Name, memberInfo.DeclaringType));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_ConverterCanConvertMultipleTypes(Type runtimePropertyType, JsonConverter jsonConverter)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.ConverterCanConvertMultipleTypes, jsonConverter.GetType(), jsonConverter.TypeToConvert, runtimePropertyType));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowNotSupportedException_ObjectWithParameterizedCtorRefMetadataNotHonored(ReadOnlySpan<byte> propertyName, ref Utf8JsonReader reader, ref ReadStack state)
	{
		state.Current.JsonPropertyName = propertyName.ToArray();
		NotSupportedException ex = new NotSupportedException(System.SR.Format(System.SR.ObjectWithParameterizedCtorRefMetadataNotHonored, state.Current.JsonTypeInfo.Type));
		ThrowNotSupportedException(ref state, in reader, ex);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ReThrowWithPath(ref ReadStack state, JsonReaderException ex)
	{
		string text = state.JsonPath();
		string message = ex.Message;
		int num = message.AsSpan().LastIndexOf(" LineNumber: ");
		message = ((num < 0) ? (message + " Path: " + text + ".") : $"{message.Substring(0, num)} Path: {text} |{message.Substring(num)}");
		throw new JsonException(message, text, ex.LineNumber, ex.BytePositionInLine, ex);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ReThrowWithPath(ref ReadStack state, in Utf8JsonReader reader, Exception ex)
	{
		JsonException ex2 = new JsonException(null, ex);
		AddJsonExceptionInformation(ref state, in reader, ex2);
		throw ex2;
	}

	public static void AddJsonExceptionInformation(ref ReadStack state, in Utf8JsonReader reader, JsonException ex)
	{
		long lineNumber = reader.CurrentState._lineNumber;
		ex.LineNumber = lineNumber;
		long bytePositionInLine = reader.CurrentState._bytePositionInLine;
		ex.BytePositionInLine = bytePositionInLine;
		string value = (ex.Path = state.JsonPath());
		string text2 = ex._message;
		if (string.IsNullOrEmpty(text2))
		{
			Type type = state.Current.JsonPropertyInfo?.RuntimePropertyType;
			if (type == null)
			{
				type = state.Current.JsonTypeInfo?.Type;
			}
			text2 = System.SR.Format(System.SR.DeserializeUnableToConvertValue, type);
			ex.AppendPathInformation = true;
		}
		if (ex.AppendPathInformation)
		{
			text2 += $" Path: {value} | LineNumber: {lineNumber} | BytePositionInLine: {bytePositionInLine}.";
			ex.SetMessage(text2);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ReThrowWithPath(ref WriteStack state, Exception ex)
	{
		JsonException ex2 = new JsonException(null, ex);
		AddJsonExceptionInformation(ref state, ex2);
		throw ex2;
	}

	public static void AddJsonExceptionInformation(ref WriteStack state, JsonException ex)
	{
		string text2 = (ex.Path = state.PropertyPath());
		string text3 = ex._message;
		if (string.IsNullOrEmpty(text3))
		{
			text3 = System.SR.Format(System.SR.SerializeUnableToSerialize);
			ex.AppendPathInformation = true;
		}
		if (ex.AppendPathInformation)
		{
			text3 = text3 + " Path: " + text2 + ".";
			ex.SetMessage(text3);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializationDuplicateAttribute(Type attribute, Type classType, MemberInfo memberInfo)
	{
		string text = classType.ToString();
		if (memberInfo != null)
		{
			text = text + "." + memberInfo.Name;
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.SerializationDuplicateAttribute, attribute, text));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializationDuplicateTypeAttribute(Type classType, Type attribute)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.SerializationDuplicateTypeAttribute, classType, attribute));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializationDuplicateTypeAttribute<TAttribute>(Type classType)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.SerializationDuplicateTypeAttribute, classType, typeof(TAttribute)));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_SerializationDataExtensionPropertyInvalid(Type type, JsonPropertyInfo jsonPropertyInfo)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.SerializationDataExtensionPropertyInvalid, type, jsonPropertyInfo.MemberInfo?.Name));
	}

	[DoesNotReturn]
	public static void ThrowNotSupportedException(ref ReadStack state, in Utf8JsonReader reader, NotSupportedException ex)
	{
		string text = ex.Message;
		Type type = state.Current.JsonPropertyInfo?.RuntimePropertyType;
		if (type == null)
		{
			type = state.Current.JsonTypeInfo.Type;
		}
		if (!text.Contains(type.ToString()))
		{
			if (text.Length > 0)
			{
				text += " ";
			}
			text += System.SR.Format(System.SR.SerializationNotSupportedParentType, type);
		}
		long lineNumber = reader.CurrentState._lineNumber;
		long bytePositionInLine = reader.CurrentState._bytePositionInLine;
		text += $" Path: {state.JsonPath()} | LineNumber: {lineNumber} | BytePositionInLine: {bytePositionInLine}.";
		throw new NotSupportedException(text, ex);
	}

	[DoesNotReturn]
	public static void ThrowNotSupportedException(ref WriteStack state, NotSupportedException ex)
	{
		string text = ex.Message;
		Type type = state.Current.DeclaredJsonPropertyInfo?.RuntimePropertyType;
		if (type == null)
		{
			type = state.Current.JsonTypeInfo.Type;
		}
		if (!text.Contains(type.ToString()))
		{
			if (text.Length > 0)
			{
				text += " ";
			}
			text += System.SR.Format(System.SR.SerializationNotSupportedParentType, type);
		}
		text = text + " Path: " + state.PropertyPath() + ".";
		throw new NotSupportedException(text, ex);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowNotSupportedException_DeserializeNoConstructor(Type type, ref Utf8JsonReader reader, ref ReadStack state)
	{
		string message = ((!type.IsInterface) ? System.SR.Format(System.SR.DeserializeNoConstructor, "JsonConstructorAttribute", type) : System.SR.Format(System.SR.DeserializePolymorphicInterface, type));
		ThrowNotSupportedException(ref state, in reader, new NotSupportedException(message));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowNotSupportedException_CannotPopulateCollection(Type type, ref Utf8JsonReader reader, ref ReadStack state)
	{
		ThrowNotSupportedException(ref state, in reader, new NotSupportedException(System.SR.Format(System.SR.CannotPopulateCollection, type)));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowJsonException_MetadataValuesInvalidToken(JsonTokenType tokenType)
	{
		ThrowJsonException(System.SR.Format(System.SR.MetadataInvalidTokenAfterValues, tokenType));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowJsonException_MetadataReferenceNotFound(string id)
	{
		ThrowJsonException(System.SR.Format(System.SR.MetadataReferenceNotFound, id));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowJsonException_MetadataValueWasNotString(JsonTokenType tokenType)
	{
		ThrowJsonException(System.SR.Format(System.SR.MetadataValueWasNotString, tokenType));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowJsonException_MetadataValueWasNotString(JsonValueKind valueKind)
	{
		ThrowJsonException(System.SR.Format(System.SR.MetadataValueWasNotString, valueKind));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties(ReadOnlySpan<byte> propertyName, ref ReadStack state)
	{
		state.Current.JsonPropertyName = propertyName.ToArray();
		ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties()
	{
		ThrowJsonException(System.SR.MetadataReferenceCannotContainOtherProperties);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowJsonException_MetadataIdIsNotFirstProperty(ReadOnlySpan<byte> propertyName, ref ReadStack state)
	{
		state.Current.JsonPropertyName = propertyName.ToArray();
		ThrowJsonException(System.SR.MetadataIdIsNotFirstProperty);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowJsonException_MetadataMissingIdBeforeValues(ref ReadStack state, ReadOnlySpan<byte> propertyName)
	{
		state.Current.JsonPropertyName = propertyName.ToArray();
		ThrowJsonException(System.SR.MetadataPreservedArrayPropertyNotFound);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowJsonException_MetadataInvalidPropertyWithLeadingDollarSign(ReadOnlySpan<byte> propertyName, ref ReadStack state, in Utf8JsonReader reader)
	{
		if (state.Current.IsProcessingDictionary())
		{
			state.Current.JsonPropertyNameAsString = reader.GetString();
		}
		else
		{
			state.Current.JsonPropertyName = propertyName.ToArray();
		}
		ThrowJsonException(System.SR.MetadataInvalidPropertyWithLeadingDollarSign);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowJsonException_MetadataDuplicateIdFound(string id)
	{
		ThrowJsonException(System.SR.Format(System.SR.MetadataDuplicateIdFound, id));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowJsonException_MetadataInvalidReferenceToValueType(Type propertyType)
	{
		ThrowJsonException(System.SR.Format(System.SR.MetadataInvalidReferenceToValueType, propertyType));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowJsonException_MetadataPreservedArrayInvalidProperty(ref ReadStack state, Type propertyType, in Utf8JsonReader reader)
	{
		ref ReadStackFrame current = ref state.Current;
		byte[] jsonPropertyName;
		if (!reader.HasValueSequence)
		{
			jsonPropertyName = reader.ValueSpan.ToArray();
		}
		else
		{
			ReadOnlySequence<byte> sequence = reader.ValueSequence;
			jsonPropertyName = BuffersExtensions.ToArray(in sequence);
		}
		current.JsonPropertyName = jsonPropertyName;
		string @string = reader.GetString();
		ThrowJsonException(System.SR.Format(System.SR.MetadataPreservedArrayFailed, System.SR.Format(System.SR.MetadataPreservedArrayInvalidProperty, @string), System.SR.Format(System.SR.DeserializeUnableToConvertValue, propertyType)));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowJsonException_MetadataPreservedArrayValuesNotFound(ref ReadStack state, Type propertyType)
	{
		state.Current.JsonPropertyName = null;
		ThrowJsonException(System.SR.Format(System.SR.MetadataPreservedArrayFailed, System.SR.MetadataPreservedArrayPropertyNotFound, System.SR.Format(System.SR.DeserializeUnableToConvertValue, propertyType)));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowJsonException_MetadataCannotParsePreservedObjectIntoImmutable(Type propertyType)
	{
		ThrowJsonException(System.SR.Format(System.SR.MetadataCannotParsePreservedObjectToImmutable, propertyType));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_MetadataReferenceOfTypeCannotBeAssignedToType(string referenceId, Type currentType, Type typeToConvert)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.MetadataReferenceOfTypeCannotBeAssignedToType, referenceId, currentType, typeToConvert));
	}

	[DoesNotReturn]
	internal static void ThrowUnexpectedMetadataException(ReadOnlySpan<byte> propertyName, ref Utf8JsonReader reader, ref ReadStack state)
	{
		if (state.Current.JsonTypeInfo.PropertyInfoForTypeInfo.ConverterBase.ConstructorIsParameterized)
		{
			ThrowNotSupportedException_ObjectWithParameterizedCtorRefMetadataNotHonored(propertyName, ref reader, ref state);
		}
		switch (JsonSerializer.GetMetadataPropertyName(propertyName))
		{
		case MetadataPropertyName.Id:
			ThrowJsonException_MetadataIdIsNotFirstProperty(propertyName, ref state);
			break;
		case MetadataPropertyName.Ref:
			ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties(propertyName, ref state);
			break;
		default:
			ThrowJsonException_MetadataInvalidPropertyWithLeadingDollarSign(propertyName, ref state, in reader);
			break;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_JsonSerializerOptionsAlreadyBoundToContext()
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.OptionsAlreadyBoundToContext));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowNotSupportedException_BuiltInConvertersNotRooted(Type type)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.BuiltInConvertersNotRooted, type));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowNotSupportedException_NoMetadataForType(Type type)
	{
		throw new NotSupportedException(System.SR.Format(System.SR.NoMetadataForType, type));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowInvalidOperationException_NoMetadataForType(Type type)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.NoMetadataForType, type));
	}

	public static void ThrowInvalidOperationException_NoMetadataForTypeProperties(JsonSerializerContext context, Type type)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.NoMetadataForTypeProperties, context.GetType(), type));
	}

	public static void ThrowInvalidOperationException_NoMetadataForTypeCtorParams(JsonSerializerContext context, Type type)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.NoMetadataForTypeCtorParams, context.GetType(), type));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	public static void ThrowMissingMemberException_MissingFSharpCoreMember(string missingFsharpCoreMember)
	{
		throw new MissingMemberException(System.SR.Format(System.SR.MissingFSharpCoreMember, missingFsharpCoreMember));
	}
}
