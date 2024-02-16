namespace System.Xml.Serialization;

internal sealed class ArrayMapping : TypeMapping
{
	private ElementAccessor[] _elements;

	private ElementAccessor[] _sortedElements;

	private ArrayMapping _next;

	private StructMapping _topLevelMapping;

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
			AccessorMapping.SortMostToLeastDerived(_sortedElements);
			return _sortedElements;
		}
	}

	internal ArrayMapping Next
	{
		get
		{
			return _next;
		}
		set
		{
			_next = value;
		}
	}

	internal StructMapping TopLevelMapping
	{
		get
		{
			return _topLevelMapping;
		}
		set
		{
			_topLevelMapping = value;
		}
	}
}
