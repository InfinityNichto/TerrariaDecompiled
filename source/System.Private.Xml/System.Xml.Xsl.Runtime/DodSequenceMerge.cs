using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct DodSequenceMerge
{
	private IList<XPathNavigator> _firstSequence;

	private List<IEnumerator<XPathNavigator>> _sequencesToMerge;

	private int _nodeCount;

	private XmlQueryRuntime _runtime;

	public void Create(XmlQueryRuntime runtime)
	{
		_firstSequence = null;
		_sequencesToMerge = null;
		_nodeCount = 0;
		_runtime = runtime;
	}

	public void AddSequence(IList<XPathNavigator> sequence)
	{
		if (sequence.Count == 0)
		{
			return;
		}
		if (_firstSequence == null)
		{
			_firstSequence = sequence;
			return;
		}
		if (_sequencesToMerge == null)
		{
			_sequencesToMerge = new List<IEnumerator<XPathNavigator>>();
			MoveAndInsertSequence(_firstSequence.GetEnumerator());
			_nodeCount = _firstSequence.Count;
		}
		MoveAndInsertSequence(sequence.GetEnumerator());
		_nodeCount += sequence.Count;
	}

	public IList<XPathNavigator> MergeSequences()
	{
		if (_firstSequence == null)
		{
			return XmlQueryNodeSequence.Empty;
		}
		if (_sequencesToMerge == null || _sequencesToMerge.Count <= 1)
		{
			return _firstSequence;
		}
		XmlQueryNodeSequence xmlQueryNodeSequence = new XmlQueryNodeSequence(_nodeCount);
		while (_sequencesToMerge.Count != 1)
		{
			IEnumerator<XPathNavigator> enumerator = _sequencesToMerge[_sequencesToMerge.Count - 1];
			_sequencesToMerge.RemoveAt(_sequencesToMerge.Count - 1);
			xmlQueryNodeSequence.Add(enumerator.Current);
			MoveAndInsertSequence(enumerator);
		}
		do
		{
			xmlQueryNodeSequence.Add(_sequencesToMerge[0].Current);
		}
		while (_sequencesToMerge[0].MoveNext());
		return xmlQueryNodeSequence;
	}

	private void MoveAndInsertSequence(IEnumerator<XPathNavigator> sequence)
	{
		if (sequence.MoveNext())
		{
			InsertSequence(sequence);
		}
	}

	private void InsertSequence(IEnumerator<XPathNavigator> sequence)
	{
		for (int num = _sequencesToMerge.Count - 1; num >= 0; num--)
		{
			switch (_runtime.ComparePosition(sequence.Current, _sequencesToMerge[num].Current))
			{
			case -1:
				_sequencesToMerge.Insert(num + 1, sequence);
				return;
			case 0:
				if (!sequence.MoveNext())
				{
					return;
				}
				break;
			}
		}
		_sequencesToMerge.Insert(0, sequence);
	}
}
