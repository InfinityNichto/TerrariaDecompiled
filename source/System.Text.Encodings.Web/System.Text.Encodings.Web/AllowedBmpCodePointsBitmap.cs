using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Unicode;

namespace System.Text.Encodings.Web;

internal struct AllowedBmpCodePointsBitmap
{
	private unsafe fixed uint Bitmap[2048];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe void AllowChar(char value)
	{
		_GetIndexAndOffset(value, out UIntPtr index, out int offset);
		ref uint reference = ref Bitmap[(ulong)index];
		reference |= (uint)(1 << offset);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe void ForbidChar(char value)
	{
		_GetIndexAndOffset(value, out UIntPtr index, out int offset);
		ref uint reference = ref Bitmap[(ulong)index];
		reference &= (uint)(~(1 << offset));
	}

	public void ForbidHtmlCharacters()
	{
		ForbidChar('<');
		ForbidChar('>');
		ForbidChar('&');
		ForbidChar('\'');
		ForbidChar('"');
		ForbidChar('+');
	}

	public unsafe void ForbidUndefinedCharacters()
	{
		fixed (uint* pointer = Bitmap)
		{
			ReadOnlySpan<byte> values = UnicodeHelpers.GetDefinedBmpCodePointsBitmapLittleEndian();
			Span<uint> span = new Span<uint>(pointer, 2048);
			if (Vector.IsHardwareAccelerated && BitConverter.IsLittleEndian)
			{
				while (!values.IsEmpty)
				{
					(new Vector<uint>(values) & new Vector<uint>(span)).CopyTo(span);
					values = values.Slice(Vector<byte>.Count);
					span = span.Slice(Vector<uint>.Count);
				}
				return;
			}
			for (int i = 0; i < span.Length; i++)
			{
				span[i] &= BinaryPrimitives.ReadUInt32LittleEndian(values.Slice(i * 4));
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe readonly bool IsCharAllowed(char value)
	{
		_GetIndexAndOffset(value, out UIntPtr index, out int offset);
		if ((Bitmap[(ulong)index] & (uint)(1 << offset)) != 0)
		{
			return true;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe readonly bool IsCodePointAllowed(uint value)
	{
		if (!System.Text.UnicodeUtility.IsBmpCodePoint(value))
		{
			return false;
		}
		_GetIndexAndOffset(value, out UIntPtr index, out int offset);
		if ((Bitmap[(ulong)index] & (uint)(1 << offset)) != 0)
		{
			return true;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void _GetIndexAndOffset(uint value, out nuint index, out int offset)
	{
		index = value >> 5;
		offset = (int)(value & 0x1F);
	}
}
