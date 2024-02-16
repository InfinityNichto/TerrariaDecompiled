using System.Buffers.Binary;

namespace System.Xml;

internal struct BinXmlSqlDecimal
{
	internal byte m_bLen;

	internal byte m_bPrec;

	internal byte m_bScale;

	internal byte m_bSign;

	internal uint m_data1;

	internal uint m_data2;

	internal uint m_data3;

	internal uint m_data4;

	public bool IsPositive => m_bSign == 0;

	public BinXmlSqlDecimal(byte[] data, int offset, bool trim)
	{
		m_bLen = data[offset] switch
		{
			7 => 1, 
			11 => 2, 
			15 => 3, 
			19 => 4, 
			_ => throw new XmlException(System.SR.XmlBinary_InvalidSqlDecimal, (string[])null), 
		};
		m_bPrec = data[offset + 1];
		m_bScale = data[offset + 2];
		m_bSign = ((data[offset + 3] == 0) ? ((byte)1) : ((byte)0));
		m_data1 = UIntFromByteArray(data, offset + 4);
		m_data2 = ((m_bLen > 1) ? UIntFromByteArray(data, offset + 8) : 0u);
		m_data3 = ((m_bLen > 2) ? UIntFromByteArray(data, offset + 12) : 0u);
		m_data4 = ((m_bLen > 3) ? UIntFromByteArray(data, offset + 16) : 0u);
		if (m_bLen == 4 && m_data4 == 0)
		{
			m_bLen = 3;
		}
		if (m_bLen == 3 && m_data3 == 0)
		{
			m_bLen = 2;
		}
		if (m_bLen == 2 && m_data2 == 0)
		{
			m_bLen = 1;
		}
		if (trim)
		{
			TrimTrailingZeros();
		}
	}

	private static uint UIntFromByteArray(byte[] data, int offset)
	{
		return BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset));
	}

	private static void MpDiv1(uint[] rgulU, ref int ciulU, uint iulD, out uint iulR)
	{
		uint num = 0u;
		ulong num2 = iulD;
		int num3 = ciulU;
		while (num3 > 0)
		{
			num3--;
			ulong num4 = ((ulong)num << 32) + rgulU[num3];
			rgulU[num3] = (uint)(num4 / num2);
			num = (uint)(num4 - rgulU[num3] * num2);
		}
		iulR = num;
		MpNormalize(rgulU, ref ciulU);
	}

	private static void MpNormalize(uint[] rgulU, ref int ciulU)
	{
		while (ciulU > 1 && rgulU[ciulU - 1] == 0)
		{
			ciulU--;
		}
	}

	private static char ChFromDigit(uint uiDigit)
	{
		return (char)(uiDigit + 48);
	}

	public decimal ToDecimal()
	{
		if (m_data4 != 0 || m_bScale > 28)
		{
			throw new XmlException(System.SR.SqlTypes_ArithOverflow, (string)null);
		}
		return new decimal((int)m_data1, (int)m_data2, (int)m_data3, !IsPositive, m_bScale);
	}

	private void TrimTrailingZeros()
	{
		uint[] array = new uint[4] { m_data1, m_data2, m_data3, m_data4 };
		int ciulU = m_bLen;
		if (ciulU == 1 && array[0] == 0)
		{
			m_bScale = 0;
			return;
		}
		while (m_bScale > 0 && (ciulU > 1 || array[0] != 0))
		{
			MpDiv1(array, ref ciulU, 10u, out var iulR);
			if (iulR != 0)
			{
				break;
			}
			m_data1 = array[0];
			m_data2 = array[1];
			m_data3 = array[2];
			m_data4 = array[3];
			m_bScale--;
		}
		if (m_bLen == 4 && m_data4 == 0)
		{
			m_bLen = 3;
		}
		if (m_bLen == 3 && m_data3 == 0)
		{
			m_bLen = 2;
		}
		if (m_bLen == 2 && m_data2 == 0)
		{
			m_bLen = 1;
		}
	}

	public override string ToString()
	{
		uint[] array = new uint[4] { m_data1, m_data2, m_data3, m_data4 };
		int ciulU = m_bLen;
		char[] array2 = new char[39];
		int num = 0;
		while (ciulU > 1 || array[0] != 0)
		{
			MpDiv1(array, ref ciulU, 10u, out var iulR);
			array2[num++] = ChFromDigit(iulR);
		}
		while (num <= m_bScale)
		{
			array2[num++] = ChFromDigit(0u);
		}
		bool isPositive = IsPositive;
		int num2 = (isPositive ? num : (num + 1));
		if (m_bScale > 0)
		{
			num2++;
		}
		char[] array3 = new char[num2];
		int num3 = 0;
		if (!isPositive)
		{
			array3[num3++] = '-';
		}
		while (num > 0)
		{
			if (num-- == m_bScale)
			{
				array3[num3++] = '.';
			}
			array3[num3++] = array2[num];
		}
		return new string(array3);
	}
}
