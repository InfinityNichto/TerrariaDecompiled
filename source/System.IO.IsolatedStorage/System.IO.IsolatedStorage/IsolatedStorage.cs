using System.Runtime.CompilerServices;

namespace System.IO.IsolatedStorage;

public abstract class IsolatedStorage : MarshalByRefObject
{
	private ulong _quota;

	private bool _validQuota;

	private object _applicationIdentity;

	private object _assemblyIdentity;

	private object _domainIdentity;

	public object ApplicationIdentity
	{
		get
		{
			if (Helper.IsApplication(Scope))
			{
				return _applicationIdentity;
			}
			throw new InvalidOperationException(System.SR.IsolatedStorage_ApplicationUndefined);
		}
	}

	public object AssemblyIdentity
	{
		get
		{
			if (Helper.IsAssembly(Scope))
			{
				return _assemblyIdentity;
			}
			throw new InvalidOperationException(System.SR.IsolatedStorage_AssemblyUndefined);
		}
	}

	public object DomainIdentity
	{
		get
		{
			if (Helper.IsDomain(Scope))
			{
				return _domainIdentity;
			}
			throw new InvalidOperationException(System.SR.IsolatedStorage_AssemblyUndefined);
		}
	}

	[CLSCompliant(false)]
	[Obsolete("IsolatedStorage.CurrentSize has been deprecated because it is not CLS Compliant. To get the current size use IsolatedStorage.UsedSize instead.")]
	public virtual ulong CurrentSize
	{
		get
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.IsolatedStorage_CurrentSizeUndefined, "CurrentSize"));
		}
	}

	public virtual long UsedSize
	{
		get
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.IsolatedStorage_QuotaIsUndefined, "UsedSize"));
		}
	}

	public virtual long AvailableFreeSpace
	{
		get
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.IsolatedStorage_QuotaIsUndefined, "AvailableFreeSpace"));
		}
	}

	[CLSCompliant(false)]
	[Obsolete("IsolatedStorage.MaximumSize has been deprecated because it is not CLS Compliant. To get the maximum size use IsolatedStorage.Quota instead.")]
	public virtual ulong MaximumSize
	{
		get
		{
			if (_validQuota)
			{
				return _quota;
			}
			throw new InvalidOperationException(System.SR.Format(System.SR.IsolatedStorage_QuotaIsUndefined, "MaximumSize"));
		}
	}

	public virtual long Quota
	{
		get
		{
			if (_validQuota)
			{
				return (long)_quota;
			}
			throw new InvalidOperationException(System.SR.Format(System.SR.IsolatedStorage_QuotaIsUndefined, "Quota"));
		}
	}

	public IsolatedStorageScope Scope { get; private set; }

	protected virtual char SeparatorExternal => Path.DirectorySeparatorChar;

	protected virtual char SeparatorInternal => '.';

	internal string? IdentityHash { get; private set; }

	public virtual bool IncreaseQuotaTo(long newQuotaSize)
	{
		return false;
	}

	public abstract void Remove();

	protected void InitStore(IsolatedStorageScope scope, Type appEvidenceType)
	{
		InitStore(scope, null, appEvidenceType);
	}

	protected void InitStore(IsolatedStorageScope scope, Type? domainEvidenceType, Type? assemblyEvidenceType)
	{
		VerifyScope(scope);
		Scope = scope;
		Helper.GetDefaultIdentityAndHash(out var identity, out var hash, SeparatorInternal);
		if (Helper.IsApplication(scope))
		{
			_applicationIdentity = identity;
		}
		else
		{
			if (Helper.IsDomain(scope))
			{
				_domainIdentity = identity;
				IFormatProvider formatProvider = null;
				IFormatProvider provider = formatProvider;
				Span<char> initialBuffer = stackalloc char[128];
				DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(0, 3, formatProvider, initialBuffer);
				handler.AppendFormatted(hash);
				handler.AppendFormatted(SeparatorExternal);
				handler.AppendFormatted(hash);
				hash = string.Create(provider, initialBuffer, ref handler);
			}
			_assemblyIdentity = identity;
		}
		IdentityHash = hash;
	}

	private static void VerifyScope(IsolatedStorageScope scope)
	{
		switch (scope)
		{
		case IsolatedStorageScope.User | IsolatedStorageScope.Assembly:
		case IsolatedStorageScope.User | IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly:
		case IsolatedStorageScope.User | IsolatedStorageScope.Assembly | IsolatedStorageScope.Roaming:
		case IsolatedStorageScope.User | IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly | IsolatedStorageScope.Roaming:
		case IsolatedStorageScope.Assembly | IsolatedStorageScope.Machine:
		case IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly | IsolatedStorageScope.Machine:
		case IsolatedStorageScope.User | IsolatedStorageScope.Application:
		case IsolatedStorageScope.User | IsolatedStorageScope.Roaming | IsolatedStorageScope.Application:
		case IsolatedStorageScope.Machine | IsolatedStorageScope.Application:
			return;
		}
		throw new ArgumentException(System.SR.IsolatedStorage_Scope_Invalid);
	}
}
