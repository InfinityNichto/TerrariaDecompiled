using System.Globalization;

namespace System.Xml;

internal struct BinXmlSqlMoney
{
	private readonly long _data;

	public BinXmlSqlMoney(int v)
	{
		_data = v;
	}

	public BinXmlSqlMoney(long v)
	{
		_data = v;
	}

	public decimal ToDecimal()
	{
		bool isNegative;
		ulong num;
		if (_data < 0)
		{
			isNegative = true;
			num = (ulong)(-_data);
		}
		else
		{
			isNegative = false;
			num = (ulong)_data;
		}
		return new decimal((int)num, (int)(num >> 32), 0, isNegative, 4);
	}

	public override string ToString()
	{
		return ToDecimal().ToString("#0.00##", CultureInfo.InvariantCulture);
	}
}
