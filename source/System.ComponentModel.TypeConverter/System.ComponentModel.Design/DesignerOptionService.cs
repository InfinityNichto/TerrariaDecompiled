using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.ComponentModel.Design;

public abstract class DesignerOptionService : IDesignerOptionService
{
	[TypeConverter(typeof(DesignerOptionConverter))]
	[Editor("", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
	public sealed class DesignerOptionCollection : IList, ICollection, IEnumerable
	{
		private sealed class WrappedPropertyDescriptor : PropertyDescriptor
		{
			private readonly object _target;

			private readonly PropertyDescriptor _property;

			public override AttributeCollection Attributes => _property.Attributes;

			public override Type ComponentType => _property.ComponentType;

			public override bool IsReadOnly => _property.IsReadOnly;

			public override Type PropertyType => _property.PropertyType;

			internal WrappedPropertyDescriptor(PropertyDescriptor property, object target)
				: base(property.Name, null)
			{
				_property = property;
				_target = target;
			}

			public override bool CanResetValue(object component)
			{
				return _property.CanResetValue(_target);
			}

			public override object GetValue(object component)
			{
				return _property.GetValue(_target);
			}

			public override void ResetValue(object component)
			{
				_property.ResetValue(_target);
			}

			public override void SetValue(object component, object value)
			{
				_property.SetValue(_target, value);
			}

			public override bool ShouldSerializeValue(object component)
			{
				return _property.ShouldSerializeValue(_target);
			}
		}

		private readonly DesignerOptionService _service;

		private readonly object _value;

		private ArrayList _children;

		private PropertyDescriptorCollection _properties;

		public int Count
		{
			get
			{
				EnsurePopulated();
				return _children.Count;
			}
		}

		public string Name { get; }

		public DesignerOptionCollection? Parent { get; }

		public PropertyDescriptorCollection Properties
		{
			[RequiresUnreferencedCode("The Type of DesignerOptionCollection's value cannot be statically discovered.")]
			get
			{
				if (_properties == null)
				{
					ArrayList arrayList;
					if (_value != null)
					{
						PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(_value);
						arrayList = new ArrayList(properties.Count);
						foreach (PropertyDescriptor item in properties)
						{
							arrayList.Add(new WrappedPropertyDescriptor(item, _value));
						}
					}
					else
					{
						arrayList = new ArrayList(1);
					}
					EnsurePopulated();
					foreach (DesignerOptionCollection child in _children)
					{
						arrayList.AddRange(child.Properties);
					}
					PropertyDescriptor[] array = new PropertyDescriptor[arrayList.Count];
					arrayList.CopyTo(array);
					_properties = new PropertyDescriptorCollection(array, readOnly: true);
				}
				return _properties;
			}
		}

		public DesignerOptionCollection? this[int index]
		{
			get
			{
				EnsurePopulated();
				if (index < 0 || index >= _children.Count)
				{
					throw new IndexOutOfRangeException("index");
				}
				return (DesignerOptionCollection)_children[index];
			}
		}

		public DesignerOptionCollection? this[string name]
		{
			get
			{
				EnsurePopulated();
				foreach (DesignerOptionCollection child in _children)
				{
					if (string.Compare(child.Name, name, ignoreCase: true, CultureInfo.InvariantCulture) == 0)
					{
						return child;
					}
				}
				return null;
			}
		}

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot => this;

		bool IList.IsFixedSize => true;

		bool IList.IsReadOnly => true;

		object? IList.this[int index]
		{
			get
			{
				return this[index];
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		internal DesignerOptionCollection(DesignerOptionService service, DesignerOptionCollection parent, string name, object value)
		{
			_service = service;
			Parent = parent;
			Name = name;
			_value = value;
			if (Parent != null)
			{
				parent._properties = null;
				if (Parent._children == null)
				{
					Parent._children = new ArrayList(1);
				}
				Parent._children.Add(this);
			}
		}

		public void CopyTo(Array array, int index)
		{
			EnsurePopulated();
			_children.CopyTo(array, index);
		}

		[MemberNotNull("_children")]
		private void EnsurePopulated()
		{
			if (_children == null)
			{
				_service.PopulateOptionCollection(this);
				if (_children == null)
				{
					_children = new ArrayList(1);
				}
			}
		}

		public IEnumerator GetEnumerator()
		{
			EnsurePopulated();
			return _children.GetEnumerator();
		}

		public int IndexOf(DesignerOptionCollection value)
		{
			EnsurePopulated();
			return _children.IndexOf(value);
		}

		private static object RecurseFindValue(DesignerOptionCollection options)
		{
			if (options._value != null)
			{
				return options._value;
			}
			foreach (DesignerOptionCollection option in options)
			{
				object obj = RecurseFindValue(option);
				if (obj != null)
				{
					return obj;
				}
			}
			return null;
		}

		public bool ShowDialog()
		{
			object obj = RecurseFindValue(this);
			if (obj == null)
			{
				return false;
			}
			return _service.ShowDialog(this, obj);
		}

		int IList.Add(object value)
		{
			throw new NotSupportedException();
		}

		void IList.Clear()
		{
			throw new NotSupportedException();
		}

		bool IList.Contains(object value)
		{
			EnsurePopulated();
			return _children.Contains(value);
		}

		int IList.IndexOf(object value)
		{
			EnsurePopulated();
			return _children.IndexOf(value);
		}

		void IList.Insert(int index, object value)
		{
			throw new NotSupportedException();
		}

		void IList.Remove(object value)
		{
			throw new NotSupportedException();
		}

		void IList.RemoveAt(int index)
		{
			throw new NotSupportedException();
		}
	}

	internal sealed class DesignerOptionConverter : TypeConverter
	{
		private sealed class OptionPropertyDescriptor : PropertyDescriptor
		{
			private readonly DesignerOptionCollection _option;

			public override Type ComponentType => _option.GetType();

			public override bool IsReadOnly => true;

			public override Type PropertyType => _option.GetType();

			internal OptionPropertyDescriptor(DesignerOptionCollection option)
				: base(option.Name, null)
			{
				_option = option;
			}

			public override bool CanResetValue(object component)
			{
				return false;
			}

			public override object GetValue(object component)
			{
				return _option;
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
		}

		public override bool GetPropertiesSupported(ITypeDescriptorContext cxt)
		{
			return true;
		}

		[RequiresUnreferencedCode("The Type of value cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
		public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext cxt, object value, Attribute[] attributes)
		{
			PropertyDescriptorCollection propertyDescriptorCollection = new PropertyDescriptorCollection(null);
			if (!(value is DesignerOptionCollection designerOptionCollection))
			{
				return propertyDescriptorCollection;
			}
			foreach (DesignerOptionCollection item in designerOptionCollection)
			{
				propertyDescriptorCollection.Add(new OptionPropertyDescriptor(item));
			}
			foreach (PropertyDescriptor property in designerOptionCollection.Properties)
			{
				propertyDescriptorCollection.Add(property);
			}
			return propertyDescriptorCollection;
		}

		public override object ConvertTo(ITypeDescriptorContext cxt, CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType == typeof(string))
			{
				return System.SR.CollectionConverterText;
			}
			return base.ConvertTo(cxt, culture, value, destinationType);
		}
	}

	private DesignerOptionCollection _options;

	public DesignerOptionCollection Options => _options ?? (_options = new DesignerOptionCollection(this, null, string.Empty, null));

	protected DesignerOptionCollection CreateOptionCollection(DesignerOptionCollection parent, string name, object value)
	{
		if (parent == null)
		{
			throw new ArgumentNullException("parent");
		}
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.InvalidArgumentValue, "name.Length"), "name");
		}
		return new DesignerOptionCollection(this, parent, name, value);
	}

	[RequiresUnreferencedCode("The Type of DesignerOptionCollection's value cannot be statically discovered.")]
	private PropertyDescriptor GetOptionProperty(string pageName, string valueName)
	{
		if (pageName == null)
		{
			throw new ArgumentNullException("pageName");
		}
		if (valueName == null)
		{
			throw new ArgumentNullException("valueName");
		}
		string[] array = pageName.Split('\\');
		DesignerOptionCollection designerOptionCollection = Options;
		string[] array2 = array;
		foreach (string name in array2)
		{
			designerOptionCollection = designerOptionCollection[name];
			if (designerOptionCollection == null)
			{
				return null;
			}
		}
		return designerOptionCollection.Properties[valueName];
	}

	protected virtual void PopulateOptionCollection(DesignerOptionCollection options)
	{
	}

	protected virtual bool ShowDialog(DesignerOptionCollection options, object optionObject)
	{
		return false;
	}

	[RequiresUnreferencedCode("The option value's Type cannot be statically discovered.")]
	object IDesignerOptionService.GetOptionValue(string pageName, string valueName)
	{
		return GetOptionProperty(pageName, valueName)?.GetValue(null);
	}

	[RequiresUnreferencedCode("The option value's Type cannot be statically discovered.")]
	void IDesignerOptionService.SetOptionValue(string pageName, string valueName, object value)
	{
		GetOptionProperty(pageName, valueName)?.SetValue(null, value);
	}
}
