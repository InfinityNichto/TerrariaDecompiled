using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System;

public class UriBuilder
{
	private string _scheme = "http";

	private string _username = string.Empty;

	private string _password = string.Empty;

	private string _host = "localhost";

	private int _port = -1;

	private string _path = "/";

	private string _query = string.Empty;

	private string _fragment = string.Empty;

	private bool _changed = true;

	private Uri _uri;

	public string Scheme
	{
		get
		{
			return _scheme;
		}
		[param: AllowNull]
		set
		{
			if (value == null)
			{
				value = string.Empty;
			}
			if (value.Length != 0)
			{
				if (!System.Uri.CheckSchemeName(value))
				{
					int num = value.IndexOf(':');
					if (num != -1)
					{
						value = value.Substring(0, num);
					}
					if (!System.Uri.CheckSchemeName(value))
					{
						throw new ArgumentException(System.SR.net_uri_BadScheme, "value");
					}
				}
				value = value.ToLowerInvariant();
			}
			_scheme = value;
			_changed = true;
		}
	}

	public string UserName
	{
		get
		{
			return _username;
		}
		[param: AllowNull]
		set
		{
			_username = value ?? string.Empty;
			_changed = true;
		}
	}

	public string Password
	{
		get
		{
			return _password;
		}
		[param: AllowNull]
		set
		{
			_password = value ?? string.Empty;
			_changed = true;
		}
	}

	public string Host
	{
		get
		{
			return _host;
		}
		[param: AllowNull]
		set
		{
			if (!string.IsNullOrEmpty(value) && value.Contains(':') && value[0] != '[')
			{
				value = "[" + value + "]";
			}
			_host = value ?? string.Empty;
			_changed = true;
		}
	}

	public int Port
	{
		get
		{
			return _port;
		}
		set
		{
			if (value < -1 || value > 65535)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_port = value;
			_changed = true;
		}
	}

	public string Path
	{
		get
		{
			return _path;
		}
		[param: AllowNull]
		set
		{
			_path = (string.IsNullOrEmpty(value) ? "/" : System.Uri.InternalEscapeString(value.Replace('\\', '/')));
			_changed = true;
		}
	}

	public string Query
	{
		get
		{
			return _query;
		}
		[param: AllowNull]
		set
		{
			if (!string.IsNullOrEmpty(value) && value[0] != '?')
			{
				value = "?" + value;
			}
			_query = value ?? string.Empty;
			_changed = true;
		}
	}

	public string Fragment
	{
		get
		{
			return _fragment;
		}
		[param: AllowNull]
		set
		{
			if (!string.IsNullOrEmpty(value) && value[0] != '#')
			{
				value = "#" + value;
			}
			_fragment = value ?? string.Empty;
			_changed = true;
		}
	}

	public Uri Uri
	{
		get
		{
			if (_changed)
			{
				_uri = new Uri(ToString());
				SetFieldsFromUri();
				_changed = false;
			}
			return _uri;
		}
	}

	public UriBuilder()
	{
	}

	public UriBuilder(string uri)
	{
		_uri = new Uri(uri, UriKind.RelativeOrAbsolute);
		if (!_uri.IsAbsoluteUri)
		{
			_uri = new Uri(System.Uri.UriSchemeHttp + System.Uri.SchemeDelimiter + uri);
		}
		SetFieldsFromUri();
	}

	public UriBuilder(Uri uri)
	{
		_uri = uri ?? throw new ArgumentNullException("uri");
		SetFieldsFromUri();
	}

	public UriBuilder(string? schemeName, string? hostName)
	{
		Scheme = schemeName;
		Host = hostName;
	}

	public UriBuilder(string? scheme, string? host, int portNumber)
		: this(scheme, host)
	{
		Port = portNumber;
	}

	public UriBuilder(string? scheme, string? host, int port, string? pathValue)
		: this(scheme, host, port)
	{
		Path = pathValue;
	}

	public UriBuilder(string? scheme, string? host, int port, string? path, string? extraValue)
		: this(scheme, host, port, path)
	{
		if (string.IsNullOrEmpty(extraValue))
		{
			return;
		}
		if (extraValue[0] == '#')
		{
			_fragment = extraValue;
		}
		else
		{
			if (extraValue[0] != '?')
			{
				throw new ArgumentException(System.SR.Argument_ExtraNotValid, "extraValue");
			}
			int num = extraValue.IndexOf('#');
			if (num == -1)
			{
				_query = extraValue;
			}
			else
			{
				_query = extraValue.Substring(0, num);
				_fragment = extraValue.Substring(num);
			}
		}
		if (_query.Length == 1)
		{
			_query = string.Empty;
		}
		if (_fragment.Length == 1)
		{
			_fragment = string.Empty;
		}
	}

	public override bool Equals([NotNullWhen(true)] object? rparam)
	{
		if (rparam != null)
		{
			return Uri.Equals(rparam.ToString());
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Uri.GetHashCode();
	}

	private void SetFieldsFromUri()
	{
		_scheme = _uri.Scheme;
		_host = _uri.Host;
		_port = _uri.Port;
		_path = _uri.AbsolutePath;
		_query = _uri.Query;
		_fragment = _uri.Fragment;
		string userInfo = _uri.UserInfo;
		if (userInfo.Length > 0)
		{
			int num = userInfo.IndexOf(':');
			if (num != -1)
			{
				_password = userInfo.Substring(num + 1);
				_username = userInfo.Substring(0, num);
			}
			else
			{
				_username = userInfo;
			}
		}
	}

	public override string ToString()
	{
		if (UserName.Length == 0 && Password.Length != 0)
		{
			throw new UriFormatException(System.SR.net_uri_BadUserPassword);
		}
		Span<char> initialBuffer = stackalloc char[512];
		System.Text.ValueStringBuilder valueStringBuilder = new System.Text.ValueStringBuilder(initialBuffer);
		string scheme = Scheme;
		string host = Host;
		if (scheme.Length != 0)
		{
			UriParser syntax = UriParser.GetSyntax(scheme);
			string s = ((syntax != null) ? ((syntax.InFact(UriSyntaxFlags.MustHaveAuthority) || (host.Length != 0 && syntax.NotAny(UriSyntaxFlags.MailToLikeUri) && syntax.InFact(UriSyntaxFlags.OptionalAuthority))) ? System.Uri.SchemeDelimiter : ":") : ((host.Length == 0) ? ":" : System.Uri.SchemeDelimiter));
			valueStringBuilder.Append(scheme);
			valueStringBuilder.Append(s);
		}
		string userName = UserName;
		if (userName.Length != 0)
		{
			valueStringBuilder.Append(userName);
			string password = Password;
			if (password.Length != 0)
			{
				valueStringBuilder.Append(':');
				valueStringBuilder.Append(password);
			}
			valueStringBuilder.Append('@');
		}
		if (host.Length != 0)
		{
			valueStringBuilder.Append(host);
			if (_port != -1)
			{
				valueStringBuilder.Append(':');
				int charsWritten;
				bool flag = _port.TryFormat(valueStringBuilder.AppendSpan(5), out charsWritten);
				valueStringBuilder.Length -= 5 - charsWritten;
			}
		}
		string path = Path;
		if (path.Length != 0)
		{
			if (!path.StartsWith('/') && host.Length != 0)
			{
				valueStringBuilder.Append('/');
			}
			valueStringBuilder.Append(path);
		}
		valueStringBuilder.Append(Query);
		valueStringBuilder.Append(Fragment);
		return valueStringBuilder.ToString();
	}
}
