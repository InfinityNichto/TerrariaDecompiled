using System.ComponentModel;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct Int64Aggregator
{
	private long _result;

	private int _cnt;

	public long SumResult => _result;

	public long AverageResult => _result / _cnt;

	public long MinimumResult => _result;

	public long MaximumResult => _result;

	public bool IsEmpty => _cnt == 0;

	public void Create()
	{
		_cnt = 0;
	}

	public void Sum(long value)
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

	public void Average(long value)
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

	public void Minimum(long value)
	{
		if (_cnt == 0 || value < _result)
		{
			_result = value;
		}
		_cnt = 1;
	}

	public void Maximum(long value)
	{
		if (_cnt == 0 || value > _result)
		{
			_result = value;
		}
		_cnt = 1;
	}
}
