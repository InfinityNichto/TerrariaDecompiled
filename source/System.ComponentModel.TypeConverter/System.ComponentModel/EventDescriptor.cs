namespace System.ComponentModel;

public abstract class EventDescriptor : MemberDescriptor
{
	public abstract Type ComponentType { get; }

	public abstract Type EventType { get; }

	public abstract bool IsMulticast { get; }

	protected EventDescriptor(string name, Attribute[]? attrs)
		: base(name, attrs)
	{
	}

	protected EventDescriptor(MemberDescriptor descr)
		: base(descr)
	{
	}

	protected EventDescriptor(MemberDescriptor descr, Attribute[]? attrs)
		: base(descr, attrs)
	{
	}

	public abstract void AddEventHandler(object component, Delegate value);

	public abstract void RemoveEventHandler(object component, Delegate value);
}
