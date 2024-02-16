using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.Design.Serialization;

public abstract class MemberRelationshipService
{
	private struct RelationshipEntry
	{
		internal WeakReference _owner;

		internal MemberDescriptor _member;

		private readonly int _hashCode;

		internal RelationshipEntry(MemberRelationship rel)
		{
			_owner = new WeakReference(rel.Owner);
			_member = rel.Member;
			_hashCode = ((rel.Owner != null) ? rel.Owner.GetHashCode() : 0);
		}

		public override bool Equals([NotNullWhen(true)] object o)
		{
			return this == (RelationshipEntry)o;
		}

		public static bool operator ==(RelationshipEntry re1, RelationshipEntry re2)
		{
			object obj = (re1._owner.IsAlive ? re1._owner.Target : null);
			object obj2 = (re2._owner.IsAlive ? re2._owner.Target : null);
			if (obj == obj2)
			{
				return re1._member.Equals(re2._member);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return _hashCode;
		}
	}

	private readonly Dictionary<RelationshipEntry, RelationshipEntry> _relationships = new Dictionary<RelationshipEntry, RelationshipEntry>();

	public MemberRelationship this[MemberRelationship source]
	{
		get
		{
			if (source.Owner == null)
			{
				throw new ArgumentException(System.SR.Format(System.SR.InvalidNullArgument, "source.Owner"), "source");
			}
			return GetRelationship(source);
		}
		set
		{
			if (source.Owner == null)
			{
				throw new ArgumentException(System.SR.Format(System.SR.InvalidNullArgument, "source.Owner"), "source");
			}
			SetRelationship(source, value);
		}
	}

	public MemberRelationship this[object sourceOwner, MemberDescriptor sourceMember]
	{
		get
		{
			if (sourceOwner == null)
			{
				throw new ArgumentNullException("sourceOwner");
			}
			if (sourceMember == null)
			{
				throw new ArgumentNullException("sourceMember");
			}
			return GetRelationship(new MemberRelationship(sourceOwner, sourceMember));
		}
		set
		{
			if (sourceOwner == null)
			{
				throw new ArgumentNullException("sourceOwner");
			}
			if (sourceMember == null)
			{
				throw new ArgumentNullException("sourceMember");
			}
			SetRelationship(new MemberRelationship(sourceOwner, sourceMember), value);
		}
	}

	protected virtual MemberRelationship GetRelationship(MemberRelationship source)
	{
		if (_relationships.TryGetValue(new RelationshipEntry(source), out var value) && value._owner.IsAlive)
		{
			return new MemberRelationship(value._owner.Target, value._member);
		}
		return MemberRelationship.Empty;
	}

	protected virtual void SetRelationship(MemberRelationship source, MemberRelationship relationship)
	{
		if (!relationship.IsEmpty && !SupportsRelationship(source, relationship))
		{
			ThrowRelationshipNotSupported(source, relationship);
		}
		_relationships[new RelationshipEntry(source)] = new RelationshipEntry(relationship);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "GetComponentName is only used to create a nice exception message, and has a fallback when null is returned.")]
	private static void ThrowRelationshipNotSupported(MemberRelationship source, MemberRelationship relationship)
	{
		string text = TypeDescriptor.GetComponentName(source.Owner);
		string text2 = TypeDescriptor.GetComponentName(relationship.Owner);
		if (text == null)
		{
			text = source.Owner.ToString();
		}
		if (text2 == null)
		{
			text2 = relationship.Owner.ToString();
		}
		throw new ArgumentException(System.SR.Format(System.SR.MemberRelationshipService_RelationshipNotSupported, text, source.Member.Name, text2, relationship.Member.Name));
	}

	public abstract bool SupportsRelationship(MemberRelationship source, MemberRelationship relationship);
}
