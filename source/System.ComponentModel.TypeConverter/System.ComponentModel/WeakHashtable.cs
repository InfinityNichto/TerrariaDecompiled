using System.Collections;
using System.Collections.Generic;

namespace System.ComponentModel;

internal sealed class WeakHashtable : Hashtable
{
	private sealed class WeakKeyComparer : IEqualityComparer
	{
		bool IEqualityComparer.Equals(object x, object y)
		{
			if (x == null)
			{
				return y == null;
			}
			if (y != null && x.GetHashCode() == y.GetHashCode())
			{
				if (x is WeakReference weakReference)
				{
					if (!weakReference.IsAlive)
					{
						return false;
					}
					x = weakReference.Target;
				}
				if (y is WeakReference weakReference2)
				{
					if (!weakReference2.IsAlive)
					{
						return false;
					}
					y = weakReference2.Target;
				}
				return x == y;
			}
			return false;
		}

		int IEqualityComparer.GetHashCode(object obj)
		{
			return obj.GetHashCode();
		}
	}

	private sealed class EqualityWeakReference : WeakReference
	{
		private readonly int _hashCode;

		internal EqualityWeakReference(object o)
			: base(o)
		{
			_hashCode = o.GetHashCode();
		}

		public override bool Equals(object o)
		{
			if (o?.GetHashCode() != _hashCode)
			{
				return false;
			}
			if (o == this || (IsAlive && o == Target))
			{
				return true;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return _hashCode;
		}
	}

	private static readonly IEqualityComparer s_comparer = new WeakKeyComparer();

	private long _lastGlobalMem;

	private int _lastHashCount;

	internal WeakHashtable()
		: base(s_comparer)
	{
	}

	public void SetWeak(object key, object value)
	{
		ScavengeKeys();
		this[new EqualityWeakReference(key)] = value;
	}

	private void ScavengeKeys()
	{
		int count = Count;
		if (count == 0)
		{
			return;
		}
		if (_lastHashCount == 0)
		{
			_lastHashCount = count;
			return;
		}
		long totalMemory = GC.GetTotalMemory(forceFullCollection: false);
		if (_lastGlobalMem == 0L)
		{
			_lastGlobalMem = totalMemory;
			return;
		}
		float num = (float)(totalMemory - _lastGlobalMem) / (float)_lastGlobalMem;
		float num2 = (float)(count - _lastHashCount) / (float)_lastHashCount;
		if (num < 0f && num2 >= 0f)
		{
			List<object> list = null;
			foreach (object key in Keys)
			{
				if (key is WeakReference { IsAlive: false } weakReference)
				{
					if (list == null)
					{
						list = new List<object>();
					}
					list.Add(weakReference);
				}
			}
			if (list != null)
			{
				foreach (object item in list)
				{
					Remove(item);
				}
			}
		}
		_lastGlobalMem = totalMemory;
		_lastHashCount = count;
	}
}
