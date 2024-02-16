namespace System.Net;

internal static class WebHeaderEncoding
{
	internal unsafe static string GetString(byte[] bytes, int byteIndex, int byteCount)
	{
		if (byteCount < 1)
		{
			return string.Empty;
		}
		return string.Create(byteCount, (bytes, byteIndex), delegate(Span<char> buffer, (byte[] bytes, int byteIndex) state)
		{
			fixed (byte* ptr = &state.bytes[state.byteIndex])
			{
				fixed (char* ptr3 = buffer)
				{
					byte* ptr2 = ptr;
					char* ptr4 = ptr3;
					int num;
					for (num = buffer.Length; num >= 8; num -= 8)
					{
						*ptr4 = (char)(*ptr2);
						ptr4[1] = (char)ptr2[1];
						ptr4[2] = (char)ptr2[2];
						ptr4[3] = (char)ptr2[3];
						ptr4[4] = (char)ptr2[4];
						ptr4[5] = (char)ptr2[5];
						ptr4[6] = (char)ptr2[6];
						ptr4[7] = (char)ptr2[7];
						ptr4 += 8;
						ptr2 += 8;
					}
					for (int i = 0; i < num; i++)
					{
						ptr4[i] = (char)ptr2[i];
					}
				}
			}
		});
	}

	internal static int GetByteCount(string myString)
	{
		return myString.Length;
	}

	internal unsafe static void GetBytes(string myString, int charIndex, int charCount, byte[] bytes, int byteIndex)
	{
		if (myString.Length == 0)
		{
			return;
		}
		fixed (byte* ptr = bytes)
		{
			byte* ptr2 = ptr + byteIndex;
			int num = charIndex + charCount;
			while (charIndex < num)
			{
				*(ptr2++) = (byte)myString[charIndex++];
			}
		}
	}
}
