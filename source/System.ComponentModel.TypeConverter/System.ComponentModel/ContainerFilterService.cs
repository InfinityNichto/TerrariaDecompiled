namespace System.ComponentModel;

public abstract class ContainerFilterService
{
	public virtual ComponentCollection FilterComponents(ComponentCollection components)
	{
		return components;
	}
}
