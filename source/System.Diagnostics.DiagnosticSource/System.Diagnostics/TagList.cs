using System.Collections;
using System.Collections.Generic;

namespace System.Diagnostics;

public struct TagList : IList<KeyValuePair<string, object?>>, ICollection<KeyValuePair<string, object?>>, IEnumerable<KeyValuePair<string, object?>>, IEnumerable, IReadOnlyList<KeyValuePair<string, object?>>, IReadOnlyCollection<KeyValuePair<string, object?>>
{
	public struct Enumerator : IEnumerator<KeyValuePair<string, object?>>, IEnumerator, IDisposable
	{
		private TagList _tagList;

		private int _index;

		public KeyValuePair<string, object?> Current => _tagList[_index];

		object IEnumerator.Current => _tagList[_index];

		internal Enumerator(in TagList tagList)
		{
			_index = -1;
			_tagList = tagList;
		}

		public void Dispose()
		{
			_index = _tagList.Count;
		}

		public bool MoveNext()
		{
			_index++;
			return _index < _tagList.Count;
		}

		public void Reset()
		{
			_index = -1;
		}
	}

	internal KeyValuePair<string, object> Tag1;

	internal KeyValuePair<string, object> Tag2;

	internal KeyValuePair<string, object> Tag3;

	internal KeyValuePair<string, object> Tag4;

	internal KeyValuePair<string, object> Tag5;

	internal KeyValuePair<string, object> Tag6;

	internal KeyValuePair<string, object> Tag7;

	internal KeyValuePair<string, object> Tag8;

	private int _tagsCount;

	private KeyValuePair<string, object>[] _overflowTags;

	public readonly int Count => _tagsCount;

	public readonly bool IsReadOnly => false;

	public KeyValuePair<string, object?> this[int index]
	{
		readonly get
		{
			if ((uint)index >= (uint)_tagsCount)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			if (_overflowTags != null)
			{
				return _overflowTags[index];
			}
			return index switch
			{
				0 => Tag1, 
				1 => Tag2, 
				2 => Tag3, 
				3 => Tag4, 
				4 => Tag5, 
				5 => Tag6, 
				6 => Tag7, 
				7 => Tag8, 
				_ => default(KeyValuePair<string, object>), 
			};
		}
		set
		{
			if ((uint)index >= (uint)_tagsCount)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			if (_overflowTags != null)
			{
				_overflowTags[index] = value;
				return;
			}
			switch (index)
			{
			case 0:
				Tag1 = value;
				break;
			case 1:
				Tag2 = value;
				break;
			case 2:
				Tag3 = value;
				break;
			case 3:
				Tag4 = value;
				break;
			case 4:
				Tag5 = value;
				break;
			case 5:
				Tag6 = value;
				break;
			case 6:
				Tag7 = value;
				break;
			case 7:
				Tag8 = value;
				break;
			}
		}
	}

	internal readonly KeyValuePair<string, object?>[]? Tags => _overflowTags;

	public TagList(ReadOnlySpan<KeyValuePair<string, object?>> tagList)
	{
		this = default(TagList);
		_tagsCount = tagList.Length;
		switch (_tagsCount)
		{
		case 8:
			Tag8 = tagList[7];
			goto case 7;
		case 7:
			Tag7 = tagList[6];
			goto case 6;
		case 6:
			Tag6 = tagList[5];
			goto case 5;
		case 5:
			Tag5 = tagList[4];
			goto case 4;
		case 4:
			Tag4 = tagList[3];
			goto case 3;
		case 3:
			Tag3 = tagList[2];
			goto case 2;
		case 2:
			Tag2 = tagList[1];
			goto case 1;
		case 1:
			Tag1 = tagList[0];
			break;
		case 0:
			break;
		default:
			_overflowTags = new KeyValuePair<string, object>[_tagsCount + 8];
			tagList.CopyTo(_overflowTags);
			break;
		}
	}

	public void Add(string key, object? value)
	{
		Add(new KeyValuePair<string, object>(key, value));
	}

	public void Add(KeyValuePair<string, object?> tag)
	{
		if (_overflowTags != null)
		{
			if (_tagsCount == _overflowTags.Length)
			{
				Array.Resize(ref _overflowTags, _tagsCount + 8);
			}
			_overflowTags[_tagsCount++] = tag;
			return;
		}
		switch (_tagsCount)
		{
		default:
			return;
		case 0:
			Tag1 = tag;
			break;
		case 1:
			Tag2 = tag;
			break;
		case 2:
			Tag3 = tag;
			break;
		case 3:
			Tag4 = tag;
			break;
		case 4:
			Tag5 = tag;
			break;
		case 5:
			Tag6 = tag;
			break;
		case 6:
			Tag7 = tag;
			break;
		case 7:
			Tag8 = tag;
			break;
		case 8:
			MoveTagsToTheArray();
			_overflowTags[8] = tag;
			break;
		}
		_tagsCount++;
	}

	public readonly void CopyTo(Span<KeyValuePair<string, object?>> tags)
	{
		if (tags.Length < _tagsCount)
		{
			throw new ArgumentException(System.SR.Arg_BufferTooSmall);
		}
		if (_overflowTags != null)
		{
			Span<KeyValuePair<string, object>> span = _overflowTags.AsSpan();
			span = span.Slice(0, _tagsCount);
			span.CopyTo(tags);
			return;
		}
		switch (_tagsCount)
		{
		default:
			return;
		case 8:
			tags[7] = Tag8;
			goto case 7;
		case 7:
			tags[6] = Tag7;
			goto case 6;
		case 6:
			tags[5] = Tag6;
			goto case 5;
		case 5:
			tags[4] = Tag5;
			goto case 4;
		case 4:
			tags[3] = Tag4;
			goto case 3;
		case 3:
			tags[2] = Tag3;
			goto case 2;
		case 2:
			tags[1] = Tag2;
			break;
		case 1:
			break;
		case 0:
			return;
		}
		tags[0] = Tag1;
	}

	public readonly void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if ((uint)arrayIndex >= array.Length)
		{
			throw new ArgumentOutOfRangeException("arrayIndex");
		}
		CopyTo(array.AsSpan().Slice(arrayIndex));
	}

	public void Insert(int index, KeyValuePair<string, object?> item)
	{
		if ((uint)index > (uint)_tagsCount)
		{
			throw new ArgumentOutOfRangeException("index");
		}
		if (index == _tagsCount)
		{
			Add(item);
			return;
		}
		if (_tagsCount == 8 && _overflowTags == null)
		{
			MoveTagsToTheArray();
		}
		if (_overflowTags != null)
		{
			if (_tagsCount == _overflowTags.Length)
			{
				Array.Resize(ref _overflowTags, _tagsCount + 8);
			}
			for (int num = _tagsCount; num > index; num--)
			{
				_overflowTags[num] = _overflowTags[num - 1];
			}
			_overflowTags[index] = item;
			_tagsCount++;
			return;
		}
		switch (index)
		{
		default:
			return;
		case 0:
			Tag8 = Tag7;
			Tag7 = Tag6;
			Tag6 = Tag5;
			Tag5 = Tag4;
			Tag4 = Tag3;
			Tag3 = Tag2;
			Tag2 = Tag1;
			Tag1 = item;
			break;
		case 1:
			Tag8 = Tag7;
			Tag7 = Tag6;
			Tag6 = Tag5;
			Tag5 = Tag4;
			Tag4 = Tag3;
			Tag3 = Tag2;
			Tag2 = item;
			break;
		case 2:
			Tag8 = Tag7;
			Tag7 = Tag6;
			Tag6 = Tag5;
			Tag5 = Tag4;
			Tag4 = Tag3;
			Tag3 = item;
			break;
		case 3:
			Tag8 = Tag7;
			Tag7 = Tag6;
			Tag6 = Tag5;
			Tag5 = Tag4;
			Tag4 = item;
			break;
		case 4:
			Tag8 = Tag7;
			Tag7 = Tag6;
			Tag6 = Tag5;
			Tag5 = item;
			break;
		case 5:
			Tag8 = Tag7;
			Tag7 = Tag6;
			Tag6 = item;
			break;
		case 6:
			Tag8 = Tag7;
			Tag7 = item;
			break;
		}
		_tagsCount++;
	}

	public void RemoveAt(int index)
	{
		if ((uint)index >= (uint)_tagsCount)
		{
			throw new ArgumentOutOfRangeException("index");
		}
		if (_overflowTags != null)
		{
			for (int i = index; i < _tagsCount - 1; i++)
			{
				_overflowTags[i] = _overflowTags[i + 1];
			}
			_tagsCount--;
			return;
		}
		switch (index)
		{
		case 0:
			Tag1 = Tag2;
			goto case 1;
		case 1:
			Tag2 = Tag3;
			goto case 2;
		case 2:
			Tag3 = Tag4;
			goto case 3;
		case 3:
			Tag4 = Tag5;
			goto case 4;
		case 4:
			Tag5 = Tag6;
			goto case 5;
		case 5:
			Tag6 = Tag7;
			goto case 6;
		case 6:
			Tag7 = Tag8;
			break;
		}
		_tagsCount--;
	}

	public void Clear()
	{
		_tagsCount = 0;
	}

	public readonly bool Contains(KeyValuePair<string, object?> item)
	{
		return IndexOf(item) >= 0;
	}

	public bool Remove(KeyValuePair<string, object?> item)
	{
		int num = IndexOf(item);
		if (num >= 0)
		{
			RemoveAt(num);
			return true;
		}
		return false;
	}

	public readonly IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
	{
		return new Enumerator(in this);
	}

	readonly IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(in this);
	}

	public readonly int IndexOf(KeyValuePair<string, object?> item)
	{
		if (_overflowTags != null)
		{
			for (int i = 0; i < _tagsCount; i++)
			{
				if (TagsEqual(_overflowTags[i], item))
				{
					return i;
				}
			}
			return -1;
		}
		switch (_tagsCount)
		{
		case 1:
			if (TagsEqual(Tag1, item))
			{
				return 0;
			}
			break;
		case 2:
			if (TagsEqual(Tag1, item))
			{
				return 0;
			}
			if (TagsEqual(Tag2, item))
			{
				return 1;
			}
			break;
		case 3:
			if (TagsEqual(Tag1, item))
			{
				return 0;
			}
			if (TagsEqual(Tag2, item))
			{
				return 1;
			}
			if (TagsEqual(Tag3, item))
			{
				return 2;
			}
			break;
		case 4:
			if (TagsEqual(Tag1, item))
			{
				return 0;
			}
			if (TagsEqual(Tag2, item))
			{
				return 1;
			}
			if (TagsEqual(Tag3, item))
			{
				return 2;
			}
			if (TagsEqual(Tag4, item))
			{
				return 3;
			}
			break;
		case 5:
			if (TagsEqual(Tag1, item))
			{
				return 0;
			}
			if (TagsEqual(Tag2, item))
			{
				return 1;
			}
			if (TagsEqual(Tag3, item))
			{
				return 2;
			}
			if (TagsEqual(Tag4, item))
			{
				return 3;
			}
			if (TagsEqual(Tag5, item))
			{
				return 4;
			}
			break;
		case 6:
			if (TagsEqual(Tag1, item))
			{
				return 0;
			}
			if (TagsEqual(Tag2, item))
			{
				return 1;
			}
			if (TagsEqual(Tag3, item))
			{
				return 2;
			}
			if (TagsEqual(Tag4, item))
			{
				return 3;
			}
			if (TagsEqual(Tag5, item))
			{
				return 4;
			}
			if (TagsEqual(Tag6, item))
			{
				return 5;
			}
			break;
		case 7:
			if (TagsEqual(Tag1, item))
			{
				return 0;
			}
			if (TagsEqual(Tag2, item))
			{
				return 1;
			}
			if (TagsEqual(Tag3, item))
			{
				return 2;
			}
			if (TagsEqual(Tag4, item))
			{
				return 3;
			}
			if (TagsEqual(Tag5, item))
			{
				return 4;
			}
			if (TagsEqual(Tag6, item))
			{
				return 5;
			}
			if (TagsEqual(Tag7, item))
			{
				return 6;
			}
			break;
		case 8:
			if (TagsEqual(Tag1, item))
			{
				return 0;
			}
			if (TagsEqual(Tag2, item))
			{
				return 1;
			}
			if (TagsEqual(Tag3, item))
			{
				return 2;
			}
			if (TagsEqual(Tag4, item))
			{
				return 3;
			}
			if (TagsEqual(Tag5, item))
			{
				return 4;
			}
			if (TagsEqual(Tag6, item))
			{
				return 5;
			}
			if (TagsEqual(Tag7, item))
			{
				return 6;
			}
			if (TagsEqual(Tag8, item))
			{
				return 7;
			}
			break;
		}
		return -1;
	}

	private static bool TagsEqual(KeyValuePair<string, object> tag1, KeyValuePair<string, object> tag2)
	{
		if (tag1.Key != tag2.Key)
		{
			return false;
		}
		if (tag1.Value == null)
		{
			if (tag2.Value != null)
			{
				return false;
			}
		}
		else if (!tag1.Value.Equals(tag2.Value))
		{
			return false;
		}
		return true;
	}

	private void MoveTagsToTheArray()
	{
		_overflowTags = new KeyValuePair<string, object>[16];
		_overflowTags[0] = Tag1;
		_overflowTags[1] = Tag2;
		_overflowTags[2] = Tag3;
		_overflowTags[3] = Tag4;
		_overflowTags[4] = Tag5;
		_overflowTags[5] = Tag6;
		_overflowTags[6] = Tag7;
		_overflowTags[7] = Tag8;
	}
}
