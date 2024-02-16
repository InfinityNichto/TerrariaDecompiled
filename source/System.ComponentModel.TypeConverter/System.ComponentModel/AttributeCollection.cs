using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.ComponentModel;

public class AttributeCollection : ICollection, IEnumerable
{
	private struct AttributeEntry
	{
		public Type type;

		public int index;
	}

	public static readonly AttributeCollection Empty = new AttributeCollection((Attribute[]?)null);

	private static Hashtable s_defaultAttributes;

	private readonly Attribute[] _attributes;

	private static readonly object s_internalSyncObject = new object();

	private AttributeEntry[] _foundAttributeTypes;

	private int _index;

	protected internal virtual Attribute[] Attributes => _attributes;

	public int Count => Attributes.Length;

	public virtual Attribute this[int index] => Attributes[index];

	public virtual Attribute? this[[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicFields)] Type attributeType]
	{
		get
		{
			if (attributeType == null)
			{
				throw new ArgumentNullException("attributeType");
			}
			lock (s_internalSyncObject)
			{
				if (_foundAttributeTypes == null)
				{
					_foundAttributeTypes = new AttributeEntry[5];
				}
				int i;
				for (i = 0; i < 5; i++)
				{
					if (_foundAttributeTypes[i].type == attributeType)
					{
						int index = _foundAttributeTypes[i].index;
						if (index != -1)
						{
							return Attributes[index];
						}
						return GetDefaultAttribute(attributeType);
					}
					if (_foundAttributeTypes[i].type == null)
					{
						break;
					}
				}
				i = _index++;
				if (_index >= 5)
				{
					_index = 0;
				}
				_foundAttributeTypes[i].type = attributeType;
				int num = Attributes.Length;
				for (int j = 0; j < num; j++)
				{
					Attribute attribute = Attributes[j];
					Type type = attribute.GetType();
					if (type == attributeType)
					{
						_foundAttributeTypes[i].index = j;
						return attribute;
					}
				}
				for (int k = 0; k < num; k++)
				{
					Attribute attribute2 = Attributes[k];
					if (attributeType.IsInstanceOfType(attribute2))
					{
						_foundAttributeTypes[i].index = k;
						return attribute2;
					}
				}
				_foundAttributeTypes[i].index = -1;
				return GetDefaultAttribute(attributeType);
			}
		}
	}

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => this;

	int ICollection.Count => Count;

	public AttributeCollection(params Attribute[]? attributes)
	{
		_attributes = attributes ?? Array.Empty<Attribute>();
		for (int i = 0; i < _attributes.Length; i++)
		{
			if (_attributes[i] == null)
			{
				throw new ArgumentNullException("attributes");
			}
		}
	}

	protected AttributeCollection()
		: this(Array.Empty<Attribute>())
	{
	}

	public static AttributeCollection FromExisting(AttributeCollection existing, params Attribute[]? newAttributes)
	{
		if (existing == null)
		{
			throw new ArgumentNullException("existing");
		}
		if (newAttributes == null)
		{
			newAttributes = Array.Empty<Attribute>();
		}
		Attribute[] array = new Attribute[existing.Count + newAttributes.Length];
		int count = existing.Count;
		existing.CopyTo(array, 0);
		for (int i = 0; i < newAttributes.Length; i++)
		{
			if (newAttributes[i] == null)
			{
				throw new ArgumentNullException("newAttributes");
			}
			bool flag = false;
			for (int j = 0; j < existing.Count; j++)
			{
				if (array[j].TypeId.Equals(newAttributes[i].TypeId))
				{
					flag = true;
					array[j] = newAttributes[i];
					break;
				}
			}
			if (!flag)
			{
				array[count++] = newAttributes[i];
			}
		}
		Attribute[] array2;
		if (count < array.Length)
		{
			array2 = new Attribute[count];
			Array.Copy(array, array2, count);
		}
		else
		{
			array2 = array;
		}
		return new AttributeCollection(array2);
	}

	[RequiresUnreferencedCode("The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	public bool Contains(Attribute? attribute)
	{
		if (attribute == null)
		{
			return false;
		}
		return this[attribute.GetType()]?.Equals(attribute) ?? false;
	}

	[RequiresUnreferencedCode("The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
	public bool Contains(Attribute[]? attributes)
	{
		if (attributes == null)
		{
			return true;
		}
		for (int i = 0; i < attributes.Length; i++)
		{
			if (!Contains(attributes[i]))
			{
				return false;
			}
		}
		return true;
	}

	protected Attribute? GetDefaultAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicFields)] Type attributeType)
	{
		if (attributeType == null)
		{
			throw new ArgumentNullException("attributeType");
		}
		lock (s_internalSyncObject)
		{
			if (s_defaultAttributes == null)
			{
				s_defaultAttributes = new Hashtable();
			}
			if (s_defaultAttributes.ContainsKey(attributeType))
			{
				return (Attribute)s_defaultAttributes[attributeType];
			}
			Attribute attribute = null;
			Type reflectionType = TypeDescriptor.GetReflectionType(attributeType);
			FieldInfo field = reflectionType.GetField("Default", BindingFlags.Static | BindingFlags.Public | BindingFlags.GetField);
			if (field != null && field.IsStatic)
			{
				attribute = (Attribute)field.GetValue(null);
			}
			else
			{
				ConstructorInfo constructor = reflectionType.UnderlyingSystemType.GetConstructor(Type.EmptyTypes);
				if (constructor != null)
				{
					attribute = (Attribute)constructor.Invoke(Array.Empty<object>());
					if (!attribute.IsDefaultAttribute())
					{
						attribute = null;
					}
				}
			}
			s_defaultAttributes[attributeType] = attribute;
			return attribute;
		}
	}

	public IEnumerator GetEnumerator()
	{
		return Attributes.GetEnumerator();
	}

	public bool Matches(Attribute? attribute)
	{
		for (int i = 0; i < Attributes.Length; i++)
		{
			if (Attributes[i].Match(attribute))
			{
				return true;
			}
		}
		return false;
	}

	public bool Matches(Attribute[]? attributes)
	{
		if (attributes == null)
		{
			return true;
		}
		for (int i = 0; i < attributes.Length; i++)
		{
			if (!Matches(attributes[i]))
			{
				return false;
			}
		}
		return true;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void CopyTo(Array array, int index)
	{
		Array.Copy(Attributes, 0, array, index, Attributes.Length);
	}
}
