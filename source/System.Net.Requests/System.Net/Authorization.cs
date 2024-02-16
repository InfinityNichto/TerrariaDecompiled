namespace System.Net;

public class Authorization
{
	private string[] _protectionRealm;

	private bool _mutualAuth;

	public string? Message { get; }

	public string? ConnectionGroupId { get; }

	public bool Complete { get; internal set; }

	public string[]? ProtectionRealm
	{
		get
		{
			return _protectionRealm;
		}
		set
		{
			_protectionRealm = ((value != null && value.Length != 0) ? value : null);
		}
	}

	public bool MutuallyAuthenticated
	{
		get
		{
			if (Complete)
			{
				return _mutualAuth;
			}
			return false;
		}
		set
		{
			_mutualAuth = value;
		}
	}

	public Authorization(string? token)
		: this(token, finished: true)
	{
	}

	public Authorization(string? token, bool finished)
		: this(token, finished, null)
	{
	}

	public Authorization(string? token, bool finished, string? connectionGroupId)
		: this(token, finished, connectionGroupId, mutualAuth: false)
	{
	}

	internal Authorization(string token, bool finished, string connectionGroupId, bool mutualAuth)
	{
		Message = (string.IsNullOrEmpty(token) ? null : token);
		ConnectionGroupId = (string.IsNullOrEmpty(connectionGroupId) ? null : connectionGroupId);
		Complete = finished;
		_mutualAuth = mutualAuth;
	}
}
