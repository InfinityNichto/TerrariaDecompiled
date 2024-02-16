namespace System.Runtime.Serialization;

internal sealed class TypeHandleRef
{
	private RuntimeTypeHandle _value;

	public RuntimeTypeHandle Value
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

	public TypeHandleRef()
	{
	}

	public TypeHandleRef(RuntimeTypeHandle value)
	{
		_value = value;
	}
}
