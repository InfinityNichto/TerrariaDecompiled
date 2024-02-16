using System.Collections;

namespace System.Xml.Schema;

internal sealed class SelectorActiveAxis : ActiveAxis
{
	private readonly ConstraintStruct _cs;

	private readonly ArrayList _KSs;

	private int _KSpointer;

	public int lastDepth
	{
		get
		{
			if (_KSpointer != 0)
			{
				return ((KSStruct)_KSs[_KSpointer - 1]).depth;
			}
			return -1;
		}
	}

	public SelectorActiveAxis(Asttree axisTree, ConstraintStruct cs)
		: base(axisTree)
	{
		_KSs = new ArrayList();
		_cs = cs;
	}

	public override bool EndElement(string localname, string URN)
	{
		base.EndElement(localname, URN);
		if (_KSpointer > 0 && base.CurrentDepth == lastDepth)
		{
			return true;
		}
		return false;
	}

	public int PushKS(int errline, int errcol)
	{
		KeySequence ks = new KeySequence(_cs.TableDim, errline, errcol);
		KSStruct kSStruct;
		if (_KSpointer < _KSs.Count)
		{
			kSStruct = (KSStruct)_KSs[_KSpointer];
			kSStruct.ks = ks;
			for (int i = 0; i < _cs.TableDim; i++)
			{
				kSStruct.fields[i].Reactivate(ks);
			}
		}
		else
		{
			kSStruct = new KSStruct(ks, _cs.TableDim);
			for (int j = 0; j < _cs.TableDim; j++)
			{
				kSStruct.fields[j] = new LocatedActiveAxis(_cs.constraint.Fields[j], ks, j);
				_cs.axisFields.Add(kSStruct.fields[j]);
			}
			_KSs.Add(kSStruct);
		}
		kSStruct.depth = base.CurrentDepth - 1;
		return _KSpointer++;
	}

	public KeySequence PopKS()
	{
		return ((KSStruct)_KSs[--_KSpointer]).ks;
	}
}
