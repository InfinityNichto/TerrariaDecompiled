namespace System.Xml.Xsl.Qil;

internal class QilLiteral : QilNode
{
	private object _value;

	public object Value
	{
		get
		{
			return _value;
		}
		set
		{
			_value = value;
		}
	}

	public QilLiteral(QilNodeType nodeType, object value)
		: base(nodeType)
	{
		Value = value;
	}

	public static implicit operator string(QilLiteral literal)
	{
		return (string)literal._value;
	}

	public static implicit operator int(QilLiteral literal)
	{
		return (int)literal._value;
	}

	public static implicit operator long(QilLiteral literal)
	{
		return (long)literal._value;
	}

	public static implicit operator double(QilLiteral literal)
	{
		return (double)literal._value;
	}

	public static implicit operator decimal(QilLiteral literal)
	{
		return (decimal)literal._value;
	}

	public static implicit operator XmlQueryType(QilLiteral literal)
	{
		return (XmlQueryType)literal._value;
	}
}
