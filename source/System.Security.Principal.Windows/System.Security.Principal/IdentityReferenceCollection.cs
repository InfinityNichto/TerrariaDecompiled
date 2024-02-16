using System.Collections;
using System.Collections.Generic;

namespace System.Security.Principal;

public class IdentityReferenceCollection : ICollection<IdentityReference>, IEnumerable<IdentityReference>, IEnumerable
{
	private readonly List<IdentityReference> _identities;

	public int Count => _identities.Count;

	bool ICollection<IdentityReference>.IsReadOnly => false;

	public IdentityReference this[int index]
	{
		get
		{
			return _identities[index];
		}
		set
		{
			if ((object)value == null)
			{
				throw new ArgumentNullException("value");
			}
			_identities[index] = value;
		}
	}

	internal List<IdentityReference> Identities => _identities;

	public IdentityReferenceCollection()
		: this(0)
	{
	}

	public IdentityReferenceCollection(int capacity)
	{
		_identities = new List<IdentityReference>(capacity);
	}

	public void CopyTo(IdentityReference[] array, int offset)
	{
		_identities.CopyTo(0, array, offset, Count);
	}

	public void Add(IdentityReference identity)
	{
		if (identity == null)
		{
			throw new ArgumentNullException("identity");
		}
		_identities.Add(identity);
	}

	public bool Remove(IdentityReference identity)
	{
		if (identity == null)
		{
			throw new ArgumentNullException("identity");
		}
		if (Contains(identity))
		{
			return _identities.Remove(identity);
		}
		return false;
	}

	public void Clear()
	{
		_identities.Clear();
	}

	public bool Contains(IdentityReference identity)
	{
		if (identity == null)
		{
			throw new ArgumentNullException("identity");
		}
		return _identities.Contains(identity);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public IEnumerator<IdentityReference> GetEnumerator()
	{
		return new IdentityReferenceEnumerator(this);
	}

	public IdentityReferenceCollection Translate(Type targetType)
	{
		return Translate(targetType, forceSuccess: false);
	}

	public IdentityReferenceCollection Translate(Type targetType, bool forceSuccess)
	{
		if (targetType == null)
		{
			throw new ArgumentNullException("targetType");
		}
		if (!targetType.IsSubclassOf(typeof(IdentityReference)))
		{
			throw new ArgumentException(System.SR.IdentityReference_MustBeIdentityReference, "targetType");
		}
		if (Identities.Count == 0)
		{
			return new IdentityReferenceCollection();
		}
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < Identities.Count; i++)
		{
			Type type = Identities[i].GetType();
			if (type == targetType)
			{
				continue;
			}
			if (type == typeof(SecurityIdentifier))
			{
				num++;
				continue;
			}
			if (type == typeof(NTAccount))
			{
				num2++;
				continue;
			}
			throw new NotSupportedException();
		}
		bool flag = false;
		IdentityReferenceCollection identityReferenceCollection = null;
		IdentityReferenceCollection identityReferenceCollection2 = null;
		if (num == Count)
		{
			flag = true;
			identityReferenceCollection = this;
		}
		else if (num > 0)
		{
			identityReferenceCollection = new IdentityReferenceCollection(num);
		}
		if (num2 == Count)
		{
			flag = true;
			identityReferenceCollection2 = this;
		}
		else if (num2 > 0)
		{
			identityReferenceCollection2 = new IdentityReferenceCollection(num2);
		}
		IdentityReferenceCollection identityReferenceCollection3 = null;
		if (!flag)
		{
			identityReferenceCollection3 = new IdentityReferenceCollection(Identities.Count);
			for (int j = 0; j < Identities.Count; j++)
			{
				IdentityReference identityReference = this[j];
				Type type2 = identityReference.GetType();
				if (type2 == targetType)
				{
					continue;
				}
				if (type2 == typeof(SecurityIdentifier))
				{
					identityReferenceCollection.Add(identityReference);
					continue;
				}
				if (type2 == typeof(NTAccount))
				{
					identityReferenceCollection2.Add(identityReference);
					continue;
				}
				throw new NotSupportedException();
			}
		}
		bool someFailed = false;
		IdentityReferenceCollection identityReferenceCollection4 = null;
		IdentityReferenceCollection identityReferenceCollection5 = null;
		if (num > 0)
		{
			identityReferenceCollection4 = SecurityIdentifier.Translate(identityReferenceCollection, targetType, out someFailed);
			if (flag && !(forceSuccess && someFailed))
			{
				identityReferenceCollection3 = identityReferenceCollection4;
			}
		}
		if (num2 > 0)
		{
			identityReferenceCollection5 = NTAccount.Translate(identityReferenceCollection2, targetType, out someFailed);
			if (flag && !(forceSuccess && someFailed))
			{
				identityReferenceCollection3 = identityReferenceCollection5;
			}
		}
		if (forceSuccess && someFailed)
		{
			identityReferenceCollection3 = new IdentityReferenceCollection();
			if (identityReferenceCollection4 != null)
			{
				foreach (IdentityReference item in identityReferenceCollection4)
				{
					if (item.GetType() != targetType)
					{
						identityReferenceCollection3.Add(item);
					}
				}
			}
			if (identityReferenceCollection5 != null)
			{
				foreach (IdentityReference item2 in identityReferenceCollection5)
				{
					if (item2.GetType() != targetType)
					{
						identityReferenceCollection3.Add(item2);
					}
				}
			}
			throw new IdentityNotMappedException(System.SR.IdentityReference_IdentityNotMapped, identityReferenceCollection3);
		}
		if (!flag)
		{
			num = 0;
			num2 = 0;
			identityReferenceCollection3 = new IdentityReferenceCollection(Identities.Count);
			for (int k = 0; k < Identities.Count; k++)
			{
				IdentityReference identityReference2 = this[k];
				Type type3 = identityReference2.GetType();
				if (type3 == targetType)
				{
					identityReferenceCollection3.Add(identityReference2);
					continue;
				}
				if (type3 == typeof(SecurityIdentifier))
				{
					identityReferenceCollection3.Add(identityReferenceCollection4[num++]);
					continue;
				}
				if (type3 == typeof(NTAccount))
				{
					identityReferenceCollection3.Add(identityReferenceCollection5[num2++]);
					continue;
				}
				throw new NotSupportedException();
			}
		}
		return identityReferenceCollection3;
	}
}
