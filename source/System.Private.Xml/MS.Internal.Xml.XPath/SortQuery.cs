using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath;

internal sealed class SortQuery : Query
{
	private readonly List<SortKey> _results;

	private readonly XPathSortComparer _comparer;

	private readonly Query _qyInput;

	public override XPathNavigator Current
	{
		get
		{
			if (count == 0)
			{
				return null;
			}
			return _results[count - 1].Node;
		}
	}

	public override XPathResultType StaticType => XPathResultType.NodeSet;

	public override int CurrentPosition => count;

	public override int Count => _results.Count;

	public override QueryProps Properties => (QueryProps)7;

	public SortQuery(Query qyInput)
	{
		_results = new List<SortKey>();
		_comparer = new XPathSortComparer();
		_qyInput = qyInput;
		count = 0;
	}

	private SortQuery(SortQuery other)
		: base(other)
	{
		_results = new List<SortKey>(other._results);
		_comparer = other._comparer.Clone();
		_qyInput = Query.Clone(other._qyInput);
		count = 0;
	}

	public override void Reset()
	{
		count = 0;
	}

	public override void SetXsltContext(XsltContext xsltContext)
	{
		_qyInput.SetXsltContext(xsltContext);
		if (_qyInput.StaticType != XPathResultType.NodeSet && _qyInput.StaticType != XPathResultType.Any)
		{
			throw XPathException.Create(System.SR.Xp_NodeSetExpected);
		}
	}

	private void BuildResultsList()
	{
		int numSorts = _comparer.NumSorts;
		XPathNavigator xPathNavigator;
		while ((xPathNavigator = _qyInput.Advance()) != null)
		{
			SortKey sortKey = new SortKey(numSorts, _results.Count, xPathNavigator.Clone());
			for (int i = 0; i < numSorts; i++)
			{
				sortKey[i] = _comparer.Expression(i).Evaluate(_qyInput);
			}
			_results.Add(sortKey);
		}
		_results.Sort(_comparer);
	}

	public override object Evaluate(XPathNodeIterator context)
	{
		_qyInput.Evaluate(context);
		_results.Clear();
		BuildResultsList();
		count = 0;
		return this;
	}

	public override XPathNavigator Advance()
	{
		if (count < _results.Count)
		{
			return _results[count++].Node;
		}
		return null;
	}

	internal void AddSort(Query evalQuery, IComparer comparer)
	{
		_comparer.AddSort(evalQuery, comparer);
	}

	public override XPathNodeIterator Clone()
	{
		return new SortQuery(this);
	}
}
