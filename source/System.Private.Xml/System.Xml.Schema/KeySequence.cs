using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Xml.Schema;

internal sealed class KeySequence
{
	private readonly TypedObject[] _ks;

	private readonly int _dim;

	private int _hashcode = -1;

	private readonly int _posline;

	private readonly int _poscol;

	public int PosLine => _posline;

	public int PosCol => _poscol;

	public object this[int index]
	{
		get
		{
			return _ks[index];
		}
		set
		{
			_ks[index] = (TypedObject)value;
		}
	}

	internal KeySequence(int dim, int line, int col)
	{
		_dim = dim;
		_ks = new TypedObject[dim];
		_posline = line;
		_poscol = col;
	}

	internal bool IsQualified()
	{
		for (int i = 0; i < _ks.Length; i++)
		{
			if (_ks[i] == null || _ks[i].Value == null)
			{
				return false;
			}
		}
		return true;
	}

	public override int GetHashCode()
	{
		if (_hashcode != -1)
		{
			return _hashcode;
		}
		_hashcode = 0;
		for (int i = 0; i < _ks.Length; i++)
		{
			_ks[i].SetDecimal();
			if (_ks[i].IsDecimal)
			{
				for (int j = 0; j < _ks[i].Dim; j++)
				{
					_hashcode += _ks[i].Dvalue[j].GetHashCode();
				}
			}
			else if (_ks[i].Value is Array array)
			{
				if (array is XmlAtomicValue[] array2)
				{
					for (int k = 0; k < array2.Length; k++)
					{
						_hashcode += ((XmlAtomicValue)array2.GetValue(k)).TypedValue.GetHashCode();
					}
				}
				else
				{
					for (int l = 0; l < ((Array)_ks[i].Value).Length; l++)
					{
						_hashcode += ((Array)_ks[i].Value).GetValue(l).GetHashCode();
					}
				}
			}
			else
			{
				_hashcode += _ks[i].Value.GetHashCode();
			}
		}
		return _hashcode;
	}

	public override bool Equals([NotNullWhen(true)] object other)
	{
		if (other is KeySequence keySequence)
		{
			for (int i = 0; i < _ks.Length; i++)
			{
				if (!_ks[i].Equals(keySequence._ks[i]))
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(_ks[0].ToString());
		for (int i = 1; i < _ks.Length; i++)
		{
			stringBuilder.Append(' ');
			stringBuilder.Append(_ks[i].ToString());
		}
		return stringBuilder.ToString();
	}
}
