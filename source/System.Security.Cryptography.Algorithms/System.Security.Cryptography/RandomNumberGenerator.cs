using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Security.Cryptography;

public abstract class RandomNumberGenerator : IDisposable
{
	public static RandomNumberGenerator Create()
	{
		return RandomNumberGeneratorImplementation.s_singleton;
	}

	[UnsupportedOSPlatform("browser")]
	[RequiresUnreferencedCode("The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.")]
	public static RandomNumberGenerator? Create(string rngName)
	{
		return (RandomNumberGenerator)CryptoConfig.CreateFromName(rngName);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
	}

	public abstract void GetBytes(byte[] data);

	public virtual void GetBytes(byte[] data, int offset, int count)
	{
		VerifyGetBytes(data, offset, count);
		if (count > 0)
		{
			if (offset == 0 && count == data.Length)
			{
				GetBytes(data);
				return;
			}
			byte[] array = new byte[count];
			GetBytes(array);
			Buffer.BlockCopy(array, 0, data, offset, count);
		}
	}

	public virtual void GetBytes(Span<byte> data)
	{
		byte[] array = ArrayPool<byte>.Shared.Rent(data.Length);
		try
		{
			GetBytes(array, 0, data.Length);
			new ReadOnlySpan<byte>(array, 0, data.Length).CopyTo(data);
		}
		finally
		{
			Array.Clear(array, 0, data.Length);
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	public virtual void GetNonZeroBytes(byte[] data)
	{
		throw new NotImplementedException();
	}

	public virtual void GetNonZeroBytes(Span<byte> data)
	{
		byte[] array = ArrayPool<byte>.Shared.Rent(data.Length);
		try
		{
			GetNonZeroBytes(array);
			new ReadOnlySpan<byte>(array, 0, data.Length).CopyTo(data);
		}
		finally
		{
			Array.Clear(array, 0, data.Length);
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	public static void Fill(Span<byte> data)
	{
		RandomNumberGeneratorImplementation.FillSpan(data);
	}

	public static int GetInt32(int fromInclusive, int toExclusive)
	{
		if (fromInclusive >= toExclusive)
		{
			throw new ArgumentException(System.SR.Argument_InvalidRandomRange);
		}
		uint num = (uint)(toExclusive - fromInclusive - 1);
		if (num == 0)
		{
			return fromInclusive;
		}
		uint num2 = num;
		num2 |= num2 >> 1;
		num2 |= num2 >> 2;
		num2 |= num2 >> 4;
		num2 |= num2 >> 8;
		num2 |= num2 >> 16;
		Span<uint> span = stackalloc uint[1];
		uint num3;
		do
		{
			RandomNumberGeneratorImplementation.FillSpan(MemoryMarshal.AsBytes(span));
			num3 = num2 & span[0];
		}
		while (num3 > num);
		return (int)num3 + fromInclusive;
	}

	public static int GetInt32(int toExclusive)
	{
		if (toExclusive <= 0)
		{
			throw new ArgumentOutOfRangeException("toExclusive", System.SR.ArgumentOutOfRange_NeedPosNum);
		}
		return GetInt32(0, toExclusive);
	}

	public static byte[] GetBytes(int count)
	{
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		byte[] array = new byte[count];
		RandomNumberGeneratorImplementation.FillSpan(array);
		return array;
	}

	internal void VerifyGetBytes(byte[] data, int offset, int count)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		if (offset < 0)
		{
			throw new ArgumentOutOfRangeException("offset", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (count > data.Length - offset)
		{
			throw new ArgumentException(System.SR.Argument_InvalidOffLen);
		}
	}
}
