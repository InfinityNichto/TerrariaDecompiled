using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ReLogic.Utilities;

public sealed class SlotVector<T> : IEnumerable<SlotVector<T>.ItemPair>, IEnumerable
{
	public sealed class Enumerator : IEnumerator<ItemPair>, IEnumerator, IDisposable
	{
		private SlotVector<T> _slotVector;

		private int _index = -1;

		ItemPair IEnumerator<ItemPair>.Current => _slotVector.GetPair(_index);

		object IEnumerator.Current => _slotVector.GetPair(_index);

		public Enumerator(SlotVector<T> slotVector)
		{
			_slotVector = slotVector;
		}

		public void Reset()
		{
			_index = -1;
		}

		public bool MoveNext()
		{
			while (++_index < _slotVector._usedSpaceLength)
			{
				if (_slotVector.Has(_index))
				{
					return true;
				}
			}
			return false;
		}

		public void Dispose()
		{
			_slotVector = null;
		}
	}

	public struct ItemPair
	{
		public readonly T Value;

		public readonly SlotId Id;

		public ItemPair(T value, SlotId id)
		{
			Value = value;
			Id = id;
		}
	}

	private const uint MAX_INDEX = 65535u;

	private readonly ItemPair[] _array;

	private uint _freeHead;

	private uint _usedSpaceLength;

	public T this[int index]
	{
		get
		{
			if (index < 0 || index >= _array.Length)
			{
				throw new ArgumentOutOfRangeException();
			}
			if (!_array[index].Id.IsActive)
			{
				throw new KeyNotFoundException();
			}
			return _array[index].Value;
		}
		set
		{
			if (index < 0 || index >= _array.Length)
			{
				throw new ArgumentOutOfRangeException();
			}
			if (!_array[index].Id.IsActive)
			{
				throw new KeyNotFoundException();
			}
			_array[index] = new ItemPair(value, _array[index].Id);
		}
	}

	public T this[SlotId id]
	{
		get
		{
			uint index = id.Index;
			if (index >= _array.Length)
			{
				throw new ArgumentOutOfRangeException();
			}
			if (!_array[index].Id.IsActive || id != _array[index].Id)
			{
				throw new KeyNotFoundException();
			}
			return _array[index].Value;
		}
		set
		{
			uint index = id.Index;
			if (index >= _array.Length)
			{
				throw new ArgumentOutOfRangeException();
			}
			if (!_array[index].Id.IsActive || id != _array[index].Id)
			{
				throw new KeyNotFoundException();
			}
			_array[index] = new ItemPair(value, id);
		}
	}

	public int Count { get; private set; }

	public int Capacity => _array.Length;

	public SlotVector(int capacity)
	{
		_array = new ItemPair[capacity];
		Clear();
	}

	public SlotId Add(T value)
	{
		if (_freeHead == 65535)
		{
			return new SlotId(65535u);
		}
		uint freeHead = _freeHead;
		ItemPair itemPair = _array[freeHead];
		if (_freeHead >= _usedSpaceLength)
		{
			_usedSpaceLength = _freeHead + 1;
		}
		_freeHead = itemPair.Id.Index;
		_array[freeHead] = new ItemPair(value, itemPair.Id.ToActive(freeHead));
		Count++;
		return _array[freeHead].Id;
	}

	public void Clear()
	{
		_usedSpaceLength = 0u;
		Count = 0;
		_freeHead = 0u;
		for (uint num = 0u; num < _array.Length - 1; num++)
		{
			_array[num] = new ItemPair(default(T), new SlotId(num + 1));
		}
		_array[_array.Length - 1] = new ItemPair(default(T), new SlotId(65535u));
	}

	public bool Remove(SlotId id)
	{
		if (id.IsActive)
		{
			uint index = id.Index;
			_array[index] = new ItemPair(default(T), id.ToInactive(_freeHead));
			_freeHead = index;
			Count--;
			return true;
		}
		return false;
	}

	public bool Has(SlotId id)
	{
		uint index = id.Index;
		if (index >= _array.Length)
		{
			return false;
		}
		if (!_array[index].Id.IsActive || id != _array[index].Id)
		{
			return false;
		}
		return true;
	}

	public bool Has(int index)
	{
		if (index < 0 || index >= _array.Length)
		{
			return false;
		}
		if (!_array[index].Id.IsActive)
		{
			return false;
		}
		return true;
	}

	public ItemPair GetPair(int index)
	{
		if (Has(index))
		{
			return _array[index];
		}
		return new ItemPair(default(T), SlotId.Invalid);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(this);
	}

	IEnumerator<ItemPair> IEnumerable<ItemPair>.GetEnumerator()
	{
		return new Enumerator(this);
	}

	public bool TryGetValue(SlotId id, [NotNullWhen(true)] out T? result)
	{
		uint index = id.Index;
		if (index >= _array.Length)
		{
			result = default(T);
			return false;
		}
		ref ItemPair arrayEntry = ref _array[index];
		if (!arrayEntry.Id.IsActive || id != arrayEntry.Id)
		{
			result = default(T);
			return false;
		}
		result = arrayEntry.Value;
		return true;
	}

	public bool TryGetValue(int index, [NotNullWhen(true)] out T? result)
	{
		if (index < 0 || index >= _array.Length)
		{
			result = default(T);
			return false;
		}
		ref ItemPair arrayEntry = ref _array[index];
		if (!arrayEntry.Id.IsActive)
		{
			result = default(T);
			return false;
		}
		result = arrayEntry.Value;
		return true;
	}
}
