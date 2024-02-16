using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Principal;

namespace System.Security.Claims;

public class ClaimsIdentity : IIdentity
{
	private enum SerializationMask
	{
		None = 0,
		AuthenticationType = 1,
		BootstrapConext = 2,
		NameClaimType = 4,
		RoleClaimType = 8,
		HasClaims = 0x10,
		HasLabel = 0x20,
		Actor = 0x40,
		UserData = 0x80
	}

	private byte[] _userSerializationData;

	private ClaimsIdentity _actor;

	private string _authenticationType;

	private object _bootstrapContext;

	private List<List<Claim>> _externalClaims;

	private string _label;

	private readonly List<Claim> _instanceClaims = new List<Claim>();

	private string _nameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";

	private string _roleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

	public const string DefaultIssuer = "LOCAL AUTHORITY";

	public const string DefaultNameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";

	public const string DefaultRoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

	public virtual string? AuthenticationType => _authenticationType;

	public virtual bool IsAuthenticated => !string.IsNullOrEmpty(_authenticationType);

	public ClaimsIdentity? Actor
	{
		get
		{
			return _actor;
		}
		set
		{
			if (value != null && IsCircular(value))
			{
				throw new InvalidOperationException(System.SR.InvalidOperationException_ActorGraphCircular);
			}
			_actor = value;
		}
	}

	public object? BootstrapContext
	{
		get
		{
			return _bootstrapContext;
		}
		set
		{
			_bootstrapContext = value;
		}
	}

	public virtual IEnumerable<Claim> Claims
	{
		get
		{
			if (_externalClaims == null)
			{
				return _instanceClaims;
			}
			return CombinedClaimsIterator();
		}
	}

	protected virtual byte[]? CustomSerializationData => _userSerializationData;

	internal List<List<Claim>> ExternalClaims
	{
		get
		{
			if (_externalClaims == null)
			{
				_externalClaims = new List<List<Claim>>();
			}
			return _externalClaims;
		}
	}

	public string? Label
	{
		get
		{
			return _label;
		}
		set
		{
			_label = value;
		}
	}

	public virtual string? Name => FindFirst(_nameClaimType)?.Value;

	public string NameClaimType => _nameClaimType;

	public string RoleClaimType => _roleClaimType;

	public ClaimsIdentity()
		: this(null, null, null, null, null)
	{
	}

	public ClaimsIdentity(IIdentity? identity)
		: this(identity, null, null, null, null)
	{
	}

	public ClaimsIdentity(IEnumerable<Claim>? claims)
		: this(null, claims, null, null, null)
	{
	}

	public ClaimsIdentity(string? authenticationType)
		: this(null, null, authenticationType, null, null)
	{
	}

	public ClaimsIdentity(IEnumerable<Claim>? claims, string? authenticationType)
		: this(null, claims, authenticationType, null, null)
	{
	}

	public ClaimsIdentity(IIdentity? identity, IEnumerable<Claim>? claims)
		: this(identity, claims, null, null, null)
	{
	}

	public ClaimsIdentity(string? authenticationType, string? nameType, string? roleType)
		: this(null, null, authenticationType, nameType, roleType)
	{
	}

	public ClaimsIdentity(IEnumerable<Claim>? claims, string? authenticationType, string? nameType, string? roleType)
		: this(null, claims, authenticationType, nameType, roleType)
	{
	}

	public ClaimsIdentity(IIdentity? identity, IEnumerable<Claim>? claims, string? authenticationType, string? nameType, string? roleType)
	{
		ClaimsIdentity claimsIdentity = identity as ClaimsIdentity;
		_authenticationType = ((identity != null && string.IsNullOrEmpty(authenticationType)) ? identity.AuthenticationType : authenticationType);
		_nameClaimType = ((!string.IsNullOrEmpty(nameType)) ? nameType : ((claimsIdentity != null) ? claimsIdentity._nameClaimType : "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"));
		_roleClaimType = ((!string.IsNullOrEmpty(roleType)) ? roleType : ((claimsIdentity != null) ? claimsIdentity._roleClaimType : "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"));
		if (claimsIdentity != null)
		{
			_label = claimsIdentity._label;
			_bootstrapContext = claimsIdentity._bootstrapContext;
			if (claimsIdentity.Actor != null)
			{
				if (IsCircular(claimsIdentity.Actor))
				{
					throw new InvalidOperationException(System.SR.InvalidOperationException_ActorGraphCircular);
				}
				_actor = claimsIdentity.Actor;
			}
			SafeAddClaims(claimsIdentity._instanceClaims);
		}
		else if (identity != null && !string.IsNullOrEmpty(identity.Name))
		{
			SafeAddClaim(new Claim(_nameClaimType, identity.Name, "http://www.w3.org/2001/XMLSchema#string", "LOCAL AUTHORITY", "LOCAL AUTHORITY", this));
		}
		if (claims != null)
		{
			SafeAddClaims(claims);
		}
	}

	public ClaimsIdentity(BinaryReader reader)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		Initialize(reader);
	}

	protected ClaimsIdentity(ClaimsIdentity other)
	{
		if (other == null)
		{
			throw new ArgumentNullException("other");
		}
		if (other._actor != null)
		{
			_actor = other._actor.Clone();
		}
		_authenticationType = other._authenticationType;
		_bootstrapContext = other._bootstrapContext;
		_label = other._label;
		_nameClaimType = other._nameClaimType;
		_roleClaimType = other._roleClaimType;
		if (other._userSerializationData != null)
		{
			_userSerializationData = other._userSerializationData.Clone() as byte[];
		}
		SafeAddClaims(other._instanceClaims);
	}

	protected ClaimsIdentity(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}

	protected ClaimsIdentity(SerializationInfo info)
	{
		throw new PlatformNotSupportedException();
	}

	private IEnumerable<Claim> CombinedClaimsIterator()
	{
		for (int j = 0; j < _instanceClaims.Count; j++)
		{
			yield return _instanceClaims[j];
		}
		for (int j = 0; j < _externalClaims.Count; j++)
		{
			if (_externalClaims[j] == null)
			{
				continue;
			}
			foreach (Claim item in _externalClaims[j])
			{
				yield return item;
			}
		}
	}

	public virtual ClaimsIdentity Clone()
	{
		return new ClaimsIdentity(this);
	}

	public virtual void AddClaim(Claim claim)
	{
		if (claim == null)
		{
			throw new ArgumentNullException("claim");
		}
		if (claim.Subject == this)
		{
			_instanceClaims.Add(claim);
		}
		else
		{
			_instanceClaims.Add(claim.Clone(this));
		}
	}

	public virtual void AddClaims(IEnumerable<Claim?> claims)
	{
		if (claims == null)
		{
			throw new ArgumentNullException("claims");
		}
		foreach (Claim claim in claims)
		{
			if (claim != null)
			{
				if (claim.Subject == this)
				{
					_instanceClaims.Add(claim);
				}
				else
				{
					_instanceClaims.Add(claim.Clone(this));
				}
			}
		}
	}

	public virtual bool TryRemoveClaim(Claim? claim)
	{
		if (claim == null)
		{
			return false;
		}
		bool result = false;
		for (int i = 0; i < _instanceClaims.Count; i++)
		{
			if (_instanceClaims[i] == claim)
			{
				_instanceClaims.RemoveAt(i);
				result = true;
				break;
			}
		}
		return result;
	}

	public virtual void RemoveClaim(Claim? claim)
	{
		if (!TryRemoveClaim(claim))
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.InvalidOperation_ClaimCannotBeRemoved, claim));
		}
	}

	private void SafeAddClaims(IEnumerable<Claim> claims)
	{
		foreach (Claim claim in claims)
		{
			if (claim != null)
			{
				if (claim.Subject == this)
				{
					_instanceClaims.Add(claim);
				}
				else
				{
					_instanceClaims.Add(claim.Clone(this));
				}
			}
		}
	}

	private void SafeAddClaim(Claim claim)
	{
		if (claim != null)
		{
			if (claim.Subject == this)
			{
				_instanceClaims.Add(claim);
			}
			else
			{
				_instanceClaims.Add(claim.Clone(this));
			}
		}
	}

	public virtual IEnumerable<Claim> FindAll(Predicate<Claim> match)
	{
		if (match == null)
		{
			throw new ArgumentNullException("match");
		}
		foreach (Claim claim in Claims)
		{
			if (match(claim))
			{
				yield return claim;
			}
		}
	}

	public virtual IEnumerable<Claim> FindAll(string type)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		foreach (Claim claim in Claims)
		{
			if (claim != null && string.Equals(claim.Type, type, StringComparison.OrdinalIgnoreCase))
			{
				yield return claim;
			}
		}
	}

	public virtual Claim? FindFirst(Predicate<Claim> match)
	{
		if (match == null)
		{
			throw new ArgumentNullException("match");
		}
		foreach (Claim claim in Claims)
		{
			if (match(claim))
			{
				return claim;
			}
		}
		return null;
	}

	public virtual Claim? FindFirst(string type)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		foreach (Claim claim in Claims)
		{
			if (claim != null && string.Equals(claim.Type, type, StringComparison.OrdinalIgnoreCase))
			{
				return claim;
			}
		}
		return null;
	}

	public virtual bool HasClaim(Predicate<Claim> match)
	{
		if (match == null)
		{
			throw new ArgumentNullException("match");
		}
		foreach (Claim claim in Claims)
		{
			if (match(claim))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool HasClaim(string type, string value)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		foreach (Claim claim in Claims)
		{
			if (claim != null && string.Equals(claim.Type, type, StringComparison.OrdinalIgnoreCase) && string.Equals(claim.Value, value, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	private void Initialize(BinaryReader reader)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		SerializationMask serializationMask = (SerializationMask)reader.ReadInt32();
		int num = 0;
		int num2 = reader.ReadInt32();
		if ((serializationMask & SerializationMask.AuthenticationType) == SerializationMask.AuthenticationType)
		{
			_authenticationType = reader.ReadString();
			num++;
		}
		if ((serializationMask & SerializationMask.BootstrapConext) == SerializationMask.BootstrapConext)
		{
			_bootstrapContext = reader.ReadString();
			num++;
		}
		if ((serializationMask & SerializationMask.NameClaimType) == SerializationMask.NameClaimType)
		{
			_nameClaimType = reader.ReadString();
			num++;
		}
		else
		{
			_nameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
		}
		if ((serializationMask & SerializationMask.RoleClaimType) == SerializationMask.RoleClaimType)
		{
			_roleClaimType = reader.ReadString();
			num++;
		}
		else
		{
			_roleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
		}
		if ((serializationMask & SerializationMask.HasLabel) == SerializationMask.HasLabel)
		{
			_label = reader.ReadString();
			num++;
		}
		if ((serializationMask & SerializationMask.HasClaims) == SerializationMask.HasClaims)
		{
			int num3 = reader.ReadInt32();
			for (int i = 0; i < num3; i++)
			{
				_instanceClaims.Add(CreateClaim(reader));
			}
			num++;
		}
		if ((serializationMask & SerializationMask.Actor) == SerializationMask.Actor)
		{
			_actor = new ClaimsIdentity(reader);
			num++;
		}
		if ((serializationMask & SerializationMask.UserData) == SerializationMask.UserData)
		{
			int count = reader.ReadInt32();
			_userSerializationData = reader.ReadBytes(count);
			num++;
		}
		for (int j = num; j < num2; j++)
		{
			reader.ReadString();
		}
	}

	protected virtual Claim CreateClaim(BinaryReader reader)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		return new Claim(reader, this);
	}

	public virtual void WriteTo(BinaryWriter writer)
	{
		WriteTo(writer, null);
	}

	protected virtual void WriteTo(BinaryWriter writer, byte[]? userData)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		int num = 0;
		SerializationMask serializationMask = SerializationMask.None;
		if (_authenticationType != null)
		{
			serializationMask |= SerializationMask.AuthenticationType;
			num++;
		}
		if (_bootstrapContext != null && _bootstrapContext is string)
		{
			serializationMask |= SerializationMask.BootstrapConext;
			num++;
		}
		if (!string.Equals(_nameClaimType, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", StringComparison.Ordinal))
		{
			serializationMask |= SerializationMask.NameClaimType;
			num++;
		}
		if (!string.Equals(_roleClaimType, "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", StringComparison.Ordinal))
		{
			serializationMask |= SerializationMask.RoleClaimType;
			num++;
		}
		if (!string.IsNullOrWhiteSpace(_label))
		{
			serializationMask |= SerializationMask.HasLabel;
			num++;
		}
		if (_instanceClaims.Count > 0)
		{
			serializationMask |= SerializationMask.HasClaims;
			num++;
		}
		if (_actor != null)
		{
			serializationMask |= SerializationMask.Actor;
			num++;
		}
		if (userData != null && userData.Length != 0)
		{
			num++;
			serializationMask |= SerializationMask.UserData;
		}
		writer.Write((int)serializationMask);
		writer.Write(num);
		if ((serializationMask & SerializationMask.AuthenticationType) == SerializationMask.AuthenticationType)
		{
			writer.Write(_authenticationType);
		}
		if ((serializationMask & SerializationMask.BootstrapConext) == SerializationMask.BootstrapConext)
		{
			writer.Write((string)_bootstrapContext);
		}
		if ((serializationMask & SerializationMask.NameClaimType) == SerializationMask.NameClaimType)
		{
			writer.Write(_nameClaimType);
		}
		if ((serializationMask & SerializationMask.RoleClaimType) == SerializationMask.RoleClaimType)
		{
			writer.Write(_roleClaimType);
		}
		if ((serializationMask & SerializationMask.HasLabel) == SerializationMask.HasLabel)
		{
			writer.Write(_label);
		}
		if ((serializationMask & SerializationMask.HasClaims) == SerializationMask.HasClaims)
		{
			writer.Write(_instanceClaims.Count);
			foreach (Claim instanceClaim in _instanceClaims)
			{
				instanceClaim.WriteTo(writer);
			}
		}
		if ((serializationMask & SerializationMask.Actor) == SerializationMask.Actor)
		{
			_actor.WriteTo(writer);
		}
		if ((serializationMask & SerializationMask.UserData) == SerializationMask.UserData)
		{
			writer.Write(userData.Length);
			writer.Write(userData);
		}
		writer.Flush();
	}

	private bool IsCircular(ClaimsIdentity subject)
	{
		if (this == subject)
		{
			return true;
		}
		ClaimsIdentity claimsIdentity = subject;
		while (claimsIdentity.Actor != null)
		{
			if (this == claimsIdentity.Actor)
			{
				return true;
			}
			claimsIdentity = claimsIdentity.Actor;
		}
		return false;
	}

	protected virtual void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		throw new PlatformNotSupportedException();
	}
}
