using System;
using System.ComponentModel;
using System.Xml.Linq;

namespace MS.Internal.Xml.Linq.ComponentModel;

internal abstract class XPropertyDescriptor<T, TProperty> : PropertyDescriptor where T : XObject
{
	public override Type ComponentType => typeof(T);

	public override bool IsReadOnly => true;

	public override Type PropertyType => typeof(TProperty);

	public override bool SupportsChangeEvents => true;

	public XPropertyDescriptor(string name)
		: base(name, null)
	{
	}

	public override void AddValueChanged(object component, EventHandler handler)
	{
		bool flag = GetValueChangedHandler(component) != null;
		base.AddValueChanged(component, handler);
		if (!flag && component is T val && GetValueChangedHandler(component) != null)
		{
			val.Changing += OnChanging;
			val.Changed += OnChanged;
		}
	}

	public override bool CanResetValue(object component)
	{
		return false;
	}

	public override void RemoveValueChanged(object component, EventHandler handler)
	{
		base.RemoveValueChanged(component, handler);
		if (component is T val && GetValueChangedHandler(component) == null)
		{
			val.Changing -= OnChanging;
			val.Changed -= OnChanged;
		}
	}

	public override void ResetValue(object component)
	{
	}

	public override void SetValue(object component, object value)
	{
	}

	public override bool ShouldSerializeValue(object component)
	{
		return false;
	}

	protected virtual void OnChanged(object sender, XObjectChangeEventArgs args)
	{
	}

	protected virtual void OnChanging(object sender, XObjectChangeEventArgs args)
	{
	}
}
