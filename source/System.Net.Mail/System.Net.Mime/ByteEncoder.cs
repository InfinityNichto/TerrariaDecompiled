using System.Text;

namespace System.Net.Mime;

internal abstract class ByteEncoder : IByteEncoder
{
	internal abstract WriteStateInfoBase WriteState { get; }

	protected abstract bool HasSpecialEncodingForCRLF { get; }

	public string GetEncodedString()
	{
		return Encoding.ASCII.GetString(WriteState.Buffer, 0, WriteState.Length);
	}

	public int EncodeBytes(byte[] buffer, int offset, int count, bool dontDeferFinalBytes, bool shouldAppendSpaceToCRLF)
	{
		WriteState.AppendHeader();
		bool hasSpecialEncodingForCRLF = HasSpecialEncodingForCRLF;
		int i;
		for (i = offset; i < count + offset; i++)
		{
			if (LineBreakNeeded(buffer[i]))
			{
				AppendPadding();
				WriteState.AppendCRLF(shouldAppendSpaceToCRLF);
			}
			if (hasSpecialEncodingForCRLF && IsCRLF(buffer, i, count + offset))
			{
				AppendEncodedCRLF();
				i++;
			}
			else
			{
				ApppendEncodedByte(buffer[i]);
			}
		}
		if (dontDeferFinalBytes)
		{
			AppendPadding();
		}
		WriteState.AppendFooter();
		return i - offset;
	}

	public int EncodeString(string value, Encoding encoding)
	{
		if (encoding == Encoding.Latin1)
		{
			byte[] bytes = encoding.GetBytes(value);
			return EncodeBytes(bytes, 0, bytes.Length, dontDeferFinalBytes: true, shouldAppendSpaceToCRLF: true);
		}
		WriteState.AppendHeader();
		bool hasSpecialEncodingForCRLF = HasSpecialEncodingForCRLF;
		int num = 0;
		byte[] bytes2 = new byte[encoding.GetMaxByteCount(2)];
		for (int i = 0; i < value.Length; i++)
		{
			int codepointSize = GetCodepointSize(value, i);
			int bytes3 = encoding.GetBytes(value, i, codepointSize, bytes2, 0);
			if (codepointSize == 2)
			{
				i++;
			}
			if (LineBreakNeeded(bytes2, bytes3))
			{
				AppendPadding();
				WriteState.AppendCRLF(includeSpace: true);
			}
			if (hasSpecialEncodingForCRLF && IsCRLF(bytes2, bytes3))
			{
				AppendEncodedCRLF();
			}
			else
			{
				AppendEncodedCodepoint(bytes2, bytes3);
			}
			num += bytes3;
		}
		AppendPadding();
		WriteState.AppendFooter();
		return num;
	}

	protected abstract void AppendEncodedCRLF();

	protected abstract bool LineBreakNeeded(byte b);

	protected abstract bool LineBreakNeeded(byte[] bytes, int count);

	protected abstract int GetCodepointSize(string value, int i);

	public abstract void AppendPadding();

	protected abstract void ApppendEncodedByte(byte b);

	private void AppendEncodedCodepoint(byte[] bytes, int count)
	{
		for (int i = 0; i < count; i++)
		{
			ApppendEncodedByte(bytes[i]);
		}
	}

	protected bool IsSurrogatePair(string value, int i)
	{
		if (char.IsSurrogate(value[i]) && i + 1 < value.Length)
		{
			return char.IsSurrogatePair(value[i], value[i + 1]);
		}
		return false;
	}

	protected bool IsCRLF(byte[] bytes, int count)
	{
		if (count == 2)
		{
			return IsCRLF(bytes, 0, count);
		}
		return false;
	}

	private bool IsCRLF(byte[] buffer, int i, int bufferSize)
	{
		if (buffer[i] == 13 && i + 1 < bufferSize)
		{
			return buffer[i + 1] == 10;
		}
		return false;
	}
}
