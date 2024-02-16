using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.RegularExpressions;

[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(CollectionDebuggerProxy<Match>))]
public class MatchCollection : IList<Match>, ICollection<Match>, IEnumerable<Match>, IEnumerable, IReadOnlyList<Match>, IReadOnlyCollection<Match>, IList, ICollection
{
	private sealed class Enumerator : IEnumerator<Match>, IEnumerator, IDisposable
	{
		private readonly MatchCollection _collection;

		private int _index;

		public Match Current
		{
			get
			{
				if (_index < 0)
				{
					throw new InvalidOperationException(System.SR.EnumNotStarted);
				}
				return _collection.GetMatch(_index);
			}
		}

		object IEnumerator.Current => Current;

		internal Enumerator(MatchCollection collection)
		{
			_collection = collection;
			_index = -1;
		}

		public bool MoveNext()
		{
			if (_index == -2)
			{
				return false;
			}
			_index++;
			Match match = _collection.GetMatch(_index);
			if (match == null)
			{
				_index = -2;
				return false;
			}
			return true;
		}

		void IEnumerator.Reset()
		{
			_index = -1;
		}

		void IDisposable.Dispose()
		{
		}
	}

	private readonly Regex _regex;

	private readonly List<Match> _matches;

	private readonly string _input;

	private int _startat;

	private int _prevlen;

	private bool _done;

	public bool IsReadOnly => true;

	public int Count
	{
		get
		{
			EnsureInitialized();
			return _matches.Count;
		}
	}

	public virtual Match this[int i]
	{
		get
		{
			Match result = null;
			if (i < 0 || (result = GetMatch(i)) == null)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.i);
			}
			return result;
		}
	}

	public bool IsSynchronized => false;

	public object SyncRoot => this;

	Match IList<Match>.this[int index]
	{
		get
		{
			return this[index];
		}
		set
		{
			throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
		}
	}

	bool IList.IsFixedSize => true;

	object? IList.this[int index]
	{
		get
		{
			return this[index];
		}
		set
		{
			throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
		}
	}

	internal MatchCollection(Regex regex, string input, int startat)
	{
		if ((uint)startat > (uint)input.Length)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startat, ExceptionResource.BeginIndexNotNegative);
		}
		_regex = regex;
		_input = input;
		_startat = startat;
		_prevlen = -1;
		_matches = new List<Match>();
		_done = false;
	}

	public IEnumerator GetEnumerator()
	{
		return new Enumerator(this);
	}

	IEnumerator<Match> IEnumerable<Match>.GetEnumerator()
	{
		return new Enumerator(this);
	}

	private Match GetMatch(int i)
	{
		if (_matches.Count > i)
		{
			return _matches[i];
		}
		if (_done)
		{
			return null;
		}
		Match match;
		do
		{
			match = _regex.Run(quick: false, _prevlen, _input, 0, _input.Length, _startat);
			if (!match.Success)
			{
				_done = true;
				return null;
			}
			_matches.Add(match);
			_prevlen = match.Length;
			_startat = match._textpos;
		}
		while (_matches.Count <= i);
		return match;
	}

	private void EnsureInitialized()
	{
		if (!_done)
		{
			GetMatch(int.MaxValue);
		}
	}

	public void CopyTo(Array array, int arrayIndex)
	{
		EnsureInitialized();
		((ICollection)_matches).CopyTo(array, arrayIndex);
	}

	public void CopyTo(Match[] array, int arrayIndex)
	{
		EnsureInitialized();
		_matches.CopyTo(array, arrayIndex);
	}

	int IList<Match>.IndexOf(Match item)
	{
		EnsureInitialized();
		return _matches.IndexOf(item);
	}

	void IList<Match>.Insert(int index, Match item)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	void IList<Match>.RemoveAt(int index)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	void ICollection<Match>.Add(Match item)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	void ICollection<Match>.Clear()
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	bool ICollection<Match>.Contains(Match item)
	{
		EnsureInitialized();
		return _matches.Contains(item);
	}

	bool ICollection<Match>.Remove(Match item)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	int IList.Add(object value)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	void IList.Clear()
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	bool IList.Contains(object value)
	{
		if (value is Match)
		{
			return ((ICollection<Match>)this).Contains((Match)value);
		}
		return false;
	}

	int IList.IndexOf(object value)
	{
		if (!(value is Match item))
		{
			return -1;
		}
		return ((IList<Match>)this).IndexOf(item);
	}

	void IList.Insert(int index, object value)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	void IList.Remove(object value)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}

	void IList.RemoveAt(int index)
	{
		throw new NotSupportedException(System.SR.NotSupported_ReadOnlyCollection);
	}
}
