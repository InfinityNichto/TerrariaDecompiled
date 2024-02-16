using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Data;

internal abstract class RBTree<K> : IEnumerable
{
	private enum NodeColor
	{
		red,
		black
	}

	private struct Node
	{
		internal int _selfId;

		internal int _leftId;

		internal int _rightId;

		internal int _parentId;

		internal int _nextId;

		internal int _subTreeSize;

		internal K _keyOfNode;

		internal NodeColor _nodeColor;
	}

	private readonly struct NodePath
	{
		internal readonly int _nodeID;

		internal readonly int _mainTreeNodeID;

		internal NodePath(int nodeID, int mainTreeNodeID)
		{
			_nodeID = nodeID;
			_mainTreeNodeID = mainTreeNodeID;
		}
	}

	private sealed class TreePage
	{
		internal readonly Node[] _slots;

		internal readonly int[] _slotMap;

		private int _inUseCount;

		private int _pageId;

		private int _nextFreeSlotLine;

		internal int InUseCount
		{
			get
			{
				return _inUseCount;
			}
			set
			{
				_inUseCount = value;
			}
		}

		internal int PageId
		{
			get
			{
				return _pageId;
			}
			set
			{
				_pageId = value;
			}
		}

		internal TreePage(int size)
		{
			if (size > 65536)
			{
				throw ExceptionBuilder.InternalRBTreeError(RBTreeError.InvalidPageSize);
			}
			_slots = new Node[size];
			_slotMap = new int[(size + 32 - 1) / 32];
		}

		internal int AllocSlot(RBTree<K> tree)
		{
			int num = 0;
			int num2 = 0;
			int num3 = -1;
			if (_inUseCount < _slots.Length)
			{
				for (num = _nextFreeSlotLine; num < _slotMap.Length; num++)
				{
					if ((uint)_slotMap[num] < uint.MaxValue)
					{
						num3 = 0;
						num2 = ~_slotMap[num] & (_slotMap[num] + 1);
						_slotMap[num] |= num2;
						_inUseCount++;
						if (_inUseCount == _slots.Length)
						{
							tree.MarkPageFull(this);
						}
						tree._inUseNodeCount++;
						num3 = RBTree<K>.GetIntValueFromBitMap((uint)num2);
						_nextFreeSlotLine = num;
						num3 = num * 32 + num3;
						break;
					}
				}
				if (num3 == -1 && _nextFreeSlotLine != 0)
				{
					_nextFreeSlotLine = 0;
					num3 = AllocSlot(tree);
				}
			}
			return num3;
		}
	}

	internal struct RBTreeEnumerator : IEnumerator<K>, IDisposable, IEnumerator
	{
		private readonly RBTree<K> _tree;

		private readonly int _version;

		private int _index;

		private int _mainTreeNodeId;

		private K _current;

		public K Current => _current;

		object IEnumerator.Current => Current;

		internal RBTreeEnumerator(RBTree<K> tree)
		{
			_tree = tree;
			_version = tree._version;
			_index = 0;
			_mainTreeNodeId = tree.root;
			_current = default(K);
		}

		internal RBTreeEnumerator(RBTree<K> tree, int position)
		{
			_tree = tree;
			_version = tree._version;
			if (position == 0)
			{
				_index = 0;
				_mainTreeNodeId = tree.root;
			}
			else
			{
				_index = tree.ComputeNodeByIndex(position - 1, out _mainTreeNodeId);
				if (_index == 0)
				{
					throw ExceptionBuilder.InternalRBTreeError(RBTreeError.IndexOutOFRangeinGetNodeByIndex);
				}
			}
			_current = default(K);
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			if (_version != _tree._version)
			{
				throw ExceptionBuilder.EnumeratorModified();
			}
			bool result = _tree.Successor(ref _index, ref _mainTreeNodeId);
			_current = _tree.Key(_index);
			return result;
		}

		void IEnumerator.Reset()
		{
			if (_version != _tree._version)
			{
				throw ExceptionBuilder.EnumeratorModified();
			}
			_index = 0;
			_mainTreeNodeId = _tree.root;
			_current = default(K);
		}
	}

	private TreePage[] _pageTable;

	private int[] _pageTableMap;

	private int _inUsePageCount;

	private int _nextFreePageLine;

	public int root;

	private int _version;

	private int _inUseNodeCount;

	private int _inUseSatelliteTreeCount;

	private readonly TreeAccessMethod _accessMethod;

	public int Count => _inUseNodeCount - 1;

	public bool HasDuplicates => _inUseSatelliteTreeCount != 0;

	public K this[int index] => Key(GetNodeByIndex(index)._nodeID);

	protected abstract int CompareNode(K record1, K record2);

	protected abstract int CompareSateliteTreeNode(K record1, K record2);

	protected RBTree(TreeAccessMethod accessMethod)
	{
		_accessMethod = accessMethod;
		InitTree();
	}

	[MemberNotNull("_pageTable")]
	[MemberNotNull("_pageTableMap")]
	private void InitTree()
	{
		root = 0;
		_pageTable = new TreePage[32];
		_pageTableMap = new int[(_pageTable.Length + 32 - 1) / 32];
		_inUsePageCount = 0;
		_nextFreePageLine = 0;
		AllocPage(32);
		_pageTable[0]._slots[0]._nodeColor = NodeColor.black;
		_pageTable[0]._slotMap[0] = 1;
		_pageTable[0].InUseCount = 1;
		_inUseNodeCount = 1;
		_inUseSatelliteTreeCount = 0;
	}

	private void FreePage(TreePage page)
	{
		MarkPageFree(page);
		_pageTable[page.PageId] = null;
		_inUsePageCount--;
	}

	private TreePage AllocPage(int size)
	{
		int num = GetIndexOfPageWithFreeSlot(allocatedPage: false);
		if (num != -1)
		{
			_pageTable[num] = new TreePage(size);
			_nextFreePageLine = num / 32;
		}
		else
		{
			TreePage[] array = new TreePage[_pageTable.Length * 2];
			Array.Copy(_pageTable, array, _pageTable.Length);
			int[] array2 = new int[(array.Length + 32 - 1) / 32];
			Array.Copy(_pageTableMap, array2, _pageTableMap.Length);
			_nextFreePageLine = _pageTableMap.Length;
			num = _pageTable.Length;
			_pageTable = array;
			_pageTableMap = array2;
			_pageTable[num] = new TreePage(size);
		}
		_pageTable[num].PageId = num;
		_inUsePageCount++;
		return _pageTable[num];
	}

	private void MarkPageFull(TreePage page)
	{
		_pageTableMap[page.PageId / 32] |= 1 << page.PageId % 32;
	}

	private void MarkPageFree(TreePage page)
	{
		_pageTableMap[page.PageId / 32] &= ~(1 << page.PageId % 32);
	}

	private static int GetIntValueFromBitMap(uint bitMap)
	{
		int num = 0;
		if ((bitMap & 0xFFFF0000u) != 0)
		{
			num += 16;
			bitMap >>= 16;
		}
		if ((bitMap & 0xFF00u) != 0)
		{
			num += 8;
			bitMap >>= 8;
		}
		if ((bitMap & 0xF0u) != 0)
		{
			num += 4;
			bitMap >>= 4;
		}
		if ((bitMap & 0xCu) != 0)
		{
			num += 2;
			bitMap >>= 2;
		}
		if ((bitMap & 2u) != 0)
		{
			num++;
		}
		return num;
	}

	private void FreeNode(int nodeId)
	{
		TreePage treePage = _pageTable[nodeId >> 16];
		int num = nodeId & 0xFFFF;
		treePage._slots[num] = default(Node);
		treePage._slotMap[num / 32] &= ~(1 << num % 32);
		treePage.InUseCount--;
		_inUseNodeCount--;
		if (treePage.InUseCount == 0)
		{
			FreePage(treePage);
		}
		else if (treePage.InUseCount == treePage._slots.Length - 1)
		{
			MarkPageFree(treePage);
		}
	}

	private int GetIndexOfPageWithFreeSlot(bool allocatedPage)
	{
		int i = _nextFreePageLine;
		int result = -1;
		for (; i < _pageTableMap.Length; i++)
		{
			if ((uint)_pageTableMap[i] >= uint.MaxValue)
			{
				continue;
			}
			uint num = (uint)_pageTableMap[i];
			while ((num ^ 0xFFFFFFFFu) != 0)
			{
				uint num2 = ~num & (num + 1);
				if ((_pageTableMap[i] & num2) != 0L)
				{
					throw ExceptionBuilder.InternalRBTreeError(RBTreeError.PagePositionInSlotInUse);
				}
				result = i * 32 + GetIntValueFromBitMap(num2);
				if (allocatedPage)
				{
					if (_pageTable[result] != null)
					{
						return result;
					}
				}
				else if (_pageTable[result] == null)
				{
					return result;
				}
				result = -1;
				num |= num2;
			}
		}
		if (_nextFreePageLine != 0)
		{
			_nextFreePageLine = 0;
			result = GetIndexOfPageWithFreeSlot(allocatedPage);
		}
		return result;
	}

	private int GetNewNode(K key)
	{
		TreePage treePage = null;
		int indexOfPageWithFreeSlot = GetIndexOfPageWithFreeSlot(allocatedPage: true);
		treePage = ((indexOfPageWithFreeSlot != -1) ? _pageTable[indexOfPageWithFreeSlot] : ((_inUsePageCount < 4) ? AllocPage(32) : ((_inUsePageCount < 32) ? AllocPage(256) : ((_inUsePageCount < 128) ? AllocPage(1024) : ((_inUsePageCount < 4096) ? AllocPage(4096) : ((_inUsePageCount >= 32768) ? AllocPage(65536) : AllocPage(8192)))))));
		int num = treePage.AllocSlot(this);
		if (num == -1)
		{
			throw ExceptionBuilder.InternalRBTreeError(RBTreeError.NoFreeSlots);
		}
		treePage._slots[num]._selfId = (treePage.PageId << 16) | num;
		treePage._slots[num]._subTreeSize = 1;
		treePage._slots[num]._keyOfNode = key;
		return treePage._slots[num]._selfId;
	}

	private int Successor(int x_id)
	{
		if (Right(x_id) != 0)
		{
			return Minimum(Right(x_id));
		}
		int num = Parent(x_id);
		while (num != 0 && x_id == Right(num))
		{
			x_id = num;
			num = Parent(num);
		}
		return num;
	}

	private bool Successor(ref int nodeId, ref int mainTreeNodeId)
	{
		if (nodeId == 0)
		{
			nodeId = Minimum(mainTreeNodeId);
			mainTreeNodeId = 0;
		}
		else
		{
			nodeId = Successor(nodeId);
			if (nodeId == 0 && mainTreeNodeId != 0)
			{
				nodeId = Successor(mainTreeNodeId);
				mainTreeNodeId = 0;
			}
		}
		if (nodeId != 0)
		{
			if (Next(nodeId) != 0)
			{
				if (mainTreeNodeId != 0)
				{
					throw ExceptionBuilder.InternalRBTreeError(RBTreeError.NestedSatelliteTreeEnumerator);
				}
				mainTreeNodeId = nodeId;
				nodeId = Minimum(Next(nodeId));
			}
			return true;
		}
		return false;
	}

	private int Minimum(int x_id)
	{
		while (Left(x_id) != 0)
		{
			x_id = Left(x_id);
		}
		return x_id;
	}

	private int LeftRotate(int root_id, int x_id, int mainTreeNode)
	{
		int num = Right(x_id);
		SetRight(x_id, Left(num));
		if (Left(num) != 0)
		{
			SetParent(Left(num), x_id);
		}
		SetParent(num, Parent(x_id));
		if (Parent(x_id) == 0)
		{
			if (root_id == 0)
			{
				root = num;
			}
			else
			{
				SetNext(mainTreeNode, num);
				SetKey(mainTreeNode, Key(num));
				root_id = num;
			}
		}
		else if (x_id == Left(Parent(x_id)))
		{
			SetLeft(Parent(x_id), num);
		}
		else
		{
			SetRight(Parent(x_id), num);
		}
		SetLeft(num, x_id);
		SetParent(x_id, num);
		if (x_id != 0)
		{
			SetSubTreeSize(x_id, SubTreeSize(Left(x_id)) + SubTreeSize(Right(x_id)) + ((Next(x_id) == 0) ? 1 : SubTreeSize(Next(x_id))));
		}
		if (num != 0)
		{
			SetSubTreeSize(num, SubTreeSize(Left(num)) + SubTreeSize(Right(num)) + ((Next(num) == 0) ? 1 : SubTreeSize(Next(num))));
		}
		return root_id;
	}

	private int RightRotate(int root_id, int x_id, int mainTreeNode)
	{
		int num = Left(x_id);
		SetLeft(x_id, Right(num));
		if (Right(num) != 0)
		{
			SetParent(Right(num), x_id);
		}
		SetParent(num, Parent(x_id));
		if (Parent(x_id) == 0)
		{
			if (root_id == 0)
			{
				root = num;
			}
			else
			{
				SetNext(mainTreeNode, num);
				SetKey(mainTreeNode, Key(num));
				root_id = num;
			}
		}
		else if (x_id == Left(Parent(x_id)))
		{
			SetLeft(Parent(x_id), num);
		}
		else
		{
			SetRight(Parent(x_id), num);
		}
		SetRight(num, x_id);
		SetParent(x_id, num);
		if (x_id != 0)
		{
			SetSubTreeSize(x_id, SubTreeSize(Left(x_id)) + SubTreeSize(Right(x_id)) + ((Next(x_id) == 0) ? 1 : SubTreeSize(Next(x_id))));
		}
		if (num != 0)
		{
			SetSubTreeSize(num, SubTreeSize(Left(num)) + SubTreeSize(Right(num)) + ((Next(num) == 0) ? 1 : SubTreeSize(Next(num))));
		}
		return root_id;
	}

	private int RBInsert(int root_id, int x_id, int mainTreeNodeID, int position, bool append)
	{
		_version++;
		int num = 0;
		int num2 = ((root_id == 0) ? root : root_id);
		if (_accessMethod == TreeAccessMethod.KEY_SEARCH_AND_INDEX && !append)
		{
			while (num2 != 0)
			{
				IncreaseSize(num2);
				num = num2;
				int num3 = ((root_id == 0) ? CompareNode(Key(x_id), Key(num2)) : CompareSateliteTreeNode(Key(x_id), Key(num2)));
				if (num3 < 0)
				{
					num2 = Left(num2);
					continue;
				}
				if (num3 > 0)
				{
					num2 = Right(num2);
					continue;
				}
				if (root_id != 0)
				{
					throw ExceptionBuilder.InternalRBTreeError(RBTreeError.InvalidStateinInsert);
				}
				if (Next(num2) != 0)
				{
					root_id = RBInsert(Next(num2), x_id, num2, -1, append: false);
					SetKey(num2, Key(Next(num2)));
				}
				else
				{
					int num4 = 0;
					num4 = GetNewNode(Key(num2));
					_inUseSatelliteTreeCount++;
					SetNext(num4, num2);
					SetColor(num4, color(num2));
					SetParent(num4, Parent(num2));
					SetLeft(num4, Left(num2));
					SetRight(num4, Right(num2));
					if (Left(Parent(num2)) == num2)
					{
						SetLeft(Parent(num2), num4);
					}
					else if (Right(Parent(num2)) == num2)
					{
						SetRight(Parent(num2), num4);
					}
					if (Left(num2) != 0)
					{
						SetParent(Left(num2), num4);
					}
					if (Right(num2) != 0)
					{
						SetParent(Right(num2), num4);
					}
					if (root == num2)
					{
						root = num4;
					}
					SetColor(num2, NodeColor.black);
					SetParent(num2, 0);
					SetLeft(num2, 0);
					SetRight(num2, 0);
					int size = SubTreeSize(num2);
					SetSubTreeSize(num2, 1);
					root_id = RBInsert(num2, x_id, num4, -1, append: false);
					SetSubTreeSize(num4, size);
				}
				return root_id;
			}
		}
		else
		{
			if (!(_accessMethod == TreeAccessMethod.INDEX_ONLY || append))
			{
				throw ExceptionBuilder.InternalRBTreeError(RBTreeError.UnsupportedAccessMethod1);
			}
			if (position == -1)
			{
				position = SubTreeSize(root);
			}
			while (num2 != 0)
			{
				IncreaseSize(num2);
				num = num2;
				int num5 = position - SubTreeSize(Left(num));
				if (num5 <= 0)
				{
					num2 = Left(num2);
					continue;
				}
				num2 = Right(num2);
				if (num2 != 0)
				{
					position = num5 - 1;
				}
			}
		}
		SetParent(x_id, num);
		if (num == 0)
		{
			if (root_id == 0)
			{
				root = x_id;
			}
			else
			{
				SetNext(mainTreeNodeID, x_id);
				SetKey(mainTreeNodeID, Key(x_id));
				root_id = x_id;
			}
		}
		else
		{
			int num6 = 0;
			if (_accessMethod == TreeAccessMethod.KEY_SEARCH_AND_INDEX)
			{
				num6 = ((root_id == 0) ? CompareNode(Key(x_id), Key(num)) : CompareSateliteTreeNode(Key(x_id), Key(num)));
			}
			else
			{
				if (_accessMethod != TreeAccessMethod.INDEX_ONLY)
				{
					throw ExceptionBuilder.InternalRBTreeError(RBTreeError.UnsupportedAccessMethod2);
				}
				num6 = ((position > 0) ? 1 : (-1));
			}
			if (num6 < 0)
			{
				SetLeft(num, x_id);
			}
			else
			{
				SetRight(num, x_id);
			}
		}
		SetLeft(x_id, 0);
		SetRight(x_id, 0);
		SetColor(x_id, NodeColor.red);
		num2 = x_id;
		while (color(Parent(x_id)) == NodeColor.red)
		{
			if (Parent(x_id) == Left(Parent(Parent(x_id))))
			{
				num = Right(Parent(Parent(x_id)));
				if (color(num) == NodeColor.red)
				{
					SetColor(Parent(x_id), NodeColor.black);
					SetColor(num, NodeColor.black);
					SetColor(Parent(Parent(x_id)), NodeColor.red);
					x_id = Parent(Parent(x_id));
					continue;
				}
				if (x_id == Right(Parent(x_id)))
				{
					x_id = Parent(x_id);
					root_id = LeftRotate(root_id, x_id, mainTreeNodeID);
				}
				SetColor(Parent(x_id), NodeColor.black);
				SetColor(Parent(Parent(x_id)), NodeColor.red);
				root_id = RightRotate(root_id, Parent(Parent(x_id)), mainTreeNodeID);
				continue;
			}
			num = Left(Parent(Parent(x_id)));
			if (color(num) == NodeColor.red)
			{
				SetColor(Parent(x_id), NodeColor.black);
				SetColor(num, NodeColor.black);
				SetColor(Parent(Parent(x_id)), NodeColor.red);
				x_id = Parent(Parent(x_id));
				continue;
			}
			if (x_id == Left(Parent(x_id)))
			{
				x_id = Parent(x_id);
				root_id = RightRotate(root_id, x_id, mainTreeNodeID);
			}
			SetColor(Parent(x_id), NodeColor.black);
			SetColor(Parent(Parent(x_id)), NodeColor.red);
			root_id = LeftRotate(root_id, Parent(Parent(x_id)), mainTreeNodeID);
		}
		if (root_id == 0)
		{
			SetColor(root, NodeColor.black);
		}
		else
		{
			SetColor(root_id, NodeColor.black);
		}
		return root_id;
	}

	public void UpdateNodeKey(K currentKey, K newKey)
	{
		NodePath nodeByKey = GetNodeByKey(currentKey);
		if (Parent(nodeByKey._nodeID) == 0 && nodeByKey._nodeID != root)
		{
			SetKey(nodeByKey._mainTreeNodeID, newKey);
		}
		SetKey(nodeByKey._nodeID, newKey);
	}

	public K DeleteByIndex(int i)
	{
		NodePath nodeByIndex = GetNodeByIndex(i);
		K result = Key(nodeByIndex._nodeID);
		RBDeleteX(0, nodeByIndex._nodeID, nodeByIndex._mainTreeNodeID);
		return result;
	}

	public int RBDelete(int z_id)
	{
		return RBDeleteX(0, z_id, 0);
	}

	private int RBDeleteX(int root_id, int z_id, int mainTreeNodeID)
	{
		int num = 0;
		if (Next(z_id) != 0)
		{
			return RBDeleteX(Next(z_id), Next(z_id), z_id);
		}
		bool flag = false;
		int num2 = ((_accessMethod == TreeAccessMethod.KEY_SEARCH_AND_INDEX) ? mainTreeNodeID : z_id);
		if (Next(num2) != 0)
		{
			root_id = Next(num2);
		}
		if (SubTreeSize(Next(num2)) == 2)
		{
			flag = true;
		}
		else if (SubTreeSize(Next(num2)) == 1)
		{
			throw ExceptionBuilder.InternalRBTreeError(RBTreeError.InvalidNextSizeInDelete);
		}
		int num3 = ((Left(z_id) != 0 && Right(z_id) != 0) ? Successor(z_id) : z_id);
		num = ((Left(num3) == 0) ? Right(num3) : Left(num3));
		int num4 = Parent(num3);
		if (num != 0)
		{
			SetParent(num, num4);
		}
		if (num4 == 0)
		{
			if (root_id == 0)
			{
				root = num;
			}
			else
			{
				root_id = num;
			}
		}
		else if (num3 == Left(num4))
		{
			SetLeft(num4, num);
		}
		else
		{
			SetRight(num4, num);
		}
		if (num3 != z_id)
		{
			SetKey(z_id, Key(num3));
			SetNext(z_id, Next(num3));
		}
		if (Next(num2) != 0)
		{
			if (root_id == 0 && z_id != num2)
			{
				throw ExceptionBuilder.InternalRBTreeError(RBTreeError.InvalidStateinDelete);
			}
			if (root_id != 0)
			{
				SetNext(num2, root_id);
				SetKey(num2, Key(root_id));
			}
		}
		for (int num5 = num4; num5 != 0; num5 = Parent(num5))
		{
			RecomputeSize(num5);
		}
		if (root_id != 0)
		{
			for (int num6 = num2; num6 != 0; num6 = Parent(num6))
			{
				DecreaseSize(num6);
			}
		}
		if (color(num3) == NodeColor.black)
		{
			root_id = RBDeleteFixup(root_id, num, num4, mainTreeNodeID);
		}
		if (flag)
		{
			if (num2 == 0 || SubTreeSize(Next(num2)) != 1)
			{
				throw ExceptionBuilder.InternalRBTreeError(RBTreeError.InvalidNodeSizeinDelete);
			}
			_inUseSatelliteTreeCount--;
			int num7 = Next(num2);
			SetLeft(num7, Left(num2));
			SetRight(num7, Right(num2));
			SetSubTreeSize(num7, SubTreeSize(num2));
			SetColor(num7, color(num2));
			if (Parent(num2) != 0)
			{
				SetParent(num7, Parent(num2));
				if (Left(Parent(num2)) == num2)
				{
					SetLeft(Parent(num2), num7);
				}
				else
				{
					SetRight(Parent(num2), num7);
				}
			}
			if (Left(num2) != 0)
			{
				SetParent(Left(num2), num7);
			}
			if (Right(num2) != 0)
			{
				SetParent(Right(num2), num7);
			}
			if (root == num2)
			{
				root = num7;
			}
			FreeNode(num2);
			num2 = 0;
		}
		else if (Next(num2) != 0)
		{
			if (root_id == 0 && z_id != num2)
			{
				throw ExceptionBuilder.InternalRBTreeError(RBTreeError.InvalidStateinEndDelete);
			}
			if (root_id != 0)
			{
				SetNext(num2, root_id);
				SetKey(num2, Key(root_id));
			}
		}
		if (num3 != z_id)
		{
			SetLeft(num3, Left(z_id));
			SetRight(num3, Right(z_id));
			SetColor(num3, color(z_id));
			SetSubTreeSize(num3, SubTreeSize(z_id));
			if (Parent(z_id) != 0)
			{
				SetParent(num3, Parent(z_id));
				if (Left(Parent(z_id)) == z_id)
				{
					SetLeft(Parent(z_id), num3);
				}
				else
				{
					SetRight(Parent(z_id), num3);
				}
			}
			else
			{
				SetParent(num3, 0);
			}
			if (Left(z_id) != 0)
			{
				SetParent(Left(z_id), num3);
			}
			if (Right(z_id) != 0)
			{
				SetParent(Right(z_id), num3);
			}
			if (root == z_id)
			{
				root = num3;
			}
			else if (root_id == z_id)
			{
				root_id = num3;
			}
			if (num2 != 0 && Next(num2) == z_id)
			{
				SetNext(num2, num3);
			}
		}
		FreeNode(z_id);
		_version++;
		return z_id;
	}

	private int RBDeleteFixup(int root_id, int x_id, int px_id, int mainTreeNodeID)
	{
		if (x_id == 0 && px_id == 0)
		{
			return 0;
		}
		while (((root_id == 0) ? root : root_id) != x_id && color(x_id) == NodeColor.black)
		{
			int num;
			if ((x_id != 0 && x_id == Left(Parent(x_id))) || (x_id == 0 && Left(px_id) == 0))
			{
				num = ((x_id == 0) ? Right(px_id) : Right(Parent(x_id)));
				if (num == 0)
				{
					throw ExceptionBuilder.InternalRBTreeError(RBTreeError.RBDeleteFixup);
				}
				if (color(num) == NodeColor.red)
				{
					SetColor(num, NodeColor.black);
					SetColor(px_id, NodeColor.red);
					root_id = LeftRotate(root_id, px_id, mainTreeNodeID);
					num = ((x_id == 0) ? Right(px_id) : Right(Parent(x_id)));
				}
				if (color(Left(num)) == NodeColor.black && color(Right(num)) == NodeColor.black)
				{
					SetColor(num, NodeColor.red);
					x_id = px_id;
					px_id = Parent(px_id);
					continue;
				}
				if (color(Right(num)) == NodeColor.black)
				{
					SetColor(Left(num), NodeColor.black);
					SetColor(num, NodeColor.red);
					root_id = RightRotate(root_id, num, mainTreeNodeID);
					num = ((x_id == 0) ? Right(px_id) : Right(Parent(x_id)));
				}
				SetColor(num, color(px_id));
				SetColor(px_id, NodeColor.black);
				SetColor(Right(num), NodeColor.black);
				root_id = LeftRotate(root_id, px_id, mainTreeNodeID);
				x_id = ((root_id == 0) ? root : root_id);
				px_id = Parent(x_id);
				continue;
			}
			num = Left(px_id);
			if (color(num) == NodeColor.red)
			{
				SetColor(num, NodeColor.black);
				if (x_id != 0)
				{
					SetColor(px_id, NodeColor.red);
					root_id = RightRotate(root_id, px_id, mainTreeNodeID);
					num = ((x_id == 0) ? Left(px_id) : Left(Parent(x_id)));
				}
				else
				{
					SetColor(px_id, NodeColor.red);
					root_id = RightRotate(root_id, px_id, mainTreeNodeID);
					num = ((x_id == 0) ? Left(px_id) : Left(Parent(x_id)));
					if (num == 0)
					{
						throw ExceptionBuilder.InternalRBTreeError(RBTreeError.CannotRotateInvalidsuccessorNodeinDelete);
					}
				}
			}
			if (color(Right(num)) == NodeColor.black && color(Left(num)) == NodeColor.black)
			{
				SetColor(num, NodeColor.red);
				x_id = px_id;
				px_id = Parent(px_id);
				continue;
			}
			if (color(Left(num)) == NodeColor.black)
			{
				SetColor(Right(num), NodeColor.black);
				SetColor(num, NodeColor.red);
				root_id = LeftRotate(root_id, num, mainTreeNodeID);
				num = ((x_id == 0) ? Left(px_id) : Left(Parent(x_id)));
			}
			SetColor(num, color(px_id));
			SetColor(px_id, NodeColor.black);
			SetColor(Left(num), NodeColor.black);
			root_id = RightRotate(root_id, px_id, mainTreeNodeID);
			x_id = ((root_id == 0) ? root : root_id);
			px_id = Parent(x_id);
		}
		SetColor(x_id, NodeColor.black);
		return root_id;
	}

	private int SearchSubTree(int root_id, K key)
	{
		if (root_id != 0 && _accessMethod != TreeAccessMethod.KEY_SEARCH_AND_INDEX)
		{
			throw ExceptionBuilder.InternalRBTreeError(RBTreeError.UnsupportedAccessMethodInNonNillRootSubtree);
		}
		int num = ((root_id == 0) ? root : root_id);
		while (num != 0)
		{
			int num2 = ((root_id == 0) ? CompareNode(key, Key(num)) : CompareSateliteTreeNode(key, Key(num)));
			if (num2 == 0)
			{
				break;
			}
			num = ((num2 >= 0) ? Right(num) : Left(num));
		}
		return num;
	}

	public int Search(K key)
	{
		int num = root;
		while (num != 0)
		{
			int num2 = CompareNode(key, Key(num));
			if (num2 == 0)
			{
				break;
			}
			num = ((num2 >= 0) ? Right(num) : Left(num));
		}
		return num;
	}

	private NodePath GetNodeByKey(K key)
	{
		int num = SearchSubTree(0, key);
		if (Next(num) != 0)
		{
			return new NodePath(SearchSubTree(Next(num), key), num);
		}
		if (!Key(num).Equals(key))
		{
			num = 0;
		}
		return new NodePath(num, 0);
	}

	public int GetIndexByKey(K key)
	{
		int result = -1;
		NodePath nodeByKey = GetNodeByKey(key);
		if (nodeByKey._nodeID != 0)
		{
			result = GetIndexByNodePath(nodeByKey);
		}
		return result;
	}

	public int GetIndexByNode(int node)
	{
		if (_inUseSatelliteTreeCount == 0)
		{
			return ComputeIndexByNode(node);
		}
		if (Next(node) != 0)
		{
			return ComputeIndexWithSatelliteByNode(node);
		}
		int num = SearchSubTree(0, Key(node));
		if (num == node)
		{
			return ComputeIndexWithSatelliteByNode(node);
		}
		return ComputeIndexWithSatelliteByNode(num) + ComputeIndexByNode(node);
	}

	private int GetIndexByNodePath(NodePath path)
	{
		if (_inUseSatelliteTreeCount == 0)
		{
			return ComputeIndexByNode(path._nodeID);
		}
		if (path._mainTreeNodeID == 0)
		{
			return ComputeIndexWithSatelliteByNode(path._nodeID);
		}
		return ComputeIndexWithSatelliteByNode(path._mainTreeNodeID) + ComputeIndexByNode(path._nodeID);
	}

	private int ComputeIndexByNode(int nodeId)
	{
		int num = SubTreeSize(Left(nodeId));
		while (nodeId != 0)
		{
			int num2 = Parent(nodeId);
			if (nodeId == Right(num2))
			{
				num += SubTreeSize(Left(num2)) + 1;
			}
			nodeId = num2;
		}
		return num;
	}

	private int ComputeIndexWithSatelliteByNode(int nodeId)
	{
		int num = SubTreeSize(Left(nodeId));
		while (nodeId != 0)
		{
			int num2 = Parent(nodeId);
			if (nodeId == Right(num2))
			{
				num += SubTreeSize(Left(num2)) + ((Next(num2) == 0) ? 1 : SubTreeSize(Next(num2)));
			}
			nodeId = num2;
		}
		return num;
	}

	private NodePath GetNodeByIndex(int userIndex)
	{
		int num;
		int satelliteRootId;
		if (_inUseSatelliteTreeCount == 0)
		{
			num = ComputeNodeByIndex(root, userIndex + 1);
			satelliteRootId = 0;
		}
		else
		{
			num = ComputeNodeByIndex(userIndex, out satelliteRootId);
		}
		if (num == 0)
		{
			if (TreeAccessMethod.INDEX_ONLY == _accessMethod)
			{
				throw ExceptionBuilder.RowOutOfRange(userIndex);
			}
			throw ExceptionBuilder.InternalRBTreeError(RBTreeError.IndexOutOFRangeinGetNodeByIndex);
		}
		return new NodePath(num, satelliteRootId);
	}

	private int ComputeNodeByIndex(int index, out int satelliteRootId)
	{
		index++;
		satelliteRootId = 0;
		int num = root;
		int num2 = -1;
		while (num != 0 && ((num2 = SubTreeSize(Left(num)) + 1) != index || Next(num) != 0))
		{
			if (index < num2)
			{
				num = Left(num);
				continue;
			}
			if (Next(num) != 0 && index >= num2 && index <= num2 + SubTreeSize(Next(num)) - 1)
			{
				satelliteRootId = num;
				index = index - num2 + 1;
				return ComputeNodeByIndex(Next(num), index);
			}
			index = ((Next(num) != 0) ? (index - (num2 + SubTreeSize(Next(num)) - 1)) : (index - num2));
			num = Right(num);
		}
		return num;
	}

	private int ComputeNodeByIndex(int x_id, int index)
	{
		while (x_id != 0)
		{
			int num = Left(x_id);
			int num2 = SubTreeSize(num) + 1;
			if (index < num2)
			{
				x_id = num;
				continue;
			}
			if (num2 >= index)
			{
				break;
			}
			x_id = Right(x_id);
			index -= num2;
		}
		return x_id;
	}

	public int Insert(K item)
	{
		int newNode = GetNewNode(item);
		RBInsert(0, newNode, 0, -1, append: false);
		return newNode;
	}

	public int Add(K item)
	{
		int newNode = GetNewNode(item);
		RBInsert(0, newNode, 0, -1, append: false);
		return newNode;
	}

	public IEnumerator GetEnumerator()
	{
		return new RBTreeEnumerator(this);
	}

	public int IndexOf(int nodeId, K item)
	{
		int result = -1;
		if (nodeId != 0)
		{
			if ((object)Key(nodeId) == (object)item)
			{
				return GetIndexByNode(nodeId);
			}
			if ((result = IndexOf(Left(nodeId), item)) != -1)
			{
				return result;
			}
			result = IndexOf(Right(nodeId), item);
			_ = -1;
			return result;
		}
		return result;
	}

	public int Insert(int position, K item)
	{
		return InsertAt(position, item, append: false);
	}

	public int InsertAt(int position, K item, bool append)
	{
		int newNode = GetNewNode(item);
		RBInsert(0, newNode, 0, position, append);
		return newNode;
	}

	public void RemoveAt(int position)
	{
		DeleteByIndex(position);
	}

	public void Clear()
	{
		InitTree();
		_version++;
	}

	public void CopyTo(Array array, int index)
	{
		if (array == null)
		{
			throw ExceptionBuilder.ArgumentNull("array");
		}
		if (index < 0)
		{
			throw ExceptionBuilder.ArgumentOutOfRange("index");
		}
		int count = Count;
		if (array.Length - index < Count)
		{
			throw ExceptionBuilder.InvalidOffsetLength();
		}
		int num = Minimum(root);
		for (int i = 0; i < count; i++)
		{
			array.SetValue(Key(num), index + i);
			num = Successor(num);
		}
	}

	public void CopyTo(K[] array, int index)
	{
		if (array == null)
		{
			throw ExceptionBuilder.ArgumentNull("array");
		}
		if (index < 0)
		{
			throw ExceptionBuilder.ArgumentOutOfRange("index");
		}
		int count = Count;
		if (array.Length - index < Count)
		{
			throw ExceptionBuilder.InvalidOffsetLength();
		}
		int num = Minimum(root);
		for (int i = 0; i < count; i++)
		{
			array[index + i] = Key(num);
			num = Successor(num);
		}
	}

	private void SetRight(int nodeId, int rightNodeId)
	{
		_pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF]._rightId = rightNodeId;
	}

	private void SetLeft(int nodeId, int leftNodeId)
	{
		_pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF]._leftId = leftNodeId;
	}

	private void SetParent(int nodeId, int parentNodeId)
	{
		_pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF]._parentId = parentNodeId;
	}

	private void SetColor(int nodeId, NodeColor color)
	{
		_pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF]._nodeColor = color;
	}

	private void SetKey(int nodeId, K key)
	{
		_pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF]._keyOfNode = key;
	}

	private void SetNext(int nodeId, int nextNodeId)
	{
		_pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF]._nextId = nextNodeId;
	}

	private void SetSubTreeSize(int nodeId, int size)
	{
		_pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF]._subTreeSize = size;
	}

	private void IncreaseSize(int nodeId)
	{
		_pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF]._subTreeSize++;
	}

	private void RecomputeSize(int nodeId)
	{
		int subTreeSize = SubTreeSize(Left(nodeId)) + SubTreeSize(Right(nodeId)) + ((Next(nodeId) == 0) ? 1 : SubTreeSize(Next(nodeId)));
		_pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF]._subTreeSize = subTreeSize;
	}

	private void DecreaseSize(int nodeId)
	{
		_pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF]._subTreeSize--;
	}

	public int Right(int nodeId)
	{
		return _pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF]._rightId;
	}

	public int Left(int nodeId)
	{
		return _pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF]._leftId;
	}

	public int Parent(int nodeId)
	{
		return _pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF]._parentId;
	}

	private NodeColor color(int nodeId)
	{
		return _pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF]._nodeColor;
	}

	public int Next(int nodeId)
	{
		return _pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF]._nextId;
	}

	public int SubTreeSize(int nodeId)
	{
		return _pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF]._subTreeSize;
	}

	public K Key(int nodeId)
	{
		return _pageTable[nodeId >> 16]._slots[nodeId & 0xFFFF]._keyOfNode;
	}
}
