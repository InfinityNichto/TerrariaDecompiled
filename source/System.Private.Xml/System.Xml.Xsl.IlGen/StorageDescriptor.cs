using System.Reflection;
using System.Reflection.Emit;

namespace System.Xml.Xsl.IlGen;

internal struct StorageDescriptor
{
	private ItemLocation _location;

	private object _locationObject;

	private Type _itemStorageType;

	private bool _isCached;

	public ItemLocation Location => _location;

	public int ParameterLocation => (int)_locationObject;

	public LocalBuilder LocalLocation => _locationObject as LocalBuilder;

	public CurrentContext CurrentLocation => _locationObject as CurrentContext;

	public MethodInfo GlobalLocation => _locationObject as MethodInfo;

	public bool IsCached => _isCached;

	public Type ItemStorageType => _itemStorageType;

	public static StorageDescriptor None()
	{
		return default(StorageDescriptor);
	}

	public static StorageDescriptor Stack(Type itemStorageType, bool isCached)
	{
		StorageDescriptor result = default(StorageDescriptor);
		result._location = ItemLocation.Stack;
		result._itemStorageType = itemStorageType;
		result._isCached = isCached;
		return result;
	}

	public static StorageDescriptor Parameter(int paramIndex, Type itemStorageType, bool isCached)
	{
		StorageDescriptor result = default(StorageDescriptor);
		result._location = ItemLocation.Parameter;
		result._locationObject = paramIndex;
		result._itemStorageType = itemStorageType;
		result._isCached = isCached;
		return result;
	}

	public static StorageDescriptor Local(LocalBuilder loc, Type itemStorageType, bool isCached)
	{
		StorageDescriptor result = default(StorageDescriptor);
		result._location = ItemLocation.Local;
		result._locationObject = loc;
		result._itemStorageType = itemStorageType;
		result._isCached = isCached;
		return result;
	}

	public static StorageDescriptor Current(LocalBuilder locIter, MethodInfo currentMethod, Type itemStorageType)
	{
		StorageDescriptor result = default(StorageDescriptor);
		result._location = ItemLocation.Current;
		result._locationObject = new CurrentContext(locIter, currentMethod);
		result._itemStorageType = itemStorageType;
		return result;
	}

	public static StorageDescriptor Global(MethodInfo methGlobal, Type itemStorageType, bool isCached)
	{
		StorageDescriptor result = default(StorageDescriptor);
		result._location = ItemLocation.Global;
		result._locationObject = methGlobal;
		result._itemStorageType = itemStorageType;
		result._isCached = isCached;
		return result;
	}

	public StorageDescriptor ToStack()
	{
		return Stack(_itemStorageType, _isCached);
	}

	public StorageDescriptor ToLocal(LocalBuilder loc)
	{
		return Local(loc, _itemStorageType, _isCached);
	}

	public StorageDescriptor ToStorageType(Type itemStorageType)
	{
		StorageDescriptor result = this;
		result._itemStorageType = itemStorageType;
		return result;
	}
}
