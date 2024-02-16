using System.Runtime.CompilerServices;

namespace System.Text.Encodings.Web;

internal struct AsciiByteMap
{
	private unsafe fixed byte Buffer[128];

	internal unsafe void InsertAsciiChar(char key, byte value)
	{
		if (key < '\u0080')
		{
			Buffer[(uint)key] = value;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal unsafe readonly bool TryLookup(Rune key, out byte value)
	{
		if (key.IsAscii)
		{
			byte b = Buffer[(uint)key.Value];
			if (b != 0)
			{
				value = b;
				return true;
			}
		}
		value = 0;
		return false;
	}
}
