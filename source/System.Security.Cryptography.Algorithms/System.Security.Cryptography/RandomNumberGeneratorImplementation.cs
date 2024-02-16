namespace System.Security.Cryptography;

internal sealed class RandomNumberGeneratorImplementation : RandomNumberGenerator
{
	internal static readonly RandomNumberGeneratorImplementation s_singleton = new RandomNumberGeneratorImplementation();

	private RandomNumberGeneratorImplementation()
	{
	}

	internal unsafe static void FillSpan(Span<byte> data)
	{
		if (data.Length > 0)
		{
			fixed (byte* pbBuffer = data)
			{
				GetBytes(pbBuffer, data.Length);
			}
		}
	}

	public override void GetBytes(byte[] data)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		GetBytes(new Span<byte>(data));
	}

	public override void GetBytes(byte[] data, int offset, int count)
	{
		VerifyGetBytes(data, offset, count);
		GetBytes(new Span<byte>(data, offset, count));
	}

	public unsafe override void GetBytes(Span<byte> data)
	{
		if (data.Length > 0)
		{
			fixed (byte* pbBuffer = data)
			{
				GetBytes(pbBuffer, data.Length);
			}
		}
	}

	public override void GetNonZeroBytes(byte[] data)
	{
		if (data == null)
		{
			throw new ArgumentNullException("data");
		}
		GetNonZeroBytes(new Span<byte>(data));
	}

	public override void GetNonZeroBytes(Span<byte> data)
	{
		while (data.Length > 0)
		{
			GetBytes(data);
			int num = data.Length;
			for (int i = 0; i < data.Length; i++)
			{
				if (data[i] == 0)
				{
					num = i;
					break;
				}
			}
			for (int j = num + 1; j < data.Length; j++)
			{
				if (data[j] != 0)
				{
					data[num++] = data[j];
				}
			}
			data = data.Slice(num);
		}
	}

	private unsafe static void GetBytes(byte* pbBuffer, int count)
	{
		global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptGenRandom(IntPtr.Zero, pbBuffer, count, 2);
		if (nTSTATUS != 0)
		{
			throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
		}
	}
}
