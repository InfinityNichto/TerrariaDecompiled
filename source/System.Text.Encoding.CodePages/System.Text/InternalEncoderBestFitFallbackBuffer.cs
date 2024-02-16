using System.Threading;

namespace System.Text;

internal sealed class InternalEncoderBestFitFallbackBuffer : EncoderFallbackBuffer
{
	private char _cBestFit;

	private readonly InternalEncoderBestFitFallback _oFallback;

	private int _iCount = -1;

	private int _iSize;

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
			if (_iCount <= 0)
			{
				return 0;
			}
			return _iCount;
		}
	}

	public InternalEncoderBestFitFallbackBuffer(InternalEncoderBestFitFallback fallback)
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
				_oFallback.arrayBestFit = fallback.encoding.GetBestFitUnicodeToBytesData();
			}
		}
	}

	public override bool Fallback(char charUnknown, int index)
	{
		_iCount = (_iSize = 1);
		_cBestFit = TryBestFit(charUnknown);
		if (_cBestFit == '\0')
		{
			_cBestFit = '?';
		}
		return true;
	}

	public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
	{
		if (!char.IsHighSurrogate(charUnknownHigh))
		{
			throw new ArgumentOutOfRangeException("charUnknownHigh", System.SR.Format(System.SR.ArgumentOutOfRange_Range, 55296, 56319));
		}
		if (!char.IsLowSurrogate(charUnknownLow))
		{
			throw new ArgumentOutOfRangeException("charUnknownLow", System.SR.Format(System.SR.ArgumentOutOfRange_Range, 56320, 57343));
		}
		_cBestFit = '?';
		_iCount = (_iSize = 2);
		return true;
	}

	public override char GetNextChar()
	{
		_iCount--;
		if (_iCount < 0)
		{
			return '\0';
		}
		if (_iCount == int.MaxValue)
		{
			_iCount = -1;
			return '\0';
		}
		return _cBestFit;
	}

	public override bool MovePrevious()
	{
		if (_iCount >= 0)
		{
			_iCount++;
		}
		if (_iCount >= 0)
		{
			return _iCount <= _iSize;
		}
		return false;
	}

	public override void Reset()
	{
		_iCount = -1;
	}

	private char TryBestFit(char cUnknown)
	{
		int num = 0;
		int num2 = _oFallback.arrayBestFit.Length;
		int num3;
		while ((num3 = num2 - num) > 6)
		{
			int num4 = (num3 / 2 + num) & 0xFFFE;
			char c = _oFallback.arrayBestFit[num4];
			if (c == cUnknown)
			{
				return _oFallback.arrayBestFit[num4 + 1];
			}
			if (c < cUnknown)
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
			if (_oFallback.arrayBestFit[num4] == cUnknown)
			{
				return _oFallback.arrayBestFit[num4 + 1];
			}
		}
		return '\0';
	}
}
