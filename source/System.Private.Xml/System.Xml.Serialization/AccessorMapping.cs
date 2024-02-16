using System.Collections.Generic;

namespace System.Xml.Serialization;

internal abstract class AccessorMapping : Mapping
{
	internal sealed class AccessorComparer : IComparer<ElementAccessor>
	{
		public int Compare(ElementAccessor a1, ElementAccessor a2)
		{
			if (a1 == a2)
			{
				return 0;
			}
			int weight = a1.Mapping.TypeDesc.Weight;
			int weight2 = a2.Mapping.TypeDesc.Weight;
			if (weight == weight2)
			{
				return 0;
			}
			if (weight < weight2)
			{
				return 1;
			}
			return -1;
		}
	}

	private TypeDesc _typeDesc;

	private AttributeAccessor _attribute;

	private ElementAccessor[] _elements;

	private ElementAccessor[] _sortedElements;

	private TextAccessor _text;

	private ChoiceIdentifierAccessor _choiceIdentifier;

	private XmlnsAccessor _xmlns;

	private bool _ignore;

	internal bool IsAttribute => _attribute != null;

	internal bool IsText
	{
		get
		{
			if (_text != null)
			{
				if (_elements != null)
				{
					return _elements.Length == 0;
				}
				return true;
			}
			return false;
		}
	}

	internal bool IsParticle
	{
		get
		{
			if (_elements != null)
			{
				return _elements.Length != 0;
			}
			return false;
		}
	}

	internal TypeDesc TypeDesc
	{
		get
		{
			return _typeDesc;
		}
		set
		{
			_typeDesc = value;
		}
	}

	internal AttributeAccessor Attribute
	{
		get
		{
			return _attribute;
		}
		set
		{
			_attribute = value;
		}
	}

	internal ElementAccessor[] Elements
	{
		get
		{
			return _elements;
		}
		set
		{
			_elements = value;
			_sortedElements = null;
		}
	}

	internal ElementAccessor[] ElementsSortedByDerivation
	{
		get
		{
			if (_sortedElements != null)
			{
				return _sortedElements;
			}
			if (_elements == null)
			{
				return null;
			}
			_sortedElements = new ElementAccessor[_elements.Length];
			Array.Copy(_elements, _sortedElements, _elements.Length);
			SortMostToLeastDerived(_sortedElements);
			return _sortedElements;
		}
	}

	internal TextAccessor Text
	{
		get
		{
			return _text;
		}
		set
		{
			_text = value;
		}
	}

	internal ChoiceIdentifierAccessor ChoiceIdentifier
	{
		get
		{
			return _choiceIdentifier;
		}
		set
		{
			_choiceIdentifier = value;
		}
	}

	internal XmlnsAccessor Xmlns
	{
		get
		{
			return _xmlns;
		}
		set
		{
			_xmlns = value;
		}
	}

	internal bool Ignore
	{
		get
		{
			return _ignore;
		}
		set
		{
			_ignore = value;
		}
	}

	internal Accessor Accessor
	{
		get
		{
			if (_xmlns != null)
			{
				return _xmlns;
			}
			if (_attribute != null)
			{
				return _attribute;
			}
			if (_elements != null && _elements.Length != 0)
			{
				return _elements[0];
			}
			return _text;
		}
	}

	internal AccessorMapping()
	{
	}

	protected AccessorMapping(AccessorMapping mapping)
		: base(mapping)
	{
		_typeDesc = mapping._typeDesc;
		_attribute = mapping._attribute;
		_elements = mapping._elements;
		_sortedElements = mapping._sortedElements;
		_text = mapping._text;
		_choiceIdentifier = mapping._choiceIdentifier;
		_xmlns = mapping._xmlns;
		_ignore = mapping._ignore;
	}

	internal static void SortMostToLeastDerived(ElementAccessor[] elements)
	{
		Array.Sort(elements, new AccessorComparer());
	}

	internal static bool ElementsMatch(ElementAccessor[] a, ElementAccessor[] b)
	{
		if (a == null)
		{
			if (b == null)
			{
				return true;
			}
			return false;
		}
		if (b == null)
		{
			return false;
		}
		if (a.Length != b.Length)
		{
			return false;
		}
		for (int i = 0; i < a.Length; i++)
		{
			if (a[i].Name != b[i].Name || a[i].Namespace != b[i].Namespace || a[i].Form != b[i].Form || a[i].IsNullable != b[i].IsNullable)
			{
				return false;
			}
		}
		return true;
	}

	internal bool Match(AccessorMapping mapping)
	{
		if (Elements != null && Elements.Length != 0)
		{
			if (!ElementsMatch(Elements, mapping.Elements))
			{
				return false;
			}
			if (Text == null)
			{
				return mapping.Text == null;
			}
		}
		if (Attribute != null)
		{
			if (mapping.Attribute == null)
			{
				return false;
			}
			if (Attribute.Name == mapping.Attribute.Name && Attribute.Namespace == mapping.Attribute.Namespace)
			{
				return Attribute.Form == mapping.Attribute.Form;
			}
			return false;
		}
		if (Text != null)
		{
			return mapping.Text != null;
		}
		return mapping.Accessor == null;
	}
}
