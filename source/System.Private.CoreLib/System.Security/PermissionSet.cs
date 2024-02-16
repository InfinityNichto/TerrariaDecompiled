using System.Collections;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.Security;

[Obsolete("Code Access Security is not supported or honored by the runtime.", DiagnosticId = "SYSLIB0003", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
public class PermissionSet : ICollection, IEnumerable, IDeserializationCallback, ISecurityEncodable, IStackWalk
{
	public virtual int Count => 0;

	public virtual bool IsReadOnly => false;

	public virtual bool IsSynchronized => false;

	public virtual object SyncRoot => this;

	public PermissionSet(PermissionState state)
	{
	}

	public PermissionSet(PermissionSet? permSet)
	{
	}

	public IPermission? AddPermission(IPermission? perm)
	{
		return AddPermissionImpl(perm);
	}

	protected virtual IPermission? AddPermissionImpl(IPermission? perm)
	{
		return null;
	}

	public void Assert()
	{
	}

	public bool ContainsNonCodeAccessPermissions()
	{
		return false;
	}

	[Obsolete]
	public static byte[] ConvertPermissionSet(string inFormat, byte[] inData, string outFormat)
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_CAS);
	}

	public virtual PermissionSet Copy()
	{
		return new PermissionSet(this);
	}

	public virtual void CopyTo(Array array, int index)
	{
	}

	public void Demand()
	{
	}

	[Obsolete]
	public void Deny()
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_CAS);
	}

	public override bool Equals(object? o)
	{
		return base.Equals(o);
	}

	public virtual void FromXml(SecurityElement et)
	{
	}

	public IEnumerator GetEnumerator()
	{
		return GetEnumeratorImpl();
	}

	protected virtual IEnumerator GetEnumeratorImpl()
	{
		return Array.Empty<object>().GetEnumerator();
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public IPermission? GetPermission(Type? permClass)
	{
		return GetPermissionImpl(permClass);
	}

	protected virtual IPermission? GetPermissionImpl(Type? permClass)
	{
		return null;
	}

	public PermissionSet? Intersect(PermissionSet? other)
	{
		return null;
	}

	public bool IsEmpty()
	{
		return false;
	}

	public bool IsSubsetOf(PermissionSet? target)
	{
		return false;
	}

	public bool IsUnrestricted()
	{
		return false;
	}

	public void PermitOnly()
	{
		throw new PlatformNotSupportedException(SR.PlatformNotSupported_CAS);
	}

	public IPermission? RemovePermission(Type? permClass)
	{
		return RemovePermissionImpl(permClass);
	}

	protected virtual IPermission? RemovePermissionImpl(Type? permClass)
	{
		return null;
	}

	public static void RevertAssert()
	{
	}

	public IPermission? SetPermission(IPermission? perm)
	{
		return SetPermissionImpl(perm);
	}

	protected virtual IPermission? SetPermissionImpl(IPermission? perm)
	{
		return null;
	}

	void IDeserializationCallback.OnDeserialization(object sender)
	{
	}

	public override string ToString()
	{
		return base.ToString();
	}

	public virtual SecurityElement? ToXml()
	{
		return null;
	}

	public PermissionSet? Union(PermissionSet? other)
	{
		return null;
	}
}
