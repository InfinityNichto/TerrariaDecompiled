using System.Globalization;

namespace System.Xml.Schema;

internal sealed class TypedObject
{
	private sealed class DecimalStruct
	{
		private bool _isDecimal;

		private readonly decimal[] _dvalue;

		public bool IsDecimal
		{
			get
			{
				return _isDecimal;
			}
			set
			{
				_isDecimal = value;
			}
		}

		public decimal[] Dvalue => _dvalue;

		public DecimalStruct()
		{
			_dvalue = new decimal[1];
		}

		public DecimalStruct(int dim)
		{
			_dvalue = new decimal[dim];
		}
	}

	private DecimalStruct _dstruct;

	private object _ovalue;

	private readonly string _svalue;

	private XmlSchemaDatatype _xsdtype;

	private readonly int _dim = 1;

	private readonly bool _isList;

	public int Dim => _dim;

	public bool IsList => _isList;

	public bool IsDecimal => _dstruct.IsDecimal;

	public decimal[] Dvalue => _dstruct.Dvalue;

	public object Value => _ovalue;

	public XmlSchemaDatatype Type => _xsdtype;

	public TypedObject(object obj, string svalue, XmlSchemaDatatype xsdtype)
	{
		_ovalue = obj;
		_svalue = svalue;
		_xsdtype = xsdtype;
		if (xsdtype.Variety == XmlSchemaDatatypeVariety.List || xsdtype is Datatype_base64Binary || xsdtype is Datatype_hexBinary)
		{
			_isList = true;
			_dim = ((Array)obj).Length;
		}
	}

	public override string ToString()
	{
		return _svalue;
	}

	public void SetDecimal()
	{
		if (_dstruct != null)
		{
			return;
		}
		XmlTypeCode typeCode = _xsdtype.TypeCode;
		if (typeCode == XmlTypeCode.Decimal || (uint)(typeCode - 40) <= 12u)
		{
			if (_isList)
			{
				_dstruct = new DecimalStruct(_dim);
				for (int i = 0; i < _dim; i++)
				{
					_dstruct.Dvalue[i] = Convert.ToDecimal(((Array)_ovalue).GetValue(i), NumberFormatInfo.InvariantInfo);
				}
			}
			else
			{
				_dstruct = new DecimalStruct();
				_dstruct.Dvalue[0] = Convert.ToDecimal(_ovalue, NumberFormatInfo.InvariantInfo);
			}
			_dstruct.IsDecimal = true;
		}
		else if (_isList)
		{
			_dstruct = new DecimalStruct(_dim);
		}
		else
		{
			_dstruct = new DecimalStruct();
		}
	}

	private bool ListDValueEquals(TypedObject other)
	{
		for (int i = 0; i < Dim; i++)
		{
			if (Dvalue[i] != other.Dvalue[i])
			{
				return false;
			}
		}
		return true;
	}

	public bool Equals(TypedObject other)
	{
		if (Dim != other.Dim)
		{
			return false;
		}
		if (Type != other.Type)
		{
			if (!Type.IsComparable(other.Type))
			{
				return false;
			}
			other.SetDecimal();
			SetDecimal();
			if (IsDecimal && other.IsDecimal)
			{
				return ListDValueEquals(other);
			}
		}
		if (IsList)
		{
			if (other.IsList)
			{
				return Type.Compare(Value, other.Value) == 0;
			}
			Array array = Value as Array;
			if (array is XmlAtomicValue[] array2)
			{
				if (array2.Length == 1)
				{
					return array2.GetValue(0).Equals(other.Value);
				}
				return false;
			}
			if (array.Length == 1)
			{
				return array.GetValue(0).Equals(other.Value);
			}
			return false;
		}
		if (other.IsList)
		{
			Array array3 = other.Value as Array;
			if (array3 is XmlAtomicValue[] array4)
			{
				if (array4.Length == 1)
				{
					return array4.GetValue(0).Equals(Value);
				}
				return false;
			}
			if (array3.Length == 1)
			{
				return array3.GetValue(0).Equals(Value);
			}
			return false;
		}
		return Value.Equals(other.Value);
	}
}
