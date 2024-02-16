namespace System.Net.Mime;

internal sealed class QEncoder : ByteEncoder
{
	private readonly WriteStateInfoBase _writeState;

	internal override WriteStateInfoBase WriteState => _writeState;

	protected override bool HasSpecialEncodingForCRLF => true;

	internal QEncoder(WriteStateInfoBase wsi)
	{
		_writeState = wsi;
	}

	protected override void AppendEncodedCRLF()
	{
		WriteState.Append(61, 48, 68, 61, 48, 65);
	}

	protected override bool LineBreakNeeded(byte b)
	{
		int num = WriteState.CurrentLineLength + 3 + WriteState.FooterLength;
		bool flag = b == 32 || b == 9 || b == 13 || b == 10;
		if (num >= WriteState.MaxLineLength && flag)
		{
			return true;
		}
		int num2 = WriteState.CurrentLineLength + WriteState.FooterLength;
		if (num2 >= WriteState.MaxLineLength)
		{
			return true;
		}
		return false;
	}

	protected override bool LineBreakNeeded(byte[] bytes, int count)
	{
		if (count == 1 || IsCRLF(bytes, count))
		{
			return LineBreakNeeded(bytes[0]);
		}
		int num = count * 3;
		return WriteState.CurrentLineLength + num + _writeState.FooterLength > WriteState.MaxLineLength;
	}

	protected override int GetCodepointSize(string value, int i)
	{
		if (value[i] == '\r' && i + 1 < value.Length && value[i + 1] == '\n')
		{
			return 2;
		}
		if (IsSurrogatePair(value, i))
		{
			return 2;
		}
		return 1;
	}

	public override void AppendPadding()
	{
	}

	protected override void ApppendEncodedByte(byte b)
	{
		if (b == 32)
		{
			WriteState.Append(95);
			return;
		}
		if (IsAsciiLetterOrDigit((char)b))
		{
			WriteState.Append(b);
			return;
		}
		WriteState.Append(61);
		WriteState.Append((byte)System.HexConverter.ToCharUpper(b >> 4));
		WriteState.Append((byte)System.HexConverter.ToCharUpper(b));
	}

	private static bool IsAsciiLetterOrDigit(char character)
	{
		if (!IsAsciiLetter(character))
		{
			if (character >= '0')
			{
				return character <= '9';
			}
			return false;
		}
		return true;
	}

	private static bool IsAsciiLetter(char character)
	{
		if (character < 'a' || character > 'z')
		{
			if (character >= 'A')
			{
				return character <= 'Z';
			}
			return false;
		}
		return true;
	}
}
