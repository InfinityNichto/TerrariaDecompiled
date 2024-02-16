using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using System.Xml.XPath;

namespace System.Xml.Xsl.IlGen;

internal sealed class IteratorDescriptor
{
	private GenerateHelper _helper;

	private IteratorDescriptor _iterParent;

	private Label _lblNext;

	private bool _hasNext;

	private LocalBuilder _locPos;

	private BranchingContext _brctxt;

	private Label _lblBranch;

	private StorageDescriptor _storage;

	public IteratorDescriptor ParentIterator => _iterParent;

	public bool HasLabelNext => _hasNext;

	public LocalBuilder LocalPosition
	{
		get
		{
			return _locPos;
		}
		set
		{
			_locPos = value;
		}
	}

	public bool IsBranching => _brctxt != BranchingContext.None;

	public Label LabelBranch => _lblBranch;

	public BranchingContext CurrentBranchingContext => _brctxt;

	public StorageDescriptor Storage
	{
		get
		{
			return _storage;
		}
		set
		{
			_storage = value;
		}
	}

	public IteratorDescriptor(GenerateHelper helper)
	{
		Init(null, helper);
	}

	public IteratorDescriptor(IteratorDescriptor iterParent)
	{
		Init(iterParent, iterParent._helper);
	}

	[MemberNotNull("_helper")]
	private void Init(IteratorDescriptor iterParent, GenerateHelper helper)
	{
		_helper = helper;
		_iterParent = iterParent;
	}

	public Label GetLabelNext()
	{
		return _lblNext;
	}

	public void SetIterator(Label lblNext, StorageDescriptor storage)
	{
		_lblNext = lblNext;
		_hasNext = true;
		_storage = storage;
	}

	public void SetIterator(IteratorDescriptor iterInfo)
	{
		if (iterInfo.HasLabelNext)
		{
			_lblNext = iterInfo.GetLabelNext();
			_hasNext = true;
		}
		_storage = iterInfo.Storage;
	}

	public void LoopToEnd(Label lblOnEnd)
	{
		if (_hasNext)
		{
			_helper.BranchAndMark(_lblNext, lblOnEnd);
			_hasNext = false;
		}
		_storage = StorageDescriptor.None();
	}

	public void CacheCount()
	{
		PushValue();
		_helper.CallCacheCount(_storage.ItemStorageType);
	}

	public void EnsureNoCache()
	{
		if (_storage.IsCached)
		{
			if (!HasLabelNext)
			{
				EnsureStack();
				_helper.LoadInteger(0);
				_helper.CallCacheItem(_storage.ItemStorageType);
				_storage = StorageDescriptor.Stack(_storage.ItemStorageType, isCached: false);
				return;
			}
			LocalBuilder locBldr = _helper.DeclareLocal("$$$idx", typeof(int));
			EnsureNoStack("$$$cache");
			_helper.LoadInteger(-1);
			_helper.Emit(OpCodes.Stloc, locBldr);
			Label label = _helper.DefineLabel();
			_helper.MarkLabel(label);
			_helper.Emit(OpCodes.Ldloc, locBldr);
			_helper.LoadInteger(1);
			_helper.Emit(OpCodes.Add);
			_helper.Emit(OpCodes.Stloc, locBldr);
			_helper.Emit(OpCodes.Ldloc, locBldr);
			CacheCount();
			_helper.Emit(OpCodes.Bge, GetLabelNext());
			PushValue();
			_helper.Emit(OpCodes.Ldloc, locBldr);
			_helper.CallCacheItem(_storage.ItemStorageType);
			SetIterator(label, StorageDescriptor.Stack(_storage.ItemStorageType, isCached: false));
		}
	}

	public void SetBranching(BranchingContext brctxt, Label lblBranch)
	{
		_brctxt = brctxt;
		_lblBranch = lblBranch;
	}

	public void PushValue()
	{
		switch (_storage.Location)
		{
		case ItemLocation.Stack:
			_helper.Emit(OpCodes.Dup);
			break;
		case ItemLocation.Parameter:
			_helper.LoadParameter(_storage.ParameterLocation);
			break;
		case ItemLocation.Local:
			_helper.Emit(OpCodes.Ldloc, _storage.LocalLocation);
			break;
		case ItemLocation.Current:
		{
			CurrentContext currentLocation = _storage.CurrentLocation;
			_helper.Emit(OpCodes.Ldloca, currentLocation.Local);
			_helper.Call(currentLocation.CurrentMethod);
			break;
		}
		}
	}

	public void EnsureStack()
	{
		switch (_storage.Location)
		{
		case ItemLocation.Stack:
			return;
		case ItemLocation.Parameter:
		case ItemLocation.Local:
		case ItemLocation.Current:
			PushValue();
			break;
		case ItemLocation.Global:
			_helper.LoadQueryRuntime();
			_helper.Call(_storage.GlobalLocation);
			break;
		}
		_storage = _storage.ToStack();
	}

	public void EnsureNoStack(string locName)
	{
		if (_storage.Location == ItemLocation.Stack)
		{
			EnsureLocal(locName);
		}
	}

	public void EnsureLocal(string locName)
	{
		if (_storage.Location != ItemLocation.Local)
		{
			if (_storage.IsCached)
			{
				EnsureLocal(_helper.DeclareLocal(locName, typeof(IList<>).MakeGenericType(_storage.ItemStorageType)));
			}
			else
			{
				EnsureLocal(_helper.DeclareLocal(locName, _storage.ItemStorageType));
			}
		}
	}

	public void EnsureLocal(LocalBuilder bldr)
	{
		if (_storage.LocalLocation != bldr)
		{
			EnsureStack();
			_helper.Emit(OpCodes.Stloc, bldr);
			_storage = _storage.ToLocal(bldr);
		}
	}

	public void DiscardStack()
	{
		if (_storage.Location == ItemLocation.Stack)
		{
			_helper.Emit(OpCodes.Pop);
			_storage = StorageDescriptor.None();
		}
	}

	public void EnsureStackNoCache()
	{
		EnsureNoCache();
		EnsureStack();
	}

	public void EnsureNoStackNoCache(string locName)
	{
		EnsureNoCache();
		EnsureNoStack(locName);
	}

	public void EnsureLocalNoCache(string locName)
	{
		EnsureNoCache();
		EnsureLocal(locName);
	}

	public void EnsureLocalNoCache(LocalBuilder bldr)
	{
		EnsureNoCache();
		EnsureLocal(bldr);
	}

	public void EnsureItemStorageType(XmlQueryType xmlType, Type storageTypeDest)
	{
		if (!(_storage.ItemStorageType == storageTypeDest))
		{
			if (!_storage.IsCached)
			{
				goto IL_0087;
			}
			if (_storage.ItemStorageType == typeof(XPathNavigator))
			{
				EnsureStack();
				_helper.Call(XmlILMethods.NavsToItems);
			}
			else
			{
				if (!(storageTypeDest == typeof(XPathNavigator)))
				{
					goto IL_0087;
				}
				EnsureStack();
				_helper.Call(XmlILMethods.ItemsToNavs);
			}
		}
		goto IL_014d;
		IL_014d:
		_storage = _storage.ToStorageType(storageTypeDest);
		return;
		IL_0087:
		EnsureStackNoCache();
		if (_storage.ItemStorageType == typeof(XPathItem))
		{
			if (storageTypeDest == typeof(XPathNavigator))
			{
				_helper.Emit(OpCodes.Castclass, typeof(XPathNavigator));
			}
			else
			{
				_helper.CallValueAs(storageTypeDest);
			}
		}
		else if (!(_storage.ItemStorageType == typeof(XPathNavigator)))
		{
			_helper.LoadInteger(_helper.StaticData.DeclareXmlType(xmlType));
			_helper.LoadQueryRuntime();
			_helper.Call(XmlILMethods.StorageMethods[_storage.ItemStorageType].ToAtomicValue);
		}
		goto IL_014d;
	}
}
