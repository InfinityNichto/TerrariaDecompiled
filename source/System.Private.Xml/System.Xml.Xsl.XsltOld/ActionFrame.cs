using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.XPath;
using MS.Internal.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal sealed class ActionFrame
{
	private sealed class XPathSortArrayIterator : XPathArrayIterator
	{
		public override XPathNavigator Current => ((SortKey)list[index - 1]).Node;

		public XPathSortArrayIterator(List<SortKey> list)
			: base(list)
		{
		}

		public XPathSortArrayIterator(XPathSortArrayIterator it)
			: base(it)
		{
		}

		public override XPathNodeIterator Clone()
		{
			return new XPathSortArrayIterator(this);
		}
	}

	private int _state;

	private int _counter;

	private object[] _variables;

	private Hashtable _withParams;

	private Action _action;

	private ActionFrame _container;

	private int _currentAction;

	private XPathNodeIterator _nodeSet;

	private XPathNodeIterator _newNodeSet;

	private PrefixQName _calulatedName;

	private string _storedOutput;

	internal PrefixQName CalulatedName
	{
		get
		{
			return _calulatedName;
		}
		set
		{
			_calulatedName = value;
		}
	}

	internal string StoredOutput
	{
		get
		{
			return _storedOutput;
		}
		set
		{
			_storedOutput = value;
		}
	}

	internal int State
	{
		get
		{
			return _state;
		}
		set
		{
			_state = value;
		}
	}

	internal int Counter
	{
		get
		{
			return _counter;
		}
		set
		{
			_counter = value;
		}
	}

	internal XPathNavigator Node
	{
		get
		{
			if (_nodeSet != null)
			{
				return _nodeSet.Current;
			}
			return null;
		}
	}

	internal XPathNodeIterator NodeSet => _nodeSet;

	internal XPathNodeIterator NewNodeSet => _newNodeSet;

	internal int IncrementCounter()
	{
		return ++_counter;
	}

	internal void AllocateVariables(int count)
	{
		if (0 < count)
		{
			_variables = new object[count];
		}
		else
		{
			_variables = null;
		}
	}

	internal object GetVariable(int index)
	{
		return _variables[index];
	}

	internal void SetVariable(int index, object value)
	{
		_variables[index] = value;
	}

	internal void SetParameter(XmlQualifiedName name, object value)
	{
		if (_withParams == null)
		{
			_withParams = new Hashtable();
		}
		_withParams[name] = value;
	}

	internal void ResetParams()
	{
		if (_withParams != null)
		{
			_withParams.Clear();
		}
	}

	internal object GetParameter(XmlQualifiedName name)
	{
		if (_withParams != null)
		{
			return _withParams[name];
		}
		return null;
	}

	internal void InitNodeSet(XPathNodeIterator nodeSet)
	{
		_nodeSet = nodeSet;
	}

	[MemberNotNull("_newNodeSet")]
	internal void InitNewNodeSet(XPathNodeIterator nodeSet)
	{
		_newNodeSet = nodeSet;
	}

	[MemberNotNull("_newNodeSet")]
	internal void SortNewNodeSet(Processor proc, ArrayList sortarray)
	{
		int count = sortarray.Count;
		XPathSortComparer xPathSortComparer = new XPathSortComparer(count);
		for (int i = 0; i < count; i++)
		{
			Sort sort = (Sort)sortarray[i];
			Query compiledQuery = proc.GetCompiledQuery(sort.select);
			xPathSortComparer.AddSort(compiledQuery, new XPathComparerHelper(sort.order, sort.caseOrder, sort.lang, sort.dataType));
		}
		List<SortKey> list = new List<SortKey>();
		while (NewNextNode(proc))
		{
			XPathNodeIterator nodeSet = _nodeSet;
			_nodeSet = _newNodeSet;
			SortKey sortKey = new SortKey(count, list.Count, _newNodeSet.Current.Clone());
			for (int j = 0; j < count; j++)
			{
				sortKey[j] = xPathSortComparer.Expression(j).Evaluate(_newNodeSet);
			}
			list.Add(sortKey);
			_nodeSet = nodeSet;
		}
		list.Sort(xPathSortComparer);
		_newNodeSet = new XPathSortArrayIterator(list);
	}

	internal void Finished()
	{
		State = -1;
	}

	internal void Inherit(ActionFrame parent)
	{
		_variables = parent._variables;
	}

	private void Init(Action action, ActionFrame container, XPathNodeIterator nodeSet)
	{
		_state = 0;
		_action = action;
		_container = container;
		_currentAction = 0;
		_nodeSet = nodeSet;
		_newNodeSet = null;
	}

	internal void Init(Action action, XPathNodeIterator nodeSet)
	{
		Init(action, null, nodeSet);
	}

	internal void Init(ActionFrame containerFrame, XPathNodeIterator nodeSet)
	{
		Init(containerFrame.GetAction(0), containerFrame, nodeSet);
	}

	internal void SetAction(Action action)
	{
		SetAction(action, 0);
	}

	internal void SetAction(Action action, int state)
	{
		_action = action;
		_state = state;
	}

	private Action GetAction(int actionIndex)
	{
		return ((ContainerAction)_action).GetAction(actionIndex);
	}

	internal void Exit()
	{
		Finished();
		_container = null;
	}

	[MemberNotNullWhen(false, "_action")]
	internal bool Execute(Processor processor)
	{
		if (_action == null)
		{
			return true;
		}
		_action.Execute(processor, this);
		if (State == -1)
		{
			if (_container != null)
			{
				_currentAction++;
				_action = _container.GetAction(_currentAction);
				State = 0;
			}
			else
			{
				_action = null;
			}
			return _action == null;
		}
		return false;
	}

	internal bool NextNode(Processor proc)
	{
		bool flag = _nodeSet.MoveNext();
		if (flag && proc.Stylesheet.Whitespace)
		{
			XPathNodeType nodeType = _nodeSet.Current.NodeType;
			if (nodeType == XPathNodeType.Whitespace)
			{
				XPathNavigator xPathNavigator = _nodeSet.Current.Clone();
				bool flag2;
				do
				{
					xPathNavigator.MoveTo(_nodeSet.Current);
					xPathNavigator.MoveToParent();
					flag2 = !proc.Stylesheet.PreserveWhiteSpace(proc, xPathNavigator) && (flag = _nodeSet.MoveNext());
					nodeType = _nodeSet.Current.NodeType;
				}
				while (flag2 && nodeType == XPathNodeType.Whitespace);
			}
		}
		return flag;
	}

	internal bool NewNextNode(Processor proc)
	{
		bool flag = _newNodeSet.MoveNext();
		if (flag && proc.Stylesheet.Whitespace)
		{
			XPathNodeType nodeType = _newNodeSet.Current.NodeType;
			if (nodeType == XPathNodeType.Whitespace)
			{
				XPathNavigator xPathNavigator = _newNodeSet.Current.Clone();
				bool flag2;
				do
				{
					xPathNavigator.MoveTo(_newNodeSet.Current);
					xPathNavigator.MoveToParent();
					flag2 = !proc.Stylesheet.PreserveWhiteSpace(proc, xPathNavigator) && (flag = _newNodeSet.MoveNext());
					nodeType = _newNodeSet.Current.NodeType;
				}
				while (flag2 && nodeType == XPathNodeType.Whitespace);
			}
		}
		return flag;
	}
}
