using System.Numerics;

namespace System.Net.Http.HPack;

internal struct IntegerDecoder
{
	private int _i;

	private int _m;

	public bool BeginTryDecode(byte b, int prefixLength, out int result)
	{
		if (b < (1 << prefixLength) - 1)
		{
			result = b;
			return true;
		}
		_i = b;
		_m = 0;
		result = 0;
		return false;
	}

	public bool TryDecode(byte b, out int result)
	{
		if (BitOperations.LeadingZeroCount(b) <= _m)
		{
			throw new HPackDecodingException(System.SR.net_http_hpack_bad_integer);
		}
		_i += (b & 0x7F) << _m;
		if (_i < 0)
		{
			throw new HPackDecodingException(System.SR.net_http_hpack_bad_integer);
		}
		_m += 7;
		if ((b & 0x80) == 0)
		{
			if (b == 0 && _m / 7 > 1)
			{
				throw new HPackDecodingException(System.SR.net_http_hpack_bad_integer);
			}
			result = _i;
			return true;
		}
		result = 0;
		return false;
	}
}
