namespace System.ComponentModel;

public abstract class ComponentEditor
{
	public bool EditComponent(object component)
	{
		return EditComponent(null, component);
	}

	public abstract bool EditComponent(ITypeDescriptorContext? context, object component);
}
