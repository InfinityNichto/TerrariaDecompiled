using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq;

public static class Enumerable
{
	private abstract class AppendPrependIterator<TSource> : Iterator<TSource>, IIListProvider<TSource>, IEnumerable<TSource>, IEnumerable
	{
		protected readonly IEnumerable<TSource> _source;

		protected IEnumerator<TSource> _enumerator;

		public abstract TSource[] ToArray();

		public abstract List<TSource> ToList();

		public abstract int GetCount(bool onlyIfCheap);

		protected AppendPrependIterator(IEnumerable<TSource> source)
		{
			_source = source;
		}

		protected void GetSourceEnumerator()
		{
			_enumerator = _source.GetEnumerator();
		}

		public abstract AppendPrependIterator<TSource> Append(TSource item);

		public abstract AppendPrependIterator<TSource> Prepend(TSource item);

		protected bool LoadFromEnumerator()
		{
			if (_enumerator.MoveNext())
			{
				_current = _enumerator.Current;
				return true;
			}
			Dispose();
			return false;
		}

		public override void Dispose()
		{
			if (_enumerator != null)
			{
				_enumerator.Dispose();
				_enumerator = null;
			}
			base.Dispose();
		}
	}

	private sealed class AppendPrepend1Iterator<TSource> : AppendPrependIterator<TSource>
	{
		private readonly TSource _item;

		private readonly bool _appending;

		private TSource[] LazyToArray()
		{
			LargeArrayBuilder<TSource> largeArrayBuilder = new LargeArrayBuilder<TSource>(initialize: true);
			if (!_appending)
			{
				largeArrayBuilder.SlowAdd(_item);
			}
			largeArrayBuilder.AddRange(_source);
			if (_appending)
			{
				largeArrayBuilder.SlowAdd(_item);
			}
			return largeArrayBuilder.ToArray();
		}

		public override TSource[] ToArray()
		{
			int count = GetCount(onlyIfCheap: true);
			if (count == -1)
			{
				return LazyToArray();
			}
			TSource[] array = new TSource[count];
			int arrayIndex;
			if (_appending)
			{
				arrayIndex = 0;
			}
			else
			{
				array[0] = _item;
				arrayIndex = 1;
			}
			System.Collections.Generic.EnumerableHelpers.Copy(_source, array, arrayIndex, count - 1);
			if (_appending)
			{
				array[^1] = _item;
			}
			return array;
		}

		public override List<TSource> ToList()
		{
			int count = GetCount(onlyIfCheap: true);
			List<TSource> list = ((count == -1) ? new List<TSource>() : new List<TSource>(count));
			if (!_appending)
			{
				list.Add(_item);
			}
			list.AddRange(_source);
			if (_appending)
			{
				list.Add(_item);
			}
			return list;
		}

		public override int GetCount(bool onlyIfCheap)
		{
			if (_source is IIListProvider<TSource> iIListProvider)
			{
				int count = iIListProvider.GetCount(onlyIfCheap);
				if (count != -1)
				{
					return count + 1;
				}
				return -1;
			}
			if (onlyIfCheap && !(_source is ICollection<TSource>))
			{
				return -1;
			}
			return _source.Count() + 1;
		}

		public AppendPrepend1Iterator(IEnumerable<TSource> source, TSource item, bool appending)
			: base(source)
		{
			_item = item;
			_appending = appending;
		}

		public override Iterator<TSource> Clone()
		{
			return new AppendPrepend1Iterator<TSource>(_source, _item, _appending);
		}

		public override bool MoveNext()
		{
			switch (_state)
			{
			case 1:
				_state = 2;
				if (!_appending)
				{
					_current = _item;
					return true;
				}
				goto case 2;
			case 2:
				GetSourceEnumerator();
				_state = 3;
				goto case 3;
			case 3:
				if (LoadFromEnumerator())
				{
					return true;
				}
				if (_appending)
				{
					_current = _item;
					return true;
				}
				break;
			}
			Dispose();
			return false;
		}

		public override AppendPrependIterator<TSource> Append(TSource item)
		{
			if (_appending)
			{
				return new AppendPrependN<TSource>(_source, null, new SingleLinkedNode<TSource>(_item).Add(item), 0, 2);
			}
			return new AppendPrependN<TSource>(_source, new SingleLinkedNode<TSource>(_item), new SingleLinkedNode<TSource>(item), 1, 1);
		}

		public override AppendPrependIterator<TSource> Prepend(TSource item)
		{
			if (_appending)
			{
				return new AppendPrependN<TSource>(_source, new SingleLinkedNode<TSource>(item), new SingleLinkedNode<TSource>(_item), 1, 1);
			}
			return new AppendPrependN<TSource>(_source, new SingleLinkedNode<TSource>(_item).Add(item), null, 2, 0);
		}
	}

	private sealed class AppendPrependN<TSource> : AppendPrependIterator<TSource>
	{
		private readonly SingleLinkedNode<TSource> _prepended;

		private readonly SingleLinkedNode<TSource> _appended;

		private readonly int _prependCount;

		private readonly int _appendCount;

		private SingleLinkedNode<TSource> _node;

		private TSource[] LazyToArray()
		{
			SparseArrayBuilder<TSource> sparseArrayBuilder = new SparseArrayBuilder<TSource>(initialize: true);
			if (_prepended != null)
			{
				sparseArrayBuilder.Reserve(_prependCount);
			}
			sparseArrayBuilder.AddRange(_source);
			if (_appended != null)
			{
				sparseArrayBuilder.Reserve(_appendCount);
			}
			TSource[] array = sparseArrayBuilder.ToArray();
			int num = 0;
			for (SingleLinkedNode<TSource> singleLinkedNode = _prepended; singleLinkedNode != null; singleLinkedNode = singleLinkedNode.Linked)
			{
				array[num++] = singleLinkedNode.Item;
			}
			num = array.Length - 1;
			for (SingleLinkedNode<TSource> singleLinkedNode2 = _appended; singleLinkedNode2 != null; singleLinkedNode2 = singleLinkedNode2.Linked)
			{
				array[num--] = singleLinkedNode2.Item;
			}
			return array;
		}

		public override TSource[] ToArray()
		{
			int count = GetCount(onlyIfCheap: true);
			if (count == -1)
			{
				return LazyToArray();
			}
			TSource[] array = new TSource[count];
			int num = 0;
			for (SingleLinkedNode<TSource> singleLinkedNode = _prepended; singleLinkedNode != null; singleLinkedNode = singleLinkedNode.Linked)
			{
				array[num] = singleLinkedNode.Item;
				num++;
			}
			if (_source is ICollection<TSource> collection)
			{
				collection.CopyTo(array, num);
			}
			else
			{
				foreach (TSource item in _source)
				{
					array[num] = item;
					num++;
				}
			}
			num = array.Length;
			for (SingleLinkedNode<TSource> singleLinkedNode2 = _appended; singleLinkedNode2 != null; singleLinkedNode2 = singleLinkedNode2.Linked)
			{
				num--;
				array[num] = singleLinkedNode2.Item;
			}
			return array;
		}

		public override List<TSource> ToList()
		{
			int count = GetCount(onlyIfCheap: true);
			List<TSource> list = ((count == -1) ? new List<TSource>() : new List<TSource>(count));
			for (SingleLinkedNode<TSource> singleLinkedNode = _prepended; singleLinkedNode != null; singleLinkedNode = singleLinkedNode.Linked)
			{
				list.Add(singleLinkedNode.Item);
			}
			list.AddRange(_source);
			if (_appended != null)
			{
				list.AddRange(_appended.ToArray(_appendCount));
			}
			return list;
		}

		public override int GetCount(bool onlyIfCheap)
		{
			if (_source is IIListProvider<TSource> iIListProvider)
			{
				int count = iIListProvider.GetCount(onlyIfCheap);
				if (count != -1)
				{
					return count + _appendCount + _prependCount;
				}
				return -1;
			}
			if (onlyIfCheap && !(_source is ICollection<TSource>))
			{
				return -1;
			}
			return _source.Count() + _appendCount + _prependCount;
		}

		public AppendPrependN(IEnumerable<TSource> source, SingleLinkedNode<TSource> prepended, SingleLinkedNode<TSource> appended, int prependCount, int appendCount)
			: base(source)
		{
			_prepended = prepended;
			_appended = appended;
			_prependCount = prependCount;
			_appendCount = appendCount;
		}

		public override Iterator<TSource> Clone()
		{
			return new AppendPrependN<TSource>(_source, _prepended, _appended, _prependCount, _appendCount);
		}

		public override bool MoveNext()
		{
			switch (_state)
			{
			case 1:
				_node = _prepended;
				_state = 2;
				goto case 2;
			case 2:
				if (_node != null)
				{
					_current = _node.Item;
					_node = _node.Linked;
					return true;
				}
				GetSourceEnumerator();
				_state = 3;
				goto case 3;
			case 3:
				if (LoadFromEnumerator())
				{
					return true;
				}
				if (_appended == null)
				{
					return false;
				}
				_enumerator = ((IEnumerable<TSource>)_appended.ToArray(_appendCount)).GetEnumerator();
				_state = 4;
				goto case 4;
			case 4:
				return LoadFromEnumerator();
			default:
				Dispose();
				return false;
			}
		}

		public override AppendPrependIterator<TSource> Append(TSource item)
		{
			SingleLinkedNode<TSource> appended = ((_appended != null) ? _appended.Add(item) : new SingleLinkedNode<TSource>(item));
			return new AppendPrependN<TSource>(_source, _prepended, appended, _prependCount, _appendCount + 1);
		}

		public override AppendPrependIterator<TSource> Prepend(TSource item)
		{
			SingleLinkedNode<TSource> prepended = ((_prepended != null) ? _prepended.Add(item) : new SingleLinkedNode<TSource>(item));
			return new AppendPrependN<TSource>(_source, prepended, _appended, _prependCount + 1, _appendCount);
		}
	}

	private sealed class Concat2Iterator<TSource> : ConcatIterator<TSource>
	{
		internal readonly IEnumerable<TSource> _first;

		internal readonly IEnumerable<TSource> _second;

		public override int GetCount(bool onlyIfCheap)
		{
			if (!_first.TryGetNonEnumeratedCount(out var count))
			{
				if (onlyIfCheap)
				{
					return -1;
				}
				count = _first.Count();
			}
			if (!_second.TryGetNonEnumeratedCount(out var count2))
			{
				if (onlyIfCheap)
				{
					return -1;
				}
				count2 = _second.Count();
			}
			return checked(count + count2);
		}

		public override TSource[] ToArray()
		{
			SparseArrayBuilder<TSource> sparseArrayBuilder = new SparseArrayBuilder<TSource>(initialize: true);
			bool flag = sparseArrayBuilder.ReserveOrAdd(_first);
			bool flag2 = sparseArrayBuilder.ReserveOrAdd(_second);
			TSource[] array = sparseArrayBuilder.ToArray();
			if (flag)
			{
				Marker marker = sparseArrayBuilder.Markers.First();
				System.Collections.Generic.EnumerableHelpers.Copy(_first, array, 0, marker.Count);
			}
			if (flag2)
			{
				Marker marker2 = sparseArrayBuilder.Markers.Last();
				System.Collections.Generic.EnumerableHelpers.Copy(_second, array, marker2.Index, marker2.Count);
			}
			return array;
		}

		internal Concat2Iterator(IEnumerable<TSource> first, IEnumerable<TSource> second)
		{
			_first = first;
			_second = second;
		}

		public override Iterator<TSource> Clone()
		{
			return new Concat2Iterator<TSource>(_first, _second);
		}

		internal override ConcatIterator<TSource> Concat(IEnumerable<TSource> next)
		{
			bool hasOnlyCollections = next is ICollection<TSource> && _first is ICollection<TSource> && _second is ICollection<TSource>;
			return new ConcatNIterator<TSource>(this, next, 2, hasOnlyCollections);
		}

		internal override IEnumerable<TSource> GetEnumerable(int index)
		{
			return index switch
			{
				0 => _first, 
				1 => _second, 
				_ => null, 
			};
		}
	}

	private sealed class ConcatNIterator<TSource> : ConcatIterator<TSource>
	{
		private readonly ConcatIterator<TSource> _tail;

		private readonly IEnumerable<TSource> _head;

		private readonly int _headIndex;

		private readonly bool _hasOnlyCollections;

		private ConcatNIterator<TSource> PreviousN => _tail as ConcatNIterator<TSource>;

		public override int GetCount(bool onlyIfCheap)
		{
			if (onlyIfCheap && !_hasOnlyCollections)
			{
				return -1;
			}
			int num = 0;
			ConcatNIterator<TSource> concatNIterator = this;
			checked
			{
				ConcatNIterator<TSource> concatNIterator2;
				do
				{
					concatNIterator2 = concatNIterator;
					IEnumerable<TSource> head = concatNIterator2._head;
					int num2 = ((head is ICollection<TSource> collection) ? collection.Count : head.Count());
					num += num2;
				}
				while ((concatNIterator = concatNIterator2.PreviousN) != null);
				return num + concatNIterator2._tail.GetCount(onlyIfCheap);
			}
		}

		public override TSource[] ToArray()
		{
			if (!_hasOnlyCollections)
			{
				return LazyToArray();
			}
			return PreallocatingToArray();
		}

		private TSource[] LazyToArray()
		{
			SparseArrayBuilder<TSource> sparseArrayBuilder = new SparseArrayBuilder<TSource>(initialize: true);
			System.Collections.Generic.ArrayBuilder<int> arrayBuilder = default(System.Collections.Generic.ArrayBuilder<int>);
			int num = 0;
			while (true)
			{
				IEnumerable<TSource> enumerable = GetEnumerable(num);
				if (enumerable == null)
				{
					break;
				}
				if (sparseArrayBuilder.ReserveOrAdd(enumerable))
				{
					arrayBuilder.Add(num);
				}
				num++;
			}
			TSource[] array = sparseArrayBuilder.ToArray();
			System.Collections.Generic.ArrayBuilder<Marker> markers = sparseArrayBuilder.Markers;
			for (int i = 0; i < markers.Count; i++)
			{
				Marker marker = markers[i];
				IEnumerable<TSource> enumerable2 = GetEnumerable(arrayBuilder[i]);
				System.Collections.Generic.EnumerableHelpers.Copy(enumerable2, array, marker.Index, marker.Count);
			}
			return array;
		}

		private TSource[] PreallocatingToArray()
		{
			int count = GetCount(onlyIfCheap: true);
			if (count == 0)
			{
				return Array.Empty<TSource>();
			}
			TSource[] array = new TSource[count];
			int num = array.Length;
			ConcatNIterator<TSource> concatNIterator = this;
			checked
			{
				ConcatNIterator<TSource> concatNIterator2;
				do
				{
					concatNIterator2 = concatNIterator;
					ICollection<TSource> collection = (ICollection<TSource>)concatNIterator2._head;
					int count2 = collection.Count;
					if (count2 > 0)
					{
						num -= count2;
						collection.CopyTo(array, num);
					}
				}
				while ((concatNIterator = concatNIterator2.PreviousN) != null);
				Concat2Iterator<TSource> concat2Iterator = (Concat2Iterator<TSource>)concatNIterator2._tail;
				ICollection<TSource> collection2 = (ICollection<TSource>)concat2Iterator._second;
				int count3 = collection2.Count;
				if (count3 > 0)
				{
					collection2.CopyTo(array, num - count3);
				}
				if (num > count3)
				{
					ICollection<TSource> collection3 = (ICollection<TSource>)concat2Iterator._first;
					collection3.CopyTo(array, 0);
				}
				return array;
			}
		}

		internal ConcatNIterator(ConcatIterator<TSource> tail, IEnumerable<TSource> head, int headIndex, bool hasOnlyCollections)
		{
			_tail = tail;
			_head = head;
			_headIndex = headIndex;
			_hasOnlyCollections = hasOnlyCollections;
		}

		public override Iterator<TSource> Clone()
		{
			return new ConcatNIterator<TSource>(_tail, _head, _headIndex, _hasOnlyCollections);
		}

		internal override ConcatIterator<TSource> Concat(IEnumerable<TSource> next)
		{
			if (_headIndex == 2147483645)
			{
				return new Concat2Iterator<TSource>(this, next);
			}
			bool hasOnlyCollections = _hasOnlyCollections && next is ICollection<TSource>;
			return new ConcatNIterator<TSource>(this, next, _headIndex + 1, hasOnlyCollections);
		}

		internal override IEnumerable<TSource> GetEnumerable(int index)
		{
			if (index > _headIndex)
			{
				return null;
			}
			ConcatNIterator<TSource> concatNIterator = this;
			ConcatNIterator<TSource> concatNIterator2;
			do
			{
				concatNIterator2 = concatNIterator;
				if (index == concatNIterator2._headIndex)
				{
					return concatNIterator2._head;
				}
			}
			while ((concatNIterator = concatNIterator2.PreviousN) != null);
			return concatNIterator2._tail.GetEnumerable(index);
		}
	}

	private abstract class ConcatIterator<TSource> : Iterator<TSource>, IIListProvider<TSource>, IEnumerable<TSource>, IEnumerable
	{
		private IEnumerator<TSource> _enumerator;

		public abstract int GetCount(bool onlyIfCheap);

		public abstract TSource[] ToArray();

		public List<TSource> ToList()
		{
			int count = GetCount(onlyIfCheap: true);
			List<TSource> list = ((count != -1) ? new List<TSource>(count) : new List<TSource>());
			int num = 0;
			while (true)
			{
				IEnumerable<TSource> enumerable = GetEnumerable(num);
				if (enumerable == null)
				{
					break;
				}
				list.AddRange(enumerable);
				num++;
			}
			return list;
		}

		public override void Dispose()
		{
			if (_enumerator != null)
			{
				_enumerator.Dispose();
				_enumerator = null;
			}
			base.Dispose();
		}

		internal abstract IEnumerable<TSource> GetEnumerable(int index);

		internal abstract ConcatIterator<TSource> Concat(IEnumerable<TSource> next);

		public override bool MoveNext()
		{
			if (_state == 1)
			{
				_enumerator = GetEnumerable(0).GetEnumerator();
				_state = 2;
			}
			if (_state > 1)
			{
				while (true)
				{
					if (_enumerator.MoveNext())
					{
						_current = _enumerator.Current;
						return true;
					}
					IEnumerable<TSource> enumerable = GetEnumerable(_state++ - 1);
					if (enumerable == null)
					{
						break;
					}
					_enumerator.Dispose();
					_enumerator = enumerable.GetEnumerator();
				}
				Dispose();
			}
			return false;
		}
	}

	private sealed class DefaultIfEmptyIterator<TSource> : Iterator<TSource>, IIListProvider<TSource>, IEnumerable<TSource>, IEnumerable
	{
		private readonly IEnumerable<TSource> _source;

		private readonly TSource _default;

		private IEnumerator<TSource> _enumerator;

		public TSource[] ToArray()
		{
			TSource[] array = _source.ToArray();
			if (array.Length != 0)
			{
				return array;
			}
			return new TSource[1] { _default };
		}

		public List<TSource> ToList()
		{
			List<TSource> list = _source.ToList();
			if (list.Count == 0)
			{
				list.Add(_default);
			}
			return list;
		}

		public int GetCount(bool onlyIfCheap)
		{
			int num = ((onlyIfCheap && !(_source is ICollection<TSource>) && !(_source is ICollection)) ? ((_source is IIListProvider<TSource> iIListProvider) ? iIListProvider.GetCount(onlyIfCheap: true) : (-1)) : _source.Count());
			if (num != 0)
			{
				return num;
			}
			return 1;
		}

		public DefaultIfEmptyIterator(IEnumerable<TSource> source, TSource defaultValue)
		{
			_source = source;
			_default = defaultValue;
		}

		public override Iterator<TSource> Clone()
		{
			return new DefaultIfEmptyIterator<TSource>(_source, _default);
		}

		public override bool MoveNext()
		{
			switch (_state)
			{
			case 1:
				_enumerator = _source.GetEnumerator();
				if (_enumerator.MoveNext())
				{
					_current = _enumerator.Current;
					_state = 2;
				}
				else
				{
					_current = _default;
					_state = -1;
				}
				return true;
			case 2:
				if (_enumerator.MoveNext())
				{
					_current = _enumerator.Current;
					return true;
				}
				break;
			}
			Dispose();
			return false;
		}

		public override void Dispose()
		{
			if (_enumerator != null)
			{
				_enumerator.Dispose();
				_enumerator = null;
			}
			base.Dispose();
		}
	}

	private sealed class DistinctIterator<TSource> : Iterator<TSource>, IIListProvider<TSource>, IEnumerable<TSource>, IEnumerable
	{
		private readonly IEnumerable<TSource> _source;

		private readonly IEqualityComparer<TSource> _comparer;

		private HashSet<TSource> _set;

		private IEnumerator<TSource> _enumerator;

		public TSource[] ToArray()
		{
			return HashSetToArray(new HashSet<TSource>(_source, _comparer));
		}

		public List<TSource> ToList()
		{
			return HashSetToList(new HashSet<TSource>(_source, _comparer));
		}

		public int GetCount(bool onlyIfCheap)
		{
			if (!onlyIfCheap)
			{
				return new HashSet<TSource>(_source, _comparer).Count;
			}
			return -1;
		}

		public DistinctIterator(IEnumerable<TSource> source, IEqualityComparer<TSource> comparer)
		{
			_source = source;
			_comparer = comparer;
		}

		public override Iterator<TSource> Clone()
		{
			return new DistinctIterator<TSource>(_source, _comparer);
		}

		public override bool MoveNext()
		{
			int state = _state;
			TSource current;
			if (state != 1)
			{
				if (state == 2)
				{
					while (_enumerator.MoveNext())
					{
						current = _enumerator.Current;
						if (_set.Add(current))
						{
							_current = current;
							return true;
						}
					}
				}
				Dispose();
				return false;
			}
			_enumerator = _source.GetEnumerator();
			if (!_enumerator.MoveNext())
			{
				Dispose();
				return false;
			}
			current = _enumerator.Current;
			_set = new HashSet<TSource>(7, _comparer);
			_set.Add(current);
			_current = current;
			_state = 2;
			return true;
		}

		public override void Dispose()
		{
			if (_enumerator != null)
			{
				_enumerator.Dispose();
				_enumerator = null;
				_set = null;
			}
			base.Dispose();
		}
	}

	[DebuggerDisplay("Count = {Count}")]
	private sealed class ListPartition<TSource> : Iterator<TSource>, IPartition<TSource>, IIListProvider<TSource>, IEnumerable<TSource>, IEnumerable
	{
		private readonly IList<TSource> _source;

		private readonly int _minIndexInclusive;

		private readonly int _maxIndexInclusive;

		private int Count
		{
			get
			{
				int count = _source.Count;
				if (count <= _minIndexInclusive)
				{
					return 0;
				}
				return Math.Min(count - 1, _maxIndexInclusive) - _minIndexInclusive + 1;
			}
		}

		public ListPartition(IList<TSource> source, int minIndexInclusive, int maxIndexInclusive)
		{
			_source = source;
			_minIndexInclusive = minIndexInclusive;
			_maxIndexInclusive = maxIndexInclusive;
		}

		public override Iterator<TSource> Clone()
		{
			return new ListPartition<TSource>(_source, _minIndexInclusive, _maxIndexInclusive);
		}

		public override bool MoveNext()
		{
			int num = _state - 1;
			if ((uint)num <= (uint)(_maxIndexInclusive - _minIndexInclusive) && num < _source.Count - _minIndexInclusive)
			{
				_current = _source[_minIndexInclusive + num];
				_state++;
				return true;
			}
			Dispose();
			return false;
		}

		public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector)
		{
			return new SelectListPartitionIterator<TSource, TResult>(_source, selector, _minIndexInclusive, _maxIndexInclusive);
		}

		public IPartition<TSource> Skip(int count)
		{
			int num = _minIndexInclusive + count;
			if ((uint)num <= (uint)_maxIndexInclusive)
			{
				return new ListPartition<TSource>(_source, num, _maxIndexInclusive);
			}
			return EmptyPartition<TSource>.Instance;
		}

		public IPartition<TSource> Take(int count)
		{
			int num = _minIndexInclusive + count - 1;
			if ((uint)num < (uint)_maxIndexInclusive)
			{
				return new ListPartition<TSource>(_source, _minIndexInclusive, num);
			}
			return this;
		}

		public TSource TryGetElementAt(int index, out bool found)
		{
			if ((uint)index <= (uint)(_maxIndexInclusive - _minIndexInclusive) && index < _source.Count - _minIndexInclusive)
			{
				found = true;
				return _source[_minIndexInclusive + index];
			}
			found = false;
			return default(TSource);
		}

		public TSource TryGetFirst(out bool found)
		{
			if (_source.Count > _minIndexInclusive)
			{
				found = true;
				return _source[_minIndexInclusive];
			}
			found = false;
			return default(TSource);
		}

		public TSource TryGetLast(out bool found)
		{
			int num = _source.Count - 1;
			if (num >= _minIndexInclusive)
			{
				found = true;
				return _source[Math.Min(num, _maxIndexInclusive)];
			}
			found = false;
			return default(TSource);
		}

		public TSource[] ToArray()
		{
			int count = Count;
			if (count == 0)
			{
				return Array.Empty<TSource>();
			}
			TSource[] array = new TSource[count];
			int num = 0;
			int num2 = _minIndexInclusive;
			while (num != array.Length)
			{
				array[num] = _source[num2];
				num++;
				num2++;
			}
			return array;
		}

		public List<TSource> ToList()
		{
			int count = Count;
			if (count == 0)
			{
				return new List<TSource>();
			}
			List<TSource> list = new List<TSource>(count);
			int num = _minIndexInclusive + count;
			for (int i = _minIndexInclusive; i != num; i++)
			{
				list.Add(_source[i]);
			}
			return list;
		}

		public int GetCount(bool onlyIfCheap)
		{
			return Count;
		}
	}

	private sealed class EnumerablePartition<TSource> : Iterator<TSource>, IPartition<TSource>, IIListProvider<TSource>, IEnumerable<TSource>, IEnumerable
	{
		private readonly IEnumerable<TSource> _source;

		private readonly int _minIndexInclusive;

		private readonly int _maxIndexInclusive;

		private IEnumerator<TSource> _enumerator;

		private bool HasLimit => _maxIndexInclusive != -1;

		private int Limit => _maxIndexInclusive + 1 - _minIndexInclusive;

		internal EnumerablePartition(IEnumerable<TSource> source, int minIndexInclusive, int maxIndexInclusive)
		{
			_source = source;
			_minIndexInclusive = minIndexInclusive;
			_maxIndexInclusive = maxIndexInclusive;
		}

		public override Iterator<TSource> Clone()
		{
			return new EnumerablePartition<TSource>(_source, _minIndexInclusive, _maxIndexInclusive);
		}

		public override void Dispose()
		{
			if (_enumerator != null)
			{
				_enumerator.Dispose();
				_enumerator = null;
			}
			base.Dispose();
		}

		public int GetCount(bool onlyIfCheap)
		{
			if (onlyIfCheap)
			{
				return -1;
			}
			if (!HasLimit)
			{
				return Math.Max(_source.Count() - _minIndexInclusive, 0);
			}
			using IEnumerator<TSource> en = _source.GetEnumerator();
			uint num = SkipAndCount((uint)(_maxIndexInclusive + 1), en);
			return Math.Max((int)num - _minIndexInclusive, 0);
		}

		public override bool MoveNext()
		{
			int num = _state - 3;
			if (num < -2)
			{
				Dispose();
				return false;
			}
			int state = _state;
			if (state != 1)
			{
				if (state != 2)
				{
					goto IL_0054;
				}
			}
			else
			{
				_enumerator = _source.GetEnumerator();
				_state = 2;
			}
			if (SkipBeforeFirst(_enumerator))
			{
				_state = 3;
				goto IL_0054;
			}
			goto IL_009b;
			IL_009b:
			Dispose();
			return false;
			IL_0054:
			if ((!HasLimit || num < Limit) && _enumerator.MoveNext())
			{
				if (HasLimit)
				{
					_state++;
				}
				_current = _enumerator.Current;
				return true;
			}
			goto IL_009b;
		}

		public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector)
		{
			return new SelectIPartitionIterator<TSource, TResult>(this, selector);
		}

		public IPartition<TSource> Skip(int count)
		{
			int num = _minIndexInclusive + count;
			if (!HasLimit)
			{
				if (num < 0)
				{
					return new EnumerablePartition<TSource>(this, count, -1);
				}
			}
			else if ((uint)num > (uint)_maxIndexInclusive)
			{
				return EmptyPartition<TSource>.Instance;
			}
			return new EnumerablePartition<TSource>(_source, num, _maxIndexInclusive);
		}

		public IPartition<TSource> Take(int count)
		{
			int num = _minIndexInclusive + count - 1;
			if (!HasLimit)
			{
				if (num < 0)
				{
					return new EnumerablePartition<TSource>(this, 0, count - 1);
				}
			}
			else if ((uint)num >= (uint)_maxIndexInclusive)
			{
				return this;
			}
			return new EnumerablePartition<TSource>(_source, _minIndexInclusive, num);
		}

		public TSource TryGetElementAt(int index, out bool found)
		{
			if (index >= 0 && (!HasLimit || index < Limit))
			{
				using IEnumerator<TSource> enumerator = _source.GetEnumerator();
				if (SkipBefore(_minIndexInclusive + index, enumerator) && enumerator.MoveNext())
				{
					found = true;
					return enumerator.Current;
				}
			}
			found = false;
			return default(TSource);
		}

		public TSource TryGetFirst(out bool found)
		{
			using (IEnumerator<TSource> enumerator = _source.GetEnumerator())
			{
				if (SkipBeforeFirst(enumerator) && enumerator.MoveNext())
				{
					found = true;
					return enumerator.Current;
				}
			}
			found = false;
			return default(TSource);
		}

		public TSource TryGetLast(out bool found)
		{
			using (IEnumerator<TSource> enumerator = _source.GetEnumerator())
			{
				if (SkipBeforeFirst(enumerator) && enumerator.MoveNext())
				{
					int num = Limit - 1;
					int num2 = ((!HasLimit) ? int.MinValue : 0);
					TSource current;
					do
					{
						num--;
						current = enumerator.Current;
					}
					while (num >= num2 && enumerator.MoveNext());
					found = true;
					return current;
				}
			}
			found = false;
			return default(TSource);
		}

		public TSource[] ToArray()
		{
			using (IEnumerator<TSource> enumerator = _source.GetEnumerator())
			{
				if (SkipBeforeFirst(enumerator) && enumerator.MoveNext())
				{
					int num = Limit - 1;
					int num2 = ((!HasLimit) ? int.MinValue : 0);
					int maxCapacity = (HasLimit ? Limit : int.MaxValue);
					LargeArrayBuilder<TSource> largeArrayBuilder = new LargeArrayBuilder<TSource>(maxCapacity);
					do
					{
						num--;
						largeArrayBuilder.Add(enumerator.Current);
					}
					while (num >= num2 && enumerator.MoveNext());
					return largeArrayBuilder.ToArray();
				}
			}
			return Array.Empty<TSource>();
		}

		public List<TSource> ToList()
		{
			List<TSource> list = new List<TSource>();
			using (IEnumerator<TSource> enumerator = _source.GetEnumerator())
			{
				if (SkipBeforeFirst(enumerator) && enumerator.MoveNext())
				{
					int num = Limit - 1;
					int num2 = ((!HasLimit) ? int.MinValue : 0);
					do
					{
						num--;
						list.Add(enumerator.Current);
					}
					while (num >= num2 && enumerator.MoveNext());
				}
			}
			return list;
		}

		private bool SkipBeforeFirst(IEnumerator<TSource> en)
		{
			return SkipBefore(_minIndexInclusive, en);
		}

		private static bool SkipBefore(int index, IEnumerator<TSource> en)
		{
			return SkipAndCount(index, en) == index;
		}

		private static int SkipAndCount(int index, IEnumerator<TSource> en)
		{
			return (int)SkipAndCount((uint)index, en);
		}

		private static uint SkipAndCount(uint index, IEnumerator<TSource> en)
		{
			for (uint num = 0u; num < index; num++)
			{
				if (!en.MoveNext())
				{
					return num;
				}
			}
			return index;
		}
	}

	[DebuggerDisplay("Count = {CountForDebugger}")]
	private sealed class RangeIterator : Iterator<int>, IPartition<int>, IIListProvider<int>, IEnumerable<int>, IEnumerable
	{
		private readonly int _start;

		private readonly int _end;

		private int CountForDebugger => _end - _start;

		public override IEnumerable<TResult> Select<TResult>(Func<int, TResult> selector)
		{
			return new SelectRangeIterator<TResult>(_start, _end, selector);
		}

		public int[] ToArray()
		{
			int[] array = new int[_end - _start];
			int num = _start;
			for (int i = 0; i != array.Length; i++)
			{
				array[i] = num;
				num++;
			}
			return array;
		}

		public List<int> ToList()
		{
			List<int> list = new List<int>(_end - _start);
			for (int i = _start; i != _end; i++)
			{
				list.Add(i);
			}
			return list;
		}

		public int GetCount(bool onlyIfCheap)
		{
			return _end - _start;
		}

		public IPartition<int> Skip(int count)
		{
			if (count >= _end - _start)
			{
				return EmptyPartition<int>.Instance;
			}
			return new RangeIterator(_start + count, _end - _start - count);
		}

		public IPartition<int> Take(int count)
		{
			int num = _end - _start;
			if (count >= num)
			{
				return this;
			}
			return new RangeIterator(_start, count);
		}

		public int TryGetElementAt(int index, out bool found)
		{
			if ((uint)index < (uint)(_end - _start))
			{
				found = true;
				return _start + index;
			}
			found = false;
			return 0;
		}

		public int TryGetFirst(out bool found)
		{
			found = true;
			return _start;
		}

		public int TryGetLast(out bool found)
		{
			found = true;
			return _end - 1;
		}

		public RangeIterator(int start, int count)
		{
			_start = start;
			_end = start + count;
		}

		public override Iterator<int> Clone()
		{
			return new RangeIterator(_start, _end - _start);
		}

		public override bool MoveNext()
		{
			switch (_state)
			{
			case 1:
				_current = _start;
				_state = 2;
				return true;
			case 2:
				if (++_current != _end)
				{
					return true;
				}
				break;
			}
			_state = -1;
			return false;
		}

		public override void Dispose()
		{
			_state = -1;
		}
	}

	[DebuggerDisplay("Count = {_count}")]
	private sealed class RepeatIterator<TResult> : Iterator<TResult>, IPartition<TResult>, IIListProvider<TResult>, IEnumerable<TResult>, IEnumerable
	{
		private readonly int _count;

		public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
		{
			return new SelectIPartitionIterator<TResult, TResult2>(this, selector);
		}

		public TResult[] ToArray()
		{
			TResult[] array = new TResult[_count];
			if (_current != null)
			{
				Array.Fill(array, _current);
			}
			return array;
		}

		public List<TResult> ToList()
		{
			List<TResult> list = new List<TResult>(_count);
			for (int i = 0; i != _count; i++)
			{
				list.Add(_current);
			}
			return list;
		}

		public int GetCount(bool onlyIfCheap)
		{
			return _count;
		}

		public IPartition<TResult> Skip(int count)
		{
			if (count >= _count)
			{
				return EmptyPartition<TResult>.Instance;
			}
			return new RepeatIterator<TResult>(_current, _count - count);
		}

		public IPartition<TResult> Take(int count)
		{
			if (count >= _count)
			{
				return this;
			}
			return new RepeatIterator<TResult>(_current, count);
		}

		public TResult TryGetElementAt(int index, out bool found)
		{
			if ((uint)index < (uint)_count)
			{
				found = true;
				return _current;
			}
			found = false;
			return default(TResult);
		}

		public TResult TryGetFirst(out bool found)
		{
			found = true;
			return _current;
		}

		public TResult TryGetLast(out bool found)
		{
			found = true;
			return _current;
		}

		public RepeatIterator(TResult element, int count)
		{
			_current = element;
			_count = count;
		}

		public override Iterator<TResult> Clone()
		{
			return new RepeatIterator<TResult>(_current, _count);
		}

		public override void Dispose()
		{
			_state = -1;
		}

		public override bool MoveNext()
		{
			int num = _state - 1;
			if (num >= 0 && num != _count)
			{
				_state++;
				return true;
			}
			Dispose();
			return false;
		}
	}

	private sealed class ReverseIterator<TSource> : Iterator<TSource>, IIListProvider<TSource>, IEnumerable<TSource>, IEnumerable
	{
		private readonly IEnumerable<TSource> _source;

		private TSource[] _buffer;

		public TSource[] ToArray()
		{
			TSource[] array = _source.ToArray();
			Array.Reverse(array);
			return array;
		}

		public List<TSource> ToList()
		{
			List<TSource> list = _source.ToList();
			list.Reverse();
			return list;
		}

		public int GetCount(bool onlyIfCheap)
		{
			if (onlyIfCheap)
			{
				IEnumerable<TSource> source = _source;
				if (!(source is IIListProvider<TSource> iIListProvider))
				{
					if (!(source is ICollection<TSource> { Count: var count }))
					{
						if (!(source is ICollection { Count: var count2 }))
						{
							return -1;
						}
						return count2;
					}
					return count;
				}
				return iIListProvider.GetCount(onlyIfCheap: true);
			}
			return _source.Count();
		}

		public ReverseIterator(IEnumerable<TSource> source)
		{
			_source = source;
		}

		public override Iterator<TSource> Clone()
		{
			return new ReverseIterator<TSource>(_source);
		}

		public override bool MoveNext()
		{
			if (_state - 2 <= -2)
			{
				Dispose();
				return false;
			}
			int state = _state;
			if (state == 1)
			{
				Buffer<TSource> buffer = new Buffer<TSource>(_source);
				_buffer = buffer._items;
				_state = buffer._count + 2;
			}
			int num = _state - 3;
			if (num != -1)
			{
				_current = _buffer[num];
				_state--;
				return true;
			}
			Dispose();
			return false;
		}

		public override void Dispose()
		{
			_buffer = null;
			base.Dispose();
		}
	}

	private sealed class SelectEnumerableIterator<TSource, TResult> : Iterator<TResult>, IIListProvider<TResult>, IEnumerable<TResult>, IEnumerable
	{
		private readonly IEnumerable<TSource> _source;

		private readonly Func<TSource, TResult> _selector;

		private IEnumerator<TSource> _enumerator;

		public TResult[] ToArray()
		{
			LargeArrayBuilder<TResult> largeArrayBuilder = new LargeArrayBuilder<TResult>(initialize: true);
			foreach (TSource item in _source)
			{
				largeArrayBuilder.Add(_selector(item));
			}
			return largeArrayBuilder.ToArray();
		}

		public List<TResult> ToList()
		{
			List<TResult> list = new List<TResult>();
			foreach (TSource item in _source)
			{
				list.Add(_selector(item));
			}
			return list;
		}

		public int GetCount(bool onlyIfCheap)
		{
			if (onlyIfCheap)
			{
				return -1;
			}
			int num = 0;
			foreach (TSource item in _source)
			{
				_selector(item);
				num = checked(num + 1);
			}
			return num;
		}

		public SelectEnumerableIterator(IEnumerable<TSource> source, Func<TSource, TResult> selector)
		{
			_source = source;
			_selector = selector;
		}

		public override Iterator<TResult> Clone()
		{
			return new SelectEnumerableIterator<TSource, TResult>(_source, _selector);
		}

		public override void Dispose()
		{
			if (_enumerator != null)
			{
				_enumerator.Dispose();
				_enumerator = null;
			}
			base.Dispose();
		}

		public override bool MoveNext()
		{
			int state = _state;
			if (state != 1)
			{
				if (state != 2)
				{
					goto IL_005a;
				}
			}
			else
			{
				_enumerator = _source.GetEnumerator();
				_state = 2;
			}
			if (_enumerator.MoveNext())
			{
				_current = _selector(_enumerator.Current);
				return true;
			}
			Dispose();
			goto IL_005a;
			IL_005a:
			return false;
		}

		public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
		{
			return new SelectEnumerableIterator<TSource, TResult2>(_source, Utilities.CombineSelectors(_selector, selector));
		}
	}

	[DebuggerDisplay("Count = {CountForDebugger}")]
	private sealed class SelectArrayIterator<TSource, TResult> : Iterator<TResult>, IPartition<TResult>, IIListProvider<TResult>, IEnumerable<TResult>, IEnumerable
	{
		private readonly TSource[] _source;

		private readonly Func<TSource, TResult> _selector;

		private int CountForDebugger => _source.Length;

		public TResult[] ToArray()
		{
			TResult[] array = new TResult[_source.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = _selector(_source[i]);
			}
			return array;
		}

		public List<TResult> ToList()
		{
			TSource[] source = _source;
			List<TResult> list = new List<TResult>(source.Length);
			for (int i = 0; i < source.Length; i++)
			{
				list.Add(_selector(source[i]));
			}
			return list;
		}

		public int GetCount(bool onlyIfCheap)
		{
			if (!onlyIfCheap)
			{
				TSource[] source = _source;
				foreach (TSource arg in source)
				{
					_selector(arg);
				}
			}
			return _source.Length;
		}

		public IPartition<TResult> Skip(int count)
		{
			if (count >= _source.Length)
			{
				return EmptyPartition<TResult>.Instance;
			}
			return new SelectListPartitionIterator<TSource, TResult>(_source, _selector, count, int.MaxValue);
		}

		public IPartition<TResult> Take(int count)
		{
			if (count < _source.Length)
			{
				return new SelectListPartitionIterator<TSource, TResult>(_source, _selector, 0, count - 1);
			}
			return this;
		}

		public TResult TryGetElementAt(int index, out bool found)
		{
			if ((uint)index < (uint)_source.Length)
			{
				found = true;
				return _selector(_source[index]);
			}
			found = false;
			return default(TResult);
		}

		public TResult TryGetFirst(out bool found)
		{
			found = true;
			return _selector(_source[0]);
		}

		public TResult TryGetLast(out bool found)
		{
			found = true;
			return _selector(_source[_source.Length - 1]);
		}

		public SelectArrayIterator(TSource[] source, Func<TSource, TResult> selector)
		{
			_source = source;
			_selector = selector;
		}

		public override Iterator<TResult> Clone()
		{
			return new SelectArrayIterator<TSource, TResult>(_source, _selector);
		}

		public override bool MoveNext()
		{
			if ((_state < 1) | (_state == _source.Length + 1))
			{
				Dispose();
				return false;
			}
			int num = _state++ - 1;
			_current = _selector(_source[num]);
			return true;
		}

		public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
		{
			return new SelectArrayIterator<TSource, TResult2>(_source, Utilities.CombineSelectors(_selector, selector));
		}
	}

	private sealed class SelectRangeIterator<TResult> : Iterator<TResult>, IPartition<TResult>, IIListProvider<TResult>, IEnumerable<TResult>, IEnumerable
	{
		private readonly int _start;

		private readonly int _end;

		private readonly Func<int, TResult> _selector;

		public SelectRangeIterator(int start, int end, Func<int, TResult> selector)
		{
			_start = start;
			_end = end;
			_selector = selector;
		}

		public override Iterator<TResult> Clone()
		{
			return new SelectRangeIterator<TResult>(_start, _end, _selector);
		}

		public override bool MoveNext()
		{
			if (_state < 1 || _state == _end - _start + 1)
			{
				Dispose();
				return false;
			}
			int num = _state++ - 1;
			_current = _selector(_start + num);
			return true;
		}

		public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
		{
			return new SelectRangeIterator<TResult2>(_start, _end, Utilities.CombineSelectors(_selector, selector));
		}

		public TResult[] ToArray()
		{
			TResult[] array = new TResult[_end - _start];
			int start = _start;
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = _selector(start++);
			}
			return array;
		}

		public List<TResult> ToList()
		{
			List<TResult> list = new List<TResult>(_end - _start);
			for (int i = _start; i != _end; i++)
			{
				list.Add(_selector(i));
			}
			return list;
		}

		public int GetCount(bool onlyIfCheap)
		{
			if (!onlyIfCheap)
			{
				for (int i = _start; i != _end; i++)
				{
					_selector(i);
				}
			}
			return _end - _start;
		}

		public IPartition<TResult> Skip(int count)
		{
			if (count >= _end - _start)
			{
				return EmptyPartition<TResult>.Instance;
			}
			return new SelectRangeIterator<TResult>(_start + count, _end, _selector);
		}

		public IPartition<TResult> Take(int count)
		{
			if (count >= _end - _start)
			{
				return this;
			}
			return new SelectRangeIterator<TResult>(_start, _start + count, _selector);
		}

		public TResult TryGetElementAt(int index, out bool found)
		{
			if ((uint)index < (uint)(_end - _start))
			{
				found = true;
				return _selector(_start + index);
			}
			found = false;
			return default(TResult);
		}

		public TResult TryGetFirst(out bool found)
		{
			found = true;
			return _selector(_start);
		}

		public TResult TryGetLast(out bool found)
		{
			found = true;
			return _selector(_end - 1);
		}
	}

	[DebuggerDisplay("Count = {CountForDebugger}")]
	private sealed class SelectListIterator<TSource, TResult> : Iterator<TResult>, IPartition<TResult>, IIListProvider<TResult>, IEnumerable<TResult>, IEnumerable
	{
		private readonly List<TSource> _source;

		private readonly Func<TSource, TResult> _selector;

		private List<TSource>.Enumerator _enumerator;

		private int CountForDebugger => _source.Count;

		public TResult[] ToArray()
		{
			int count = _source.Count;
			if (count == 0)
			{
				return Array.Empty<TResult>();
			}
			TResult[] array = new TResult[count];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = _selector(_source[i]);
			}
			return array;
		}

		public List<TResult> ToList()
		{
			int count = _source.Count;
			List<TResult> list = new List<TResult>(count);
			for (int i = 0; i < count; i++)
			{
				list.Add(_selector(_source[i]));
			}
			return list;
		}

		public int GetCount(bool onlyIfCheap)
		{
			int count = _source.Count;
			if (!onlyIfCheap)
			{
				for (int i = 0; i < count; i++)
				{
					_selector(_source[i]);
				}
			}
			return count;
		}

		public IPartition<TResult> Skip(int count)
		{
			return new SelectListPartitionIterator<TSource, TResult>(_source, _selector, count, int.MaxValue);
		}

		public IPartition<TResult> Take(int count)
		{
			return new SelectListPartitionIterator<TSource, TResult>(_source, _selector, 0, count - 1);
		}

		public TResult TryGetElementAt(int index, out bool found)
		{
			if ((uint)index < (uint)_source.Count)
			{
				found = true;
				return _selector(_source[index]);
			}
			found = false;
			return default(TResult);
		}

		public TResult TryGetFirst(out bool found)
		{
			if (_source.Count != 0)
			{
				found = true;
				return _selector(_source[0]);
			}
			found = false;
			return default(TResult);
		}

		public TResult TryGetLast(out bool found)
		{
			int count = _source.Count;
			if (count != 0)
			{
				found = true;
				return _selector(_source[count - 1]);
			}
			found = false;
			return default(TResult);
		}

		public SelectListIterator(List<TSource> source, Func<TSource, TResult> selector)
		{
			_source = source;
			_selector = selector;
		}

		public override Iterator<TResult> Clone()
		{
			return new SelectListIterator<TSource, TResult>(_source, _selector);
		}

		public override bool MoveNext()
		{
			int state = _state;
			if (state != 1)
			{
				if (state != 2)
				{
					goto IL_005a;
				}
			}
			else
			{
				_enumerator = _source.GetEnumerator();
				_state = 2;
			}
			if (_enumerator.MoveNext())
			{
				_current = _selector(_enumerator.Current);
				return true;
			}
			Dispose();
			goto IL_005a;
			IL_005a:
			return false;
		}

		public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
		{
			return new SelectListIterator<TSource, TResult2>(_source, Utilities.CombineSelectors(_selector, selector));
		}
	}

	[DebuggerDisplay("Count = {CountForDebugger}")]
	private sealed class SelectIListIterator<TSource, TResult> : Iterator<TResult>, IPartition<TResult>, IIListProvider<TResult>, IEnumerable<TResult>, IEnumerable
	{
		private readonly IList<TSource> _source;

		private readonly Func<TSource, TResult> _selector;

		private IEnumerator<TSource> _enumerator;

		private int CountForDebugger => _source.Count;

		public TResult[] ToArray()
		{
			int count = _source.Count;
			if (count == 0)
			{
				return Array.Empty<TResult>();
			}
			TResult[] array = new TResult[count];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = _selector(_source[i]);
			}
			return array;
		}

		public List<TResult> ToList()
		{
			int count = _source.Count;
			List<TResult> list = new List<TResult>(count);
			for (int i = 0; i < count; i++)
			{
				list.Add(_selector(_source[i]));
			}
			return list;
		}

		public int GetCount(bool onlyIfCheap)
		{
			int count = _source.Count;
			if (!onlyIfCheap)
			{
				for (int i = 0; i < count; i++)
				{
					_selector(_source[i]);
				}
			}
			return count;
		}

		public IPartition<TResult> Skip(int count)
		{
			return new SelectListPartitionIterator<TSource, TResult>(_source, _selector, count, int.MaxValue);
		}

		public IPartition<TResult> Take(int count)
		{
			return new SelectListPartitionIterator<TSource, TResult>(_source, _selector, 0, count - 1);
		}

		public TResult TryGetElementAt(int index, out bool found)
		{
			if ((uint)index < (uint)_source.Count)
			{
				found = true;
				return _selector(_source[index]);
			}
			found = false;
			return default(TResult);
		}

		public TResult TryGetFirst(out bool found)
		{
			if (_source.Count != 0)
			{
				found = true;
				return _selector(_source[0]);
			}
			found = false;
			return default(TResult);
		}

		public TResult TryGetLast(out bool found)
		{
			int count = _source.Count;
			if (count != 0)
			{
				found = true;
				return _selector(_source[count - 1]);
			}
			found = false;
			return default(TResult);
		}

		public SelectIListIterator(IList<TSource> source, Func<TSource, TResult> selector)
		{
			_source = source;
			_selector = selector;
		}

		public override Iterator<TResult> Clone()
		{
			return new SelectIListIterator<TSource, TResult>(_source, _selector);
		}

		public override bool MoveNext()
		{
			int state = _state;
			if (state != 1)
			{
				if (state != 2)
				{
					goto IL_005a;
				}
			}
			else
			{
				_enumerator = _source.GetEnumerator();
				_state = 2;
			}
			if (_enumerator.MoveNext())
			{
				_current = _selector(_enumerator.Current);
				return true;
			}
			Dispose();
			goto IL_005a;
			IL_005a:
			return false;
		}

		public override void Dispose()
		{
			if (_enumerator != null)
			{
				_enumerator.Dispose();
				_enumerator = null;
			}
			base.Dispose();
		}

		public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
		{
			return new SelectIListIterator<TSource, TResult2>(_source, Utilities.CombineSelectors(_selector, selector));
		}
	}

	private sealed class SelectIPartitionIterator<TSource, TResult> : Iterator<TResult>, IPartition<TResult>, IIListProvider<TResult>, IEnumerable<TResult>, IEnumerable
	{
		private readonly IPartition<TSource> _source;

		private readonly Func<TSource, TResult> _selector;

		private IEnumerator<TSource> _enumerator;

		public SelectIPartitionIterator(IPartition<TSource> source, Func<TSource, TResult> selector)
		{
			_source = source;
			_selector = selector;
		}

		public override Iterator<TResult> Clone()
		{
			return new SelectIPartitionIterator<TSource, TResult>(_source, _selector);
		}

		public override bool MoveNext()
		{
			int state = _state;
			if (state != 1)
			{
				if (state != 2)
				{
					goto IL_005a;
				}
			}
			else
			{
				_enumerator = _source.GetEnumerator();
				_state = 2;
			}
			if (_enumerator.MoveNext())
			{
				_current = _selector(_enumerator.Current);
				return true;
			}
			Dispose();
			goto IL_005a;
			IL_005a:
			return false;
		}

		public override void Dispose()
		{
			if (_enumerator != null)
			{
				_enumerator.Dispose();
				_enumerator = null;
			}
			base.Dispose();
		}

		public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
		{
			return new SelectIPartitionIterator<TSource, TResult2>(_source, Utilities.CombineSelectors(_selector, selector));
		}

		public IPartition<TResult> Skip(int count)
		{
			return new SelectIPartitionIterator<TSource, TResult>(_source.Skip(count), _selector);
		}

		public IPartition<TResult> Take(int count)
		{
			return new SelectIPartitionIterator<TSource, TResult>(_source.Take(count), _selector);
		}

		public TResult TryGetElementAt(int index, out bool found)
		{
			bool found2;
			TSource arg = _source.TryGetElementAt(index, out found2);
			found = found2;
			if (!found2)
			{
				return default(TResult);
			}
			return _selector(arg);
		}

		public TResult TryGetFirst(out bool found)
		{
			bool found2;
			TSource arg = _source.TryGetFirst(out found2);
			found = found2;
			if (!found2)
			{
				return default(TResult);
			}
			return _selector(arg);
		}

		public TResult TryGetLast(out bool found)
		{
			bool found2;
			TSource arg = _source.TryGetLast(out found2);
			found = found2;
			if (!found2)
			{
				return default(TResult);
			}
			return _selector(arg);
		}

		private TResult[] LazyToArray()
		{
			LargeArrayBuilder<TResult> largeArrayBuilder = new LargeArrayBuilder<TResult>(initialize: true);
			foreach (TSource item in _source)
			{
				largeArrayBuilder.Add(_selector(item));
			}
			return largeArrayBuilder.ToArray();
		}

		private TResult[] PreallocatingToArray(int count)
		{
			TResult[] array = new TResult[count];
			int num = 0;
			foreach (TSource item in _source)
			{
				array[num] = _selector(item);
				num++;
			}
			return array;
		}

		public TResult[] ToArray()
		{
			int count = _source.GetCount(onlyIfCheap: true);
			return count switch
			{
				-1 => LazyToArray(), 
				0 => Array.Empty<TResult>(), 
				_ => PreallocatingToArray(count), 
			};
		}

		public List<TResult> ToList()
		{
			int count = _source.GetCount(onlyIfCheap: true);
			List<TResult> list;
			switch (count)
			{
			case -1:
				list = new List<TResult>();
				break;
			case 0:
				return new List<TResult>();
			default:
				list = new List<TResult>(count);
				break;
			}
			foreach (TSource item in _source)
			{
				list.Add(_selector(item));
			}
			return list;
		}

		public int GetCount(bool onlyIfCheap)
		{
			if (!onlyIfCheap)
			{
				int num = 0;
				{
					foreach (TSource item in _source)
					{
						_selector(item);
						num = checked(num + 1);
					}
					return num;
				}
			}
			return _source.GetCount(onlyIfCheap);
		}
	}

	[DebuggerDisplay("Count = {Count}")]
	private sealed class SelectListPartitionIterator<TSource, TResult> : Iterator<TResult>, IPartition<TResult>, IIListProvider<TResult>, IEnumerable<TResult>, IEnumerable
	{
		private readonly IList<TSource> _source;

		private readonly Func<TSource, TResult> _selector;

		private readonly int _minIndexInclusive;

		private readonly int _maxIndexInclusive;

		private int Count
		{
			get
			{
				int count = _source.Count;
				if (count <= _minIndexInclusive)
				{
					return 0;
				}
				return Math.Min(count - 1, _maxIndexInclusive) - _minIndexInclusive + 1;
			}
		}

		public SelectListPartitionIterator(IList<TSource> source, Func<TSource, TResult> selector, int minIndexInclusive, int maxIndexInclusive)
		{
			_source = source;
			_selector = selector;
			_minIndexInclusive = minIndexInclusive;
			_maxIndexInclusive = maxIndexInclusive;
		}

		public override Iterator<TResult> Clone()
		{
			return new SelectListPartitionIterator<TSource, TResult>(_source, _selector, _minIndexInclusive, _maxIndexInclusive);
		}

		public override bool MoveNext()
		{
			int num = _state - 1;
			if ((uint)num <= (uint)(_maxIndexInclusive - _minIndexInclusive) && num < _source.Count - _minIndexInclusive)
			{
				_current = _selector(_source[_minIndexInclusive + num]);
				_state++;
				return true;
			}
			Dispose();
			return false;
		}

		public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
		{
			return new SelectListPartitionIterator<TSource, TResult2>(_source, Utilities.CombineSelectors(_selector, selector), _minIndexInclusive, _maxIndexInclusive);
		}

		public IPartition<TResult> Skip(int count)
		{
			int num = _minIndexInclusive + count;
			if ((uint)num <= (uint)_maxIndexInclusive)
			{
				return new SelectListPartitionIterator<TSource, TResult>(_source, _selector, num, _maxIndexInclusive);
			}
			return EmptyPartition<TResult>.Instance;
		}

		public IPartition<TResult> Take(int count)
		{
			int num = _minIndexInclusive + count - 1;
			if ((uint)num < (uint)_maxIndexInclusive)
			{
				return new SelectListPartitionIterator<TSource, TResult>(_source, _selector, _minIndexInclusive, num);
			}
			return this;
		}

		public TResult TryGetElementAt(int index, out bool found)
		{
			if ((uint)index <= (uint)(_maxIndexInclusive - _minIndexInclusive) && index < _source.Count - _minIndexInclusive)
			{
				found = true;
				return _selector(_source[_minIndexInclusive + index]);
			}
			found = false;
			return default(TResult);
		}

		public TResult TryGetFirst(out bool found)
		{
			if (_source.Count > _minIndexInclusive)
			{
				found = true;
				return _selector(_source[_minIndexInclusive]);
			}
			found = false;
			return default(TResult);
		}

		public TResult TryGetLast(out bool found)
		{
			int num = _source.Count - 1;
			if (num >= _minIndexInclusive)
			{
				found = true;
				return _selector(_source[Math.Min(num, _maxIndexInclusive)]);
			}
			found = false;
			return default(TResult);
		}

		public TResult[] ToArray()
		{
			int count = Count;
			if (count == 0)
			{
				return Array.Empty<TResult>();
			}
			TResult[] array = new TResult[count];
			int num = 0;
			int num2 = _minIndexInclusive;
			while (num != array.Length)
			{
				array[num] = _selector(_source[num2]);
				num++;
				num2++;
			}
			return array;
		}

		public List<TResult> ToList()
		{
			int count = Count;
			if (count == 0)
			{
				return new List<TResult>();
			}
			List<TResult> list = new List<TResult>(count);
			int num = _minIndexInclusive + count;
			for (int i = _minIndexInclusive; i != num; i++)
			{
				list.Add(_selector(_source[i]));
			}
			return list;
		}

		public int GetCount(bool onlyIfCheap)
		{
			int count = Count;
			if (!onlyIfCheap)
			{
				int num = _minIndexInclusive + count;
				for (int i = _minIndexInclusive; i != num; i++)
				{
					_selector(_source[i]);
				}
			}
			return count;
		}
	}

	private sealed class SelectManySingleSelectorIterator<TSource, TResult> : Iterator<TResult>, IIListProvider<TResult>, IEnumerable<TResult>, IEnumerable
	{
		private readonly IEnumerable<TSource> _source;

		private readonly Func<TSource, IEnumerable<TResult>> _selector;

		private IEnumerator<TSource> _sourceEnumerator;

		private IEnumerator<TResult> _subEnumerator;

		public int GetCount(bool onlyIfCheap)
		{
			if (onlyIfCheap)
			{
				return -1;
			}
			int num = 0;
			foreach (TSource item in _source)
			{
				num = checked(num + _selector(item).Count());
			}
			return num;
		}

		public TResult[] ToArray()
		{
			SparseArrayBuilder<TResult> sparseArrayBuilder = new SparseArrayBuilder<TResult>(initialize: true);
			System.Collections.Generic.ArrayBuilder<IEnumerable<TResult>> arrayBuilder = default(System.Collections.Generic.ArrayBuilder<IEnumerable<TResult>>);
			foreach (TSource item in _source)
			{
				IEnumerable<TResult> enumerable = _selector(item);
				if (sparseArrayBuilder.ReserveOrAdd(enumerable))
				{
					arrayBuilder.Add(enumerable);
				}
			}
			TResult[] array = sparseArrayBuilder.ToArray();
			System.Collections.Generic.ArrayBuilder<Marker> markers = sparseArrayBuilder.Markers;
			for (int i = 0; i < markers.Count; i++)
			{
				Marker marker = markers[i];
				IEnumerable<TResult> source = arrayBuilder[i];
				System.Collections.Generic.EnumerableHelpers.Copy(source, array, marker.Index, marker.Count);
			}
			return array;
		}

		public List<TResult> ToList()
		{
			List<TResult> list = new List<TResult>();
			foreach (TSource item in _source)
			{
				list.AddRange(_selector(item));
			}
			return list;
		}

		internal SelectManySingleSelectorIterator(IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
		{
			_source = source;
			_selector = selector;
		}

		public override Iterator<TResult> Clone()
		{
			return new SelectManySingleSelectorIterator<TSource, TResult>(_source, _selector);
		}

		public override void Dispose()
		{
			if (_subEnumerator != null)
			{
				_subEnumerator.Dispose();
				_subEnumerator = null;
			}
			if (_sourceEnumerator != null)
			{
				_sourceEnumerator.Dispose();
				_sourceEnumerator = null;
			}
			base.Dispose();
		}

		public override bool MoveNext()
		{
			switch (_state)
			{
			case 1:
				_sourceEnumerator = _source.GetEnumerator();
				_state = 2;
				goto case 2;
			case 2:
			{
				if (!_sourceEnumerator.MoveNext())
				{
					break;
				}
				TSource current = _sourceEnumerator.Current;
				_subEnumerator = _selector(current).GetEnumerator();
				_state = 3;
				goto case 3;
			}
			case 3:
				if (!_subEnumerator.MoveNext())
				{
					_subEnumerator.Dispose();
					_subEnumerator = null;
					_state = 2;
					goto case 2;
				}
				_current = _subEnumerator.Current;
				return true;
			}
			Dispose();
			return false;
		}
	}

	private abstract class UnionIterator<TSource> : Iterator<TSource>, IIListProvider<TSource>, IEnumerable<TSource>, IEnumerable
	{
		internal readonly IEqualityComparer<TSource> _comparer;

		private IEnumerator<TSource> _enumerator;

		private HashSet<TSource> _set;

		private HashSet<TSource> FillSet()
		{
			HashSet<TSource> hashSet = new HashSet<TSource>(_comparer);
			int num = 0;
			while (true)
			{
				IEnumerable<TSource> enumerable = GetEnumerable(num);
				if (enumerable == null)
				{
					break;
				}
				hashSet.UnionWith(enumerable);
				num++;
			}
			return hashSet;
		}

		public TSource[] ToArray()
		{
			return HashSetToArray(FillSet());
		}

		public List<TSource> ToList()
		{
			return HashSetToList(FillSet());
		}

		public int GetCount(bool onlyIfCheap)
		{
			if (!onlyIfCheap)
			{
				return FillSet().Count;
			}
			return -1;
		}

		protected UnionIterator(IEqualityComparer<TSource> comparer)
		{
			_comparer = comparer;
		}

		public sealed override void Dispose()
		{
			if (_enumerator != null)
			{
				_enumerator.Dispose();
				_enumerator = null;
				_set = null;
			}
			base.Dispose();
		}

		internal abstract IEnumerable<TSource> GetEnumerable(int index);

		internal abstract UnionIterator<TSource> Union(IEnumerable<TSource> next);

		private void SetEnumerator(IEnumerator<TSource> enumerator)
		{
			_enumerator?.Dispose();
			_enumerator = enumerator;
		}

		private void StoreFirst()
		{
			HashSet<TSource> hashSet = new HashSet<TSource>(7, _comparer);
			TSource current = _enumerator.Current;
			hashSet.Add(current);
			_current = current;
			_set = hashSet;
		}

		private bool GetNext()
		{
			HashSet<TSource> set = _set;
			while (_enumerator.MoveNext())
			{
				TSource current = _enumerator.Current;
				if (set.Add(current))
				{
					_current = current;
					return true;
				}
			}
			return false;
		}

		public sealed override bool MoveNext()
		{
			if (_state == 1)
			{
				for (IEnumerable<TSource> enumerable = GetEnumerable(0); enumerable != null; enumerable = GetEnumerable(_state - 1))
				{
					IEnumerator<TSource> enumerator = enumerable.GetEnumerator();
					SetEnumerator(enumerator);
					_state++;
					if (enumerator.MoveNext())
					{
						StoreFirst();
						return true;
					}
				}
			}
			else if (_state > 0)
			{
				while (true)
				{
					if (GetNext())
					{
						return true;
					}
					IEnumerable<TSource> enumerable2 = GetEnumerable(_state - 1);
					if (enumerable2 == null)
					{
						break;
					}
					SetEnumerator(enumerable2.GetEnumerator());
					_state++;
				}
			}
			Dispose();
			return false;
		}
	}

	private sealed class WhereEnumerableIterator<TSource> : Iterator<TSource>, IIListProvider<TSource>, IEnumerable<TSource>, IEnumerable
	{
		private readonly IEnumerable<TSource> _source;

		private readonly Func<TSource, bool> _predicate;

		private IEnumerator<TSource> _enumerator;

		public int GetCount(bool onlyIfCheap)
		{
			if (onlyIfCheap)
			{
				return -1;
			}
			int num = 0;
			foreach (TSource item in _source)
			{
				if (_predicate(item))
				{
					num = checked(num + 1);
				}
			}
			return num;
		}

		public TSource[] ToArray()
		{
			LargeArrayBuilder<TSource> largeArrayBuilder = new LargeArrayBuilder<TSource>(initialize: true);
			foreach (TSource item in _source)
			{
				if (_predicate(item))
				{
					largeArrayBuilder.Add(item);
				}
			}
			return largeArrayBuilder.ToArray();
		}

		public List<TSource> ToList()
		{
			List<TSource> list = new List<TSource>();
			foreach (TSource item in _source)
			{
				if (_predicate(item))
				{
					list.Add(item);
				}
			}
			return list;
		}

		public WhereEnumerableIterator(IEnumerable<TSource> source, Func<TSource, bool> predicate)
		{
			_source = source;
			_predicate = predicate;
		}

		public override Iterator<TSource> Clone()
		{
			return new WhereEnumerableIterator<TSource>(_source, _predicate);
		}

		public override void Dispose()
		{
			if (_enumerator != null)
			{
				_enumerator.Dispose();
				_enumerator = null;
			}
			base.Dispose();
		}

		public override bool MoveNext()
		{
			int state = _state;
			if (state != 1)
			{
				if (state != 2)
				{
					goto IL_0061;
				}
			}
			else
			{
				_enumerator = _source.GetEnumerator();
				_state = 2;
			}
			while (_enumerator.MoveNext())
			{
				TSource current = _enumerator.Current;
				if (_predicate(current))
				{
					_current = current;
					return true;
				}
			}
			Dispose();
			goto IL_0061;
			IL_0061:
			return false;
		}

		public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector)
		{
			return new WhereSelectEnumerableIterator<TSource, TResult>(_source, _predicate, selector);
		}

		public override IEnumerable<TSource> Where(Func<TSource, bool> predicate)
		{
			return new WhereEnumerableIterator<TSource>(_source, Utilities.CombinePredicates(_predicate, predicate));
		}
	}

	internal sealed class WhereArrayIterator<TSource> : Iterator<TSource>, IIListProvider<TSource>, IEnumerable<TSource>, IEnumerable
	{
		private readonly TSource[] _source;

		private readonly Func<TSource, bool> _predicate;

		public int GetCount(bool onlyIfCheap)
		{
			if (onlyIfCheap)
			{
				return -1;
			}
			int num = 0;
			TSource[] source = _source;
			foreach (TSource arg in source)
			{
				if (_predicate(arg))
				{
					num = checked(num + 1);
				}
			}
			return num;
		}

		public TSource[] ToArray()
		{
			LargeArrayBuilder<TSource> largeArrayBuilder = new LargeArrayBuilder<TSource>(_source.Length);
			TSource[] source = _source;
			foreach (TSource val in source)
			{
				if (_predicate(val))
				{
					largeArrayBuilder.Add(val);
				}
			}
			return largeArrayBuilder.ToArray();
		}

		public List<TSource> ToList()
		{
			List<TSource> list = new List<TSource>();
			TSource[] source = _source;
			foreach (TSource val in source)
			{
				if (_predicate(val))
				{
					list.Add(val);
				}
			}
			return list;
		}

		public WhereArrayIterator(TSource[] source, Func<TSource, bool> predicate)
		{
			_source = source;
			_predicate = predicate;
		}

		public override Iterator<TSource> Clone()
		{
			return new WhereArrayIterator<TSource>(_source, _predicate);
		}

		public override bool MoveNext()
		{
			int num = _state - 1;
			TSource[] source = _source;
			while ((uint)num < (uint)source.Length)
			{
				TSource val = source[num];
				num = _state++;
				if (_predicate(val))
				{
					_current = val;
					return true;
				}
			}
			Dispose();
			return false;
		}

		public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector)
		{
			return new WhereSelectArrayIterator<TSource, TResult>(_source, _predicate, selector);
		}

		public override IEnumerable<TSource> Where(Func<TSource, bool> predicate)
		{
			return new WhereArrayIterator<TSource>(_source, Utilities.CombinePredicates(_predicate, predicate));
		}
	}

	private sealed class WhereListIterator<TSource> : Iterator<TSource>, IIListProvider<TSource>, IEnumerable<TSource>, IEnumerable
	{
		private readonly List<TSource> _source;

		private readonly Func<TSource, bool> _predicate;

		private List<TSource>.Enumerator _enumerator;

		public int GetCount(bool onlyIfCheap)
		{
			if (onlyIfCheap)
			{
				return -1;
			}
			int num = 0;
			for (int i = 0; i < _source.Count; i++)
			{
				TSource arg = _source[i];
				if (_predicate(arg))
				{
					num = checked(num + 1);
				}
			}
			return num;
		}

		public TSource[] ToArray()
		{
			LargeArrayBuilder<TSource> largeArrayBuilder = new LargeArrayBuilder<TSource>(_source.Count);
			for (int i = 0; i < _source.Count; i++)
			{
				TSource val = _source[i];
				if (_predicate(val))
				{
					largeArrayBuilder.Add(val);
				}
			}
			return largeArrayBuilder.ToArray();
		}

		public List<TSource> ToList()
		{
			List<TSource> list = new List<TSource>();
			for (int i = 0; i < _source.Count; i++)
			{
				TSource val = _source[i];
				if (_predicate(val))
				{
					list.Add(val);
				}
			}
			return list;
		}

		public WhereListIterator(List<TSource> source, Func<TSource, bool> predicate)
		{
			_source = source;
			_predicate = predicate;
		}

		public override Iterator<TSource> Clone()
		{
			return new WhereListIterator<TSource>(_source, _predicate);
		}

		public override bool MoveNext()
		{
			int state = _state;
			if (state != 1)
			{
				if (state != 2)
				{
					goto IL_0061;
				}
			}
			else
			{
				_enumerator = _source.GetEnumerator();
				_state = 2;
			}
			while (_enumerator.MoveNext())
			{
				TSource current = _enumerator.Current;
				if (_predicate(current))
				{
					_current = current;
					return true;
				}
			}
			Dispose();
			goto IL_0061;
			IL_0061:
			return false;
		}

		public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector)
		{
			return new WhereSelectListIterator<TSource, TResult>(_source, _predicate, selector);
		}

		public override IEnumerable<TSource> Where(Func<TSource, bool> predicate)
		{
			return new WhereListIterator<TSource>(_source, Utilities.CombinePredicates(_predicate, predicate));
		}
	}

	private sealed class WhereSelectArrayIterator<TSource, TResult> : Iterator<TResult>, IIListProvider<TResult>, IEnumerable<TResult>, IEnumerable
	{
		private readonly TSource[] _source;

		private readonly Func<TSource, bool> _predicate;

		private readonly Func<TSource, TResult> _selector;

		public int GetCount(bool onlyIfCheap)
		{
			if (onlyIfCheap)
			{
				return -1;
			}
			int num = 0;
			TSource[] source = _source;
			foreach (TSource arg in source)
			{
				if (_predicate(arg))
				{
					_selector(arg);
					num = checked(num + 1);
				}
			}
			return num;
		}

		public TResult[] ToArray()
		{
			LargeArrayBuilder<TResult> largeArrayBuilder = new LargeArrayBuilder<TResult>(_source.Length);
			TSource[] source = _source;
			foreach (TSource arg in source)
			{
				if (_predicate(arg))
				{
					largeArrayBuilder.Add(_selector(arg));
				}
			}
			return largeArrayBuilder.ToArray();
		}

		public List<TResult> ToList()
		{
			List<TResult> list = new List<TResult>();
			TSource[] source = _source;
			foreach (TSource arg in source)
			{
				if (_predicate(arg))
				{
					list.Add(_selector(arg));
				}
			}
			return list;
		}

		public WhereSelectArrayIterator(TSource[] source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
		{
			_source = source;
			_predicate = predicate;
			_selector = selector;
		}

		public override Iterator<TResult> Clone()
		{
			return new WhereSelectArrayIterator<TSource, TResult>(_source, _predicate, _selector);
		}

		public override bool MoveNext()
		{
			int num = _state - 1;
			TSource[] source = _source;
			while ((uint)num < (uint)source.Length)
			{
				TSource arg = source[num];
				num = _state++;
				if (_predicate(arg))
				{
					_current = _selector(arg);
					return true;
				}
			}
			Dispose();
			return false;
		}

		public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
		{
			return new WhereSelectArrayIterator<TSource, TResult2>(_source, _predicate, Utilities.CombineSelectors(_selector, selector));
		}
	}

	private sealed class WhereSelectListIterator<TSource, TResult> : Iterator<TResult>, IIListProvider<TResult>, IEnumerable<TResult>, IEnumerable
	{
		private readonly List<TSource> _source;

		private readonly Func<TSource, bool> _predicate;

		private readonly Func<TSource, TResult> _selector;

		private List<TSource>.Enumerator _enumerator;

		public int GetCount(bool onlyIfCheap)
		{
			if (onlyIfCheap)
			{
				return -1;
			}
			int num = 0;
			for (int i = 0; i < _source.Count; i++)
			{
				TSource arg = _source[i];
				if (_predicate(arg))
				{
					_selector(arg);
					num = checked(num + 1);
				}
			}
			return num;
		}

		public TResult[] ToArray()
		{
			LargeArrayBuilder<TResult> largeArrayBuilder = new LargeArrayBuilder<TResult>(_source.Count);
			for (int i = 0; i < _source.Count; i++)
			{
				TSource arg = _source[i];
				if (_predicate(arg))
				{
					largeArrayBuilder.Add(_selector(arg));
				}
			}
			return largeArrayBuilder.ToArray();
		}

		public List<TResult> ToList()
		{
			List<TResult> list = new List<TResult>();
			for (int i = 0; i < _source.Count; i++)
			{
				TSource arg = _source[i];
				if (_predicate(arg))
				{
					list.Add(_selector(arg));
				}
			}
			return list;
		}

		public WhereSelectListIterator(List<TSource> source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
		{
			_source = source;
			_predicate = predicate;
			_selector = selector;
		}

		public override Iterator<TResult> Clone()
		{
			return new WhereSelectListIterator<TSource, TResult>(_source, _predicate, _selector);
		}

		public override bool MoveNext()
		{
			int state = _state;
			if (state != 1)
			{
				if (state != 2)
				{
					goto IL_006c;
				}
			}
			else
			{
				_enumerator = _source.GetEnumerator();
				_state = 2;
			}
			while (_enumerator.MoveNext())
			{
				TSource current = _enumerator.Current;
				if (_predicate(current))
				{
					_current = _selector(current);
					return true;
				}
			}
			Dispose();
			goto IL_006c;
			IL_006c:
			return false;
		}

		public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
		{
			return new WhereSelectListIterator<TSource, TResult2>(_source, _predicate, Utilities.CombineSelectors(_selector, selector));
		}
	}

	private sealed class WhereSelectEnumerableIterator<TSource, TResult> : Iterator<TResult>, IIListProvider<TResult>, IEnumerable<TResult>, IEnumerable
	{
		private readonly IEnumerable<TSource> _source;

		private readonly Func<TSource, bool> _predicate;

		private readonly Func<TSource, TResult> _selector;

		private IEnumerator<TSource> _enumerator;

		public int GetCount(bool onlyIfCheap)
		{
			if (onlyIfCheap)
			{
				return -1;
			}
			int num = 0;
			foreach (TSource item in _source)
			{
				if (_predicate(item))
				{
					_selector(item);
					num = checked(num + 1);
				}
			}
			return num;
		}

		public TResult[] ToArray()
		{
			LargeArrayBuilder<TResult> largeArrayBuilder = new LargeArrayBuilder<TResult>(initialize: true);
			foreach (TSource item in _source)
			{
				if (_predicate(item))
				{
					largeArrayBuilder.Add(_selector(item));
				}
			}
			return largeArrayBuilder.ToArray();
		}

		public List<TResult> ToList()
		{
			List<TResult> list = new List<TResult>();
			foreach (TSource item in _source)
			{
				if (_predicate(item))
				{
					list.Add(_selector(item));
				}
			}
			return list;
		}

		public WhereSelectEnumerableIterator(IEnumerable<TSource> source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
		{
			_source = source;
			_predicate = predicate;
			_selector = selector;
		}

		public override Iterator<TResult> Clone()
		{
			return new WhereSelectEnumerableIterator<TSource, TResult>(_source, _predicate, _selector);
		}

		public override void Dispose()
		{
			if (_enumerator != null)
			{
				_enumerator.Dispose();
				_enumerator = null;
			}
			base.Dispose();
		}

		public override bool MoveNext()
		{
			int state = _state;
			if (state != 1)
			{
				if (state != 2)
				{
					goto IL_006c;
				}
			}
			else
			{
				_enumerator = _source.GetEnumerator();
				_state = 2;
			}
			while (_enumerator.MoveNext())
			{
				TSource current = _enumerator.Current;
				if (_predicate(current))
				{
					_current = _selector(current);
					return true;
				}
			}
			Dispose();
			goto IL_006c;
			IL_006c:
			return false;
		}

		public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
		{
			return new WhereSelectEnumerableIterator<TSource, TResult2>(_source, _predicate, Utilities.CombineSelectors(_selector, selector));
		}
	}

	internal abstract class Iterator<TSource> : IEnumerable<TSource>, IEnumerable, IEnumerator<TSource>, IEnumerator, IDisposable
	{
		private readonly int _threadId;

		internal int _state;

		internal TSource _current;

		public TSource Current => _current;

		object IEnumerator.Current => Current;

		protected Iterator()
		{
			_threadId = Environment.CurrentManagedThreadId;
		}

		public abstract Iterator<TSource> Clone();

		public virtual void Dispose()
		{
			_current = default(TSource);
			_state = -1;
		}

		public IEnumerator<TSource> GetEnumerator()
		{
			Iterator<TSource> iterator = ((_state == 0 && _threadId == Environment.CurrentManagedThreadId) ? this : Clone());
			iterator._state = 1;
			return iterator;
		}

		public abstract bool MoveNext();

		public virtual IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector)
		{
			return new SelectEnumerableIterator<TSource, TResult>(this, selector);
		}

		public virtual IEnumerable<TSource> Where(Func<TSource, bool> predicate)
		{
			return new WhereEnumerableIterator<TSource>(this, predicate);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		void IEnumerator.Reset()
		{
			ThrowHelper.ThrowNotSupportedException();
		}
	}

	private sealed class UnionIterator2<TSource> : UnionIterator<TSource>
	{
		private readonly IEnumerable<TSource> _first;

		private readonly IEnumerable<TSource> _second;

		public UnionIterator2(IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
			: base(comparer)
		{
			_first = first;
			_second = second;
		}

		public override Iterator<TSource> Clone()
		{
			return new UnionIterator2<TSource>(_first, _second, _comparer);
		}

		internal override IEnumerable<TSource> GetEnumerable(int index)
		{
			return index switch
			{
				0 => _first, 
				1 => _second, 
				_ => null, 
			};
		}

		internal override UnionIterator<TSource> Union(IEnumerable<TSource> next)
		{
			SingleLinkedNode<IEnumerable<TSource>> sources = new SingleLinkedNode<IEnumerable<TSource>>(_first).Add(_second).Add(next);
			return new UnionIteratorN<TSource>(sources, 2, _comparer);
		}
	}

	private sealed class UnionIteratorN<TSource> : UnionIterator<TSource>
	{
		private readonly SingleLinkedNode<IEnumerable<TSource>> _sources;

		private readonly int _headIndex;

		public UnionIteratorN(SingleLinkedNode<IEnumerable<TSource>> sources, int headIndex, IEqualityComparer<TSource> comparer)
			: base(comparer)
		{
			_sources = sources;
			_headIndex = headIndex;
		}

		public override Iterator<TSource> Clone()
		{
			return new UnionIteratorN<TSource>(_sources, _headIndex, _comparer);
		}

		internal override IEnumerable<TSource> GetEnumerable(int index)
		{
			if (index <= _headIndex)
			{
				return _sources.GetNode(_headIndex - index).Item;
			}
			return null;
		}

		internal override UnionIterator<TSource> Union(IEnumerable<TSource> next)
		{
			if (_headIndex == 2147483645)
			{
				return new UnionIterator2<TSource>(this, next, _comparer);
			}
			return new UnionIteratorN<TSource>(_sources.Add(next), _headIndex + 1, _comparer);
		}
	}

	public static IEnumerable<TResult> Empty<TResult>()
	{
		return EmptyPartition<TResult>.Instance;
	}

	private static IEnumerable<TSource> SkipIterator<TSource>(IEnumerable<TSource> source, int count)
	{
		if (!(source is IList<TSource> source2))
		{
			return new EnumerablePartition<TSource>(source, count, -1);
		}
		return new ListPartition<TSource>(source2, count, int.MaxValue);
	}

	private static IEnumerable<TSource> TakeIterator<TSource>(IEnumerable<TSource> source, int count)
	{
		if (!(source is IPartition<TSource> partition))
		{
			if (!(source is IList<TSource> source2))
			{
				return new EnumerablePartition<TSource>(source, 0, count - 1);
			}
			return new ListPartition<TSource>(source2, 0, count - 1);
		}
		return partition.Take(count);
	}

	private static IEnumerable<TSource> TakeRangeIterator<TSource>(IEnumerable<TSource> source, int startIndex, int endIndex)
	{
		if (!(source is IPartition<TSource> partition2))
		{
			if (!(source is IList<TSource> source2))
			{
				return new EnumerablePartition<TSource>(source, startIndex, endIndex - 1);
			}
			return new ListPartition<TSource>(source2, startIndex, endIndex - 1);
		}
		return TakePartitionRange(partition2, startIndex, endIndex);
		static IPartition<TSource> TakePartitionRange(IPartition<TSource> partition, int startIndex, int endIndex)
		{
			partition = ((endIndex == 0) ? EmptyPartition<TSource>.Instance : partition.Take(endIndex));
			if (startIndex != 0)
			{
				return partition.Skip(startIndex);
			}
			return partition;
		}
	}

	public static TSource Aggregate<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, TSource> func)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (func == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.func);
		}
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		TSource val = enumerator.Current;
		while (enumerator.MoveNext())
		{
			val = func(val, enumerator.Current);
		}
		return val;
	}

	public static TAccumulate Aggregate<TSource, TAccumulate>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (func == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.func);
		}
		TAccumulate val = seed;
		foreach (TSource item in source)
		{
			val = func(val, item);
		}
		return val;
	}

	public static TResult Aggregate<TSource, TAccumulate, TResult>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func, Func<TAccumulate, TResult> resultSelector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (func == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.func);
		}
		if (resultSelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
		}
		TAccumulate val = seed;
		foreach (TSource item in source)
		{
			val = func(val, item);
		}
		return resultSelector(val);
	}

	public static bool Any<TSource>(this IEnumerable<TSource> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (source is ICollection<TSource> collection)
		{
			return collection.Count != 0;
		}
		if (source is IIListProvider<TSource> iIListProvider)
		{
			int count = iIListProvider.GetCount(onlyIfCheap: true);
			if (count >= 0)
			{
				return count != 0;
			}
		}
		else if (source is ICollection collection2)
		{
			return collection2.Count != 0;
		}
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		return enumerator.MoveNext();
	}

	public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (predicate == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
		}
		foreach (TSource item in source)
		{
			if (predicate(item))
			{
				return true;
			}
		}
		return false;
	}

	public static bool All<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (predicate == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
		}
		foreach (TSource item in source)
		{
			if (!predicate(item))
			{
				return false;
			}
		}
		return true;
	}

	public static IEnumerable<TSource> Append<TSource>(this IEnumerable<TSource> source, TSource element)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (!(source is AppendPrependIterator<TSource> appendPrependIterator))
		{
			return new AppendPrepend1Iterator<TSource>(source, element, appending: true);
		}
		return appendPrependIterator.Append(element);
	}

	public static IEnumerable<TSource> Prepend<TSource>(this IEnumerable<TSource> source, TSource element)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (!(source is AppendPrependIterator<TSource> appendPrependIterator))
		{
			return new AppendPrepend1Iterator<TSource>(source, element, appending: false);
		}
		return appendPrependIterator.Prepend(element);
	}

	public static double Average(this IEnumerable<int> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		checked
		{
			using IEnumerator<int> enumerator = source.GetEnumerator();
			if (!enumerator.MoveNext())
			{
				ThrowHelper.ThrowNoElementsException();
			}
			long num = enumerator.Current;
			long num2 = 1L;
			while (enumerator.MoveNext())
			{
				num += enumerator.Current;
				num2++;
			}
			return (double)num / (double)num2;
		}
	}

	public static double? Average(this IEnumerable<int?> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		checked
		{
			using (IEnumerator<int?> enumerator = source.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					int? current = enumerator.Current;
					if (!current.HasValue)
					{
						continue;
					}
					long num = current.GetValueOrDefault();
					long num2 = 1L;
					while (enumerator.MoveNext())
					{
						current = enumerator.Current;
						if (current.HasValue)
						{
							num += current.GetValueOrDefault();
							num2++;
						}
					}
					return (double)num / (double)num2;
				}
			}
			return null;
		}
	}

	public static double Average(this IEnumerable<long> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		checked
		{
			using IEnumerator<long> enumerator = source.GetEnumerator();
			if (!enumerator.MoveNext())
			{
				ThrowHelper.ThrowNoElementsException();
			}
			long num = enumerator.Current;
			long num2 = 1L;
			while (enumerator.MoveNext())
			{
				num += enumerator.Current;
				num2++;
			}
			return (double)num / (double)num2;
		}
	}

	public static double? Average(this IEnumerable<long?> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		checked
		{
			using (IEnumerator<long?> enumerator = source.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					long? current = enumerator.Current;
					if (!current.HasValue)
					{
						continue;
					}
					long num = current.GetValueOrDefault();
					long num2 = 1L;
					while (enumerator.MoveNext())
					{
						current = enumerator.Current;
						if (current.HasValue)
						{
							num += current.GetValueOrDefault();
							num2++;
						}
					}
					return (double)num / (double)num2;
				}
			}
			return null;
		}
	}

	public static float Average(this IEnumerable<float> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		using IEnumerator<float> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		double num = enumerator.Current;
		long num2 = 1L;
		while (enumerator.MoveNext())
		{
			num += (double)enumerator.Current;
			num2++;
		}
		return (float)(num / (double)num2);
	}

	public static float? Average(this IEnumerable<float?> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		using (IEnumerator<float?> enumerator = source.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				float? current = enumerator.Current;
				if (!current.HasValue)
				{
					continue;
				}
				double num = current.GetValueOrDefault();
				long num2 = 1L;
				while (enumerator.MoveNext())
				{
					current = enumerator.Current;
					if (current.HasValue)
					{
						num += (double)current.GetValueOrDefault();
						num2 = checked(num2 + 1);
					}
				}
				return (float)(num / (double)num2);
			}
		}
		return null;
	}

	public static double Average(this IEnumerable<double> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		using IEnumerator<double> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		double num = enumerator.Current;
		long num2 = 1L;
		while (enumerator.MoveNext())
		{
			num += enumerator.Current;
			num2++;
		}
		return num / (double)num2;
	}

	public static double? Average(this IEnumerable<double?> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		using (IEnumerator<double?> enumerator = source.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				double? current = enumerator.Current;
				if (!current.HasValue)
				{
					continue;
				}
				double num = current.GetValueOrDefault();
				long num2 = 1L;
				while (enumerator.MoveNext())
				{
					current = enumerator.Current;
					if (current.HasValue)
					{
						num += current.GetValueOrDefault();
						num2 = checked(num2 + 1);
					}
				}
				return num / (double)num2;
			}
		}
		return null;
	}

	public static decimal Average(this IEnumerable<decimal> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		using IEnumerator<decimal> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		decimal current = enumerator.Current;
		long num = 1L;
		while (enumerator.MoveNext())
		{
			current += enumerator.Current;
			num++;
		}
		return current / (decimal)num;
	}

	public static decimal? Average(this IEnumerable<decimal?> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		using (IEnumerator<decimal?> enumerator = source.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				decimal? current = enumerator.Current;
				if (!current.HasValue)
				{
					continue;
				}
				decimal valueOrDefault = current.GetValueOrDefault();
				long num = 1L;
				while (enumerator.MoveNext())
				{
					current = enumerator.Current;
					if (current.HasValue)
					{
						valueOrDefault += current.GetValueOrDefault();
						num++;
					}
				}
				return valueOrDefault / (decimal)num;
			}
		}
		return null;
	}

	public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		checked
		{
			using IEnumerator<TSource> enumerator = source.GetEnumerator();
			if (!enumerator.MoveNext())
			{
				ThrowHelper.ThrowNoElementsException();
			}
			long num = selector(enumerator.Current);
			long num2 = 1L;
			while (enumerator.MoveNext())
			{
				num += selector(enumerator.Current);
				num2++;
			}
			return (double)num / (double)num2;
		}
	}

	public static double? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		checked
		{
			using (IEnumerator<TSource> enumerator = source.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					int? num = selector(enumerator.Current);
					if (!num.HasValue)
					{
						continue;
					}
					long num2 = num.GetValueOrDefault();
					long num3 = 1L;
					while (enumerator.MoveNext())
					{
						num = selector(enumerator.Current);
						if (num.HasValue)
						{
							num2 += num.GetValueOrDefault();
							num3++;
						}
					}
					return (double)num2 / (double)num3;
				}
			}
			return null;
		}
	}

	public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		checked
		{
			using IEnumerator<TSource> enumerator = source.GetEnumerator();
			if (!enumerator.MoveNext())
			{
				ThrowHelper.ThrowNoElementsException();
			}
			long num = selector(enumerator.Current);
			long num2 = 1L;
			while (enumerator.MoveNext())
			{
				num += selector(enumerator.Current);
				num2++;
			}
			return (double)num / (double)num2;
		}
	}

	public static double? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		checked
		{
			using (IEnumerator<TSource> enumerator = source.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					long? num = selector(enumerator.Current);
					if (!num.HasValue)
					{
						continue;
					}
					long num2 = num.GetValueOrDefault();
					long num3 = 1L;
					while (enumerator.MoveNext())
					{
						num = selector(enumerator.Current);
						if (num.HasValue)
						{
							num2 += num.GetValueOrDefault();
							num3++;
						}
					}
					return (double)num2 / (double)num3;
				}
			}
			return null;
		}
	}

	public static float Average<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		double num = selector(enumerator.Current);
		long num2 = 1L;
		while (enumerator.MoveNext())
		{
			num += (double)selector(enumerator.Current);
			num2++;
		}
		return (float)(num / (double)num2);
	}

	public static float? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		using (IEnumerator<TSource> enumerator = source.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				float? num = selector(enumerator.Current);
				if (!num.HasValue)
				{
					continue;
				}
				double num2 = num.GetValueOrDefault();
				long num3 = 1L;
				while (enumerator.MoveNext())
				{
					num = selector(enumerator.Current);
					if (num.HasValue)
					{
						num2 += (double)num.GetValueOrDefault();
						num3 = checked(num3 + 1);
					}
				}
				return (float)(num2 / (double)num3);
			}
		}
		return null;
	}

	public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		double num = selector(enumerator.Current);
		long num2 = 1L;
		while (enumerator.MoveNext())
		{
			num += selector(enumerator.Current);
			num2++;
		}
		return num / (double)num2;
	}

	public static double? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		using (IEnumerator<TSource> enumerator = source.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				double? num = selector(enumerator.Current);
				if (!num.HasValue)
				{
					continue;
				}
				double num2 = num.GetValueOrDefault();
				long num3 = 1L;
				while (enumerator.MoveNext())
				{
					num = selector(enumerator.Current);
					if (num.HasValue)
					{
						num2 += num.GetValueOrDefault();
						num3 = checked(num3 + 1);
					}
				}
				return num2 / (double)num3;
			}
		}
		return null;
	}

	public static decimal Average<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		decimal num = selector(enumerator.Current);
		long num2 = 1L;
		while (enumerator.MoveNext())
		{
			num += selector(enumerator.Current);
			num2++;
		}
		return num / (decimal)num2;
	}

	public static decimal? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		using (IEnumerator<TSource> enumerator = source.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				decimal? num = selector(enumerator.Current);
				if (!num.HasValue)
				{
					continue;
				}
				decimal valueOrDefault = num.GetValueOrDefault();
				long num2 = 1L;
				while (enumerator.MoveNext())
				{
					num = selector(enumerator.Current);
					if (num.HasValue)
					{
						valueOrDefault += num.GetValueOrDefault();
						num2++;
					}
				}
				return valueOrDefault / (decimal)num2;
			}
		}
		return null;
	}

	public static IEnumerable<TResult> OfType<TResult>(this IEnumerable source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		return OfTypeIterator<TResult>(source);
	}

	private static IEnumerable<TResult> OfTypeIterator<TResult>(IEnumerable source)
	{
		foreach (object item in source)
		{
			if (item is TResult)
			{
				yield return (TResult)item;
			}
		}
	}

	public static IEnumerable<TResult> Cast<TResult>(this IEnumerable source)
	{
		if (source is IEnumerable<TResult> result)
		{
			return result;
		}
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		return CastIterator<TResult>(source);
	}

	private static IEnumerable<TResult> CastIterator<TResult>(IEnumerable source)
	{
		foreach (object item in source)
		{
			yield return (TResult)item;
		}
	}

	public static IEnumerable<TSource[]> Chunk<TSource>(this IEnumerable<TSource> source, int size)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (size < 1)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.size);
		}
		return ChunkIterator(source, size);
	}

	private static IEnumerable<TSource[]> ChunkIterator<TSource>(IEnumerable<TSource> source, int size)
	{
		using IEnumerator<TSource> e = source.GetEnumerator();
		while (e.MoveNext())
		{
			TSource[] array = new TSource[size];
			array[0] = e.Current;
			int i;
			for (i = 1; i < array.Length; i++)
			{
				if (!e.MoveNext())
				{
					break;
				}
				array[i] = e.Current;
			}
			if (i == array.Length)
			{
				yield return array;
				continue;
			}
			Array.Resize(ref array, i);
			yield return array;
			break;
		}
	}

	public static IEnumerable<TSource> Concat<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
	{
		if (first == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
		}
		if (second == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
		}
		if (!(first is ConcatIterator<TSource> concatIterator))
		{
			return new Concat2Iterator<TSource>(first, second);
		}
		return concatIterator.Concat(second);
	}

	public static bool Contains<TSource>(this IEnumerable<TSource> source, TSource value)
	{
		if (!(source is ICollection<TSource> collection))
		{
			return source.Contains(value, null);
		}
		return collection.Contains(value);
	}

	public static bool Contains<TSource>(this IEnumerable<TSource> source, TSource value, IEqualityComparer<TSource>? comparer)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (comparer == null)
		{
			foreach (TSource item in source)
			{
				if (EqualityComparer<TSource>.Default.Equals(item, value))
				{
					return true;
				}
			}
		}
		else
		{
			foreach (TSource item2 in source)
			{
				if (comparer.Equals(item2, value))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static int Count<TSource>(this IEnumerable<TSource> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (source is ICollection<TSource> collection)
		{
			return collection.Count;
		}
		if (source is IIListProvider<TSource> iIListProvider)
		{
			return iIListProvider.GetCount(onlyIfCheap: false);
		}
		if (source is ICollection collection2)
		{
			return collection2.Count;
		}
		int num = 0;
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			num = checked(num + 1);
		}
		return num;
	}

	public static int Count<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (predicate == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
		}
		int num = 0;
		foreach (TSource item in source)
		{
			if (predicate(item))
			{
				num = checked(num + 1);
			}
		}
		return num;
	}

	public static bool TryGetNonEnumeratedCount<TSource>(this IEnumerable<TSource> source, out int count)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (source is ICollection<TSource> collection)
		{
			count = collection.Count;
			return true;
		}
		if (source is IIListProvider<TSource> iIListProvider)
		{
			int count2 = iIListProvider.GetCount(onlyIfCheap: true);
			if (count2 >= 0)
			{
				count = count2;
				return true;
			}
		}
		if (source is ICollection collection2)
		{
			count = collection2.Count;
			return true;
		}
		count = 0;
		return false;
	}

	public static long LongCount<TSource>(this IEnumerable<TSource> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		long num = 0L;
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			num = checked(num + 1);
		}
		return num;
	}

	public static long LongCount<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (predicate == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
		}
		long num = 0L;
		foreach (TSource item in source)
		{
			if (predicate(item))
			{
				num = checked(num + 1);
			}
		}
		return num;
	}

	public static IEnumerable<TSource?> DefaultIfEmpty<TSource>(this IEnumerable<TSource> source)
	{
		return source.DefaultIfEmpty(default(TSource));
	}

	public static IEnumerable<TSource> DefaultIfEmpty<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		return new DefaultIfEmptyIterator<TSource>(source, defaultValue);
	}

	public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source)
	{
		return source.Distinct(null);
	}

	public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource>? comparer)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		return new DistinctIterator<TSource>(source, comparer);
	}

	public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
	{
		return source.DistinctBy(keySelector, null);
	}

	public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (keySelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
		}
		return DistinctByIterator(source, keySelector, comparer);
	}

	private static IEnumerable<TSource> DistinctByIterator<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
	{
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			yield break;
		}
		HashSet<TKey> set = new HashSet<TKey>(7, comparer);
		do
		{
			TSource current = enumerator.Current;
			if (set.Add(keySelector(current)))
			{
				yield return current;
			}
		}
		while (enumerator.MoveNext());
	}

	public static TSource ElementAt<TSource>(this IEnumerable<TSource> source, int index)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (source is IPartition<TSource> partition)
		{
			bool found;
			TSource result = partition.TryGetElementAt(index, out found);
			if (found)
			{
				return result;
			}
		}
		else
		{
			if (source is IList<TSource> list)
			{
				return list[index];
			}
			if (TryGetElement<TSource>(source, index, out var element))
			{
				return element;
			}
		}
		ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		return default(TSource);
	}

	public static TSource ElementAt<TSource>(this IEnumerable<TSource> source, Index index)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (!index.IsFromEnd)
		{
			return source.ElementAt(index.Value);
		}
		if (source.TryGetNonEnumeratedCount(out var count))
		{
			return source.ElementAt(count - index.Value);
		}
		if (!TryGetElementFromEnd<TSource>(source, index.Value, out var element))
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return element;
	}

	public static TSource? ElementAtOrDefault<TSource>(this IEnumerable<TSource> source, int index)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (source is IPartition<TSource> partition)
		{
			bool found;
			return partition.TryGetElementAt(index, out found);
		}
		if (source is IList<TSource> list)
		{
			if (index < 0 || index >= list.Count)
			{
				return default(TSource);
			}
			return list[index];
		}
		TryGetElement<TSource>(source, index, out var element);
		return element;
	}

	public static TSource? ElementAtOrDefault<TSource>(this IEnumerable<TSource> source, Index index)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (!index.IsFromEnd)
		{
			return source.ElementAtOrDefault(index.Value);
		}
		if (source.TryGetNonEnumeratedCount(out var count))
		{
			return source.ElementAtOrDefault(count - index.Value);
		}
		TryGetElementFromEnd<TSource>(source, index.Value, out var element);
		return element;
	}

	private static bool TryGetElement<TSource>(IEnumerable<TSource> source, int index, [MaybeNullWhen(false)] out TSource element)
	{
		if (index >= 0)
		{
			using IEnumerator<TSource> enumerator = source.GetEnumerator();
			while (enumerator.MoveNext())
			{
				if (index == 0)
				{
					element = enumerator.Current;
					return true;
				}
				index--;
			}
		}
		element = default(TSource);
		return false;
	}

	private static bool TryGetElementFromEnd<TSource>(IEnumerable<TSource> source, int indexFromEnd, [MaybeNullWhen(false)] out TSource element)
	{
		if (indexFromEnd > 0)
		{
			using IEnumerator<TSource> enumerator = source.GetEnumerator();
			if (enumerator.MoveNext())
			{
				Queue<TSource> queue = new Queue<TSource>();
				queue.Enqueue(enumerator.Current);
				while (enumerator.MoveNext())
				{
					if (queue.Count == indexFromEnd)
					{
						queue.Dequeue();
					}
					queue.Enqueue(enumerator.Current);
				}
				if (queue.Count == indexFromEnd)
				{
					element = queue.Dequeue();
					return true;
				}
			}
		}
		element = default(TSource);
		return false;
	}

	public static IEnumerable<TSource> AsEnumerable<TSource>(this IEnumerable<TSource> source)
	{
		return source;
	}

	public static IEnumerable<TSource> Except<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
	{
		if (first == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
		}
		if (second == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
		}
		return ExceptIterator(first, second, null);
	}

	public static IEnumerable<TSource> Except<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
	{
		if (first == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
		}
		if (second == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
		}
		return ExceptIterator(first, second, comparer);
	}

	public static IEnumerable<TSource> ExceptBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector)
	{
		return first.ExceptBy(second, keySelector, null);
	}

	public static IEnumerable<TSource> ExceptBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
	{
		if (first == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
		}
		if (second == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
		}
		if (keySelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
		}
		return ExceptByIterator(first, second, keySelector, comparer);
	}

	private static IEnumerable<TSource> ExceptIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
	{
		HashSet<TSource> set = new HashSet<TSource>(second, comparer);
		foreach (TSource item in first)
		{
			if (set.Add(item))
			{
				yield return item;
			}
		}
	}

	private static IEnumerable<TSource> ExceptByIterator<TSource, TKey>(IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
	{
		HashSet<TKey> set = new HashSet<TKey>(second, comparer);
		foreach (TSource item in first)
		{
			if (set.Add(keySelector(item)))
			{
				yield return item;
			}
		}
	}

	public static TSource First<TSource>(this IEnumerable<TSource> source)
	{
		bool found;
		TSource result = source.TryGetFirst(out found);
		if (!found)
		{
			ThrowHelper.ThrowNoElementsException();
		}
		return result;
	}

	public static TSource First<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
	{
		bool found;
		TSource result = source.TryGetFirst(predicate, out found);
		if (!found)
		{
			ThrowHelper.ThrowNoMatchException();
		}
		return result;
	}

	public static TSource? FirstOrDefault<TSource>(this IEnumerable<TSource> source)
	{
		bool found;
		return source.TryGetFirst(out found);
	}

	public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
	{
		bool found;
		TSource result = source.TryGetFirst(out found);
		if (!found)
		{
			return defaultValue;
		}
		return result;
	}

	public static TSource? FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
	{
		bool found;
		return source.TryGetFirst(predicate, out found);
	}

	public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, TSource defaultValue)
	{
		bool found;
		TSource result = source.TryGetFirst(predicate, out found);
		if (!found)
		{
			return defaultValue;
		}
		return result;
	}

	private static TSource TryGetFirst<TSource>(this IEnumerable<TSource> source, out bool found)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (source is IPartition<TSource> partition)
		{
			return partition.TryGetFirst(out found);
		}
		if (source is IList<TSource> list)
		{
			if (list.Count > 0)
			{
				found = true;
				return list[0];
			}
		}
		else
		{
			using IEnumerator<TSource> enumerator = source.GetEnumerator();
			if (enumerator.MoveNext())
			{
				found = true;
				return enumerator.Current;
			}
		}
		found = false;
		return default(TSource);
	}

	private static TSource TryGetFirst<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, out bool found)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (predicate == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
		}
		foreach (TSource item in source)
		{
			if (predicate(item))
			{
				found = true;
				return item;
			}
		}
		found = false;
		return default(TSource);
	}

	public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
	{
		return new GroupedEnumerable<TSource, TKey>(source, keySelector, null);
	}

	public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
	{
		return new GroupedEnumerable<TSource, TKey>(source, keySelector, comparer);
	}

	public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
	{
		return new GroupedEnumerable<TSource, TKey, TElement>(source, keySelector, elementSelector, null);
	}

	public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer)
	{
		return new GroupedEnumerable<TSource, TKey, TElement>(source, keySelector, elementSelector, comparer);
	}

	public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector)
	{
		return new GroupedResultEnumerable<TSource, TKey, TResult>(source, keySelector, resultSelector, null);
	}

	public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
	{
		return new GroupedResultEnumerable<TSource, TKey, TElement, TResult>(source, keySelector, elementSelector, resultSelector, null);
	}

	public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
	{
		return new GroupedResultEnumerable<TSource, TKey, TResult>(source, keySelector, resultSelector, comparer);
	}

	public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
	{
		return new GroupedResultEnumerable<TSource, TKey, TElement, TResult>(source, keySelector, elementSelector, resultSelector, comparer);
	}

	public static IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector)
	{
		if (outer == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.outer);
		}
		if (inner == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.inner);
		}
		if (outerKeySelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.outerKeySelector);
		}
		if (innerKeySelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.innerKeySelector);
		}
		if (resultSelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
		}
		return GroupJoinIterator(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);
	}

	public static IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
	{
		if (outer == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.outer);
		}
		if (inner == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.inner);
		}
		if (outerKeySelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.outerKeySelector);
		}
		if (innerKeySelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.innerKeySelector);
		}
		if (resultSelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
		}
		return GroupJoinIterator(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);
	}

	private static IEnumerable<TResult> GroupJoinIterator<TOuter, TInner, TKey, TResult>(IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
	{
		using IEnumerator<TOuter> e = outer.GetEnumerator();
		if (e.MoveNext())
		{
			Lookup<TKey, TInner> lookup = Lookup<TKey, TInner>.CreateForJoin(inner, innerKeySelector, comparer);
			do
			{
				TOuter current = e.Current;
				yield return resultSelector(current, lookup[outerKeySelector(current)]);
			}
			while (e.MoveNext());
		}
	}

	public static IEnumerable<TSource> Intersect<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
	{
		return first.Intersect(second, null);
	}

	public static IEnumerable<TSource> Intersect<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
	{
		if (first == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
		}
		if (second == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
		}
		return IntersectIterator(first, second, comparer);
	}

	public static IEnumerable<TSource> IntersectBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector)
	{
		return first.IntersectBy(second, keySelector, null);
	}

	public static IEnumerable<TSource> IntersectBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
	{
		if (first == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
		}
		if (second == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
		}
		if (keySelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
		}
		return IntersectByIterator(first, second, keySelector, comparer);
	}

	private static IEnumerable<TSource> IntersectIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
	{
		HashSet<TSource> set = new HashSet<TSource>(second, comparer);
		foreach (TSource item in first)
		{
			if (set.Remove(item))
			{
				yield return item;
			}
		}
	}

	private static IEnumerable<TSource> IntersectByIterator<TSource, TKey>(IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
	{
		HashSet<TKey> set = new HashSet<TKey>(second, comparer);
		foreach (TSource item in first)
		{
			if (set.Remove(keySelector(item)))
			{
				yield return item;
			}
		}
	}

	public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector)
	{
		if (outer == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.outer);
		}
		if (inner == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.inner);
		}
		if (outerKeySelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.outerKeySelector);
		}
		if (innerKeySelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.innerKeySelector);
		}
		if (resultSelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
		}
		return JoinIterator(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);
	}

	public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
	{
		if (outer == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.outer);
		}
		if (inner == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.inner);
		}
		if (outerKeySelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.outerKeySelector);
		}
		if (innerKeySelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.innerKeySelector);
		}
		if (resultSelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
		}
		return JoinIterator(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);
	}

	private static IEnumerable<TResult> JoinIterator<TOuter, TInner, TKey, TResult>(IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey> comparer)
	{
		using IEnumerator<TOuter> e = outer.GetEnumerator();
		if (!e.MoveNext())
		{
			yield break;
		}
		Lookup<TKey, TInner> lookup = Lookup<TKey, TInner>.CreateForJoin(inner, innerKeySelector, comparer);
		if (lookup.Count == 0)
		{
			yield break;
		}
		do
		{
			TOuter item = e.Current;
			Grouping<TKey, TInner> grouping = lookup.GetGrouping(outerKeySelector(item), create: false);
			if (grouping != null)
			{
				int count = grouping._count;
				TInner[] elements = grouping._elements;
				int i = 0;
				while (i != count)
				{
					yield return resultSelector(item, elements[i]);
					int num = i + 1;
					i = num;
				}
			}
		}
		while (e.MoveNext());
	}

	public static TSource Last<TSource>(this IEnumerable<TSource> source)
	{
		bool found;
		TSource result = source.TryGetLast(out found);
		if (!found)
		{
			ThrowHelper.ThrowNoElementsException();
		}
		return result;
	}

	public static TSource Last<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
	{
		bool found;
		TSource result = source.TryGetLast(predicate, out found);
		if (!found)
		{
			ThrowHelper.ThrowNoMatchException();
		}
		return result;
	}

	public static TSource? LastOrDefault<TSource>(this IEnumerable<TSource> source)
	{
		bool found;
		return source.TryGetLast(out found);
	}

	public static TSource LastOrDefault<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
	{
		bool found;
		TSource result = source.TryGetLast(out found);
		if (!found)
		{
			return defaultValue;
		}
		return result;
	}

	public static TSource? LastOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
	{
		bool found;
		return source.TryGetLast(predicate, out found);
	}

	public static TSource LastOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, TSource defaultValue)
	{
		bool found;
		TSource result = source.TryGetLast(predicate, out found);
		if (!found)
		{
			return defaultValue;
		}
		return result;
	}

	private static TSource TryGetLast<TSource>(this IEnumerable<TSource> source, out bool found)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (source is IPartition<TSource> partition)
		{
			return partition.TryGetLast(out found);
		}
		if (source is IList<TSource> { Count: var count } list)
		{
			if (count > 0)
			{
				found = true;
				return list[count - 1];
			}
		}
		else
		{
			using IEnumerator<TSource> enumerator = source.GetEnumerator();
			if (enumerator.MoveNext())
			{
				TSource current;
				do
				{
					current = enumerator.Current;
				}
				while (enumerator.MoveNext());
				found = true;
				return current;
			}
		}
		found = false;
		return default(TSource);
	}

	private static TSource TryGetLast<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, out bool found)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (predicate == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
		}
		if (source is OrderedEnumerable<TSource> orderedEnumerable)
		{
			return orderedEnumerable.TryGetLast(predicate, out found);
		}
		if (source is IList<TSource> list)
		{
			for (int num = list.Count - 1; num >= 0; num--)
			{
				TSource val = list[num];
				if (predicate(val))
				{
					found = true;
					return val;
				}
			}
		}
		else
		{
			using IEnumerator<TSource> enumerator = source.GetEnumerator();
			while (enumerator.MoveNext())
			{
				TSource val2 = enumerator.Current;
				if (!predicate(val2))
				{
					continue;
				}
				while (enumerator.MoveNext())
				{
					TSource current = enumerator.Current;
					if (predicate(current))
					{
						val2 = current;
					}
				}
				found = true;
				return val2;
			}
		}
		found = false;
		return default(TSource);
	}

	public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
	{
		return source.ToLookup(keySelector, null);
	}

	public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (keySelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
		}
		return Lookup<TKey, TSource>.Create(source, keySelector, comparer);
	}

	public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
	{
		return source.ToLookup(keySelector, elementSelector, null);
	}

	public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (keySelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
		}
		if (elementSelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementSelector);
		}
		return Lookup<TKey, TElement>.Create(source, keySelector, elementSelector, comparer);
	}

	public static int Max(this IEnumerable<int> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		using IEnumerator<int> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		int num = enumerator.Current;
		while (enumerator.MoveNext())
		{
			int current = enumerator.Current;
			if (current > num)
			{
				num = current;
			}
		}
		return num;
	}

	public static int? Max(this IEnumerable<int?> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		int? result = null;
		using (IEnumerator<int?> enumerator = source.GetEnumerator())
		{
			do
			{
				if (!enumerator.MoveNext())
				{
					return result;
				}
				result = enumerator.Current;
			}
			while (!result.HasValue);
			int num = result.GetValueOrDefault();
			if (num >= 0)
			{
				while (enumerator.MoveNext())
				{
					int? current = enumerator.Current;
					int valueOrDefault = current.GetValueOrDefault();
					if (valueOrDefault > num)
					{
						num = valueOrDefault;
						result = current;
					}
				}
			}
			else
			{
				while (enumerator.MoveNext())
				{
					int? current2 = enumerator.Current;
					int valueOrDefault2 = current2.GetValueOrDefault();
					if (current2.HasValue && valueOrDefault2 > num)
					{
						num = valueOrDefault2;
						result = current2;
					}
				}
			}
		}
		return result;
	}

	public static long Max(this IEnumerable<long> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		using IEnumerator<long> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		long num = enumerator.Current;
		while (enumerator.MoveNext())
		{
			long current = enumerator.Current;
			if (current > num)
			{
				num = current;
			}
		}
		return num;
	}

	public static long? Max(this IEnumerable<long?> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		long? result = null;
		using (IEnumerator<long?> enumerator = source.GetEnumerator())
		{
			do
			{
				if (!enumerator.MoveNext())
				{
					return result;
				}
				result = enumerator.Current;
			}
			while (!result.HasValue);
			long num = result.GetValueOrDefault();
			if (num >= 0)
			{
				while (enumerator.MoveNext())
				{
					long? current = enumerator.Current;
					long valueOrDefault = current.GetValueOrDefault();
					if (valueOrDefault > num)
					{
						num = valueOrDefault;
						result = current;
					}
				}
			}
			else
			{
				while (enumerator.MoveNext())
				{
					long? current2 = enumerator.Current;
					long valueOrDefault2 = current2.GetValueOrDefault();
					if (current2.HasValue && valueOrDefault2 > num)
					{
						num = valueOrDefault2;
						result = current2;
					}
				}
			}
		}
		return result;
	}

	public static double Max(this IEnumerable<double> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		using IEnumerator<double> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		double num = enumerator.Current;
		while (double.IsNaN(num))
		{
			if (!enumerator.MoveNext())
			{
				return num;
			}
			num = enumerator.Current;
		}
		while (enumerator.MoveNext())
		{
			double current = enumerator.Current;
			if (current > num)
			{
				num = current;
			}
		}
		return num;
	}

	public static double? Max(this IEnumerable<double?> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		double? result = null;
		using IEnumerator<double?> enumerator = source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			result = enumerator.Current;
			if (!result.HasValue)
			{
				continue;
			}
			double num = result.GetValueOrDefault();
			while (double.IsNaN(num))
			{
				if (!enumerator.MoveNext())
				{
					return result;
				}
				double? current = enumerator.Current;
				if (current.HasValue)
				{
					double? num2 = (result = current);
					num = num2.GetValueOrDefault();
				}
			}
			while (enumerator.MoveNext())
			{
				double? current2 = enumerator.Current;
				double valueOrDefault = current2.GetValueOrDefault();
				if (current2.HasValue && valueOrDefault > num)
				{
					num = valueOrDefault;
					result = current2;
				}
			}
			return result;
		}
		return result;
	}

	public static float Max(this IEnumerable<float> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		using IEnumerator<float> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		float num = enumerator.Current;
		while (float.IsNaN(num))
		{
			if (!enumerator.MoveNext())
			{
				return num;
			}
			num = enumerator.Current;
		}
		while (enumerator.MoveNext())
		{
			float current = enumerator.Current;
			if (current > num)
			{
				num = current;
			}
		}
		return num;
	}

	public static float? Max(this IEnumerable<float?> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		float? result = null;
		using IEnumerator<float?> enumerator = source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			result = enumerator.Current;
			if (!result.HasValue)
			{
				continue;
			}
			float num = result.GetValueOrDefault();
			while (float.IsNaN(num))
			{
				if (!enumerator.MoveNext())
				{
					return result;
				}
				float? current = enumerator.Current;
				if (current.HasValue)
				{
					float? num2 = (result = current);
					num = num2.GetValueOrDefault();
				}
			}
			while (enumerator.MoveNext())
			{
				float? current2 = enumerator.Current;
				float valueOrDefault = current2.GetValueOrDefault();
				if (current2.HasValue && valueOrDefault > num)
				{
					num = valueOrDefault;
					result = current2;
				}
			}
			return result;
		}
		return result;
	}

	public static decimal Max(this IEnumerable<decimal> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		using IEnumerator<decimal> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		decimal num = enumerator.Current;
		while (enumerator.MoveNext())
		{
			decimal current = enumerator.Current;
			if (current > num)
			{
				num = current;
			}
		}
		return num;
	}

	public static decimal? Max(this IEnumerable<decimal?> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		decimal? result = null;
		using IEnumerator<decimal?> enumerator = source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			result = enumerator.Current;
			if (!result.HasValue)
			{
				continue;
			}
			decimal num = result.GetValueOrDefault();
			while (enumerator.MoveNext())
			{
				decimal? current = enumerator.Current;
				decimal valueOrDefault = current.GetValueOrDefault();
				if (current.HasValue && valueOrDefault > num)
				{
					num = valueOrDefault;
					result = current;
				}
			}
			return result;
		}
		return result;
	}

	public static TSource? Max<TSource>(this IEnumerable<TSource> source)
	{
		return source.Max((IComparer<TSource>?)null);
	}

	public static TSource? Max<TSource>(this IEnumerable<TSource> source, IComparer<TSource>? comparer)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (comparer == null)
		{
			comparer = Comparer<TSource>.Default;
		}
		TSource val = default(TSource);
		using (IEnumerator<TSource> enumerator = source.GetEnumerator())
		{
			if (val == null)
			{
				do
				{
					if (!enumerator.MoveNext())
					{
						return val;
					}
					val = enumerator.Current;
				}
				while (val == null);
				while (enumerator.MoveNext())
				{
					TSource current = enumerator.Current;
					if (current != null && comparer.Compare(current, val) > 0)
					{
						val = current;
					}
				}
			}
			else
			{
				if (!enumerator.MoveNext())
				{
					ThrowHelper.ThrowNoElementsException();
				}
				val = enumerator.Current;
				if (comparer == Comparer<TSource>.Default)
				{
					while (enumerator.MoveNext())
					{
						TSource current2 = enumerator.Current;
						if (Comparer<TSource>.Default.Compare(current2, val) > 0)
						{
							val = current2;
						}
					}
				}
				else
				{
					while (enumerator.MoveNext())
					{
						TSource current3 = enumerator.Current;
						if (comparer.Compare(current3, val) > 0)
						{
							val = current3;
						}
					}
				}
			}
		}
		return val;
	}

	public static TSource? MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
	{
		return source.MaxBy(keySelector, null);
	}

	public static TSource? MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (keySelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
		}
		if (comparer == null)
		{
			comparer = Comparer<TKey>.Default;
		}
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			if (default(TSource) == null)
			{
				return default(TSource);
			}
			ThrowHelper.ThrowNoElementsException();
		}
		TSource val = enumerator.Current;
		TKey val2 = keySelector(val);
		if (default(TKey) == null)
		{
			while (val2 == null)
			{
				if (!enumerator.MoveNext())
				{
					return val;
				}
				val = enumerator.Current;
				val2 = keySelector(val);
			}
			while (enumerator.MoveNext())
			{
				TSource current = enumerator.Current;
				TKey val3 = keySelector(current);
				if (val3 != null && comparer.Compare(val3, val2) > 0)
				{
					val2 = val3;
					val = current;
				}
			}
		}
		else if (comparer == Comparer<TKey>.Default)
		{
			while (enumerator.MoveNext())
			{
				TSource current2 = enumerator.Current;
				TKey val4 = keySelector(current2);
				if (Comparer<TKey>.Default.Compare(val4, val2) > 0)
				{
					val2 = val4;
					val = current2;
				}
			}
		}
		else
		{
			while (enumerator.MoveNext())
			{
				TSource current3 = enumerator.Current;
				TKey val5 = keySelector(current3);
				if (comparer.Compare(val5, val2) > 0)
				{
					val2 = val5;
					val = current3;
				}
			}
		}
		return val;
	}

	public static int Max<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		int num = selector(enumerator.Current);
		while (enumerator.MoveNext())
		{
			int num2 = selector(enumerator.Current);
			if (num2 > num)
			{
				num = num2;
			}
		}
		return num;
	}

	public static int? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		int? result = null;
		using (IEnumerator<TSource> enumerator = source.GetEnumerator())
		{
			do
			{
				if (!enumerator.MoveNext())
				{
					return result;
				}
				result = selector(enumerator.Current);
			}
			while (!result.HasValue);
			int num = result.GetValueOrDefault();
			if (num >= 0)
			{
				while (enumerator.MoveNext())
				{
					int? num2 = selector(enumerator.Current);
					int valueOrDefault = num2.GetValueOrDefault();
					if (valueOrDefault > num)
					{
						num = valueOrDefault;
						result = num2;
					}
				}
			}
			else
			{
				while (enumerator.MoveNext())
				{
					int? num3 = selector(enumerator.Current);
					int valueOrDefault2 = num3.GetValueOrDefault();
					if (num3.HasValue && valueOrDefault2 > num)
					{
						num = valueOrDefault2;
						result = num3;
					}
				}
			}
		}
		return result;
	}

	public static long Max<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		long num = selector(enumerator.Current);
		while (enumerator.MoveNext())
		{
			long num2 = selector(enumerator.Current);
			if (num2 > num)
			{
				num = num2;
			}
		}
		return num;
	}

	public static long? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		long? result = null;
		using (IEnumerator<TSource> enumerator = source.GetEnumerator())
		{
			do
			{
				if (!enumerator.MoveNext())
				{
					return result;
				}
				result = selector(enumerator.Current);
			}
			while (!result.HasValue);
			long num = result.GetValueOrDefault();
			if (num >= 0)
			{
				while (enumerator.MoveNext())
				{
					long? num2 = selector(enumerator.Current);
					long valueOrDefault = num2.GetValueOrDefault();
					if (valueOrDefault > num)
					{
						num = valueOrDefault;
						result = num2;
					}
				}
			}
			else
			{
				while (enumerator.MoveNext())
				{
					long? num3 = selector(enumerator.Current);
					long valueOrDefault2 = num3.GetValueOrDefault();
					if (num3.HasValue && valueOrDefault2 > num)
					{
						num = valueOrDefault2;
						result = num3;
					}
				}
			}
		}
		return result;
	}

	public static float Max<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		float num = selector(enumerator.Current);
		while (float.IsNaN(num))
		{
			if (!enumerator.MoveNext())
			{
				return num;
			}
			num = selector(enumerator.Current);
		}
		while (enumerator.MoveNext())
		{
			float num2 = selector(enumerator.Current);
			if (num2 > num)
			{
				num = num2;
			}
		}
		return num;
	}

	public static float? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		float? result = null;
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			result = selector(enumerator.Current);
			if (!result.HasValue)
			{
				continue;
			}
			float num = result.GetValueOrDefault();
			while (float.IsNaN(num))
			{
				if (!enumerator.MoveNext())
				{
					return result;
				}
				float? num2 = selector(enumerator.Current);
				if (num2.HasValue)
				{
					float? num3 = (result = num2);
					num = num3.GetValueOrDefault();
				}
			}
			while (enumerator.MoveNext())
			{
				float? num4 = selector(enumerator.Current);
				float valueOrDefault = num4.GetValueOrDefault();
				if (num4.HasValue && valueOrDefault > num)
				{
					num = valueOrDefault;
					result = num4;
				}
			}
			return result;
		}
		return result;
	}

	public static double Max<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		double num = selector(enumerator.Current);
		while (double.IsNaN(num))
		{
			if (!enumerator.MoveNext())
			{
				return num;
			}
			num = selector(enumerator.Current);
		}
		while (enumerator.MoveNext())
		{
			double num2 = selector(enumerator.Current);
			if (num2 > num)
			{
				num = num2;
			}
		}
		return num;
	}

	public static double? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		double? result = null;
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			result = selector(enumerator.Current);
			if (!result.HasValue)
			{
				continue;
			}
			double num = result.GetValueOrDefault();
			while (double.IsNaN(num))
			{
				if (!enumerator.MoveNext())
				{
					return result;
				}
				double? num2 = selector(enumerator.Current);
				if (num2.HasValue)
				{
					double? num3 = (result = num2);
					num = num3.GetValueOrDefault();
				}
			}
			while (enumerator.MoveNext())
			{
				double? num4 = selector(enumerator.Current);
				double valueOrDefault = num4.GetValueOrDefault();
				if (num4.HasValue && valueOrDefault > num)
				{
					num = valueOrDefault;
					result = num4;
				}
			}
			return result;
		}
		return result;
	}

	public static decimal Max<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		decimal num = selector(enumerator.Current);
		while (enumerator.MoveNext())
		{
			decimal num2 = selector(enumerator.Current);
			if (num2 > num)
			{
				num = num2;
			}
		}
		return num;
	}

	public static decimal? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		decimal? result = null;
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			result = selector(enumerator.Current);
			if (!result.HasValue)
			{
				continue;
			}
			decimal num = result.GetValueOrDefault();
			while (enumerator.MoveNext())
			{
				decimal? num2 = selector(enumerator.Current);
				decimal valueOrDefault = num2.GetValueOrDefault();
				if (num2.HasValue && valueOrDefault > num)
				{
					num = valueOrDefault;
					result = num2;
				}
			}
			return result;
		}
		return result;
	}

	public static TResult? Max<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		TResult val = default(TResult);
		using (IEnumerator<TSource> enumerator = source.GetEnumerator())
		{
			if (val == null)
			{
				do
				{
					if (!enumerator.MoveNext())
					{
						return val;
					}
					val = selector(enumerator.Current);
				}
				while (val == null);
				Comparer<TResult> @default = Comparer<TResult>.Default;
				while (enumerator.MoveNext())
				{
					TResult val2 = selector(enumerator.Current);
					if (val2 != null && @default.Compare(val2, val) > 0)
					{
						val = val2;
					}
				}
			}
			else
			{
				if (!enumerator.MoveNext())
				{
					ThrowHelper.ThrowNoElementsException();
				}
				val = selector(enumerator.Current);
				while (enumerator.MoveNext())
				{
					TResult val3 = selector(enumerator.Current);
					if (Comparer<TResult>.Default.Compare(val3, val) > 0)
					{
						val = val3;
					}
				}
			}
		}
		return val;
	}

	public static int Min(this IEnumerable<int> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		using IEnumerator<int> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		int num = enumerator.Current;
		while (enumerator.MoveNext())
		{
			int current = enumerator.Current;
			if (current < num)
			{
				num = current;
			}
		}
		return num;
	}

	public static int? Min(this IEnumerable<int?> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		int? result = null;
		using IEnumerator<int?> enumerator = source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			result = enumerator.Current;
			if (!result.HasValue)
			{
				continue;
			}
			int num = result.GetValueOrDefault();
			while (enumerator.MoveNext())
			{
				int? current = enumerator.Current;
				int valueOrDefault = current.GetValueOrDefault();
				if (current.HasValue && valueOrDefault < num)
				{
					num = valueOrDefault;
					result = current;
				}
			}
			return result;
		}
		return result;
	}

	public static long Min(this IEnumerable<long> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		using IEnumerator<long> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		long num = enumerator.Current;
		while (enumerator.MoveNext())
		{
			long current = enumerator.Current;
			if (current < num)
			{
				num = current;
			}
		}
		return num;
	}

	public static long? Min(this IEnumerable<long?> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		long? result = null;
		using IEnumerator<long?> enumerator = source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			result = enumerator.Current;
			if (!result.HasValue)
			{
				continue;
			}
			long num = result.GetValueOrDefault();
			while (enumerator.MoveNext())
			{
				long? current = enumerator.Current;
				long valueOrDefault = current.GetValueOrDefault();
				if (current.HasValue && valueOrDefault < num)
				{
					num = valueOrDefault;
					result = current;
				}
			}
			return result;
		}
		return result;
	}

	public static float Min(this IEnumerable<float> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		using IEnumerator<float> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		float num = enumerator.Current;
		if (float.IsNaN(num))
		{
			return num;
		}
		while (enumerator.MoveNext())
		{
			float current = enumerator.Current;
			if (current < num)
			{
				num = current;
			}
			else if (float.IsNaN(current))
			{
				return current;
			}
		}
		return num;
	}

	public static float? Min(this IEnumerable<float?> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		float? result = null;
		using IEnumerator<float?> enumerator = source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			result = enumerator.Current;
			if (!result.HasValue)
			{
				continue;
			}
			float num = result.GetValueOrDefault();
			if (float.IsNaN(num))
			{
				return result;
			}
			while (enumerator.MoveNext())
			{
				float? current = enumerator.Current;
				if (current.HasValue)
				{
					float valueOrDefault = current.GetValueOrDefault();
					if (valueOrDefault < num)
					{
						num = valueOrDefault;
						result = current;
					}
					else if (float.IsNaN(valueOrDefault))
					{
						return current;
					}
				}
			}
			return result;
		}
		return result;
	}

	public static double Min(this IEnumerable<double> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		using IEnumerator<double> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		double num = enumerator.Current;
		if (double.IsNaN(num))
		{
			return num;
		}
		while (enumerator.MoveNext())
		{
			double current = enumerator.Current;
			if (current < num)
			{
				num = current;
			}
			else if (double.IsNaN(current))
			{
				return current;
			}
		}
		return num;
	}

	public static double? Min(this IEnumerable<double?> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		double? result = null;
		using IEnumerator<double?> enumerator = source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			result = enumerator.Current;
			if (!result.HasValue)
			{
				continue;
			}
			double num = result.GetValueOrDefault();
			if (double.IsNaN(num))
			{
				return result;
			}
			while (enumerator.MoveNext())
			{
				double? current = enumerator.Current;
				if (current.HasValue)
				{
					double valueOrDefault = current.GetValueOrDefault();
					if (valueOrDefault < num)
					{
						num = valueOrDefault;
						result = current;
					}
					else if (double.IsNaN(valueOrDefault))
					{
						return current;
					}
				}
			}
			return result;
		}
		return result;
	}

	public static decimal Min(this IEnumerable<decimal> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		using IEnumerator<decimal> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		decimal num = enumerator.Current;
		while (enumerator.MoveNext())
		{
			decimal current = enumerator.Current;
			if (current < num)
			{
				num = current;
			}
		}
		return num;
	}

	public static decimal? Min(this IEnumerable<decimal?> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		decimal? result = null;
		using IEnumerator<decimal?> enumerator = source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			result = enumerator.Current;
			if (!result.HasValue)
			{
				continue;
			}
			decimal num = result.GetValueOrDefault();
			while (enumerator.MoveNext())
			{
				decimal? current = enumerator.Current;
				decimal valueOrDefault = current.GetValueOrDefault();
				if (current.HasValue && valueOrDefault < num)
				{
					num = valueOrDefault;
					result = current;
				}
			}
			return result;
		}
		return result;
	}

	public static TSource? Min<TSource>(this IEnumerable<TSource> source)
	{
		return source.Min((IComparer<TSource>?)null);
	}

	public static TSource? Min<TSource>(this IEnumerable<TSource> source, IComparer<TSource>? comparer)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (comparer == null)
		{
			comparer = Comparer<TSource>.Default;
		}
		TSource val = default(TSource);
		using (IEnumerator<TSource> enumerator = source.GetEnumerator())
		{
			if (val == null)
			{
				do
				{
					if (!enumerator.MoveNext())
					{
						return val;
					}
					val = enumerator.Current;
				}
				while (val == null);
				while (enumerator.MoveNext())
				{
					TSource current = enumerator.Current;
					if (current != null && comparer.Compare(current, val) < 0)
					{
						val = current;
					}
				}
			}
			else
			{
				if (!enumerator.MoveNext())
				{
					ThrowHelper.ThrowNoElementsException();
				}
				val = enumerator.Current;
				if (comparer == Comparer<TSource>.Default)
				{
					while (enumerator.MoveNext())
					{
						TSource current2 = enumerator.Current;
						if (Comparer<TSource>.Default.Compare(current2, val) < 0)
						{
							val = current2;
						}
					}
				}
				else
				{
					while (enumerator.MoveNext())
					{
						TSource current3 = enumerator.Current;
						if (comparer.Compare(current3, val) < 0)
						{
							val = current3;
						}
					}
				}
			}
		}
		return val;
	}

	public static TSource? MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
	{
		return source.MinBy(keySelector, null);
	}

	public static TSource? MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (keySelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
		}
		if (comparer == null)
		{
			comparer = Comparer<TKey>.Default;
		}
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			if (default(TSource) == null)
			{
				return default(TSource);
			}
			ThrowHelper.ThrowNoElementsException();
		}
		TSource val = enumerator.Current;
		TKey val2 = keySelector(val);
		if (default(TKey) == null)
		{
			while (val2 == null)
			{
				if (!enumerator.MoveNext())
				{
					return val;
				}
				val = enumerator.Current;
				val2 = keySelector(val);
			}
			while (enumerator.MoveNext())
			{
				TSource current = enumerator.Current;
				TKey val3 = keySelector(current);
				if (val3 != null && comparer.Compare(val3, val2) < 0)
				{
					val2 = val3;
					val = current;
				}
			}
		}
		else if (comparer == Comparer<TKey>.Default)
		{
			while (enumerator.MoveNext())
			{
				TSource current2 = enumerator.Current;
				TKey val4 = keySelector(current2);
				if (Comparer<TKey>.Default.Compare(val4, val2) < 0)
				{
					val2 = val4;
					val = current2;
				}
			}
		}
		else
		{
			while (enumerator.MoveNext())
			{
				TSource current3 = enumerator.Current;
				TKey val5 = keySelector(current3);
				if (comparer.Compare(val5, val2) < 0)
				{
					val2 = val5;
					val = current3;
				}
			}
		}
		return val;
	}

	public static int Min<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		int num = selector(enumerator.Current);
		while (enumerator.MoveNext())
		{
			int num2 = selector(enumerator.Current);
			if (num2 < num)
			{
				num = num2;
			}
		}
		return num;
	}

	public static int? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		int? result = null;
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			result = selector(enumerator.Current);
			if (!result.HasValue)
			{
				continue;
			}
			int num = result.GetValueOrDefault();
			while (enumerator.MoveNext())
			{
				int? num2 = selector(enumerator.Current);
				int valueOrDefault = num2.GetValueOrDefault();
				if (num2.HasValue && valueOrDefault < num)
				{
					num = valueOrDefault;
					result = num2;
				}
			}
			return result;
		}
		return result;
	}

	public static long Min<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		long num = selector(enumerator.Current);
		while (enumerator.MoveNext())
		{
			long num2 = selector(enumerator.Current);
			if (num2 < num)
			{
				num = num2;
			}
		}
		return num;
	}

	public static long? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		long? result = null;
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			result = selector(enumerator.Current);
			if (!result.HasValue)
			{
				continue;
			}
			long num = result.GetValueOrDefault();
			while (enumerator.MoveNext())
			{
				long? num2 = selector(enumerator.Current);
				long valueOrDefault = num2.GetValueOrDefault();
				if (num2.HasValue && valueOrDefault < num)
				{
					num = valueOrDefault;
					result = num2;
				}
			}
			return result;
		}
		return result;
	}

	public static float Min<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		float num = selector(enumerator.Current);
		if (float.IsNaN(num))
		{
			return num;
		}
		while (enumerator.MoveNext())
		{
			float num2 = selector(enumerator.Current);
			if (num2 < num)
			{
				num = num2;
			}
			else if (float.IsNaN(num2))
			{
				return num2;
			}
		}
		return num;
	}

	public static float? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		float? result = null;
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			result = selector(enumerator.Current);
			if (!result.HasValue)
			{
				continue;
			}
			float num = result.GetValueOrDefault();
			if (float.IsNaN(num))
			{
				return result;
			}
			while (enumerator.MoveNext())
			{
				float? num2 = selector(enumerator.Current);
				if (num2.HasValue)
				{
					float valueOrDefault = num2.GetValueOrDefault();
					if (valueOrDefault < num)
					{
						num = valueOrDefault;
						result = num2;
					}
					else if (float.IsNaN(valueOrDefault))
					{
						return num2;
					}
				}
			}
			return result;
		}
		return result;
	}

	public static double Min<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		double num = selector(enumerator.Current);
		if (double.IsNaN(num))
		{
			return num;
		}
		while (enumerator.MoveNext())
		{
			double num2 = selector(enumerator.Current);
			if (num2 < num)
			{
				num = num2;
			}
			else if (double.IsNaN(num2))
			{
				return num2;
			}
		}
		return num;
	}

	public static double? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		double? result = null;
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			result = selector(enumerator.Current);
			if (!result.HasValue)
			{
				continue;
			}
			double num = result.GetValueOrDefault();
			if (double.IsNaN(num))
			{
				return result;
			}
			while (enumerator.MoveNext())
			{
				double? num2 = selector(enumerator.Current);
				if (num2.HasValue)
				{
					double valueOrDefault = num2.GetValueOrDefault();
					if (valueOrDefault < num)
					{
						num = valueOrDefault;
						result = num2;
					}
					else if (double.IsNaN(valueOrDefault))
					{
						return num2;
					}
				}
			}
			return result;
		}
		return result;
	}

	public static decimal Min<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			ThrowHelper.ThrowNoElementsException();
		}
		decimal num = selector(enumerator.Current);
		while (enumerator.MoveNext())
		{
			decimal num2 = selector(enumerator.Current);
			if (num2 < num)
			{
				num = num2;
			}
		}
		return num;
	}

	public static decimal? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		decimal? result = null;
		using IEnumerator<TSource> enumerator = source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			result = selector(enumerator.Current);
			if (!result.HasValue)
			{
				continue;
			}
			decimal num = result.GetValueOrDefault();
			while (enumerator.MoveNext())
			{
				decimal? num2 = selector(enumerator.Current);
				decimal valueOrDefault = num2.GetValueOrDefault();
				if (num2.HasValue && valueOrDefault < num)
				{
					num = valueOrDefault;
					result = num2;
				}
			}
			return result;
		}
		return result;
	}

	public static TResult? Min<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		TResult val = default(TResult);
		using (IEnumerator<TSource> enumerator = source.GetEnumerator())
		{
			if (val == null)
			{
				do
				{
					if (!enumerator.MoveNext())
					{
						return val;
					}
					val = selector(enumerator.Current);
				}
				while (val == null);
				Comparer<TResult> @default = Comparer<TResult>.Default;
				while (enumerator.MoveNext())
				{
					TResult val2 = selector(enumerator.Current);
					if (val2 != null && @default.Compare(val2, val) < 0)
					{
						val = val2;
					}
				}
			}
			else
			{
				if (!enumerator.MoveNext())
				{
					ThrowHelper.ThrowNoElementsException();
				}
				val = selector(enumerator.Current);
				while (enumerator.MoveNext())
				{
					TResult val3 = selector(enumerator.Current);
					if (Comparer<TResult>.Default.Compare(val3, val) < 0)
					{
						val = val3;
					}
				}
			}
		}
		return val;
	}

	public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
	{
		return new OrderedEnumerable<TSource, TKey>(source, keySelector, null, descending: false, null);
	}

	public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
	{
		return new OrderedEnumerable<TSource, TKey>(source, keySelector, comparer, descending: false, null);
	}

	public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
	{
		return new OrderedEnumerable<TSource, TKey>(source, keySelector, null, descending: true, null);
	}

	public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
	{
		return new OrderedEnumerable<TSource, TKey>(source, keySelector, comparer, descending: true, null);
	}

	public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		return source.CreateOrderedEnumerable(keySelector, null, descending: false);
	}

	public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		return source.CreateOrderedEnumerable(keySelector, comparer, descending: false);
	}

	public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		return source.CreateOrderedEnumerable(keySelector, null, descending: true);
	}

	public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		return source.CreateOrderedEnumerable(keySelector, comparer, descending: true);
	}

	public static IEnumerable<int> Range(int start, int count)
	{
		long num = (long)start + (long)count - 1;
		if (count < 0 || num > int.MaxValue)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count);
		}
		if (count == 0)
		{
			return Empty<int>();
		}
		return new RangeIterator(start, count);
	}

	public static IEnumerable<TResult> Repeat<TResult>(TResult element, int count)
	{
		if (count < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count);
		}
		if (count == 0)
		{
			return Empty<TResult>();
		}
		return new RepeatIterator<TResult>(element, count);
	}

	public static IEnumerable<TSource> Reverse<TSource>(this IEnumerable<TSource> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		return new ReverseIterator<TSource>(source);
	}

	public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		if (source is Iterator<TSource> iterator)
		{
			return iterator.Select(selector);
		}
		if (source is IList<TSource> source2)
		{
			if (source is TSource[] array)
			{
				if (array.Length != 0)
				{
					return new SelectArrayIterator<TSource, TResult>(array, selector);
				}
				return Empty<TResult>();
			}
			if (source is List<TSource> source3)
			{
				return new SelectListIterator<TSource, TResult>(source3, selector);
			}
			return new SelectIListIterator<TSource, TResult>(source2, selector);
		}
		if (source is IPartition<TSource> partition)
		{
			IEnumerable<TResult> result = null;
			CreateSelectIPartitionIterator<TResult, TSource>(selector, partition, ref result);
			if (result != null)
			{
				return result;
			}
		}
		return new SelectEnumerableIterator<TSource, TResult>(source, selector);
	}

	private static void CreateSelectIPartitionIterator<TResult, TSource>(Func<TSource, TResult> selector, IPartition<TSource> partition, [NotNull] ref IEnumerable<TResult> result)
	{
		IPartition<TResult> partition3;
		if (!(partition is EmptyPartition<TSource>))
		{
			IPartition<TResult> partition2 = new SelectIPartitionIterator<TSource, TResult>(partition, selector);
			partition3 = partition2;
		}
		else
		{
			partition3 = EmptyPartition<TResult>.Instance;
		}
		result = partition3;
	}

	public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		return SelectIterator(source, selector);
	}

	private static IEnumerable<TResult> SelectIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
	{
		int index = -1;
		foreach (TSource item in source)
		{
			index = checked(index + 1);
			yield return selector(item, index);
		}
	}

	public static IEnumerable<TResult> SelectMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		return new SelectManySingleSelectorIterator<TSource, TResult>(source, selector);
	}

	public static IEnumerable<TResult> SelectMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TResult>> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		return SelectManyIterator(source, selector);
	}

	private static IEnumerable<TResult> SelectManyIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TResult>> selector)
	{
		int index = -1;
		foreach (TSource item in source)
		{
			index = checked(index + 1);
			foreach (TResult item2 in selector(item, index))
			{
				yield return item2;
			}
		}
	}

	public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (collectionSelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collectionSelector);
		}
		if (resultSelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
		}
		return SelectManyIterator(source, collectionSelector, resultSelector);
	}

	private static IEnumerable<TResult> SelectManyIterator<TSource, TCollection, TResult>(IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
	{
		int index = -1;
		foreach (TSource element in source)
		{
			index = checked(index + 1);
			foreach (TCollection item in collectionSelector(element, index))
			{
				yield return resultSelector(element, item);
			}
		}
	}

	public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (collectionSelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collectionSelector);
		}
		if (resultSelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
		}
		return SelectManyIterator(source, collectionSelector, resultSelector);
	}

	private static IEnumerable<TResult> SelectManyIterator<TSource, TCollection, TResult>(IEnumerable<TSource> source, Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
	{
		foreach (TSource element in source)
		{
			foreach (TCollection item in collectionSelector(element))
			{
				yield return resultSelector(element, item);
			}
		}
	}

	public static bool SequenceEqual<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
	{
		return first.SequenceEqual(second, null);
	}

	public static bool SequenceEqual<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
	{
		if (first == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
		}
		if (second == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
		}
		if (first is ICollection<TSource> collection && second is ICollection<TSource> collection2)
		{
			if (first is TSource[] array && second is TSource[] array2)
			{
				return ((ReadOnlySpan<TSource>)array).SequenceEqual((ReadOnlySpan<TSource>)array2, comparer);
			}
			if (collection.Count != collection2.Count)
			{
				return false;
			}
			if (collection is IList<TSource> list && collection2 is IList<TSource> list2)
			{
				if (comparer == null)
				{
					comparer = EqualityComparer<TSource>.Default;
				}
				int count = collection.Count;
				for (int i = 0; i < count; i++)
				{
					if (!comparer.Equals(list[i], list2[i]))
					{
						return false;
					}
				}
				return true;
			}
		}
		using IEnumerator<TSource> enumerator = first.GetEnumerator();
		using IEnumerator<TSource> enumerator2 = second.GetEnumerator();
		if (comparer == null)
		{
			comparer = EqualityComparer<TSource>.Default;
		}
		while (enumerator.MoveNext())
		{
			if (!enumerator2.MoveNext() || !comparer.Equals(enumerator.Current, enumerator2.Current))
			{
				return false;
			}
		}
		return !enumerator2.MoveNext();
	}

	public static TSource Single<TSource>(this IEnumerable<TSource> source)
	{
		bool found;
		TSource result = source.TryGetSingle(out found);
		if (!found)
		{
			ThrowHelper.ThrowNoElementsException();
		}
		return result;
	}

	public static TSource Single<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
	{
		bool found;
		TSource result = source.TryGetSingle(predicate, out found);
		if (!found)
		{
			ThrowHelper.ThrowNoMatchException();
		}
		return result;
	}

	public static TSource? SingleOrDefault<TSource>(this IEnumerable<TSource> source)
	{
		bool found;
		return source.TryGetSingle(out found);
	}

	public static TSource SingleOrDefault<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
	{
		bool found;
		TSource result = source.TryGetSingle(out found);
		if (!found)
		{
			return defaultValue;
		}
		return result;
	}

	public static TSource? SingleOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
	{
		bool found;
		return source.TryGetSingle(predicate, out found);
	}

	public static TSource SingleOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, TSource defaultValue)
	{
		bool found;
		TSource result = source.TryGetSingle(predicate, out found);
		if (!found)
		{
			return defaultValue;
		}
		return result;
	}

	private static TSource TryGetSingle<TSource>(this IEnumerable<TSource> source, out bool found)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (source is IList<TSource> { Count: var count } list)
		{
			switch (count)
			{
			case 0:
				found = false;
				return default(TSource);
			case 1:
				found = true;
				return list[0];
			}
		}
		else
		{
			using IEnumerator<TSource> enumerator = source.GetEnumerator();
			if (!enumerator.MoveNext())
			{
				found = false;
				return default(TSource);
			}
			TSource current = enumerator.Current;
			if (!enumerator.MoveNext())
			{
				found = true;
				return current;
			}
		}
		found = false;
		ThrowHelper.ThrowMoreThanOneElementException();
		return default(TSource);
	}

	private static TSource TryGetSingle<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, out bool found)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (predicate == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
		}
		using (IEnumerator<TSource> enumerator = source.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				TSource current = enumerator.Current;
				if (!predicate(current))
				{
					continue;
				}
				while (enumerator.MoveNext())
				{
					if (predicate(enumerator.Current))
					{
						ThrowHelper.ThrowMoreThanOneMatchException();
					}
				}
				found = true;
				return current;
			}
		}
		found = false;
		return default(TSource);
	}

	public static IEnumerable<TSource> Skip<TSource>(this IEnumerable<TSource> source, int count)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (count <= 0)
		{
			if (source is Iterator<TSource> || source is IPartition<TSource>)
			{
				return source;
			}
			count = 0;
		}
		else if (source is IPartition<TSource> partition)
		{
			return partition.Skip(count);
		}
		return SkipIterator(source, count);
	}

	public static IEnumerable<TSource> SkipWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (predicate == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
		}
		return SkipWhileIterator(source, predicate);
	}

	private static IEnumerable<TSource> SkipWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
	{
		using IEnumerator<TSource> e = source.GetEnumerator();
		while (e.MoveNext())
		{
			TSource current = e.Current;
			if (!predicate(current))
			{
				yield return current;
				while (e.MoveNext())
				{
					yield return e.Current;
				}
				yield break;
			}
		}
	}

	public static IEnumerable<TSource> SkipWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (predicate == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
		}
		return SkipWhileIterator(source, predicate);
	}

	private static IEnumerable<TSource> SkipWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
	{
		using IEnumerator<TSource> e = source.GetEnumerator();
		int num = -1;
		while (e.MoveNext())
		{
			num = checked(num + 1);
			TSource current = e.Current;
			if (!predicate(current, num))
			{
				yield return current;
				while (e.MoveNext())
				{
					yield return e.Current;
				}
				yield break;
			}
		}
	}

	public static IEnumerable<TSource> SkipLast<TSource>(this IEnumerable<TSource> source, int count)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (count > 0)
		{
			return TakeRangeFromEndIterator(source, isStartIndexFromEnd: false, 0, isEndIndexFromEnd: true, count);
		}
		return source.Skip(0);
	}

	public static int Sum(this IEnumerable<int> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		int num = 0;
		foreach (int item in source)
		{
			num = checked(num + item);
		}
		return num;
	}

	public static int? Sum(this IEnumerable<int?> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		int num = 0;
		foreach (int? item in source)
		{
			if (item.HasValue)
			{
				num = checked(num + item.GetValueOrDefault());
			}
		}
		return num;
	}

	public static long Sum(this IEnumerable<long> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		long num = 0L;
		foreach (long item in source)
		{
			num = checked(num + item);
		}
		return num;
	}

	public static long? Sum(this IEnumerable<long?> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		long num = 0L;
		foreach (long? item in source)
		{
			if (item.HasValue)
			{
				num = checked(num + item.GetValueOrDefault());
			}
		}
		return num;
	}

	public static float Sum(this IEnumerable<float> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		double num = 0.0;
		foreach (float item in source)
		{
			num += (double)item;
		}
		return (float)num;
	}

	public static float? Sum(this IEnumerable<float?> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		double num = 0.0;
		foreach (float? item in source)
		{
			if (item.HasValue)
			{
				num += (double)item.GetValueOrDefault();
			}
		}
		return (float)num;
	}

	public static double Sum(this IEnumerable<double> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		double num = 0.0;
		foreach (double item in source)
		{
			num += item;
		}
		return num;
	}

	public static double? Sum(this IEnumerable<double?> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		double num = 0.0;
		foreach (double? item in source)
		{
			if (item.HasValue)
			{
				num += item.GetValueOrDefault();
			}
		}
		return num;
	}

	public static decimal Sum(this IEnumerable<decimal> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		decimal result = default(decimal);
		foreach (decimal item in source)
		{
			result += item;
		}
		return result;
	}

	public static decimal? Sum(this IEnumerable<decimal?> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		decimal value = default(decimal);
		foreach (decimal? item in source)
		{
			if (item.HasValue)
			{
				value += item.GetValueOrDefault();
			}
		}
		return value;
	}

	public static int Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		int num = 0;
		foreach (TSource item in source)
		{
			num = checked(num + selector(item));
		}
		return num;
	}

	public static int? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		int num = 0;
		foreach (TSource item in source)
		{
			int? num2 = selector(item);
			if (num2.HasValue)
			{
				num = checked(num + num2.GetValueOrDefault());
			}
		}
		return num;
	}

	public static long Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
	{
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		long num = 0L;
		foreach (TSource item in source)
		{
			num = checked(num + selector(item));
		}
		return num;
	}

	public static long? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		long num = 0L;
		foreach (TSource item in source)
		{
			long? num2 = selector(item);
			if (num2.HasValue)
			{
				num = checked(num + num2.GetValueOrDefault());
			}
		}
		return num;
	}

	public static float Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		double num = 0.0;
		foreach (TSource item in source)
		{
			num += (double)selector(item);
		}
		return (float)num;
	}

	public static float? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		double num = 0.0;
		foreach (TSource item in source)
		{
			float? num2 = selector(item);
			if (num2.HasValue)
			{
				num += (double)num2.GetValueOrDefault();
			}
		}
		return (float)num;
	}

	public static double Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		double num = 0.0;
		foreach (TSource item in source)
		{
			num += selector(item);
		}
		return num;
	}

	public static double? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		double num = 0.0;
		foreach (TSource item in source)
		{
			double? num2 = selector(item);
			if (num2.HasValue)
			{
				num += num2.GetValueOrDefault();
			}
		}
		return num;
	}

	public static decimal Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		decimal result = default(decimal);
		foreach (TSource item in source)
		{
			result += selector(item);
		}
		return result;
	}

	public static decimal? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (selector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
		}
		decimal value = default(decimal);
		foreach (TSource item in source)
		{
			decimal? num = selector(item);
			if (num.HasValue)
			{
				value += num.GetValueOrDefault();
			}
		}
		return value;
	}

	public static IEnumerable<TSource> Take<TSource>(this IEnumerable<TSource> source, int count)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (count > 0)
		{
			return TakeIterator(source, count);
		}
		return Empty<TSource>();
	}

	public static IEnumerable<TSource> Take<TSource>(this IEnumerable<TSource> source, Range range)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		Index start = range.Start;
		Index end = range.End;
		bool isFromEnd = start.IsFromEnd;
		bool isFromEnd2 = end.IsFromEnd;
		int value = start.Value;
		int value2 = end.Value;
		if (isFromEnd)
		{
			if (value == 0 || (isFromEnd2 && value2 >= value))
			{
				return Empty<TSource>();
			}
		}
		else if (!isFromEnd2)
		{
			if (value < value2)
			{
				return TakeRangeIterator(source, value, value2);
			}
			return Empty<TSource>();
		}
		return TakeRangeFromEndIterator(source, isFromEnd, value, isFromEnd2, value2);
	}

	private static IEnumerable<TSource> TakeRangeFromEndIterator<TSource>(IEnumerable<TSource> source, bool isStartIndexFromEnd, int startIndex, bool isEndIndexFromEnd, int endIndex)
	{
		if (source.TryGetNonEnumeratedCount(out var count2))
		{
			startIndex = CalculateStartIndex(isStartIndexFromEnd, startIndex, count2);
			endIndex = CalculateEndIndex(isEndIndexFromEnd, endIndex, count2);
			if (startIndex >= endIndex)
			{
				yield break;
			}
			foreach (TSource item in TakeRangeIterator(source, startIndex, endIndex))
			{
				yield return item;
			}
			yield break;
		}
		if (isStartIndexFromEnd)
		{
			Queue<TSource> queue;
			using (IEnumerator<TSource> enumerator2 = source.GetEnumerator())
			{
				if (!enumerator2.MoveNext())
				{
					yield break;
				}
				queue = new Queue<TSource>();
				queue.Enqueue(enumerator2.Current);
				count2 = 1;
				while (enumerator2.MoveNext())
				{
					if (count2 < startIndex)
					{
						queue.Enqueue(enumerator2.Current);
						count2++;
						continue;
					}
					do
					{
						queue.Dequeue();
						queue.Enqueue(enumerator2.Current);
						count2 = checked(count2 + 1);
					}
					while (enumerator2.MoveNext());
					break;
				}
			}
			startIndex = CalculateStartIndex(isStartIndexFromEnd: true, startIndex, count2);
			endIndex = CalculateEndIndex(isEndIndexFromEnd, endIndex, count2);
			for (int rangeIndex = startIndex; rangeIndex < endIndex; rangeIndex++)
			{
				yield return queue.Dequeue();
			}
			yield break;
		}
		using (IEnumerator<TSource> enumerator = source.GetEnumerator())
		{
			for (count2 = 0; count2 < startIndex; count2++)
			{
				if (!enumerator.MoveNext())
				{
					break;
				}
			}
			if (count2 != startIndex)
			{
				yield break;
			}
			Queue<TSource> queue = new Queue<TSource>();
			while (enumerator.MoveNext())
			{
				if (queue.Count == endIndex)
				{
					do
					{
						queue.Enqueue(enumerator.Current);
						yield return queue.Dequeue();
					}
					while (enumerator.MoveNext());
					break;
				}
				queue.Enqueue(enumerator.Current);
			}
		}
		static int CalculateEndIndex(bool isEndIndexFromEnd, int endIndex, int count)
		{
			return Math.Min(count, isEndIndexFromEnd ? (count - endIndex) : endIndex);
		}
		static int CalculateStartIndex(bool isStartIndexFromEnd, int startIndex, int count)
		{
			return Math.Max(0, isStartIndexFromEnd ? (count - startIndex) : startIndex);
		}
	}

	public static IEnumerable<TSource> TakeWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (predicate == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
		}
		return TakeWhileIterator(source, predicate);
	}

	private static IEnumerable<TSource> TakeWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
	{
		foreach (TSource item in source)
		{
			if (!predicate(item))
			{
				break;
			}
			yield return item;
		}
	}

	public static IEnumerable<TSource> TakeWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (predicate == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
		}
		return TakeWhileIterator(source, predicate);
	}

	private static IEnumerable<TSource> TakeWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
	{
		int index = -1;
		foreach (TSource item in source)
		{
			index = checked(index + 1);
			if (!predicate(item, index))
			{
				break;
			}
			yield return item;
		}
	}

	public static IEnumerable<TSource> TakeLast<TSource>(this IEnumerable<TSource> source, int count)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (count > 0)
		{
			return TakeRangeFromEndIterator(source, isStartIndexFromEnd: true, count, isEndIndexFromEnd: true, 0);
		}
		return Empty<TSource>();
	}

	public static TSource[] ToArray<TSource>(this IEnumerable<TSource> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (!(source is IIListProvider<TSource> iIListProvider))
		{
			return System.Collections.Generic.EnumerableHelpers.ToArray(source);
		}
		return iIListProvider.ToArray();
	}

	public static List<TSource> ToList<TSource>(this IEnumerable<TSource> source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (!(source is IIListProvider<TSource> iIListProvider))
		{
			return new List<TSource>(source);
		}
		return iIListProvider.ToList();
	}

	public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) where TKey : notnull
	{
		return source.ToDictionary(keySelector, null);
	}

	public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (keySelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
		}
		int num = 0;
		if (source is ICollection<TSource> collection)
		{
			num = collection.Count;
			if (num == 0)
			{
				return new Dictionary<TKey, TSource>(comparer);
			}
			if (collection is TSource[] source2)
			{
				return ToDictionary(source2, keySelector, comparer);
			}
			if (collection is List<TSource> source3)
			{
				return ToDictionary<TSource, TKey>(source3, keySelector, comparer);
			}
		}
		Dictionary<TKey, TSource> dictionary = new Dictionary<TKey, TSource>(num, comparer);
		foreach (TSource item in source)
		{
			dictionary.Add(keySelector(item), item);
		}
		return dictionary;
	}

	private static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(TSource[] source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
	{
		Dictionary<TKey, TSource> dictionary = new Dictionary<TKey, TSource>(source.Length, comparer);
		for (int i = 0; i < source.Length; i++)
		{
			dictionary.Add(keySelector(source[i]), source[i]);
		}
		return dictionary;
	}

	private static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(List<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
	{
		Dictionary<TKey, TSource> dictionary = new Dictionary<TKey, TSource>(source.Count, comparer);
		foreach (TSource item in source)
		{
			dictionary.Add(keySelector(item), item);
		}
		return dictionary;
	}

	public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector) where TKey : notnull
	{
		return source.ToDictionary(keySelector, elementSelector, null);
	}

	public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (keySelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
		}
		if (elementSelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementSelector);
		}
		int num = 0;
		if (source is ICollection<TSource> collection)
		{
			num = collection.Count;
			if (num == 0)
			{
				return new Dictionary<TKey, TElement>(comparer);
			}
			if (collection is TSource[] source2)
			{
				return ToDictionary(source2, keySelector, elementSelector, comparer);
			}
			if (collection is List<TSource> source3)
			{
				return ToDictionary<TSource, TKey, TElement>(source3, keySelector, elementSelector, comparer);
			}
		}
		Dictionary<TKey, TElement> dictionary = new Dictionary<TKey, TElement>(num, comparer);
		foreach (TSource item in source)
		{
			dictionary.Add(keySelector(item), elementSelector(item));
		}
		return dictionary;
	}

	private static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(TSource[] source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
	{
		Dictionary<TKey, TElement> dictionary = new Dictionary<TKey, TElement>(source.Length, comparer);
		for (int i = 0; i < source.Length; i++)
		{
			dictionary.Add(keySelector(source[i]), elementSelector(source[i]));
		}
		return dictionary;
	}

	private static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(List<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
	{
		Dictionary<TKey, TElement> dictionary = new Dictionary<TKey, TElement>(source.Count, comparer);
		foreach (TSource item in source)
		{
			dictionary.Add(keySelector(item), elementSelector(item));
		}
		return dictionary;
	}

	public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source)
	{
		return source.ToHashSet(null);
	}

	public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource>? comparer)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		return new HashSet<TSource>(source, comparer);
	}

	private static TSource[] HashSetToArray<TSource>(HashSet<TSource> set)
	{
		TSource[] array = new TSource[set.Count];
		set.CopyTo(array);
		return array;
	}

	private static List<TSource> HashSetToList<TSource>(HashSet<TSource> set)
	{
		List<TSource> list = new List<TSource>(set.Count);
		foreach (TSource item in set)
		{
			list.Add(item);
		}
		return list;
	}

	public static IEnumerable<TSource> Union<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
	{
		return first.Union(second, null);
	}

	public static IEnumerable<TSource> Union<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
	{
		if (first == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
		}
		if (second == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
		}
		if (!(first is UnionIterator<TSource> unionIterator) || !Utilities.AreEqualityComparersEqual<TSource>(comparer, unionIterator._comparer))
		{
			return new UnionIterator2<TSource>(first, second, comparer);
		}
		return unionIterator.Union(second);
	}

	public static IEnumerable<TSource> UnionBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector)
	{
		return first.UnionBy(second, keySelector, null);
	}

	public static IEnumerable<TSource> UnionBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
	{
		if (first == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
		}
		if (second == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
		}
		if (keySelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
		}
		return UnionByIterator(first, second, keySelector, comparer);
	}

	private static IEnumerable<TSource> UnionByIterator<TSource, TKey>(IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
	{
		HashSet<TKey> set = new HashSet<TKey>(7, comparer);
		foreach (TSource item in first)
		{
			if (set.Add(keySelector(item)))
			{
				yield return item;
			}
		}
		foreach (TSource item2 in second)
		{
			if (set.Add(keySelector(item2)))
			{
				yield return item2;
			}
		}
	}

	public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (predicate == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
		}
		if (source is Iterator<TSource> iterator)
		{
			return iterator.Where(predicate);
		}
		if (source is TSource[] array)
		{
			if (array.Length != 0)
			{
				return new WhereArrayIterator<TSource>(array, predicate);
			}
			return Empty<TSource>();
		}
		if (source is List<TSource> source2)
		{
			return new WhereListIterator<TSource>(source2, predicate);
		}
		return new WhereEnumerableIterator<TSource>(source, predicate);
	}

	public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (predicate == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
		}
		return WhereIterator(source, predicate);
	}

	private static IEnumerable<TSource> WhereIterator<TSource>(IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
	{
		int index = -1;
		foreach (TSource item in source)
		{
			index = checked(index + 1);
			if (predicate(item, index))
			{
				yield return item;
			}
		}
	}

	public static IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
	{
		if (first == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
		}
		if (second == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
		}
		if (resultSelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
		}
		return ZipIterator(first, second, resultSelector);
	}

	public static IEnumerable<(TFirst First, TSecond Second)> Zip<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second)
	{
		if (first == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
		}
		if (second == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
		}
		return ZipIterator(first, second);
	}

	public static IEnumerable<(TFirst First, TSecond Second, TThird Third)> Zip<TFirst, TSecond, TThird>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, IEnumerable<TThird> third)
	{
		if (first == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
		}
		if (second == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
		}
		if (third == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.third);
		}
		return ZipIterator(first, second, third);
	}

	private static IEnumerable<(TFirst First, TSecond Second)> ZipIterator<TFirst, TSecond>(IEnumerable<TFirst> first, IEnumerable<TSecond> second)
	{
		using IEnumerator<TFirst> e1 = first.GetEnumerator();
		using IEnumerator<TSecond> e2 = second.GetEnumerator();
		while (e1.MoveNext() && e2.MoveNext())
		{
			yield return (First: e1.Current, Second: e2.Current);
		}
	}

	private static IEnumerable<TResult> ZipIterator<TFirst, TSecond, TResult>(IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
	{
		using IEnumerator<TFirst> e1 = first.GetEnumerator();
		using IEnumerator<TSecond> e2 = second.GetEnumerator();
		while (e1.MoveNext() && e2.MoveNext())
		{
			yield return resultSelector(e1.Current, e2.Current);
		}
	}

	private static IEnumerable<(TFirst First, TSecond Second, TThird Third)> ZipIterator<TFirst, TSecond, TThird>(IEnumerable<TFirst> first, IEnumerable<TSecond> second, IEnumerable<TThird> third)
	{
		using IEnumerator<TFirst> e1 = first.GetEnumerator();
		using IEnumerator<TSecond> e2 = second.GetEnumerator();
		using IEnumerator<TThird> e3 = third.GetEnumerator();
		while (e1.MoveNext() && e2.MoveNext() && e3.MoveNext())
		{
			yield return (First: e1.Current, Second: e2.Current, Third: e3.Current);
		}
	}
}
