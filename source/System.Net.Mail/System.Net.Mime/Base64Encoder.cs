namespace System.Net.Mime;

internal sealed class Base64Encoder : ByteEncoder
{
	private readonly int _lineLength;

	private readonly Base64WriteStateInfo _writeState;

	private static ReadOnlySpan<byte> Base64EncodeMap => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/="u8;

	internal override WriteStateInfoBase WriteState => _writeState;

	protected override bool HasSpecialEncodingForCRLF => false;

	internal Base64Encoder(Base64WriteStateInfo writeStateInfo, int lineLength)
	{
		_writeState = writeStateInfo;
		_lineLength = lineLength;
	}

	protected override void AppendEncodedCRLF()
	{
		throw new InvalidOperationException();
	}

	protected override bool LineBreakNeeded(byte b)
	{
		return LineBreakNeeded(1);
	}

	protected override bool LineBreakNeeded(byte[] bytes, int count)
	{
		return LineBreakNeeded(count);
	}

	private bool LineBreakNeeded(int numberOfBytesToAppend)
	{
		if (_lineLength == -1)
		{
			return false;
		}
		int num;
		int num2;
		switch (_writeState.Padding)
		{
		case 2:
			num = 2;
			num2 = 3;
			break;
		case 1:
			num = 1;
			num2 = 2;
			break;
		case 0:
			num = 0;
			num2 = 0;
			break;
		default:
			num = 0;
			num2 = 0;
			break;
		}
		int num3 = numberOfBytesToAppend - num;
		if (num3 <= 0)
		{
			return false;
		}
		int num4 = num3 / 3 + ((num3 % 3 != 0) ? 1 : 0);
		int num5 = num2 + num4 * 4;
		return WriteState.CurrentLineLength + num5 + _writeState.FooterLength > _lineLength;
	}

	protected override int GetCodepointSize(string value, int i)
	{
		if (!IsSurrogatePair(value, i))
		{
			return 1;
		}
		return 2;
	}

	public override void AppendPadding()
	{
		switch (_writeState.Padding)
		{
		case 2:
			_writeState.Append(Base64EncodeMap[_writeState.LastBits]);
			_writeState.Append(Base64EncodeMap[64]);
			_writeState.Append(Base64EncodeMap[64]);
			_writeState.Padding = 0;
			break;
		case 1:
			_writeState.Append(Base64EncodeMap[_writeState.LastBits]);
			_writeState.Append(Base64EncodeMap[64]);
			_writeState.Padding = 0;
			break;
		case 0:
			break;
		}
	}

	protected override void ApppendEncodedByte(byte b)
	{
		switch (_writeState.Padding)
		{
		case 0:
			_writeState.Append(Base64EncodeMap[(b & 0xFC) >> 2]);
			_writeState.LastBits = (byte)((b & 3) << 4);
			_writeState.Padding = 2;
			break;
		case 2:
			_writeState.Append(Base64EncodeMap[_writeState.LastBits | ((b & 0xF0) >> 4)]);
			_writeState.LastBits = (byte)((b & 0xF) << 2);
			_writeState.Padding = 1;
			break;
		case 1:
			_writeState.Append(Base64EncodeMap[_writeState.LastBits | ((b & 0xC0) >> 6)]);
			_writeState.Append(Base64EncodeMap[b & 0x3F]);
			_writeState.Padding = 0;
			break;
		}
	}
}
