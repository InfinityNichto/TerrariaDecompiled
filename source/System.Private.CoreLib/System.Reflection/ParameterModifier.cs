namespace System.Reflection;

public readonly struct ParameterModifier
{
	private readonly bool[] _byRef;

	public bool this[int index]
	{
		get
		{
			return _byRef[index];
		}
		set
		{
			_byRef[index] = value;
		}
	}

	internal bool[] IsByRefArray => _byRef;

	public ParameterModifier(int parameterCount)
	{
		if (parameterCount <= 0)
		{
			throw new ArgumentException(SR.Arg_ParmArraySize);
		}
		_byRef = new bool[parameterCount];
	}
}
