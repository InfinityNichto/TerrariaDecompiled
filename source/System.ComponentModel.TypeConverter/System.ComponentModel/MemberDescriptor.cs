using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.ComponentModel;

public abstract class MemberDescriptor
{
	private readonly string _name;

	private readonly string _displayName;

	private readonly int _nameHash;

	private AttributeCollection _attributeCollection;

	private Attribute[] _attributes;

	private Attribute[] _originalAttributes;

	private bool _attributesFiltered;

	private bool _attributesFilled;

	private int _metadataVersion;

	private string _category;

	private string _description;

	private readonly object _lockCookie = new object();

	protected virtual Attribute[]? AttributeArray
	{
		get
		{
			CheckAttributesValid();
			FilterAttributesIfNeeded();
			return _attributes;
		}
		set
		{
			lock (_lockCookie)
			{
				_attributes = value;
				_originalAttributes = value;
				_attributesFiltered = false;
				_attributeCollection = null;
			}
		}
	}

	public virtual AttributeCollection Attributes
	{
		get
		{
			CheckAttributesValid();
			AttributeCollection attributeCollection = _attributeCollection;
			if (attributeCollection == null)
			{
				lock (_lockCookie)
				{
					attributeCollection = (_attributeCollection = CreateAttributeCollection());
				}
			}
			return attributeCollection;
		}
	}

	public virtual string Category => _category ?? (_category = ((CategoryAttribute)Attributes[typeof(CategoryAttribute)]).Category);

	public virtual string Description => _description ?? (_description = ((DescriptionAttribute)Attributes[typeof(DescriptionAttribute)]).Description);

	public virtual bool IsBrowsable => ((BrowsableAttribute)Attributes[typeof(BrowsableAttribute)]).Browsable;

	public virtual string Name => _name ?? "";

	protected virtual int NameHashCode => _nameHash;

	public virtual bool DesignTimeOnly => DesignOnlyAttribute.Yes.Equals(Attributes[typeof(DesignOnlyAttribute)]);

	public virtual string DisplayName
	{
		get
		{
			if (!(Attributes[typeof(DisplayNameAttribute)] is DisplayNameAttribute displayNameAttribute) || displayNameAttribute.IsDefaultAttribute())
			{
				return _displayName;
			}
			return displayNameAttribute.DisplayName;
		}
	}

	protected MemberDescriptor(string name)
		: this(name, null)
	{
	}

	protected MemberDescriptor(string name, Attribute[]? attributes)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException(System.SR.InvalidMemberName, "name");
		}
		_name = name;
		_displayName = name;
		_nameHash = name.GetHashCode();
		if (attributes != null)
		{
			_attributes = attributes;
			_attributesFiltered = false;
		}
		_originalAttributes = _attributes;
	}

	protected MemberDescriptor(MemberDescriptor descr)
	{
		if (descr == null)
		{
			throw new ArgumentNullException("descr");
		}
		_name = descr.Name;
		_displayName = _name;
		_nameHash = _name?.GetHashCode() ?? 0;
		_attributes = new Attribute[descr.Attributes.Count];
		descr.Attributes.CopyTo(_attributes, 0);
		_attributesFiltered = true;
		_originalAttributes = _attributes;
	}

	protected MemberDescriptor(MemberDescriptor oldMemberDescriptor, Attribute[]? newAttributes)
	{
		if (oldMemberDescriptor == null)
		{
			throw new ArgumentNullException("oldMemberDescriptor");
		}
		_name = oldMemberDescriptor.Name;
		_displayName = oldMemberDescriptor.DisplayName;
		_nameHash = _name.GetHashCode();
		List<Attribute> list = new List<Attribute>();
		if (oldMemberDescriptor.Attributes.Count != 0)
		{
			foreach (Attribute attribute in oldMemberDescriptor.Attributes)
			{
				list.Add(attribute);
			}
		}
		if (newAttributes != null)
		{
			foreach (Attribute item2 in newAttributes)
			{
				list.Add(item2);
			}
		}
		_attributes = new Attribute[list.Count];
		list.CopyTo(_attributes, 0);
		_attributesFiltered = false;
		_originalAttributes = _attributes;
	}

	private void CheckAttributesValid()
	{
		if (_attributesFiltered && _metadataVersion != TypeDescriptor.MetadataVersion)
		{
			_attributesFilled = false;
			_attributesFiltered = false;
			_attributeCollection = null;
		}
	}

	protected virtual AttributeCollection CreateAttributeCollection()
	{
		return new AttributeCollection(AttributeArray);
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (this == obj)
		{
			return true;
		}
		if (obj == null)
		{
			return false;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		MemberDescriptor memberDescriptor = (MemberDescriptor)obj;
		FilterAttributesIfNeeded();
		memberDescriptor.FilterAttributesIfNeeded();
		if (memberDescriptor._nameHash != _nameHash)
		{
			return false;
		}
		if (memberDescriptor._category == null != (_category == null) || (_category != null && !memberDescriptor._category.Equals(_category)))
		{
			return false;
		}
		if (memberDescriptor._description == null != (_description == null) || (_description != null && !memberDescriptor._description.Equals(_description)))
		{
			return false;
		}
		if (memberDescriptor._attributes == null != (_attributes == null))
		{
			return false;
		}
		bool result = true;
		if (_attributes != null)
		{
			if (_attributes.Length != memberDescriptor._attributes.Length)
			{
				return false;
			}
			for (int i = 0; i < _attributes.Length; i++)
			{
				if (!_attributes[i].Equals(memberDescriptor._attributes[i]))
				{
					result = false;
					break;
				}
			}
		}
		return result;
	}

	protected virtual void FillAttributes(IList attributeList)
	{
		if (attributeList == null)
		{
			throw new ArgumentNullException("attributeList");
		}
		if (_originalAttributes != null)
		{
			Attribute[] originalAttributes = _originalAttributes;
			foreach (Attribute value in originalAttributes)
			{
				attributeList.Add(value);
			}
		}
	}

	private void FilterAttributesIfNeeded()
	{
		if (_attributesFiltered)
		{
			return;
		}
		List<Attribute> list;
		if (!_attributesFilled)
		{
			list = new List<Attribute>();
			try
			{
				FillAttributes(list);
			}
			catch (Exception)
			{
			}
		}
		else
		{
			list = new List<Attribute>(_attributes);
		}
		Dictionary<object, int> dictionary = new Dictionary<object, int>();
		int num = 0;
		while (num < list.Count)
		{
			int value = -1;
			object obj = list[num]?.TypeId;
			if (obj == null)
			{
				list.RemoveAt(num);
			}
			else if (!dictionary.TryGetValue(obj, out value))
			{
				dictionary.Add(obj, num);
				num++;
			}
			else
			{
				list[value] = list[num];
				list.RemoveAt(num);
			}
		}
		Attribute[] attributes = list.ToArray();
		lock (_lockCookie)
		{
			_attributes = attributes;
			_attributesFiltered = true;
			_attributesFilled = true;
			_metadataVersion = TypeDescriptor.MetadataVersion;
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern", Justification = "This method only looks for public methods by hard-coding publicOnly=true")]
	protected static MethodInfo? FindMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type componentClass, string name, Type[] args, Type returnType)
	{
		return FindMethod(componentClass, name, args, returnType, publicOnly: true);
	}

	protected static MethodInfo? FindMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type componentClass, string name, Type[] args, Type returnType, bool publicOnly)
	{
		if (componentClass == null)
		{
			throw new ArgumentNullException("componentClass");
		}
		MethodInfo methodInfo = null;
		methodInfo = ((!publicOnly) ? componentClass.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, args, null) : componentClass.GetMethod(name, args));
		if (methodInfo != null && !methodInfo.ReturnType.IsEquivalentTo(returnType))
		{
			methodInfo = null;
		}
		return methodInfo;
	}

	public override int GetHashCode()
	{
		return _nameHash;
	}

	protected virtual object? GetInvocationTarget(Type type, object instance)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (instance == null)
		{
			throw new ArgumentNullException("instance");
		}
		return TypeDescriptor.GetAssociation(type, instance);
	}

	protected static ISite? GetSite(object? component)
	{
		return (component as IComponent)?.Site;
	}

	[Obsolete("MemberDescriptor.GetInvokee has been deprecated. Use GetInvocationTarget instead.")]
	protected static object GetInvokee(Type componentClass, object component)
	{
		if (componentClass == null)
		{
			throw new ArgumentNullException("componentClass");
		}
		if (component == null)
		{
			throw new ArgumentNullException("component");
		}
		return TypeDescriptor.GetAssociation(componentClass, component);
	}
}
