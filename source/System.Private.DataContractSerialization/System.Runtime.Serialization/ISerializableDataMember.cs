namespace System.Runtime.Serialization;

internal sealed class ISerializableDataMember
{
	private IDataNode _value;

	internal string Name { get; }

	internal IDataNode Value
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

	public ISerializableDataMember(string name)
	{
		Name = name;
	}
}
