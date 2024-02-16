using System.ComponentModel;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct DoubleAggregator
{
	private double _result;

	private int _cnt;

	public double SumResult => _result;

	public double AverageResult => _result / (double)_cnt;

	public double MinimumResult => _result;

	public double MaximumResult => _result;

	public bool IsEmpty => _cnt == 0;

	public void Create()
	{
		_cnt = 0;
	}

	public void Sum(double value)
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

	public void Average(double value)
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

	public void Minimum(double value)
	{
		if (_cnt == 0 || value < _result || double.IsNaN(value))
		{
			_result = value;
		}
		_cnt = 1;
	}

	public void Maximum(double value)
	{
		if (_cnt == 0 || value > _result || double.IsNaN(value))
		{
			_result = value;
		}
		_cnt = 1;
	}
}
