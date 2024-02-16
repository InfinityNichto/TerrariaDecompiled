namespace System;

internal struct Currency
{
	internal long m_value;

	public Currency(decimal value)
	{
		m_value = decimal.ToOACurrency(value);
	}
}
