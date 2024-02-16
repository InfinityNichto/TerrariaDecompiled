namespace System.Runtime.Serialization;

internal sealed class ExtensionDataMember
{
	private IDataNode _value;

	private int _memberIndex;

	public string Name { get; }

	public string Namespace { get; }

	public IDataNode Value
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

	public int MemberIndex
	{
		get
		{
			return _memberIndex;
		}
		set
		{
			_memberIndex = value;
		}
	}

	public ExtensionDataMember(string name, string ns)
	{
		Name = name;
		Namespace = ns;
	}
}
