using System.ComponentModel;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct Int32Aggregator
{
	private int _result;

	private int _cnt;

	public int SumResult => _result;

	public int AverageResult => _result / _cnt;

	public int MinimumResult => _result;

	public int MaximumResult => _result;

	public bool IsEmpty => _cnt == 0;

	public void Create()
	{
		_cnt = 0;
	}

	public void Sum(int value)
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

	public void Average(int value)
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

	public void Minimum(int value)
	{
		if (_cnt == 0 || value < _result)
		{
			_result = value;
		}
		_cnt = 1;
	}

	public void Maximum(int value)
	{
		if (_cnt == 0 || value > _result)
		{
			_result = value;
		}
		_cnt = 1;
	}
}
