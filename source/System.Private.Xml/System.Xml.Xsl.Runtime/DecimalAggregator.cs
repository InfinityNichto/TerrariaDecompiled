using System.ComponentModel;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct DecimalAggregator
{
	private decimal _result;

	private int _cnt;

	public decimal SumResult => _result;

	public decimal AverageResult => _result / (decimal)_cnt;

	public decimal MinimumResult => _result;

	public decimal MaximumResult => _result;

	public bool IsEmpty => _cnt == 0;

	public void Create()
	{
		_cnt = 0;
	}

	public void Sum(decimal value)
	{
		if (_cnt == 0)
		{
			_result = value;
			_cnt = 1;
		}
		else
		{
			_result += value;
		}
	}

	public void Average(decimal value)
	{
		if (_cnt == 0)
		{
			_result = value;
		}
		else
		{
			_result += value;
		}
		_cnt++;
	}

	public void Minimum(decimal value)
	{
		if (_cnt == 0 || value < _result)
		{
			_result = value;
		}
		_cnt = 1;
	}

	public void Maximum(decimal value)
	{
		if (_cnt == 0 || value > _result)
		{
			_result = value;
		}
		_cnt = 1;
	}
}
