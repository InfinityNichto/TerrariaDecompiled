namespace System.Diagnostics.Metrics;

internal struct ObjectSequenceMany : IEquatable<ObjectSequenceMany>, IObjectSequence
{
	private readonly object[] _values;

	public ObjectSequenceMany(object[] values)
	{
		_values = values;
	}

	public bool Equals(ObjectSequenceMany other)
	{
		if (_values.Length != other._values.Length)
		{
			return false;
		}
		for (int i = 0; i < _values.Length; i++)
		{
			object obj = _values[i];
			object obj2 = other._values[i];
			if (obj == null)
			{
				if (obj2 != null)
				{
					return false;
				}
			}
			else if (!obj.Equals(obj2))
			{
				return false;
			}
		}
		return true;
	}

	public override bool Equals(object obj)
	{
		if (obj is ObjectSequenceMany other)
		{
			return Equals(other);
		}
		return false;
	}

	public Span<object> AsSpan()
	{
		return _values.AsSpan();
	}

	public override int GetHashCode()
	{
		HashCode hashCode = default(HashCode);
		for (int i = 0; i < _values.Length; i++)
		{
			hashCode.Add(_values[i]);
		}
		return hashCode.ToHashCode();
	}
}
