using System.Threading;

namespace System.Text;

internal sealed class InternalDecoderBestFitFallbackBuffer : DecoderFallbackBuffer
{
	internal char cBestFit;

	internal int iCount = -1;

	internal int iSize;

	private readonly InternalDecoderBestFitFallback _oFallback;

	private static object s_InternalSyncObject;

	private static object InternalSyncObject
	{
		get
		{
			if (s_InternalSyncObject == null)
			{
				object value = new object();
				Interlocked.CompareExchange<object>(ref s_InternalSyncObject, value, (object)null);
			}
			return s_InternalSyncObject;
		}
	}

	public override int Remaining
	{
		get
		{
			if (iCount <= 0)
			{
				return 0;
			}
			return iCount;
		}
	}

	public InternalDecoderBestFitFallbackBuffer(InternalDecoderBestFitFallback fallback)
	{
		_oFallback = fallback;
		if (_oFallback.arrayBestFit != null)
		{
			return;
		}
		lock (InternalSyncObject)
		{
			if (_oFallback.arrayBestFit == null)
			{
				_oFallback.arrayBestFit = fallback.encoding.GetBestFitBytesToUnicodeData();
			}
		}
	}

	public override bool Fallback(byte[] bytesUnknown, int index)
	{
		cBestFit = TryBestFit(bytesUnknown);
		if (cBestFit == '\0')
		{
			cBestFit = _oFallback.cReplacement;
		}
		iCount = (iSize = 1);
		return true;
	}

	public override char GetNextChar()
	{
		iCount--;
		if (iCount < 0)
		{
			return '\0';
		}
		if (iCount == int.MaxValue)
		{
			iCount = -1;
			return '\0';
		}
		return cBestFit;
	}

	public override bool MovePrevious()
	{
		if (iCount >= 0)
		{
			iCount++;
		}
		if (iCount >= 0)
		{
			return iCount <= iSize;
		}
		return false;
	}

	public override void Reset()
	{
		iCount = -1;
	}

	private char TryBestFit(byte[] bytesCheck)
	{
		int num = 0;
		int num2 = _oFallback.arrayBestFit.Length;
		if (num2 == 0)
		{
			return '\0';
		}
		if (bytesCheck.Length == 0 || bytesCheck.Length > 2)
		{
			return '\0';
		}
		char c = ((bytesCheck.Length != 1) ? ((char)((bytesCheck[0] << 8) + bytesCheck[1])) : ((char)bytesCheck[0]));
		if (c < _oFallback.arrayBestFit[0] || c > _oFallback.arrayBestFit[num2 - 2])
		{
			return '\0';
		}
		int num3;
		while ((num3 = num2 - num) > 6)
		{
			int num4 = (num3 / 2 + num) & 0xFFFE;
			char c2 = _oFallback.arrayBestFit[num4];
			if (c2 == c)
			{
				return _oFallback.arrayBestFit[num4 + 1];
			}
			if (c2 < c)
			{
				num = num4;
			}
			else
			{
				num2 = num4;
			}
		}
		for (int num4 = num; num4 < num2; num4 += 2)
		{
			if (_oFallback.arrayBestFit[num4] == c)
			{
				return _oFallback.arrayBestFit[num4 + 1];
			}
		}
		return '\0';
	}
}
