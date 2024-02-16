using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Reflection.Internal;

[DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
internal readonly struct MemoryBlock
{
	internal enum FastComparisonResult
	{
		Equal,
		BytesStartWithText,
		TextStartsWithBytes,
		Unequal,
		Inconclusive
	}

	internal unsafe readonly byte* Pointer;

	internal readonly int Length;

	internal unsafe MemoryBlock(byte* buffer, int length)
	{
		Pointer = buffer;
		Length = length;
	}

	internal unsafe static MemoryBlock CreateChecked(byte* buffer, int length)
	{
		if (length < 0)
		{
			throw new ArgumentOutOfRangeException("length");
		}
		if (buffer == null && length != 0)
		{
			throw new ArgumentNullException("buffer");
		}
		return new MemoryBlock(buffer, length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void CheckBounds(int offset, int byteCount)
	{
		if ((ulong)((long)(uint)offset + (long)(uint)byteCount) > (ulong)Length)
		{
			Throw.OutOfBounds();
		}
	}

	internal unsafe byte[]? ToArray()
	{
		if (Pointer != null)
		{
			return PeekBytes(0, Length);
		}
		return null;
	}

	private unsafe string GetDebuggerDisplay()
	{
		if (Pointer == null)
		{
			return "<null>";
		}
		int displayedBytes;
		return GetDebuggerDisplay(out displayedBytes);
	}

	internal string GetDebuggerDisplay(out int displayedBytes)
	{
		displayedBytes = Math.Min(Length, 64);
		string text = BitConverter.ToString(PeekBytes(0, displayedBytes));
		if (displayedBytes < Length)
		{
			text += "-...";
		}
		return text;
	}

	internal unsafe string GetDebuggerDisplay(int offset)
	{
		if (Pointer == null)
		{
			return "<null>";
		}
		int displayedBytes;
		string debuggerDisplay = GetDebuggerDisplay(out displayedBytes);
		if (offset < displayedBytes)
		{
			return debuggerDisplay.Insert(offset * 3, "*");
		}
		if (displayedBytes == Length)
		{
			return debuggerDisplay + "*";
		}
		return debuggerDisplay + "*...";
	}

	internal unsafe MemoryBlock GetMemoryBlockAt(int offset, int length)
	{
		CheckBounds(offset, length);
		return new MemoryBlock(Pointer + offset, length);
	}

	internal unsafe byte PeekByte(int offset)
	{
		CheckBounds(offset, 1);
		return Pointer[offset];
	}

	internal int PeekInt32(int offset)
	{
		uint num = PeekUInt32(offset);
		if ((int)num != num)
		{
			Throw.ValueOverflow();
		}
		return (int)num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe uint PeekUInt32(int offset)
	{
		CheckBounds(offset, 4);
		byte* ptr = Pointer + offset;
		return (uint)(*ptr | (ptr[1] << 8) | (ptr[2] << 16) | (ptr[3] << 24));
	}

	internal unsafe int PeekCompressedInteger(int offset, out int numberOfBytesRead)
	{
		CheckBounds(offset, 0);
		byte* ptr = Pointer + offset;
		long num = Length - offset;
		if (num == 0L)
		{
			numberOfBytesRead = 0;
			return int.MaxValue;
		}
		byte b = *ptr;
		if ((b & 0x80) == 0)
		{
			numberOfBytesRead = 1;
			return b;
		}
		if ((b & 0x40) == 0)
		{
			if (num >= 2)
			{
				numberOfBytesRead = 2;
				return ((b & 0x3F) << 8) | ptr[1];
			}
		}
		else if ((b & 0x20) == 0 && num >= 4)
		{
			numberOfBytesRead = 4;
			return ((b & 0x1F) << 24) | (ptr[1] << 16) | (ptr[2] << 8) | ptr[3];
		}
		numberOfBytesRead = 0;
		return int.MaxValue;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe ushort PeekUInt16(int offset)
	{
		CheckBounds(offset, 2);
		byte* ptr = Pointer + offset;
		return (ushort)(*ptr | (ptr[1] << 8));
	}

	internal uint PeekTaggedReference(int offset, bool smallRefSize)
	{
		return PeekReferenceUnchecked(offset, smallRefSize);
	}

	internal uint PeekReferenceUnchecked(int offset, bool smallRefSize)
	{
		if (!smallRefSize)
		{
			return PeekUInt32(offset);
		}
		return PeekUInt16(offset);
	}

	internal int PeekReference(int offset, bool smallRefSize)
	{
		if (smallRefSize)
		{
			return PeekUInt16(offset);
		}
		uint num = PeekUInt32(offset);
		if (!TokenTypeIds.IsValidRowId(num))
		{
			Throw.ReferenceOverflow();
		}
		return (int)num;
	}

	internal int PeekHeapReference(int offset, bool smallRefSize)
	{
		if (smallRefSize)
		{
			return PeekUInt16(offset);
		}
		uint num = PeekUInt32(offset);
		if (!HeapHandleType.IsValidHeapOffset(num))
		{
			Throw.ReferenceOverflow();
		}
		return (int)num;
	}

	internal unsafe Guid PeekGuid(int offset)
	{
		CheckBounds(offset, sizeof(Guid));
		byte* ptr = Pointer + offset;
		if (BitConverter.IsLittleEndian)
		{
			return *(Guid*)ptr;
		}
		return new Guid(*ptr | (ptr[1] << 8) | (ptr[2] << 16) | (ptr[3] << 24), (short)(ptr[4] | (ptr[5] << 8)), (short)(ptr[6] | (ptr[7] << 8)), ptr[8], ptr[9], ptr[10], ptr[11], ptr[12], ptr[13], ptr[14], ptr[15]);
	}

	internal unsafe string PeekUtf16(int offset, int byteCount)
	{
		CheckBounds(offset, byteCount);
		byte* ptr = Pointer + offset;
		if (BitConverter.IsLittleEndian)
		{
			return new string((char*)ptr, 0, byteCount / 2);
		}
		return Encoding.Unicode.GetString(ptr, byteCount);
	}

	internal unsafe string PeekUtf8(int offset, int byteCount)
	{
		CheckBounds(offset, byteCount);
		return Encoding.UTF8.GetString(Pointer + offset, byteCount);
	}

	internal unsafe string PeekUtf8NullTerminated(int offset, byte[]? prefix, MetadataStringDecoder utf8Decoder, out int numberOfBytesRead, char terminator = '\0')
	{
		CheckBounds(offset, 0);
		int utf8NullTerminatedLength = GetUtf8NullTerminatedLength(offset, out numberOfBytesRead, terminator);
		return EncodingHelper.DecodeUtf8(Pointer + offset, utf8NullTerminatedLength, prefix, utf8Decoder);
	}

	internal unsafe int GetUtf8NullTerminatedLength(int offset, out int numberOfBytesRead, char terminator = '\0')
	{
		CheckBounds(offset, 0);
		byte* ptr = Pointer + offset;
		byte* ptr2 = Pointer + Length;
		byte* ptr3;
		for (ptr3 = ptr; ptr3 < ptr2; ptr3++)
		{
			byte b = *ptr3;
			if (b == 0 || b == terminator)
			{
				break;
			}
		}
		int result = (numberOfBytesRead = (int)(ptr3 - ptr));
		if (ptr3 < ptr2)
		{
			numberOfBytesRead++;
		}
		return result;
	}

	internal unsafe int Utf8NullTerminatedOffsetOfAsciiChar(int startOffset, char asciiChar)
	{
		CheckBounds(startOffset, 0);
		for (int i = startOffset; i < Length; i++)
		{
			byte b = Pointer[i];
			if (b == 0)
			{
				break;
			}
			if (b == asciiChar)
			{
				return i;
			}
		}
		return -1;
	}

	internal bool Utf8NullTerminatedEquals(int offset, string text, MetadataStringDecoder utf8Decoder, char terminator, bool ignoreCase)
	{
		int firstDifferenceIndex;
		FastComparisonResult fastComparisonResult = Utf8NullTerminatedFastCompare(offset, text, 0, out firstDifferenceIndex, terminator, ignoreCase);
		if (fastComparisonResult == FastComparisonResult.Inconclusive)
		{
			int numberOfBytesRead;
			string text2 = PeekUtf8NullTerminated(offset, null, utf8Decoder, out numberOfBytesRead, terminator);
			return text2.Equals(text, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		}
		return fastComparisonResult == FastComparisonResult.Equal;
	}

	internal bool Utf8NullTerminatedStartsWith(int offset, string text, MetadataStringDecoder utf8Decoder, char terminator, bool ignoreCase)
	{
		int firstDifferenceIndex;
		switch (Utf8NullTerminatedFastCompare(offset, text, 0, out firstDifferenceIndex, terminator, ignoreCase))
		{
		case FastComparisonResult.Equal:
		case FastComparisonResult.BytesStartWithText:
			return true;
		case FastComparisonResult.TextStartsWithBytes:
		case FastComparisonResult.Unequal:
			return false;
		default:
		{
			int numberOfBytesRead;
			string text2 = PeekUtf8NullTerminated(offset, null, utf8Decoder, out numberOfBytesRead, terminator);
			return text2.StartsWith(text, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		}
		}
	}

	internal unsafe FastComparisonResult Utf8NullTerminatedFastCompare(int offset, string text, int textStart, out int firstDifferenceIndex, char terminator, bool ignoreCase)
	{
		CheckBounds(offset, 0);
		byte* ptr = Pointer + offset;
		byte* ptr2 = Pointer + Length;
		byte* ptr3 = ptr;
		int ignoreCaseMask = StringUtils.IgnoreCaseMask(ignoreCase);
		int num = textStart;
		while (num < text.Length && ptr3 != ptr2)
		{
			byte b = *ptr3;
			if (b == 0 || b == terminator)
			{
				break;
			}
			char c = text[num];
			if ((b & 0x80) == 0 && StringUtils.IsEqualAscii(c, b, ignoreCaseMask))
			{
				num++;
				ptr3++;
				continue;
			}
			firstDifferenceIndex = num;
			if (c <= '\u007f')
			{
				return FastComparisonResult.Unequal;
			}
			return FastComparisonResult.Inconclusive;
		}
		firstDifferenceIndex = num;
		bool flag = num == text.Length;
		bool flag2 = ptr3 == ptr2 || *ptr3 == 0 || *ptr3 == terminator;
		if (flag && flag2)
		{
			return FastComparisonResult.Equal;
		}
		if (!flag)
		{
			return FastComparisonResult.TextStartsWithBytes;
		}
		return FastComparisonResult.BytesStartWithText;
	}

	internal unsafe bool Utf8NullTerminatedStringStartsWithAsciiPrefix(int offset, string asciiPrefix)
	{
		CheckBounds(offset, 0);
		if (asciiPrefix.Length > Length - offset)
		{
			return false;
		}
		byte* ptr = Pointer + offset;
		for (int i = 0; i < asciiPrefix.Length; i++)
		{
			if (asciiPrefix[i] != *ptr)
			{
				return false;
			}
			ptr++;
		}
		return true;
	}

	internal unsafe int CompareUtf8NullTerminatedStringWithAsciiString(int offset, string asciiString)
	{
		CheckBounds(offset, 0);
		byte* ptr = Pointer + offset;
		int num = Length - offset;
		for (int i = 0; i < asciiString.Length; i++)
		{
			if (i > num)
			{
				return -1;
			}
			if (*ptr != asciiString[i])
			{
				return *ptr - asciiString[i];
			}
			ptr++;
		}
		if (*ptr != 0)
		{
			return 1;
		}
		return 0;
	}

	internal unsafe byte[] PeekBytes(int offset, int byteCount)
	{
		CheckBounds(offset, byteCount);
		return BlobUtilities.ReadBytes(Pointer + offset, byteCount);
	}

	internal int IndexOf(byte b, int start)
	{
		CheckBounds(start, 0);
		return IndexOfUnchecked(b, start);
	}

	internal unsafe int IndexOfUnchecked(byte b, int start)
	{
		byte* ptr = Pointer + start;
		for (byte* ptr2 = Pointer + Length; ptr < ptr2; ptr++)
		{
			if (*ptr == b)
			{
				return (int)(ptr - Pointer);
			}
		}
		return -1;
	}

	internal int BinarySearch(string[] asciiKeys, int offset)
	{
		int num = 0;
		int num2 = asciiKeys.Length - 1;
		while (num <= num2)
		{
			int num3 = num + (num2 - num >> 1);
			string asciiString = asciiKeys[num3];
			int num4 = CompareUtf8NullTerminatedStringWithAsciiString(offset, asciiString);
			if (num4 == 0)
			{
				return num3;
			}
			if (num4 < 0)
			{
				num2 = num3 - 1;
			}
			else
			{
				num = num3 + 1;
			}
		}
		return ~num;
	}

	internal int BinarySearchForSlot(int rowCount, int rowSize, int referenceListOffset, uint referenceValue, bool isReferenceSmall)
	{
		int num = 0;
		int num2 = rowCount - 1;
		uint num3 = PeekReferenceUnchecked(num * rowSize + referenceListOffset, isReferenceSmall);
		uint num4 = PeekReferenceUnchecked(num2 * rowSize + referenceListOffset, isReferenceSmall);
		if (num2 == 1)
		{
			if (referenceValue >= num4)
			{
				return num2;
			}
			return num;
		}
		while (num2 - num > 1)
		{
			if (referenceValue <= num3)
			{
				if (referenceValue != num3)
				{
					return num - 1;
				}
				return num;
			}
			if (referenceValue >= num4)
			{
				if (referenceValue != num4)
				{
					return num2 + 1;
				}
				return num2;
			}
			int num5 = (num + num2) / 2;
			uint num6 = PeekReferenceUnchecked(num5 * rowSize + referenceListOffset, isReferenceSmall);
			if (referenceValue > num6)
			{
				num = num5;
				num3 = num6;
				continue;
			}
			if (referenceValue < num6)
			{
				num2 = num5;
				num4 = num6;
				continue;
			}
			return num5;
		}
		return num;
	}

	internal int BinarySearchReference(int rowCount, int rowSize, int referenceOffset, uint referenceValue, bool isReferenceSmall)
	{
		int num = 0;
		int num2 = rowCount - 1;
		while (num <= num2)
		{
			int num3 = (num + num2) / 2;
			uint num4 = PeekReferenceUnchecked(num3 * rowSize + referenceOffset, isReferenceSmall);
			if (referenceValue > num4)
			{
				num = num3 + 1;
				continue;
			}
			if (referenceValue < num4)
			{
				num2 = num3 - 1;
				continue;
			}
			return num3;
		}
		return -1;
	}

	internal int BinarySearchReference(int[] ptrTable, int rowSize, int referenceOffset, uint referenceValue, bool isReferenceSmall)
	{
		int num = 0;
		int num2 = ptrTable.Length - 1;
		while (num <= num2)
		{
			int num3 = (num + num2) / 2;
			uint num4 = PeekReferenceUnchecked((ptrTable[num3] - 1) * rowSize + referenceOffset, isReferenceSmall);
			if (referenceValue > num4)
			{
				num = num3 + 1;
				continue;
			}
			if (referenceValue < num4)
			{
				num2 = num3 - 1;
				continue;
			}
			return num3;
		}
		return -1;
	}

	internal void BinarySearchReferenceRange(int rowCount, int rowSize, int referenceOffset, uint referenceValue, bool isReferenceSmall, out int startRowNumber, out int endRowNumber)
	{
		int num = BinarySearchReference(rowCount, rowSize, referenceOffset, referenceValue, isReferenceSmall);
		if (num == -1)
		{
			startRowNumber = -1;
			endRowNumber = -1;
			return;
		}
		startRowNumber = num;
		while (startRowNumber > 0 && PeekReferenceUnchecked((startRowNumber - 1) * rowSize + referenceOffset, isReferenceSmall) == referenceValue)
		{
			startRowNumber--;
		}
		endRowNumber = num;
		while (endRowNumber + 1 < rowCount && PeekReferenceUnchecked((endRowNumber + 1) * rowSize + referenceOffset, isReferenceSmall) == referenceValue)
		{
			endRowNumber++;
		}
	}

	internal void BinarySearchReferenceRange(int[] ptrTable, int rowSize, int referenceOffset, uint referenceValue, bool isReferenceSmall, out int startRowNumber, out int endRowNumber)
	{
		int num = BinarySearchReference(ptrTable, rowSize, referenceOffset, referenceValue, isReferenceSmall);
		if (num == -1)
		{
			startRowNumber = -1;
			endRowNumber = -1;
			return;
		}
		startRowNumber = num;
		while (startRowNumber > 0 && PeekReferenceUnchecked((ptrTable[startRowNumber - 1] - 1) * rowSize + referenceOffset, isReferenceSmall) == referenceValue)
		{
			startRowNumber--;
		}
		endRowNumber = num;
		while (endRowNumber + 1 < ptrTable.Length && PeekReferenceUnchecked((ptrTable[endRowNumber + 1] - 1) * rowSize + referenceOffset, isReferenceSmall) == referenceValue)
		{
			endRowNumber++;
		}
	}

	internal int LinearSearchReference(int rowSize, int referenceOffset, uint referenceValue, bool isReferenceSmall)
	{
		int i = referenceOffset;
		for (int length = Length; i < length; i += rowSize)
		{
			uint num = PeekReferenceUnchecked(i, isReferenceSmall);
			if (num == referenceValue)
			{
				return i / rowSize;
			}
		}
		return -1;
	}

	internal bool IsOrderedByReferenceAscending(int rowSize, int referenceOffset, bool isReferenceSmall)
	{
		int i = referenceOffset;
		int length = Length;
		uint num = 0u;
		for (; i < length; i += rowSize)
		{
			uint num2 = PeekReferenceUnchecked(i, isReferenceSmall);
			if (num2 < num)
			{
				return false;
			}
			num = num2;
		}
		return true;
	}

	internal int[] BuildPtrTable(int numberOfRows, int rowSize, int referenceOffset, bool isReferenceSmall)
	{
		int[] array = new int[numberOfRows];
		uint[] unsortedReferences = new uint[numberOfRows];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = i + 1;
		}
		ReadColumn(unsortedReferences, rowSize, referenceOffset, isReferenceSmall);
		Array.Sort(array, (int a, int b) => unsortedReferences[a - 1].CompareTo(unsortedReferences[b - 1]));
		return array;
	}

	private void ReadColumn(uint[] result, int rowSize, int referenceOffset, bool isReferenceSmall)
	{
		int num = referenceOffset;
		int length = Length;
		int num2 = 0;
		while (num < length)
		{
			result[num2] = PeekReferenceUnchecked(num, isReferenceSmall);
			num += rowSize;
			num2++;
		}
	}

	internal bool PeekHeapValueOffsetAndSize(int index, out int offset, out int size)
	{
		int numberOfBytesRead;
		int num = PeekCompressedInteger(index, out numberOfBytesRead);
		if (num == int.MaxValue)
		{
			offset = 0;
			size = 0;
			return false;
		}
		offset = index + numberOfBytesRead;
		size = num;
		return true;
	}
}
