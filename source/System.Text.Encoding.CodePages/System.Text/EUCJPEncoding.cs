namespace System.Text;

internal sealed class EUCJPEncoding : DBCSCodePageEncoding
{
	public EUCJPEncoding()
		: base(51932, 932)
	{
	}

	protected override bool CleanUpBytes(ref int bytes)
	{
		if (bytes >= 256)
		{
			if (bytes >= 64064 && bytes <= 64587)
			{
				if (bytes >= 64064 && bytes <= 64091)
				{
					if (bytes <= 64073)
					{
						bytes -= 2897;
					}
					else if (bytes >= 64074 && bytes <= 64083)
					{
						bytes -= 29430;
					}
					else if (bytes >= 64084 && bytes <= 64087)
					{
						bytes -= 2907;
					}
					else if (bytes == 64088)
					{
						bytes = 34698;
					}
					else if (bytes == 64089)
					{
						bytes = 34690;
					}
					else if (bytes == 64090)
					{
						bytes = 34692;
					}
					else if (bytes == 64091)
					{
						bytes = 34714;
					}
				}
				else if (bytes >= 64092 && bytes <= 64587)
				{
					byte b = (byte)bytes;
					if (b < 92)
					{
						bytes -= 3423;
					}
					else if (b >= 128 && b <= 155)
					{
						bytes -= 3357;
					}
					else
					{
						bytes -= 3356;
					}
				}
			}
			byte b2 = (byte)(bytes >> 8);
			byte b3 = (byte)bytes;
			b2 = (byte)(b2 - ((b2 > 159) ? 177 : 113));
			b2 = (byte)((b2 << 1) + 1);
			if (b3 > 158)
			{
				b3 -= 126;
				b2++;
			}
			else
			{
				if (b3 > 126)
				{
					b3--;
				}
				b3 -= 31;
			}
			bytes = (b2 << 8) | b3 | 0x8080;
			if ((bytes & 0xFF00) < 41216 || (bytes & 0xFF00) > 65024 || (bytes & 0xFF) < 161 || (bytes & 0xFF) > 254)
			{
				return false;
			}
		}
		else
		{
			if (bytes >= 161 && bytes <= 223)
			{
				bytes |= 36352;
				return true;
			}
			if (bytes >= 129 && bytes != 160 && bytes != 255)
			{
				return false;
			}
		}
		return true;
	}

	protected unsafe override void CleanUpEndBytes(char* chars)
	{
		for (int i = 161; i <= 254; i++)
		{
			chars[i] = '\ufffe';
		}
		chars[142] = '\ufffe';
	}
}
