using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

namespace System.Reflection;

internal static class Throw
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void InvalidCast()
	{
		throw new InvalidCastException();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void InvalidArgument(string message, string parameterName)
	{
		throw new ArgumentException(message, parameterName);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void InvalidArgument_OffsetForVirtualHeapHandle()
	{
		throw new ArgumentException(System.SR.CantGetOffsetForVirtualHeapHandle, "handle");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static Exception InvalidArgument_UnexpectedHandleKind(HandleKind kind)
	{
		throw new ArgumentException(System.SR.Format(System.SR.UnexpectedHandleKind, kind));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static Exception InvalidArgument_Handle(string parameterName)
	{
		throw new ArgumentException(System.SR.InvalidHandle, parameterName);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void SignatureNotVarArg()
	{
		throw new InvalidOperationException(System.SR.SignatureNotVarArg);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void ControlFlowBuilderNotAvailable()
	{
		throw new InvalidOperationException(System.SR.ControlFlowBuilderNotAvailable);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void InvalidOperationBuilderAlreadyLinked()
	{
		throw new InvalidOperationException(System.SR.BuilderAlreadyLinked);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void InvalidOperation(string message)
	{
		throw new InvalidOperationException(message);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void InvalidOperation_LabelNotMarked(int id)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.LabelNotMarked, id));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void LabelDoesntBelongToBuilder(string parameterName)
	{
		throw new ArgumentException(System.SR.LabelDoesntBelongToBuilder, parameterName);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void HeapHandleRequired()
	{
		throw new ArgumentException(System.SR.NotMetadataHeapHandle, "handle");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void EntityOrUserStringHandleRequired()
	{
		throw new ArgumentException(System.SR.NotMetadataTableOrUserStringHandle, "handle");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void InvalidToken()
	{
		throw new ArgumentException(System.SR.InvalidToken, "token");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void ArgumentNull(string parameterName)
	{
		throw new ArgumentNullException(parameterName);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void ArgumentEmptyString(string parameterName)
	{
		throw new ArgumentException(System.SR.ExpectedNonEmptyString, parameterName);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void ArgumentEmptyArray(string parameterName)
	{
		throw new ArgumentException(System.SR.ExpectedNonEmptyArray, parameterName);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void ValueArgumentNull()
	{
		throw new ArgumentNullException("value");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void BuilderArgumentNull()
	{
		throw new ArgumentNullException("builder");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void ArgumentOutOfRange(string parameterName)
	{
		throw new ArgumentOutOfRangeException(parameterName);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void ArgumentOutOfRange(string parameterName, string message)
	{
		throw new ArgumentOutOfRangeException(parameterName, message);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void BlobTooLarge(string parameterName)
	{
		throw new ArgumentOutOfRangeException(parameterName, System.SR.BlobTooLarge);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void IndexOutOfRange()
	{
		throw new ArgumentOutOfRangeException("index");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void TableIndexOutOfRange()
	{
		throw new ArgumentOutOfRangeException("tableIndex");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void ValueArgumentOutOfRange()
	{
		throw new ArgumentOutOfRangeException("value");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void OutOfBounds()
	{
		throw new BadImageFormatException(System.SR.OutOfBoundsRead);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void WriteOutOfBounds()
	{
		throw new InvalidOperationException(System.SR.OutOfBoundsWrite);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void InvalidCodedIndex()
	{
		throw new BadImageFormatException(System.SR.InvalidCodedIndex);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void InvalidHandle()
	{
		throw new BadImageFormatException(System.SR.InvalidHandle);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void InvalidCompressedInteger()
	{
		throw new BadImageFormatException(System.SR.InvalidCompressedInteger);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void InvalidSerializedString()
	{
		throw new BadImageFormatException(System.SR.InvalidSerializedString);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void ImageTooSmall()
	{
		throw new BadImageFormatException(System.SR.ImageTooSmall);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void ImageTooSmallOrContainsInvalidOffsetOrCount()
	{
		throw new BadImageFormatException(System.SR.ImageTooSmallOrContainsInvalidOffsetOrCount);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void ReferenceOverflow()
	{
		throw new BadImageFormatException(System.SR.RowIdOrHeapOffsetTooLarge);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void TableNotSorted(TableIndex tableIndex)
	{
		throw new BadImageFormatException(System.SR.Format(System.SR.MetadataTableNotSorted, tableIndex));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void InvalidOperation_TableNotSorted(TableIndex tableIndex)
	{
		throw new InvalidOperationException(System.SR.Format(System.SR.MetadataTableNotSorted, tableIndex));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void InvalidOperation_PEImageNotAvailable()
	{
		throw new InvalidOperationException(System.SR.PEImageNotAvailable);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void TooManySubnamespaces()
	{
		throw new BadImageFormatException(System.SR.TooManySubnamespaces);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void ValueOverflow()
	{
		throw new BadImageFormatException(System.SR.ValueTooLarge);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void SequencePointValueOutOfRange()
	{
		throw new BadImageFormatException(System.SR.SequencePointValueOutOfRange);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void HeapSizeLimitExceeded(HeapIndex heap)
	{
		throw new ImageFormatLimitationException(System.SR.Format(System.SR.HeapSizeLimitExceeded, heap));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DoesNotReturn]
	internal static void PEReaderDisposed()
	{
		throw new ObjectDisposedException("PEReader");
	}
}
