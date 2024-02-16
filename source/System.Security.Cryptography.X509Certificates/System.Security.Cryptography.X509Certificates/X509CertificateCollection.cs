using System.Collections;

namespace System.Security.Cryptography.X509Certificates;

public class X509CertificateCollection : CollectionBase
{
	public class X509CertificateEnumerator : IEnumerator
	{
		private readonly IEnumerator _enumerator;

		public X509Certificate Current => (X509Certificate)_enumerator.Current;

		object IEnumerator.Current => Current;

		public X509CertificateEnumerator(X509CertificateCollection mappings)
		{
			if (mappings == null)
			{
				throw new ArgumentNullException("mappings");
			}
			_enumerator = ((IEnumerable)mappings).GetEnumerator();
		}

		public bool MoveNext()
		{
			return _enumerator.MoveNext();
		}

		bool IEnumerator.MoveNext()
		{
			return MoveNext();
		}

		public void Reset()
		{
			_enumerator.Reset();
		}

		void IEnumerator.Reset()
		{
			Reset();
		}
	}

	public X509Certificate this[int index]
	{
		get
		{
			return (X509Certificate)base.List[index];
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			base.List[index] = value;
		}
	}

	public X509CertificateCollection()
	{
	}

	public X509CertificateCollection(X509Certificate[] value)
	{
		AddRange(value);
	}

	public X509CertificateCollection(X509CertificateCollection value)
	{
		AddRange(value);
	}

	public int Add(X509Certificate value)
	{
		return base.List.Add(value);
	}

	public void AddRange(X509Certificate[] value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		for (int i = 0; i < value.Length; i++)
		{
			Add(value[i]);
		}
	}

	public void AddRange(X509CertificateCollection value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		for (int i = 0; i < value.Count; i++)
		{
			Add(value[i]);
		}
	}

	public bool Contains(X509Certificate value)
	{
		return base.List.Contains(value);
	}

	public void CopyTo(X509Certificate[] array, int index)
	{
		base.List.CopyTo(array, index);
	}

	public new X509CertificateEnumerator GetEnumerator()
	{
		return new X509CertificateEnumerator(this);
	}

	public override int GetHashCode()
	{
		int num = 0;
		foreach (X509Certificate item in base.List)
		{
			num += item.GetHashCode();
		}
		return num;
	}

	public int IndexOf(X509Certificate value)
	{
		return base.List.IndexOf(value);
	}

	public void Insert(int index, X509Certificate value)
	{
		base.List.Insert(index, value);
	}

	public void Remove(X509Certificate value)
	{
		base.List.Remove(value);
	}

	protected override void OnValidate(object value)
	{
		base.OnValidate(value);
		if (!(value is X509Certificate))
		{
			throw new ArgumentException(System.SR.Arg_InvalidType, "value");
		}
	}
}
