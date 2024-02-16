namespace System.ComponentModel;

public abstract class InstanceCreationEditor
{
	public virtual string Text => System.SR.InstanceCreationEditorDefaultText;

	public abstract object? CreateInstance(ITypeDescriptorContext context, Type instanceType);
}
