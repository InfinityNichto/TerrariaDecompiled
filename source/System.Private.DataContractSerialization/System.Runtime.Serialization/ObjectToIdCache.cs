using System.Runtime.CompilerServices;

namespace System.Runtime.Serialization;

internal sealed class ObjectToIdCache
{
	internal int m_currentCount;

	internal int[] m_ids;

	internal object[] m_objs;

	internal bool[] m_isWrapped;

	internal static readonly int[] primes = new int[30]
	{
		3, 7, 17, 37, 89, 197, 431, 919, 1931, 4049,
		8419, 17519, 36353, 75431, 156437, 324449, 672827, 1395263, 2893249, 5999471,
		11998949, 23997907, 47995853, 95991737, 191983481, 383966977, 767933981, 1535867969, 2146435069, 2147483591
	};

	public ObjectToIdCache()
	{
		m_currentCount = 1;
		m_ids = new int[GetPrime(1)];
		m_objs = new object[m_ids.Length];
		m_isWrapped = new bool[m_ids.Length];
	}

	public int GetId(object obj, ref bool newId)
	{
		bool isEmpty;
		bool isWrapped;
		int num = FindElement(obj, out isEmpty, out isWrapped);
		if (!isEmpty)
		{
			newId = false;
			return m_ids[num];
		}
		if (!newId)
		{
			return -1;
		}
		int num2 = m_currentCount++;
		m_objs[num] = obj;
		m_ids[num] = num2;
		m_isWrapped[num] = isWrapped;
		if (m_currentCount >= m_objs.Length - 1)
		{
			Rehash();
		}
		return num2;
	}

	public int ReassignId(int oldObjId, object oldObj, object newObj)
	{
		int num = FindElement(oldObj, out var isEmpty, out var isWrapped);
		if (isEmpty)
		{
			return 0;
		}
		int num2 = m_ids[num];
		if (oldObjId > 0)
		{
			m_ids[num] = oldObjId;
		}
		else
		{
			RemoveAt(num);
		}
		num = FindElement(newObj, out isEmpty, out isWrapped);
		int result = 0;
		if (!isEmpty)
		{
			result = m_ids[num];
		}
		m_objs[num] = newObj;
		m_ids[num] = num2;
		m_isWrapped[num] = isWrapped;
		return result;
	}

	private int FindElement(object obj, out bool isEmpty, out bool isWrapped)
	{
		isWrapped = false;
		int num = ComputeStartPosition(obj);
		for (int i = num; i != num - 1; i++)
		{
			if (m_objs[i] == null)
			{
				isEmpty = true;
				return i;
			}
			if (m_objs[i] == obj)
			{
				isEmpty = false;
				return i;
			}
			if (i == m_objs.Length - 1)
			{
				isWrapped = true;
				i = -1;
			}
		}
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.ObjectTableOverflow));
	}

	private void RemoveAt(int position)
	{
		int num = m_objs.Length;
		int num2 = position;
		for (int i = ((position != num - 1) ? (position + 1) : 0); i != position; i++)
		{
			if (m_objs[i] == null)
			{
				m_objs[num2] = null;
				m_ids[num2] = 0;
				m_isWrapped[num2] = false;
				return;
			}
			int num3 = ComputeStartPosition(m_objs[i]);
			bool flag = i < position && !m_isWrapped[i];
			bool flag2 = num2 < position;
			if ((num3 <= num2 && (!flag || flag2)) || (flag2 && !flag))
			{
				m_objs[num2] = m_objs[i];
				m_ids[num2] = m_ids[i];
				m_isWrapped[num2] = m_isWrapped[i] && i > num2;
				num2 = i;
			}
			if (i == num - 1)
			{
				i = -1;
			}
		}
		throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(System.SR.ObjectTableOverflow));
	}

	private int ComputeStartPosition(object o)
	{
		return (RuntimeHelpers.GetHashCode(o) & 0x7FFFFFFF) % m_objs.Length;
	}

	private void Rehash()
	{
		int prime = GetPrime(m_objs.Length + 1);
		int[] ids = m_ids;
		object[] objs = m_objs;
		m_ids = new int[prime];
		m_objs = new object[prime];
		m_isWrapped = new bool[prime];
		for (int i = 0; i < objs.Length; i++)
		{
			object obj = objs[i];
			if (obj != null)
			{
				bool isEmpty;
				bool isWrapped;
				int num = FindElement(obj, out isEmpty, out isWrapped);
				m_objs[num] = obj;
				m_ids[num] = ids[i];
				m_isWrapped[num] = isWrapped;
			}
		}
	}

	private static int GetPrime(int min)
	{
		for (int i = 0; i < primes.Length; i++)
		{
			int num = primes[i];
			if (num >= min)
			{
				return num;
			}
		}
		return min;
	}
}
