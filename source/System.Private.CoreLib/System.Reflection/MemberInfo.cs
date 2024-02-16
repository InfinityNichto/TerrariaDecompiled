using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Reflection;

public abstract class MemberInfo : ICustomAttributeProvider
{
	public abstract MemberTypes MemberType { get; }

	public abstract string Name { get; }

	public abstract Type? DeclaringType { get; }

	public abstract Type? ReflectedType { get; }

	public virtual Module Module
	{
		get
		{
			if (this is Type type)
			{
				return type.Module;
			}
			throw NotImplemented.ByDesign;
		}
	}

	public virtual IEnumerable<CustomAttributeData> CustomAttributes => GetCustomAttributesData();

	public virtual bool IsCollectible => true;

	public virtual int MetadataToken
	{
		get
		{
			throw new InvalidOperationException();
		}
	}

	internal virtual bool CacheEquals(object o)
	{
		throw new NotImplementedException();
	}

	internal bool HasSameMetadataDefinitionAsCore<TOther>(MemberInfo other) where TOther : MemberInfo
	{
		if ((object)other == null)
		{
			throw new ArgumentNullException("other");
		}
		if (!(other is TOther))
		{
			return false;
		}
		if (MetadataToken != other.MetadataToken)
		{
			return false;
		}
		if (!Module.Equals(other.Module))
		{
			return false;
		}
		return true;
	}

	public virtual bool HasSameMetadataDefinitionAs(MemberInfo other)
	{
		throw NotImplemented.ByDesign;
	}

	public abstract bool IsDefined(Type attributeType, bool inherit);

	public abstract object[] GetCustomAttributes(bool inherit);

	public abstract object[] GetCustomAttributes(Type attributeType, bool inherit);

	public virtual IList<CustomAttributeData> GetCustomAttributesData()
	{
		throw NotImplemented.ByDesign;
	}

	public override bool Equals(object? obj)
	{
		return base.Equals(obj);
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool operator ==(MemberInfo? left, MemberInfo? right)
	{
		if ((object)right == null)
		{
			if ((object)left != null)
			{
				return false;
			}
			return true;
		}
		if ((object)left == right)
		{
			return true;
		}
		return left?.Equals(right) ?? false;
	}

	public static bool operator !=(MemberInfo? left, MemberInfo? right)
	{
		return !(left == right);
	}
}
