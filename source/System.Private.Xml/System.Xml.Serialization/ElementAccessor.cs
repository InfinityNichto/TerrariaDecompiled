namespace System.Xml.Serialization;

internal sealed class ElementAccessor : Accessor
{
	private bool _nullable;

	private bool _isSoap;

	private bool _unbounded;

	internal bool IsSoap
	{
		get
		{
			return _isSoap;
		}
		set
		{
			_isSoap = value;
		}
	}

	internal bool IsNullable
	{
		get
		{
			return _nullable;
		}
		set
		{
			_nullable = value;
		}
	}

	internal bool IsUnbounded
	{
		get
		{
			return _unbounded;
		}
		set
		{
			_unbounded = value;
		}
	}

	internal ElementAccessor Clone()
	{
		ElementAccessor elementAccessor = new ElementAccessor();
		elementAccessor._nullable = _nullable;
		elementAccessor.IsTopLevelInSchema = base.IsTopLevelInSchema;
		elementAccessor.Form = base.Form;
		elementAccessor._isSoap = _isSoap;
		elementAccessor.Name = Name;
		elementAccessor.Default = base.Default;
		elementAccessor.Namespace = base.Namespace;
		elementAccessor.Mapping = base.Mapping;
		elementAccessor.Any = base.Any;
		return elementAccessor;
	}
}
