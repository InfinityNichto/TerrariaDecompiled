using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System;

[Serializable]
[StructLayout(LayoutKind.Sequential, Size = 1)]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public struct ValueTuple : IEquatable<ValueTuple>, IStructuralEquatable, IStructuralComparable, IComparable, IComparable<ValueTuple>, IValueTupleInternal, ITuple
{
	int ITuple.Length => 0;

	object? ITuple.this[int index]
	{
		get
		{
			throw new IndexOutOfRangeException();
		}
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		return obj is ValueTuple;
	}

	public bool Equals(ValueTuple other)
	{
		return true;
	}

	bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
	{
		return other is ValueTuple;
	}

	int IComparable.CompareTo(object other)
	{
		if (other == null)
		{
			return 1;
		}
		if (!(other is ValueTuple))
		{
			ThrowHelper.ThrowArgumentException_TupleIncorrectType(this);
		}
		return 0;
	}

	public int CompareTo(ValueTuple other)
	{
		return 0;
	}

	int IStructuralComparable.CompareTo(object other, IComparer comparer)
	{
		if (other == null)
		{
			return 1;
		}
		if (!(other is ValueTuple))
		{
			ThrowHelper.ThrowArgumentException_TupleIncorrectType(this);
		}
		return 0;
	}

	public override int GetHashCode()
	{
		return 0;
	}

	int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
	{
		return 0;
	}

	int IValueTupleInternal.GetHashCode(IEqualityComparer comparer)
	{
		return 0;
	}

	public override string ToString()
	{
		return "()";
	}

	string IValueTupleInternal.ToStringEnd()
	{
		return ")";
	}

	public static ValueTuple Create()
	{
		return default(ValueTuple);
	}

	public static ValueTuple<T1> Create<T1>(T1 item1)
	{
		return new ValueTuple<T1>(item1);
	}

	public static (T1, T2) Create<T1, T2>(T1 item1, T2 item2)
	{
		return (item1, item2);
	}

	public static (T1, T2, T3) Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3)
	{
		return (item1, item2, item3);
	}

	public static (T1, T2, T3, T4) Create<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4)
	{
		return (item1, item2, item3, item4);
	}

	public static (T1, T2, T3, T4, T5) Create<T1, T2, T3, T4, T5>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
	{
		return (item1, item2, item3, item4, item5);
	}

	public static (T1, T2, T3, T4, T5, T6) Create<T1, T2, T3, T4, T5, T6>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
	{
		return (item1, item2, item3, item4, item5, item6);
	}

	public static (T1, T2, T3, T4, T5, T6, T7) Create<T1, T2, T3, T4, T5, T6, T7>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
	{
		return (item1, item2, item3, item4, item5, item6, item7);
	}

	public static (T1, T2, T3, T4, T5, T6, T7, T8) Create<T1, T2, T3, T4, T5, T6, T7, T8>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
	{
		return new ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>>(item1, item2, item3, item4, item5, item6, item7, Create(item8));
	}
}
[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public struct ValueTuple<T1> : IEquatable<ValueTuple<T1>>, IStructuralEquatable, IStructuralComparable, IComparable, IComparable<ValueTuple<T1>>, IValueTupleInternal, ITuple
{
	public T1 Item1;

	int ITuple.Length => 1;

	object? ITuple.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return Item1;
		}
	}

	public ValueTuple(T1 item1)
	{
		Item1 = item1;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is ValueTuple<T1> other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(ValueTuple<T1> other)
	{
		return EqualityComparer<T1>.Default.Equals(Item1, other.Item1);
	}

	bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
	{
		if (other is ValueTuple<T1> valueTuple)
		{
			return comparer.Equals(Item1, valueTuple.Item1);
		}
		return false;
	}

	int IComparable.CompareTo(object other)
	{
		if (other != null)
		{
			if (other is ValueTuple<T1> valueTuple)
			{
				return Comparer<T1>.Default.Compare(Item1, valueTuple.Item1);
			}
			ThrowHelper.ThrowArgumentException_TupleIncorrectType(this);
		}
		return 1;
	}

	public int CompareTo(ValueTuple<T1> other)
	{
		return Comparer<T1>.Default.Compare(Item1, other.Item1);
	}

	int IStructuralComparable.CompareTo(object other, IComparer comparer)
	{
		if (other != null)
		{
			if (other is ValueTuple<T1> valueTuple)
			{
				return comparer.Compare(Item1, valueTuple.Item1);
			}
			ThrowHelper.ThrowArgumentException_TupleIncorrectType(this);
		}
		return 1;
	}

	public override int GetHashCode()
	{
		return Item1?.GetHashCode() ?? 0;
	}

	int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
	{
		return comparer.GetHashCode(Item1);
	}

	int IValueTupleInternal.GetHashCode(IEqualityComparer comparer)
	{
		return comparer.GetHashCode(Item1);
	}

	public override string ToString()
	{
		return "(" + Item1?.ToString() + ")";
	}

	string IValueTupleInternal.ToStringEnd()
	{
		return Item1?.ToString() + ")";
	}
}
[Serializable]
[StructLayout(LayoutKind.Auto)]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public struct ValueTuple<T1, T2> : IEquatable<(T1, T2)>, IStructuralEquatable, IStructuralComparable, IComparable, IComparable<(T1, T2)>, IValueTupleInternal, ITuple
{
	public T1 Item1;

	public T2 Item2;

	int ITuple.Length => 2;

	object? ITuple.this[int index] => index switch
	{
		0 => Item1, 
		1 => Item2, 
		_ => throw new IndexOutOfRangeException(), 
	};

	public ValueTuple(T1 item1, T2 item2)
	{
		Item1 = item1;
		Item2 = item2;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is (T1, T2) other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals((T1, T2) other)
	{
		if (EqualityComparer<T1>.Default.Equals(Item1, other.Item1))
		{
			return EqualityComparer<T2>.Default.Equals(Item2, other.Item2);
		}
		return false;
	}

	bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
	{
		if (other is (T1, T2) tuple && comparer.Equals(Item1, tuple.Item1))
		{
			return comparer.Equals(Item2, tuple.Item2);
		}
		return false;
	}

	int IComparable.CompareTo(object other)
	{
		if (other != null)
		{
			if (other is (T1, T2) other2)
			{
				return CompareTo(other2);
			}
			ThrowHelper.ThrowArgumentException_TupleIncorrectType(this);
		}
		return 1;
	}

	public int CompareTo((T1, T2) other)
	{
		int num = Comparer<T1>.Default.Compare(Item1, other.Item1);
		if (num != 0)
		{
			return num;
		}
		return Comparer<T2>.Default.Compare(Item2, other.Item2);
	}

	int IStructuralComparable.CompareTo(object other, IComparer comparer)
	{
		if (other != null)
		{
			if (other is (T1, T2) tuple)
			{
				int num = comparer.Compare(Item1, tuple.Item1);
				if (num != 0)
				{
					return num;
				}
				return comparer.Compare(Item2, tuple.Item2);
			}
			ThrowHelper.ThrowArgumentException_TupleIncorrectType(this);
		}
		return 1;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Item1?.GetHashCode() ?? 0, Item2?.GetHashCode() ?? 0);
	}

	int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
	{
		return GetHashCodeCore(comparer);
	}

	private int GetHashCodeCore(IEqualityComparer comparer)
	{
		return HashCode.Combine(comparer.GetHashCode(Item1), comparer.GetHashCode(Item2));
	}

	int IValueTupleInternal.GetHashCode(IEqualityComparer comparer)
	{
		return GetHashCodeCore(comparer);
	}

	public override string ToString()
	{
		return "(" + Item1?.ToString() + ", " + Item2?.ToString() + ")";
	}

	string IValueTupleInternal.ToStringEnd()
	{
		return Item1?.ToString() + ", " + Item2?.ToString() + ")";
	}
}
[Serializable]
[StructLayout(LayoutKind.Auto)]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public struct ValueTuple<T1, T2, T3> : IEquatable<(T1, T2, T3)>, IStructuralEquatable, IStructuralComparable, IComparable, IComparable<(T1, T2, T3)>, IValueTupleInternal, ITuple
{
	public T1 Item1;

	public T2 Item2;

	public T3 Item3;

	int ITuple.Length => 3;

	object? ITuple.this[int index] => index switch
	{
		0 => Item1, 
		1 => Item2, 
		2 => Item3, 
		_ => throw new IndexOutOfRangeException(), 
	};

	public ValueTuple(T1 item1, T2 item2, T3 item3)
	{
		Item1 = item1;
		Item2 = item2;
		Item3 = item3;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is (T1, T2, T3) other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals((T1, T2, T3) other)
	{
		if (EqualityComparer<T1>.Default.Equals(Item1, other.Item1) && EqualityComparer<T2>.Default.Equals(Item2, other.Item2))
		{
			return EqualityComparer<T3>.Default.Equals(Item3, other.Item3);
		}
		return false;
	}

	bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
	{
		if (other is (T1, T2, T3) tuple && comparer.Equals(Item1, tuple.Item1) && comparer.Equals(Item2, tuple.Item2))
		{
			return comparer.Equals(Item3, tuple.Item3);
		}
		return false;
	}

	int IComparable.CompareTo(object other)
	{
		if (other != null)
		{
			if (other is (T1, T2, T3) other2)
			{
				return CompareTo(other2);
			}
			ThrowHelper.ThrowArgumentException_TupleIncorrectType(this);
		}
		return 1;
	}

	public int CompareTo((T1, T2, T3) other)
	{
		int num = Comparer<T1>.Default.Compare(Item1, other.Item1);
		if (num != 0)
		{
			return num;
		}
		num = Comparer<T2>.Default.Compare(Item2, other.Item2);
		if (num != 0)
		{
			return num;
		}
		return Comparer<T3>.Default.Compare(Item3, other.Item3);
	}

	int IStructuralComparable.CompareTo(object other, IComparer comparer)
	{
		if (other != null)
		{
			if (other is (T1, T2, T3) tuple)
			{
				int num = comparer.Compare(Item1, tuple.Item1);
				if (num != 0)
				{
					return num;
				}
				num = comparer.Compare(Item2, tuple.Item2);
				if (num != 0)
				{
					return num;
				}
				return comparer.Compare(Item3, tuple.Item3);
			}
			ThrowHelper.ThrowArgumentException_TupleIncorrectType(this);
		}
		return 1;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Item1?.GetHashCode() ?? 0, Item2?.GetHashCode() ?? 0, Item3?.GetHashCode() ?? 0);
	}

	int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
	{
		return GetHashCodeCore(comparer);
	}

	private int GetHashCodeCore(IEqualityComparer comparer)
	{
		return HashCode.Combine(comparer.GetHashCode(Item1), comparer.GetHashCode(Item2), comparer.GetHashCode(Item3));
	}

	int IValueTupleInternal.GetHashCode(IEqualityComparer comparer)
	{
		return GetHashCodeCore(comparer);
	}

	public override string ToString()
	{
		return "(" + Item1?.ToString() + ", " + Item2?.ToString() + ", " + Item3?.ToString() + ")";
	}

	string IValueTupleInternal.ToStringEnd()
	{
		return Item1?.ToString() + ", " + Item2?.ToString() + ", " + Item3?.ToString() + ")";
	}
}
[Serializable]
[StructLayout(LayoutKind.Auto)]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public struct ValueTuple<T1, T2, T3, T4> : IEquatable<(T1, T2, T3, T4)>, IStructuralEquatable, IStructuralComparable, IComparable, IComparable<(T1, T2, T3, T4)>, IValueTupleInternal, ITuple
{
	public T1 Item1;

	public T2 Item2;

	public T3 Item3;

	public T4 Item4;

	int ITuple.Length => 4;

	object? ITuple.this[int index] => index switch
	{
		0 => Item1, 
		1 => Item2, 
		2 => Item3, 
		3 => Item4, 
		_ => throw new IndexOutOfRangeException(), 
	};

	public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4)
	{
		Item1 = item1;
		Item2 = item2;
		Item3 = item3;
		Item4 = item4;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is (T1, T2, T3, T4) other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals((T1, T2, T3, T4) other)
	{
		if (EqualityComparer<T1>.Default.Equals(Item1, other.Item1) && EqualityComparer<T2>.Default.Equals(Item2, other.Item2) && EqualityComparer<T3>.Default.Equals(Item3, other.Item3))
		{
			return EqualityComparer<T4>.Default.Equals(Item4, other.Item4);
		}
		return false;
	}

	bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
	{
		if (other is (T1, T2, T3, T4) tuple && comparer.Equals(Item1, tuple.Item1) && comparer.Equals(Item2, tuple.Item2) && comparer.Equals(Item3, tuple.Item3))
		{
			return comparer.Equals(Item4, tuple.Item4);
		}
		return false;
	}

	int IComparable.CompareTo(object other)
	{
		if (other != null)
		{
			if (other is (T1, T2, T3, T4) other2)
			{
				return CompareTo(other2);
			}
			ThrowHelper.ThrowArgumentException_TupleIncorrectType(this);
		}
		return 1;
	}

	public int CompareTo((T1, T2, T3, T4) other)
	{
		int num = Comparer<T1>.Default.Compare(Item1, other.Item1);
		if (num != 0)
		{
			return num;
		}
		num = Comparer<T2>.Default.Compare(Item2, other.Item2);
		if (num != 0)
		{
			return num;
		}
		num = Comparer<T3>.Default.Compare(Item3, other.Item3);
		if (num != 0)
		{
			return num;
		}
		return Comparer<T4>.Default.Compare(Item4, other.Item4);
	}

	int IStructuralComparable.CompareTo(object other, IComparer comparer)
	{
		if (other != null)
		{
			if (other is (T1, T2, T3, T4) tuple)
			{
				int num = comparer.Compare(Item1, tuple.Item1);
				if (num != 0)
				{
					return num;
				}
				num = comparer.Compare(Item2, tuple.Item2);
				if (num != 0)
				{
					return num;
				}
				num = comparer.Compare(Item3, tuple.Item3);
				if (num != 0)
				{
					return num;
				}
				return comparer.Compare(Item4, tuple.Item4);
			}
			ThrowHelper.ThrowArgumentException_TupleIncorrectType(this);
		}
		return 1;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Item1?.GetHashCode() ?? 0, Item2?.GetHashCode() ?? 0, Item3?.GetHashCode() ?? 0, Item4?.GetHashCode() ?? 0);
	}

	int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
	{
		return GetHashCodeCore(comparer);
	}

	private int GetHashCodeCore(IEqualityComparer comparer)
	{
		return HashCode.Combine(comparer.GetHashCode(Item1), comparer.GetHashCode(Item2), comparer.GetHashCode(Item3), comparer.GetHashCode(Item4));
	}

	int IValueTupleInternal.GetHashCode(IEqualityComparer comparer)
	{
		return GetHashCodeCore(comparer);
	}

	public override string ToString()
	{
		return "(" + Item1?.ToString() + ", " + Item2?.ToString() + ", " + Item3?.ToString() + ", " + Item4?.ToString() + ")";
	}

	string IValueTupleInternal.ToStringEnd()
	{
		return Item1?.ToString() + ", " + Item2?.ToString() + ", " + Item3?.ToString() + ", " + Item4?.ToString() + ")";
	}
}
[Serializable]
[StructLayout(LayoutKind.Auto)]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public struct ValueTuple<T1, T2, T3, T4, T5> : IEquatable<(T1, T2, T3, T4, T5)>, IStructuralEquatable, IStructuralComparable, IComparable, IComparable<(T1, T2, T3, T4, T5)>, IValueTupleInternal, ITuple
{
	public T1 Item1;

	public T2 Item2;

	public T3 Item3;

	public T4 Item4;

	public T5 Item5;

	int ITuple.Length => 5;

	object? ITuple.this[int index] => index switch
	{
		0 => Item1, 
		1 => Item2, 
		2 => Item3, 
		3 => Item4, 
		4 => Item5, 
		_ => throw new IndexOutOfRangeException(), 
	};

	public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
	{
		Item1 = item1;
		Item2 = item2;
		Item3 = item3;
		Item4 = item4;
		Item5 = item5;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is (T1, T2, T3, T4, T5) other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals((T1, T2, T3, T4, T5) other)
	{
		if (EqualityComparer<T1>.Default.Equals(Item1, other.Item1) && EqualityComparer<T2>.Default.Equals(Item2, other.Item2) && EqualityComparer<T3>.Default.Equals(Item3, other.Item3) && EqualityComparer<T4>.Default.Equals(Item4, other.Item4))
		{
			return EqualityComparer<T5>.Default.Equals(Item5, other.Item5);
		}
		return false;
	}

	bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
	{
		if (other is (T1, T2, T3, T4, T5) tuple && comparer.Equals(Item1, tuple.Item1) && comparer.Equals(Item2, tuple.Item2) && comparer.Equals(Item3, tuple.Item3))
		{
			return comparer.Equals(Item5, tuple.Item5);
		}
		return false;
	}

	int IComparable.CompareTo(object other)
	{
		if (other != null)
		{
			if (other is (T1, T2, T3, T4, T5) other2)
			{
				return CompareTo(other2);
			}
			ThrowHelper.ThrowArgumentException_TupleIncorrectType(this);
		}
		return 1;
	}

	public int CompareTo((T1, T2, T3, T4, T5) other)
	{
		int num = Comparer<T1>.Default.Compare(Item1, other.Item1);
		if (num != 0)
		{
			return num;
		}
		num = Comparer<T2>.Default.Compare(Item2, other.Item2);
		if (num != 0)
		{
			return num;
		}
		num = Comparer<T3>.Default.Compare(Item3, other.Item3);
		if (num != 0)
		{
			return num;
		}
		num = Comparer<T4>.Default.Compare(Item4, other.Item4);
		if (num != 0)
		{
			return num;
		}
		return Comparer<T5>.Default.Compare(Item5, other.Item5);
	}

	int IStructuralComparable.CompareTo(object other, IComparer comparer)
	{
		if (other != null)
		{
			if (other is (T1, T2, T3, T4, T5) tuple)
			{
				int num = comparer.Compare(Item1, tuple.Item1);
				if (num != 0)
				{
					return num;
				}
				num = comparer.Compare(Item2, tuple.Item2);
				if (num != 0)
				{
					return num;
				}
				num = comparer.Compare(Item3, tuple.Item3);
				if (num != 0)
				{
					return num;
				}
				num = comparer.Compare(Item4, tuple.Item4);
				if (num != 0)
				{
					return num;
				}
				return comparer.Compare(Item5, tuple.Item5);
			}
			ThrowHelper.ThrowArgumentException_TupleIncorrectType(this);
		}
		return 1;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Item1?.GetHashCode() ?? 0, Item2?.GetHashCode() ?? 0, Item3?.GetHashCode() ?? 0, Item4?.GetHashCode() ?? 0, Item5?.GetHashCode() ?? 0);
	}

	int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
	{
		return GetHashCodeCore(comparer);
	}

	private int GetHashCodeCore(IEqualityComparer comparer)
	{
		return HashCode.Combine(comparer.GetHashCode(Item1), comparer.GetHashCode(Item2), comparer.GetHashCode(Item3), comparer.GetHashCode(Item4), comparer.GetHashCode(Item5));
	}

	int IValueTupleInternal.GetHashCode(IEqualityComparer comparer)
	{
		return GetHashCodeCore(comparer);
	}

	public override string ToString()
	{
		return "(" + Item1?.ToString() + ", " + Item2?.ToString() + ", " + Item3?.ToString() + ", " + Item4?.ToString() + ", " + Item5?.ToString() + ")";
	}

	string IValueTupleInternal.ToStringEnd()
	{
		return Item1?.ToString() + ", " + Item2?.ToString() + ", " + Item3?.ToString() + ", " + Item4?.ToString() + ", " + Item5?.ToString() + ")";
	}
}
[Serializable]
[StructLayout(LayoutKind.Auto)]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public struct ValueTuple<T1, T2, T3, T4, T5, T6> : IEquatable<(T1, T2, T3, T4, T5, T6)>, IStructuralEquatable, IStructuralComparable, IComparable, IComparable<(T1, T2, T3, T4, T5, T6)>, IValueTupleInternal, ITuple
{
	public T1 Item1;

	public T2 Item2;

	public T3 Item3;

	public T4 Item4;

	public T5 Item5;

	public T6 Item6;

	int ITuple.Length => 6;

	object? ITuple.this[int index] => index switch
	{
		0 => Item1, 
		1 => Item2, 
		2 => Item3, 
		3 => Item4, 
		4 => Item5, 
		5 => Item6, 
		_ => throw new IndexOutOfRangeException(), 
	};

	public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
	{
		Item1 = item1;
		Item2 = item2;
		Item3 = item3;
		Item4 = item4;
		Item5 = item5;
		Item6 = item6;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is (T1, T2, T3, T4, T5, T6) other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals((T1, T2, T3, T4, T5, T6) other)
	{
		if (EqualityComparer<T1>.Default.Equals(Item1, other.Item1) && EqualityComparer<T2>.Default.Equals(Item2, other.Item2) && EqualityComparer<T3>.Default.Equals(Item3, other.Item3) && EqualityComparer<T4>.Default.Equals(Item4, other.Item4) && EqualityComparer<T5>.Default.Equals(Item5, other.Item5))
		{
			return EqualityComparer<T6>.Default.Equals(Item6, other.Item6);
		}
		return false;
	}

	bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
	{
		if (other is (T1, T2, T3, T4, T5, T6) tuple && comparer.Equals(Item1, tuple.Item1) && comparer.Equals(Item2, tuple.Item2) && comparer.Equals(Item3, tuple.Item3) && comparer.Equals(Item5, tuple.Item5))
		{
			return comparer.Equals(Item6, tuple.Item6);
		}
		return false;
	}

	int IComparable.CompareTo(object other)
	{
		if (other != null)
		{
			if (other is (T1, T2, T3, T4, T5, T6) other2)
			{
				return CompareTo(other2);
			}
			ThrowHelper.ThrowArgumentException_TupleIncorrectType(this);
		}
		return 1;
	}

	public int CompareTo((T1, T2, T3, T4, T5, T6) other)
	{
		int num = Comparer<T1>.Default.Compare(Item1, other.Item1);
		if (num != 0)
		{
			return num;
		}
		num = Comparer<T2>.Default.Compare(Item2, other.Item2);
		if (num != 0)
		{
			return num;
		}
		num = Comparer<T3>.Default.Compare(Item3, other.Item3);
		if (num != 0)
		{
			return num;
		}
		num = Comparer<T4>.Default.Compare(Item4, other.Item4);
		if (num != 0)
		{
			return num;
		}
		num = Comparer<T5>.Default.Compare(Item5, other.Item5);
		if (num != 0)
		{
			return num;
		}
		return Comparer<T6>.Default.Compare(Item6, other.Item6);
	}

	int IStructuralComparable.CompareTo(object other, IComparer comparer)
	{
		if (other != null)
		{
			if (other is (T1, T2, T3, T4, T5, T6) tuple)
			{
				int num = comparer.Compare(Item1, tuple.Item1);
				if (num != 0)
				{
					return num;
				}
				num = comparer.Compare(Item2, tuple.Item2);
				if (num != 0)
				{
					return num;
				}
				num = comparer.Compare(Item3, tuple.Item3);
				if (num != 0)
				{
					return num;
				}
				num = comparer.Compare(Item4, tuple.Item4);
				if (num != 0)
				{
					return num;
				}
				num = comparer.Compare(Item5, tuple.Item5);
				if (num != 0)
				{
					return num;
				}
				return comparer.Compare(Item6, tuple.Item6);
			}
			ThrowHelper.ThrowArgumentException_TupleIncorrectType(this);
		}
		return 1;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Item1?.GetHashCode() ?? 0, Item2?.GetHashCode() ?? 0, Item3?.GetHashCode() ?? 0, Item4?.GetHashCode() ?? 0, Item5?.GetHashCode() ?? 0, Item6?.GetHashCode() ?? 0);
	}

	int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
	{
		return GetHashCodeCore(comparer);
	}

	private int GetHashCodeCore(IEqualityComparer comparer)
	{
		return HashCode.Combine(comparer.GetHashCode(Item1), comparer.GetHashCode(Item2), comparer.GetHashCode(Item3), comparer.GetHashCode(Item4), comparer.GetHashCode(Item5), comparer.GetHashCode(Item6));
	}

	int IValueTupleInternal.GetHashCode(IEqualityComparer comparer)
	{
		return GetHashCodeCore(comparer);
	}

	public override string ToString()
	{
		return "(" + Item1?.ToString() + ", " + Item2?.ToString() + ", " + Item3?.ToString() + ", " + Item4?.ToString() + ", " + Item5?.ToString() + ", " + Item6?.ToString() + ")";
	}

	string IValueTupleInternal.ToStringEnd()
	{
		return Item1?.ToString() + ", " + Item2?.ToString() + ", " + Item3?.ToString() + ", " + Item4?.ToString() + ", " + Item5?.ToString() + ", " + Item6?.ToString() + ")";
	}
}
[Serializable]
[StructLayout(LayoutKind.Auto)]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7> : IEquatable<(T1, T2, T3, T4, T5, T6, T7)>, IStructuralEquatable, IStructuralComparable, IComparable, IComparable<(T1, T2, T3, T4, T5, T6, T7)>, IValueTupleInternal, ITuple
{
	public T1 Item1;

	public T2 Item2;

	public T3 Item3;

	public T4 Item4;

	public T5 Item5;

	public T6 Item6;

	public T7 Item7;

	int ITuple.Length => 7;

	object? ITuple.this[int index] => index switch
	{
		0 => Item1, 
		1 => Item2, 
		2 => Item3, 
		3 => Item4, 
		4 => Item5, 
		5 => Item6, 
		6 => Item7, 
		_ => throw new IndexOutOfRangeException(), 
	};

	public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
	{
		Item1 = item1;
		Item2 = item2;
		Item3 = item3;
		Item4 = item4;
		Item5 = item5;
		Item6 = item6;
		Item7 = item7;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is (T1, T2, T3, T4, T5, T6, T7) other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals((T1, T2, T3, T4, T5, T6, T7) other)
	{
		if (EqualityComparer<T1>.Default.Equals(Item1, other.Item1) && EqualityComparer<T2>.Default.Equals(Item2, other.Item2) && EqualityComparer<T3>.Default.Equals(Item3, other.Item3) && EqualityComparer<T4>.Default.Equals(Item4, other.Item4) && EqualityComparer<T5>.Default.Equals(Item5, other.Item5) && EqualityComparer<T6>.Default.Equals(Item6, other.Item6))
		{
			return EqualityComparer<T7>.Default.Equals(Item7, other.Item7);
		}
		return false;
	}

	bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
	{
		if (other is (T1, T2, T3, T4, T5, T6, T7) tuple && comparer.Equals(Item1, tuple.Item1) && comparer.Equals(Item2, tuple.Item2) && comparer.Equals(Item3, tuple.Item3) && comparer.Equals(Item5, tuple.Item5) && comparer.Equals(Item6, tuple.Item6))
		{
			return comparer.Equals(Item7, tuple.Item7);
		}
		return false;
	}

	int IComparable.CompareTo(object other)
	{
		if (other != null)
		{
			if (other is (T1, T2, T3, T4, T5, T6, T7) other2)
			{
				return CompareTo(other2);
			}
			ThrowHelper.ThrowArgumentException_TupleIncorrectType(this);
		}
		return 1;
	}

	public int CompareTo((T1, T2, T3, T4, T5, T6, T7) other)
	{
		int num = Comparer<T1>.Default.Compare(Item1, other.Item1);
		if (num != 0)
		{
			return num;
		}
		num = Comparer<T2>.Default.Compare(Item2, other.Item2);
		if (num != 0)
		{
			return num;
		}
		num = Comparer<T3>.Default.Compare(Item3, other.Item3);
		if (num != 0)
		{
			return num;
		}
		num = Comparer<T4>.Default.Compare(Item4, other.Item4);
		if (num != 0)
		{
			return num;
		}
		num = Comparer<T5>.Default.Compare(Item5, other.Item5);
		if (num != 0)
		{
			return num;
		}
		num = Comparer<T6>.Default.Compare(Item6, other.Item6);
		if (num != 0)
		{
			return num;
		}
		return Comparer<T7>.Default.Compare(Item7, other.Item7);
	}

	int IStructuralComparable.CompareTo(object other, IComparer comparer)
	{
		if (other != null)
		{
			if (other is (T1, T2, T3, T4, T5, T6, T7) tuple)
			{
				int num = comparer.Compare(Item1, tuple.Item1);
				if (num != 0)
				{
					return num;
				}
				num = comparer.Compare(Item2, tuple.Item2);
				if (num != 0)
				{
					return num;
				}
				num = comparer.Compare(Item3, tuple.Item3);
				if (num != 0)
				{
					return num;
				}
				num = comparer.Compare(Item4, tuple.Item4);
				if (num != 0)
				{
					return num;
				}
				num = comparer.Compare(Item5, tuple.Item5);
				if (num != 0)
				{
					return num;
				}
				num = comparer.Compare(Item6, tuple.Item6);
				if (num != 0)
				{
					return num;
				}
				return comparer.Compare(Item7, tuple.Item7);
			}
			ThrowHelper.ThrowArgumentException_TupleIncorrectType(this);
		}
		return 1;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Item1?.GetHashCode() ?? 0, Item2?.GetHashCode() ?? 0, Item3?.GetHashCode() ?? 0, Item4?.GetHashCode() ?? 0, Item5?.GetHashCode() ?? 0, Item6?.GetHashCode() ?? 0, Item7?.GetHashCode() ?? 0);
	}

	int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
	{
		return GetHashCodeCore(comparer);
	}

	private int GetHashCodeCore(IEqualityComparer comparer)
	{
		return HashCode.Combine(comparer.GetHashCode(Item1), comparer.GetHashCode(Item2), comparer.GetHashCode(Item3), comparer.GetHashCode(Item4), comparer.GetHashCode(Item5), comparer.GetHashCode(Item6), comparer.GetHashCode(Item7));
	}

	int IValueTupleInternal.GetHashCode(IEqualityComparer comparer)
	{
		return GetHashCodeCore(comparer);
	}

	public override string ToString()
	{
		return "(" + Item1?.ToString() + ", " + Item2?.ToString() + ", " + Item3?.ToString() + ", " + Item4?.ToString() + ", " + Item5?.ToString() + ", " + Item6?.ToString() + ", " + Item7?.ToString() + ")";
	}

	string IValueTupleInternal.ToStringEnd()
	{
		return Item1?.ToString() + ", " + Item2?.ToString() + ", " + Item3?.ToString() + ", " + Item4?.ToString() + ", " + Item5?.ToString() + ", " + Item6?.ToString() + ", " + Item7?.ToString() + ")";
	}
}
[Serializable]
[StructLayout(LayoutKind.Auto)]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> : IEquatable<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>, IStructuralEquatable, IStructuralComparable, IComparable, IComparable<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>, IValueTupleInternal, ITuple where TRest : struct
{
	public T1 Item1;

	public T2 Item2;

	public T3 Item3;

	public T4 Item4;

	public T5 Item5;

	public T6 Item6;

	public T7 Item7;

	public TRest Rest;

	int ITuple.Length
	{
		get
		{
			if (!(Rest is IValueTupleInternal))
			{
				return 8;
			}
			return 7 + ((IValueTupleInternal)(object)Rest).Length;
		}
	}

	object? ITuple.this[int index]
	{
		get
		{
			switch (index)
			{
			case 0:
				return Item1;
			case 1:
				return Item2;
			case 2:
				return Item3;
			case 3:
				return Item4;
			case 4:
				return Item5;
			case 5:
				return Item6;
			case 6:
				return Item7;
			default:
				if (Rest is IValueTupleInternal)
				{
					return ((IValueTupleInternal)(object)Rest)[index - 7];
				}
				if (index == 7)
				{
					return Rest;
				}
				throw new IndexOutOfRangeException();
			}
		}
	}

	public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest)
	{
		if (!(rest is IValueTupleInternal))
		{
			throw new ArgumentException(SR.ArgumentException_ValueTupleLastArgumentNotAValueTuple);
		}
		Item1 = item1;
		Item2 = item2;
		Item3 = item3;
		Item4 = item4;
		Item5 = item5;
		Item6 = item6;
		Item7 = item7;
		Rest = rest;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> other)
	{
		if (EqualityComparer<T1>.Default.Equals(Item1, other.Item1) && EqualityComparer<T2>.Default.Equals(Item2, other.Item2) && EqualityComparer<T3>.Default.Equals(Item3, other.Item3) && EqualityComparer<T4>.Default.Equals(Item4, other.Item4) && EqualityComparer<T5>.Default.Equals(Item5, other.Item5) && EqualityComparer<T6>.Default.Equals(Item6, other.Item6) && EqualityComparer<T7>.Default.Equals(Item7, other.Item7))
		{
			return EqualityComparer<TRest>.Default.Equals(Rest, other.Rest);
		}
		return false;
	}

	bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
	{
		if (other is ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> valueTuple && comparer.Equals(Item1, valueTuple.Item1) && comparer.Equals(Item2, valueTuple.Item2) && comparer.Equals(Item3, valueTuple.Item3) && comparer.Equals(Item5, valueTuple.Item5) && comparer.Equals(Item6, valueTuple.Item6) && comparer.Equals(Item7, valueTuple.Item7))
		{
			return comparer.Equals(Rest, valueTuple.Rest);
		}
		return false;
	}

	int IComparable.CompareTo(object other)
	{
		if (other != null)
		{
			if (other is ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> other2)
			{
				return CompareTo(other2);
			}
			ThrowHelper.ThrowArgumentException_TupleIncorrectType(this);
		}
		return 1;
	}

	public int CompareTo(ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> other)
	{
		int num = Comparer<T1>.Default.Compare(Item1, other.Item1);
		if (num != 0)
		{
			return num;
		}
		num = Comparer<T2>.Default.Compare(Item2, other.Item2);
		if (num != 0)
		{
			return num;
		}
		num = Comparer<T3>.Default.Compare(Item3, other.Item3);
		if (num != 0)
		{
			return num;
		}
		num = Comparer<T4>.Default.Compare(Item4, other.Item4);
		if (num != 0)
		{
			return num;
		}
		num = Comparer<T5>.Default.Compare(Item5, other.Item5);
		if (num != 0)
		{
			return num;
		}
		num = Comparer<T6>.Default.Compare(Item6, other.Item6);
		if (num != 0)
		{
			return num;
		}
		num = Comparer<T7>.Default.Compare(Item7, other.Item7);
		if (num != 0)
		{
			return num;
		}
		return Comparer<TRest>.Default.Compare(Rest, other.Rest);
	}

	int IStructuralComparable.CompareTo(object other, IComparer comparer)
	{
		if (other != null)
		{
			if (other is ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> valueTuple)
			{
				int num = comparer.Compare(Item1, valueTuple.Item1);
				if (num != 0)
				{
					return num;
				}
				num = comparer.Compare(Item2, valueTuple.Item2);
				if (num != 0)
				{
					return num;
				}
				num = comparer.Compare(Item3, valueTuple.Item3);
				if (num != 0)
				{
					return num;
				}
				num = comparer.Compare(Item4, valueTuple.Item4);
				if (num != 0)
				{
					return num;
				}
				num = comparer.Compare(Item5, valueTuple.Item5);
				if (num != 0)
				{
					return num;
				}
				num = comparer.Compare(Item6, valueTuple.Item6);
				if (num != 0)
				{
					return num;
				}
				num = comparer.Compare(Item7, valueTuple.Item7);
				if (num != 0)
				{
					return num;
				}
				return comparer.Compare(Rest, valueTuple.Rest);
			}
			ThrowHelper.ThrowArgumentException_TupleIncorrectType(this);
		}
		return 1;
	}

	public override int GetHashCode()
	{
		T1 val;
		int value;
		if (!(Rest is IValueTupleInternal))
		{
			ref T1 reference = ref Item1;
			val = default(T1);
			if (val == null)
			{
				val = reference;
				reference = ref val;
				if (val == null)
				{
					value = 0;
					goto IL_004a;
				}
			}
			value = reference.GetHashCode();
			goto IL_004a;
		}
		int length = ((IValueTupleInternal)(object)Rest).Length;
		int hashCode = Rest.GetHashCode();
		if (length >= 8)
		{
			return hashCode;
		}
		T7 val2;
		int value24;
		T6 val7;
		int value12;
		T5 val5;
		int value10;
		T4 val3;
		int value5;
		T3 val6;
		int value26;
		T2 val4;
		int value27;
		int value2;
		ref T7 reference3;
		int value3;
		ref T4 reference4;
		int value4;
		int value6;
		int value7;
		int value8;
		ref T2 reference5;
		int value9;
		ref T5 reference7;
		int value11;
		int value13;
		ref T3 reference8;
		int value14;
		ref T6 reference9;
		int value15;
		ref T6 reference10;
		int value16;
		ref T5 reference11;
		int value17;
		ref T4 reference12;
		int value18;
		ref T6 reference13;
		int value19;
		ref T6 reference14;
		ref T7 reference15;
		ref T5 reference16;
		int value20;
		ref T7 reference18;
		int value21;
		ref T7 reference19;
		int value22;
		ref T6 reference20;
		int value23;
		ref T7 reference22;
		ref T7 reference23;
		int value25;
		int value28;
		int value29;
		ref T4 reference27;
		ref T3 reference28;
		ref T5 reference29;
		switch (8 - length)
		{
		case 1:
		{
			ref T7 reference21 = ref Item7;
			val2 = default(T7);
			if (val2 == null)
			{
				val2 = reference21;
				reference21 = ref val2;
				if (val2 == null)
				{
					value24 = 0;
					goto IL_0237;
				}
			}
			value24 = reference21.GetHashCode();
			goto IL_0237;
		}
		case 2:
		{
			ref T6 reference17 = ref Item6;
			val7 = default(T6);
			if (val7 == null)
			{
				val7 = reference17;
				reference17 = ref val7;
				if (val7 == null)
				{
					value12 = 0;
					goto IL_0276;
				}
			}
			value12 = reference17.GetHashCode();
			goto IL_0276;
		}
		case 3:
		{
			ref T5 reference6 = ref Item5;
			val5 = default(T5);
			if (val5 == null)
			{
				val5 = reference6;
				reference6 = ref val5;
				if (val5 == null)
				{
					value10 = 0;
					goto IL_02ed;
				}
			}
			value10 = reference6.GetHashCode();
			goto IL_02ed;
		}
		case 4:
		{
			ref T4 reference25 = ref Item4;
			val3 = default(T4);
			if (val3 == null)
			{
				val3 = reference25;
				reference25 = ref val3;
				if (val3 == null)
				{
					value5 = 0;
					goto IL_039c;
				}
			}
			value5 = reference25.GetHashCode();
			goto IL_039c;
		}
		case 5:
		{
			ref T3 reference24 = ref Item3;
			val6 = default(T3);
			if (val6 == null)
			{
				val6 = reference24;
				reference24 = ref val6;
				if (val6 == null)
				{
					value26 = 0;
					goto IL_0483;
				}
			}
			value26 = reference24.GetHashCode();
			goto IL_0483;
		}
		case 6:
		{
			ref T2 reference26 = ref Item2;
			val4 = default(T2);
			if (val4 == null)
			{
				val4 = reference26;
				reference26 = ref val4;
				if (val4 == null)
				{
					value27 = 0;
					goto IL_05a2;
				}
			}
			value27 = reference26.GetHashCode();
			goto IL_05a2;
		}
		case 7:
		case 8:
		{
			ref T1 reference2 = ref Item1;
			val = default(T1);
			if (val == null)
			{
				val = reference2;
				reference2 = ref val;
				if (val == null)
				{
					value2 = 0;
					goto IL_06f6;
				}
			}
			value2 = reference2.GetHashCode();
			goto IL_06f6;
		}
		default:
			{
				return -1;
			}
			IL_0325:
			reference3 = ref Item7;
			val2 = default(T7);
			if (val2 == null)
			{
				val2 = reference3;
				reference3 = ref val2;
				if (val2 == null)
				{
					value3 = 0;
					goto IL_035d;
				}
			}
			value3 = reference3.GetHashCode();
			goto IL_035d;
			IL_05da:
			reference4 = ref Item4;
			val3 = default(T4);
			if (val3 == null)
			{
				val3 = reference4;
				reference4 = ref val3;
				if (val3 == null)
				{
					value4 = 0;
					goto IL_0612;
				}
			}
			value4 = reference4.GetHashCode();
			goto IL_0612;
			IL_0444:
			return HashCode.Combine(value5, value6, value7, value8, hashCode);
			IL_06f6:
			reference5 = ref Item2;
			val4 = default(T2);
			if (val4 == null)
			{
				val4 = reference5;
				reference5 = ref val4;
				if (val4 == null)
				{
					value9 = 0;
					goto IL_072e;
				}
			}
			value9 = reference5.GetHashCode();
			goto IL_072e;
			IL_04bb:
			reference7 = ref Item5;
			val5 = default(T5);
			if (val5 == null)
			{
				val5 = reference7;
				reference7 = ref val5;
				if (val5 == null)
				{
					value11 = 0;
					goto IL_04f3;
				}
			}
			value11 = reference7.GetHashCode();
			goto IL_04f3;
			IL_02ae:
			return HashCode.Combine(value12, value13, hashCode);
			IL_072e:
			reference8 = ref Item3;
			val6 = default(T3);
			if (val6 == null)
			{
				val6 = reference8;
				reference8 = ref val6;
				if (val6 == null)
				{
					value14 = 0;
					goto IL_0766;
				}
			}
			value14 = reference8.GetHashCode();
			goto IL_0766;
			IL_04f3:
			reference9 = ref Item6;
			val7 = default(T6);
			if (val7 == null)
			{
				val7 = reference9;
				reference9 = ref val7;
				if (val7 == null)
				{
					value15 = 0;
					goto IL_052b;
				}
			}
			value15 = reference9.GetHashCode();
			goto IL_052b;
			IL_02ed:
			reference10 = ref Item6;
			val7 = default(T6);
			if (val7 == null)
			{
				val7 = reference10;
				reference10 = ref val7;
				if (val7 == null)
				{
					value16 = 0;
					goto IL_0325;
				}
			}
			value16 = reference10.GetHashCode();
			goto IL_0325;
			IL_0612:
			reference11 = ref Item5;
			val5 = default(T5);
			if (val5 == null)
			{
				val5 = reference11;
				reference11 = ref val5;
				if (val5 == null)
				{
					value17 = 0;
					goto IL_064a;
				}
			}
			value17 = reference11.GetHashCode();
			goto IL_064a;
			IL_0766:
			reference12 = ref Item4;
			val3 = default(T4);
			if (val3 == null)
			{
				val3 = reference12;
				reference12 = ref val3;
				if (val3 == null)
				{
					value18 = 0;
					goto IL_079e;
				}
			}
			value18 = reference12.GetHashCode();
			goto IL_079e;
			IL_064a:
			reference13 = ref Item6;
			val7 = default(T6);
			if (val7 == null)
			{
				val7 = reference13;
				reference13 = ref val7;
				if (val7 == null)
				{
					value19 = 0;
					goto IL_0682;
				}
			}
			value19 = reference13.GetHashCode();
			goto IL_0682;
			IL_03d4:
			reference14 = ref Item6;
			val7 = default(T6);
			if (val7 == null)
			{
				val7 = reference14;
				reference14 = ref val7;
				if (val7 == null)
				{
					value7 = 0;
					goto IL_040c;
				}
			}
			value7 = reference14.GetHashCode();
			goto IL_040c;
			IL_0276:
			reference15 = ref Item7;
			val2 = default(T7);
			if (val2 == null)
			{
				val2 = reference15;
				reference15 = ref val2;
				if (val2 == null)
				{
					value13 = 0;
					goto IL_02ae;
				}
			}
			value13 = reference15.GetHashCode();
			goto IL_02ae;
			IL_079e:
			reference16 = ref Item5;
			val5 = default(T5);
			if (val5 == null)
			{
				val5 = reference16;
				reference16 = ref val5;
				if (val5 == null)
				{
					value20 = 0;
					goto IL_07d6;
				}
			}
			value20 = reference16.GetHashCode();
			goto IL_07d6;
			IL_0682:
			reference18 = ref Item7;
			val2 = default(T7);
			if (val2 == null)
			{
				val2 = reference18;
				reference18 = ref val2;
				if (val2 == null)
				{
					value21 = 0;
					goto IL_06ba;
				}
			}
			value21 = reference18.GetHashCode();
			goto IL_06ba;
			IL_052b:
			reference19 = ref Item7;
			val2 = default(T7);
			if (val2 == null)
			{
				val2 = reference19;
				reference19 = ref val2;
				if (val2 == null)
				{
					value22 = 0;
					goto IL_0563;
				}
			}
			value22 = reference19.GetHashCode();
			goto IL_0563;
			IL_07d6:
			reference20 = ref Item6;
			val7 = default(T6);
			if (val7 == null)
			{
				val7 = reference20;
				reference20 = ref val7;
				if (val7 == null)
				{
					value23 = 0;
					goto IL_080e;
				}
			}
			value23 = reference20.GetHashCode();
			goto IL_080e;
			IL_040c:
			reference22 = ref Item7;
			val2 = default(T7);
			if (val2 == null)
			{
				val2 = reference22;
				reference22 = ref val2;
				if (val2 == null)
				{
					value8 = 0;
					goto IL_0444;
				}
			}
			value8 = reference22.GetHashCode();
			goto IL_0444;
			IL_035d:
			return HashCode.Combine(value10, value16, value3, hashCode);
			IL_080e:
			reference23 = ref Item7;
			val2 = default(T7);
			if (val2 == null)
			{
				val2 = reference23;
				reference23 = ref val2;
				if (val2 == null)
				{
					value25 = 0;
					goto IL_0846;
				}
			}
			value25 = reference23.GetHashCode();
			goto IL_0846;
			IL_06ba:
			return HashCode.Combine(value27, value28, value4, value17, value19, value21, hashCode);
			IL_0846:
			return HashCode.Combine(value2, value9, value14, value18, value20, value23, value25, hashCode);
			IL_0563:
			return HashCode.Combine(value26, value29, value11, value15, value22, hashCode);
			IL_0483:
			reference27 = ref Item4;
			val3 = default(T4);
			if (val3 == null)
			{
				val3 = reference27;
				reference27 = ref val3;
				if (val3 == null)
				{
					value29 = 0;
					goto IL_04bb;
				}
			}
			value29 = reference27.GetHashCode();
			goto IL_04bb;
			IL_0237:
			return HashCode.Combine(value24, hashCode);
			IL_05a2:
			reference28 = ref Item3;
			val6 = default(T3);
			if (val6 == null)
			{
				val6 = reference28;
				reference28 = ref val6;
				if (val6 == null)
				{
					value28 = 0;
					goto IL_05da;
				}
			}
			value28 = reference28.GetHashCode();
			goto IL_05da;
			IL_039c:
			reference29 = ref Item5;
			val5 = default(T5);
			if (val5 == null)
			{
				val5 = reference29;
				reference29 = ref val5;
				if (val5 == null)
				{
					value6 = 0;
					goto IL_03d4;
				}
			}
			value6 = reference29.GetHashCode();
			goto IL_03d4;
		}
		IL_00f2:
		ref T5 reference30 = ref Item5;
		val5 = default(T5);
		int value30;
		if (val5 == null)
		{
			val5 = reference30;
			reference30 = ref val5;
			if (val5 == null)
			{
				value30 = 0;
				goto IL_012a;
			}
		}
		value30 = reference30.GetHashCode();
		goto IL_012a;
		IL_019a:
		int value31;
		int value32;
		int value33;
		int value34;
		int value35;
		return HashCode.Combine(value, value31, value32, value33, value30, value34, value35);
		IL_0162:
		ref T7 reference31 = ref Item7;
		val2 = default(T7);
		if (val2 == null)
		{
			val2 = reference31;
			reference31 = ref val2;
			if (val2 == null)
			{
				value35 = 0;
				goto IL_019a;
			}
		}
		value35 = reference31.GetHashCode();
		goto IL_019a;
		IL_0082:
		ref T3 reference32 = ref Item3;
		val6 = default(T3);
		if (val6 == null)
		{
			val6 = reference32;
			reference32 = ref val6;
			if (val6 == null)
			{
				value32 = 0;
				goto IL_00ba;
			}
		}
		value32 = reference32.GetHashCode();
		goto IL_00ba;
		IL_012a:
		ref T6 reference33 = ref Item6;
		val7 = default(T6);
		if (val7 == null)
		{
			val7 = reference33;
			reference33 = ref val7;
			if (val7 == null)
			{
				value34 = 0;
				goto IL_0162;
			}
		}
		value34 = reference33.GetHashCode();
		goto IL_0162;
		IL_00ba:
		ref T4 reference34 = ref Item4;
		val3 = default(T4);
		if (val3 == null)
		{
			val3 = reference34;
			reference34 = ref val3;
			if (val3 == null)
			{
				value33 = 0;
				goto IL_00f2;
			}
		}
		value33 = reference34.GetHashCode();
		goto IL_00f2;
		IL_004a:
		ref T2 reference35 = ref Item2;
		val4 = default(T2);
		if (val4 == null)
		{
			val4 = reference35;
			reference35 = ref val4;
			if (val4 == null)
			{
				value31 = 0;
				goto IL_0082;
			}
		}
		value31 = reference35.GetHashCode();
		goto IL_0082;
	}

	int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
	{
		return GetHashCodeCore(comparer);
	}

	private int GetHashCodeCore(IEqualityComparer comparer)
	{
		if (!((object)Rest is IValueTupleInternal { Length: var length } valueTupleInternal))
		{
			return HashCode.Combine(comparer.GetHashCode(Item1), comparer.GetHashCode(Item2), comparer.GetHashCode(Item3), comparer.GetHashCode(Item4), comparer.GetHashCode(Item5), comparer.GetHashCode(Item6), comparer.GetHashCode(Item7));
		}
		int hashCode = valueTupleInternal.GetHashCode(comparer);
		if (length >= 8)
		{
			return hashCode;
		}
		switch (8 - length)
		{
		case 1:
			return HashCode.Combine(comparer.GetHashCode(Item7), hashCode);
		case 2:
			return HashCode.Combine(comparer.GetHashCode(Item6), comparer.GetHashCode(Item7), hashCode);
		case 3:
			return HashCode.Combine(comparer.GetHashCode(Item5), comparer.GetHashCode(Item6), comparer.GetHashCode(Item7), hashCode);
		case 4:
			return HashCode.Combine(comparer.GetHashCode(Item4), comparer.GetHashCode(Item5), comparer.GetHashCode(Item6), comparer.GetHashCode(Item7), hashCode);
		case 5:
			return HashCode.Combine(comparer.GetHashCode(Item3), comparer.GetHashCode(Item4), comparer.GetHashCode(Item5), comparer.GetHashCode(Item6), comparer.GetHashCode(Item7), hashCode);
		case 6:
			return HashCode.Combine(comparer.GetHashCode(Item2), comparer.GetHashCode(Item3), comparer.GetHashCode(Item4), comparer.GetHashCode(Item5), comparer.GetHashCode(Item6), comparer.GetHashCode(Item7), hashCode);
		case 7:
		case 8:
			return HashCode.Combine(comparer.GetHashCode(Item1), comparer.GetHashCode(Item2), comparer.GetHashCode(Item3), comparer.GetHashCode(Item4), comparer.GetHashCode(Item5), comparer.GetHashCode(Item6), comparer.GetHashCode(Item7), hashCode);
		default:
			return -1;
		}
	}

	int IValueTupleInternal.GetHashCode(IEqualityComparer comparer)
	{
		return GetHashCodeCore(comparer);
	}

	public override string ToString()
	{
		string[] obj;
		T1 val;
		object obj2;
		if (Rest is IValueTupleInternal)
		{
			obj = new string[16]
			{
				"(", null, null, null, null, null, null, null, null, null,
				null, null, null, null, null, null
			};
			ref T1 reference = ref Item1;
			val = default(T1);
			if (val == null)
			{
				val = reference;
				reference = ref val;
				if (val == null)
				{
					obj2 = null;
					goto IL_005b;
				}
			}
			obj2 = reference.ToString();
			goto IL_005b;
		}
		string[] obj3 = new string[17]
		{
			"(", null, null, null, null, null, null, null, null, null,
			null, null, null, null, null, null, null
		};
		ref T1 reference2 = ref Item1;
		val = default(T1);
		object obj4;
		if (val == null)
		{
			val = reference2;
			reference2 = ref val;
			if (val == null)
			{
				obj4 = null;
				goto IL_0258;
			}
		}
		obj4 = reference2.ToString();
		goto IL_0258;
		IL_015f:
		object obj5;
		obj[9] = (string)obj5;
		obj[10] = ", ";
		ref T6 reference3 = ref Item6;
		T6 val2 = default(T6);
		object obj6;
		if (val2 == null)
		{
			val2 = reference3;
			reference3 = ref val2;
			if (val2 == null)
			{
				obj6 = null;
				goto IL_01a4;
			}
		}
		obj6 = reference3.ToString();
		goto IL_01a4;
		IL_0318:
		object obj7;
		obj3[7] = (string)obj7;
		obj3[8] = ", ";
		ref T5 reference4 = ref Item5;
		T5 val3 = default(T5);
		object obj8;
		if (val3 == null)
		{
			val3 = reference4;
			reference4 = ref val3;
			if (val3 == null)
			{
				obj8 = null;
				goto IL_035c;
			}
		}
		obj8 = reference4.ToString();
		goto IL_035c;
		IL_009b:
		object obj9;
		obj[3] = (string)obj9;
		obj[4] = ", ";
		ref T3 reference5 = ref Item3;
		T3 val4 = default(T3);
		object obj10;
		if (val4 == null)
		{
			val4 = reference5;
			reference5 = ref val4;
			if (val4 == null)
			{
				obj10 = null;
				goto IL_00db;
			}
		}
		obj10 = reference5.ToString();
		goto IL_00db;
		IL_005b:
		obj[1] = (string)obj2;
		obj[2] = ", ";
		ref T2 reference6 = ref Item2;
		T2 val5 = default(T2);
		if (val5 == null)
		{
			val5 = reference6;
			reference6 = ref val5;
			if (val5 == null)
			{
				obj9 = null;
				goto IL_009b;
			}
		}
		obj9 = reference6.ToString();
		goto IL_009b;
		IL_0258:
		obj3[1] = (string)obj4;
		obj3[2] = ", ";
		ref T2 reference7 = ref Item2;
		val5 = default(T2);
		object obj11;
		if (val5 == null)
		{
			val5 = reference7;
			reference7 = ref val5;
			if (val5 == null)
			{
				obj11 = null;
				goto IL_0298;
			}
		}
		obj11 = reference7.ToString();
		goto IL_0298;
		IL_00db:
		obj[5] = (string)obj10;
		obj[6] = ", ";
		ref T4 reference8 = ref Item4;
		T4 val6 = default(T4);
		object obj12;
		if (val6 == null)
		{
			val6 = reference8;
			reference8 = ref val6;
			if (val6 == null)
			{
				obj12 = null;
				goto IL_011b;
			}
		}
		obj12 = reference8.ToString();
		goto IL_011b;
		IL_03a1:
		object obj13;
		obj3[11] = (string)obj13;
		obj3[12] = ", ";
		ref T7 reference9 = ref Item7;
		T7 val7 = default(T7);
		object obj14;
		if (val7 == null)
		{
			val7 = reference9;
			reference9 = ref val7;
			if (val7 == null)
			{
				obj14 = null;
				goto IL_03e6;
			}
		}
		obj14 = reference9.ToString();
		goto IL_03e6;
		IL_035c:
		obj3[9] = (string)obj8;
		obj3[10] = ", ";
		ref T6 reference10 = ref Item6;
		val2 = default(T6);
		if (val2 == null)
		{
			val2 = reference10;
			reference10 = ref val2;
			if (val2 == null)
			{
				obj13 = null;
				goto IL_03a1;
			}
		}
		obj13 = reference10.ToString();
		goto IL_03a1;
		IL_0298:
		obj3[3] = (string)obj11;
		obj3[4] = ", ";
		ref T3 reference11 = ref Item3;
		val4 = default(T3);
		object obj15;
		if (val4 == null)
		{
			val4 = reference11;
			reference11 = ref val4;
			if (val4 == null)
			{
				obj15 = null;
				goto IL_02d8;
			}
		}
		obj15 = reference11.ToString();
		goto IL_02d8;
		IL_01e9:
		object obj16;
		obj[13] = (string)obj16;
		obj[14] = ", ";
		obj[15] = ((IValueTupleInternal)(object)Rest).ToStringEnd();
		return string.Concat(obj);
		IL_01a4:
		obj[11] = (string)obj6;
		obj[12] = ", ";
		ref T7 reference12 = ref Item7;
		val7 = default(T7);
		if (val7 == null)
		{
			val7 = reference12;
			reference12 = ref val7;
			if (val7 == null)
			{
				obj16 = null;
				goto IL_01e9;
			}
		}
		obj16 = reference12.ToString();
		goto IL_01e9;
		IL_011b:
		obj[7] = (string)obj12;
		obj[8] = ", ";
		ref T5 reference13 = ref Item5;
		val3 = default(T5);
		if (val3 == null)
		{
			val3 = reference13;
			reference13 = ref val3;
			if (val3 == null)
			{
				obj5 = null;
				goto IL_015f;
			}
		}
		obj5 = reference13.ToString();
		goto IL_015f;
		IL_02d8:
		obj3[5] = (string)obj15;
		obj3[6] = ", ";
		ref T4 reference14 = ref Item4;
		val6 = default(T4);
		if (val6 == null)
		{
			val6 = reference14;
			reference14 = ref val6;
			if (val6 == null)
			{
				obj7 = null;
				goto IL_0318;
			}
		}
		obj7 = reference14.ToString();
		goto IL_0318;
		IL_03e6:
		obj3[13] = (string)obj14;
		obj3[14] = ", ";
		obj3[15] = Rest.ToString();
		obj3[16] = ")";
		return string.Concat(obj3);
	}

	string IValueTupleInternal.ToStringEnd()
	{
		string[] array;
		T1 val;
		object obj;
		if (Rest is IValueTupleInternal)
		{
			array = new string[15];
			ref T1 reference = ref Item1;
			val = default(T1);
			if (val == null)
			{
				val = reference;
				reference = ref val;
				if (val == null)
				{
					obj = null;
					goto IL_0053;
				}
			}
			obj = reference.ToString();
			goto IL_0053;
		}
		string[] array2 = new string[16];
		ref T1 reference2 = ref Item1;
		val = default(T1);
		object obj2;
		if (val == null)
		{
			val = reference2;
			reference2 = ref val;
			if (val == null)
			{
				obj2 = null;
				goto IL_0247;
			}
		}
		obj2 = reference2.ToString();
		goto IL_0247;
		IL_0156:
		object obj3;
		array[8] = (string)obj3;
		array[9] = ", ";
		ref T6 reference3 = ref Item6;
		T6 val2 = default(T6);
		object obj4;
		if (val2 == null)
		{
			val2 = reference3;
			reference3 = ref val2;
			if (val2 == null)
			{
				obj4 = null;
				goto IL_019b;
			}
		}
		obj4 = reference3.ToString();
		goto IL_019b;
		IL_0307:
		object obj5;
		array2[6] = (string)obj5;
		array2[7] = ", ";
		ref T5 reference4 = ref Item5;
		T5 val3 = default(T5);
		object obj6;
		if (val3 == null)
		{
			val3 = reference4;
			reference4 = ref val3;
			if (val3 == null)
			{
				obj6 = null;
				goto IL_034a;
			}
		}
		obj6 = reference4.ToString();
		goto IL_034a;
		IL_0093:
		object obj7;
		array[2] = (string)obj7;
		array[3] = ", ";
		ref T3 reference5 = ref Item3;
		T3 val4 = default(T3);
		object obj8;
		if (val4 == null)
		{
			val4 = reference5;
			reference5 = ref val4;
			if (val4 == null)
			{
				obj8 = null;
				goto IL_00d3;
			}
		}
		obj8 = reference5.ToString();
		goto IL_00d3;
		IL_0053:
		array[0] = (string)obj;
		array[1] = ", ";
		ref T2 reference6 = ref Item2;
		T2 val5 = default(T2);
		if (val5 == null)
		{
			val5 = reference6;
			reference6 = ref val5;
			if (val5 == null)
			{
				obj7 = null;
				goto IL_0093;
			}
		}
		obj7 = reference6.ToString();
		goto IL_0093;
		IL_0247:
		array2[0] = (string)obj2;
		array2[1] = ", ";
		ref T2 reference7 = ref Item2;
		val5 = default(T2);
		object obj9;
		if (val5 == null)
		{
			val5 = reference7;
			reference7 = ref val5;
			if (val5 == null)
			{
				obj9 = null;
				goto IL_0287;
			}
		}
		obj9 = reference7.ToString();
		goto IL_0287;
		IL_00d3:
		array[4] = (string)obj8;
		array[5] = ", ";
		ref T4 reference8 = ref Item4;
		T4 val6 = default(T4);
		object obj10;
		if (val6 == null)
		{
			val6 = reference8;
			reference8 = ref val6;
			if (val6 == null)
			{
				obj10 = null;
				goto IL_0113;
			}
		}
		obj10 = reference8.ToString();
		goto IL_0113;
		IL_038f:
		object obj11;
		array2[10] = (string)obj11;
		array2[11] = ", ";
		ref T7 reference9 = ref Item7;
		T7 val7 = default(T7);
		object obj12;
		if (val7 == null)
		{
			val7 = reference9;
			reference9 = ref val7;
			if (val7 == null)
			{
				obj12 = null;
				goto IL_03d4;
			}
		}
		obj12 = reference9.ToString();
		goto IL_03d4;
		IL_034a:
		array2[8] = (string)obj6;
		array2[9] = ", ";
		ref T6 reference10 = ref Item6;
		val2 = default(T6);
		if (val2 == null)
		{
			val2 = reference10;
			reference10 = ref val2;
			if (val2 == null)
			{
				obj11 = null;
				goto IL_038f;
			}
		}
		obj11 = reference10.ToString();
		goto IL_038f;
		IL_0287:
		array2[2] = (string)obj9;
		array2[3] = ", ";
		ref T3 reference11 = ref Item3;
		val4 = default(T3);
		object obj13;
		if (val4 == null)
		{
			val4 = reference11;
			reference11 = ref val4;
			if (val4 == null)
			{
				obj13 = null;
				goto IL_02c7;
			}
		}
		obj13 = reference11.ToString();
		goto IL_02c7;
		IL_01e0:
		object obj14;
		array[12] = (string)obj14;
		array[13] = ", ";
		array[14] = ((IValueTupleInternal)(object)Rest).ToStringEnd();
		return string.Concat(array);
		IL_019b:
		array[10] = (string)obj4;
		array[11] = ", ";
		ref T7 reference12 = ref Item7;
		val7 = default(T7);
		if (val7 == null)
		{
			val7 = reference12;
			reference12 = ref val7;
			if (val7 == null)
			{
				obj14 = null;
				goto IL_01e0;
			}
		}
		obj14 = reference12.ToString();
		goto IL_01e0;
		IL_0113:
		array[6] = (string)obj10;
		array[7] = ", ";
		ref T5 reference13 = ref Item5;
		val3 = default(T5);
		if (val3 == null)
		{
			val3 = reference13;
			reference13 = ref val3;
			if (val3 == null)
			{
				obj3 = null;
				goto IL_0156;
			}
		}
		obj3 = reference13.ToString();
		goto IL_0156;
		IL_02c7:
		array2[4] = (string)obj13;
		array2[5] = ", ";
		ref T4 reference14 = ref Item4;
		val6 = default(T4);
		if (val6 == null)
		{
			val6 = reference14;
			reference14 = ref val6;
			if (val6 == null)
			{
				obj5 = null;
				goto IL_0307;
			}
		}
		obj5 = reference14.ToString();
		goto IL_0307;
		IL_03d4:
		array2[12] = (string)obj12;
		array2[13] = ", ";
		array2[14] = Rest.ToString();
		array2[15] = ")";
		return string.Concat(array2);
	}
}
