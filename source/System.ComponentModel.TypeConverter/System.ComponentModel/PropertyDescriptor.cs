using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.ComponentModel;

public abstract class PropertyDescriptor : MemberDescriptor
{
	private TypeConverter _converter;

	private Hashtable _valueChangedHandlers;

	private object[] _editors;

	private Type[] _editorTypes;

	private int _editorCount;

	public abstract Type ComponentType { get; }

	public virtual TypeConverter Converter
	{
		[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
		get
		{
			AttributeCollection attributes = Attributes;
			if (_converter == null)
			{
				TypeConverterAttribute typeConverterAttribute = (TypeConverterAttribute)attributes[typeof(TypeConverterAttribute)];
				if (typeConverterAttribute.ConverterTypeName != null && typeConverterAttribute.ConverterTypeName.Length > 0)
				{
					Type typeFromName = GetTypeFromName(typeConverterAttribute.ConverterTypeName);
					if (typeFromName != null && typeof(TypeConverter).IsAssignableFrom(typeFromName))
					{
						_converter = (TypeConverter)CreateInstance(typeFromName);
					}
				}
				if (_converter == null)
				{
					_converter = TypeDescriptor.GetConverter(PropertyType);
				}
			}
			return _converter;
		}
	}

	public virtual bool IsLocalizable => LocalizableAttribute.Yes.Equals(Attributes[typeof(LocalizableAttribute)]);

	public abstract bool IsReadOnly { get; }

	public DesignerSerializationVisibility SerializationVisibility
	{
		get
		{
			DesignerSerializationVisibilityAttribute designerSerializationVisibilityAttribute = (DesignerSerializationVisibilityAttribute)Attributes[typeof(DesignerSerializationVisibilityAttribute)];
			return designerSerializationVisibilityAttribute.Visibility;
		}
	}

	public abstract Type PropertyType { get; }

	public virtual bool SupportsChangeEvents => false;

	protected PropertyDescriptor(string name, Attribute[]? attrs)
		: base(name, attrs)
	{
	}

	protected PropertyDescriptor(MemberDescriptor descr)
		: base(descr)
	{
	}

	protected PropertyDescriptor(MemberDescriptor descr, Attribute[]? attrs)
		: base(descr, attrs)
	{
	}

	public virtual void AddValueChanged(object component, EventHandler handler)
	{
		if (component == null)
		{
			throw new ArgumentNullException("component");
		}
		if (handler == null)
		{
			throw new ArgumentNullException("handler");
		}
		if (_valueChangedHandlers == null)
		{
			_valueChangedHandlers = new Hashtable();
		}
		EventHandler a = (EventHandler)_valueChangedHandlers[component];
		_valueChangedHandlers[component] = Delegate.Combine(a, handler);
	}

	public abstract bool CanResetValue(object component);

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		try
		{
			if (obj == this)
			{
				return true;
			}
			if (obj == null)
			{
				return false;
			}
			if (obj is PropertyDescriptor propertyDescriptor && propertyDescriptor.NameHashCode == NameHashCode && propertyDescriptor.PropertyType == PropertyType && propertyDescriptor.Name.Equals(Name))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	protected object? CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
	{
		Type[] array = new Type[1] { typeof(Type) };
		ConstructorInfo constructor = type.GetConstructor(array);
		if (constructor != null)
		{
			return TypeDescriptor.CreateInstance(null, type, array, new object[1] { PropertyType });
		}
		return TypeDescriptor.CreateInstance(null, type, null, null);
	}

	protected override void FillAttributes(IList attributeList)
	{
		_converter = null;
		_editors = null;
		_editorTypes = null;
		_editorCount = 0;
		base.FillAttributes(attributeList);
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered.")]
	public PropertyDescriptorCollection GetChildProperties()
	{
		return GetChildProperties(null, null);
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	public PropertyDescriptorCollection GetChildProperties(Attribute[] filter)
	{
		return GetChildProperties(null, filter);
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The Type of instance cannot be statically discovered.")]
	public PropertyDescriptorCollection GetChildProperties(object instance)
	{
		return GetChildProperties(instance, null);
	}

	[RequiresUnreferencedCode("PropertyDescriptor's PropertyType cannot be statically discovered. The Type of instance cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	public virtual PropertyDescriptorCollection GetChildProperties(object? instance, Attribute[]? filter)
	{
		if (instance == null)
		{
			return TypeDescriptor.GetProperties(PropertyType, filter);
		}
		return TypeDescriptor.GetProperties(instance, filter);
	}

	[RequiresUnreferencedCode("Editors registered in TypeDescriptor.AddEditorTable may be trimmed. PropertyDescriptor's PropertyType cannot be statically discovered.")]
	public virtual object? GetEditor(Type editorBaseType)
	{
		object obj = null;
		AttributeCollection attributes = Attributes;
		if (_editorTypes != null)
		{
			for (int i = 0; i < _editorCount; i++)
			{
				if (_editorTypes[i] == editorBaseType)
				{
					return _editors[i];
				}
			}
		}
		if (obj == null)
		{
			for (int j = 0; j < attributes.Count; j++)
			{
				if (!(attributes[j] is EditorAttribute editorAttribute))
				{
					continue;
				}
				Type typeFromName = GetTypeFromName(editorAttribute.EditorBaseTypeName);
				if (editorBaseType == typeFromName)
				{
					Type typeFromName2 = GetTypeFromName(editorAttribute.EditorTypeName);
					if (typeFromName2 != null)
					{
						obj = CreateInstance(typeFromName2);
						break;
					}
				}
			}
			if (obj == null)
			{
				obj = TypeDescriptor.GetEditor(PropertyType, editorBaseType);
			}
			if (_editorTypes == null)
			{
				_editorTypes = new Type[5];
				_editors = new object[5];
			}
			if (_editorCount >= _editorTypes.Length)
			{
				Type[] array = new Type[_editorTypes.Length * 2];
				object[] array2 = new object[_editors.Length * 2];
				Array.Copy(_editorTypes, array, _editorTypes.Length);
				Array.Copy(_editors, array2, _editors.Length);
				_editorTypes = array;
				_editors = array2;
			}
			_editorTypes[_editorCount] = editorBaseType;
			_editors[_editorCount++] = obj;
		}
		return obj;
	}

	public override int GetHashCode()
	{
		return NameHashCode ^ PropertyType.GetHashCode();
	}

	protected override object? GetInvocationTarget(Type type, object instance)
	{
		object obj = base.GetInvocationTarget(type, instance);
		if (obj is ICustomTypeDescriptor customTypeDescriptor)
		{
			obj = customTypeDescriptor.GetPropertyOwner(this);
		}
		return obj;
	}

	[RequiresUnreferencedCode("Calls ComponentType.Assembly.GetType on the non-fully qualified typeName, which the trimmer cannot recognize.")]
	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	protected Type? GetTypeFromName([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] string? typeName)
	{
		if (typeName == null || typeName.Length == 0)
		{
			return null;
		}
		Type type = Type.GetType(typeName);
		Type type2 = null;
		if (ComponentType != null && (type == null || ComponentType.Assembly.FullName.Equals(type.Assembly.FullName)))
		{
			int num = typeName.IndexOf(',');
			if (num != -1)
			{
				typeName = typeName.Substring(0, num);
			}
			type2 = ComponentType.Assembly.GetType(typeName);
		}
		return type2 ?? type;
	}

	public abstract object? GetValue(object? component);

	protected virtual void OnValueChanged(object? component, EventArgs e)
	{
		if (component != null)
		{
			((EventHandler)(_valueChangedHandlers?[component]))?.Invoke(component, e);
		}
	}

	public virtual void RemoveValueChanged(object component, EventHandler handler)
	{
		if (component == null)
		{
			throw new ArgumentNullException("component");
		}
		if (handler == null)
		{
			throw new ArgumentNullException("handler");
		}
		if (_valueChangedHandlers != null)
		{
			EventHandler source = (EventHandler)_valueChangedHandlers[component];
			source = (EventHandler)Delegate.Remove(source, handler);
			if (source != null)
			{
				_valueChangedHandlers[component] = source;
			}
			else
			{
				_valueChangedHandlers.Remove(component);
			}
		}
	}

	protected internal EventHandler? GetValueChangedHandler(object component)
	{
		if (component != null && _valueChangedHandlers != null)
		{
			return (EventHandler)_valueChangedHandlers[component];
		}
		return null;
	}

	public abstract void ResetValue(object component);

	public abstract void SetValue(object? component, object? value);

	public abstract bool ShouldSerializeValue(object component);
}
