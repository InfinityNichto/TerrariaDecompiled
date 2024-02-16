using System.Collections.ObjectModel;

namespace System.Runtime.Serialization;

public class ExportOptions
{
	private Collection<Type> _knownTypes;

	public Collection<Type> KnownTypes
	{
		get
		{
			if (_knownTypes == null)
			{
				_knownTypes = new Collection<Type>();
			}
			return _knownTypes;
		}
	}
}
