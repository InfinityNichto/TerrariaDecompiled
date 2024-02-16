using System.Collections;
using System.Collections.Generic;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal sealed class XPathSortComparer : IComparer<SortKey>
{
	private Query[] _expressions;

	private IComparer[] _comparers;

	private int _numSorts;

	public int NumSorts => _numSorts;

	public XPathSortComparer(int size)
	{
		if (size <= 0)
		{
			size = 3;
		}
		_expressions = new Query[size];
		_comparers = new IComparer[size];
	}

	public XPathSortComparer()
		: this(3)
	{
	}

	public void AddSort(Query evalQuery, IComparer comparer)
	{
		if (_numSorts == _expressions.Length)
		{
			Query[] array = new Query[_numSorts * 2];
			IComparer[] array2 = new IComparer[_numSorts * 2];
			for (int i = 0; i < _numSorts; i++)
			{
				array[i] = _expressions[i];
				array2[i] = _comparers[i];
			}
			_expressions = array;
			_comparers = array2;
		}
		if (evalQuery.StaticType == XPathResultType.NodeSet || evalQuery.StaticType == XPathResultType.Any)
		{
			evalQuery = new StringFunctions(Function.FunctionType.FuncString, new Query[1] { evalQuery });
		}
		_expressions[_numSorts] = evalQuery;
		_comparers[_numSorts] = comparer;
		_numSorts++;
	}

	public Query Expression(int i)
	{
		return _expressions[i];
	}

	int IComparer<SortKey>.Compare(SortKey x, SortKey y)
	{
		int num = 0;
		for (int i = 0; i < x.NumKeys; i++)
		{
			num = _comparers[i].Compare(x[i], y[i]);
			if (num != 0)
			{
				return num;
			}
		}
		return x.OriginalPosition - y.OriginalPosition;
	}

	internal XPathSortComparer Clone()
	{
		XPathSortComparer xPathSortComparer = new XPathSortComparer(_numSorts);
		for (int i = 0; i < _numSorts; i++)
		{
			xPathSortComparer._comparers[i] = _comparers[i];
			xPathSortComparer._expressions[i] = (Query)_expressions[i].Clone();
		}
		xPathSortComparer._numSorts = _numSorts;
		return xPathSortComparer;
	}
}
