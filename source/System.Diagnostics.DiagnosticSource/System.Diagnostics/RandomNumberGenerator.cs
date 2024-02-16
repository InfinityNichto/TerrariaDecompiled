namespace System.Diagnostics;

internal sealed class RandomNumberGenerator
{
	[ThreadStatic]
	private static RandomNumberGenerator t_random;

	private ulong _s0;

	private ulong _s1;

	private ulong _s2;

	private ulong _s3;

	public static RandomNumberGenerator Current
	{
		get
		{
			if (t_random == null)
			{
				t_random = new RandomNumberGenerator();
			}
			return t_random;
		}
	}

	public unsafe RandomNumberGenerator()
	{
		do
		{
			Guid guid = Guid.NewGuid();
			Guid guid2 = Guid.NewGuid();
			ulong* ptr = (ulong*)(&guid);
			ulong* ptr2 = (ulong*)(&guid2);
			_s0 = *ptr;
			_s1 = ptr[1];
			_s2 = *ptr2;
			_s3 = ptr2[1];
			_s0 = (_s0 & 0xFFFFFFFFFFFFFFFuL) | (_s1 & 0xF000000000000000uL);
			_s2 = (_s2 & 0xFFFFFFFFFFFFFFFuL) | (_s3 & 0xF000000000000000uL);
			_s1 = (_s1 & 0xFFFFFFFFFFFFFF3FuL) | (_s0 & 0xC0);
			_s3 = (_s3 & 0xFFFFFFFFFFFFFF3FuL) | (_s2 & 0xC0);
		}
		while ((_s0 | _s1 | _s2 | _s3) == 0L);
	}

	private ulong Rol64(ulong x, int k)
	{
		return (x << k) | (x >> 64 - k);
	}

	public long Next()
	{
		ulong result = Rol64(_s1 * 5, 7) * 9;
		ulong num = _s1 << 17;
		_s2 ^= _s0;
		_s3 ^= _s1;
		_s1 ^= _s2;
		_s0 ^= _s3;
		_s2 ^= num;
		_s3 = Rol64(_s3, 45);
		return (long)result;
	}
}
