using System.Collections.Generic;
using System.Globalization;
using System.Net.Sockets;
using System.Security;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net;

internal sealed class ServiceNameStore
{
	private readonly List<string> _serviceNames;

	private ServiceNameCollection _serviceNameCollection;

	public ServiceNameCollection ServiceNames
	{
		get
		{
			if (_serviceNameCollection == null)
			{
				_serviceNameCollection = new ServiceNameCollection(_serviceNames);
			}
			return _serviceNameCollection;
		}
	}

	public ServiceNameStore()
	{
		_serviceNames = new List<string>();
		_serviceNameCollection = null;
	}

	private static string NormalizeServiceName(string inputServiceName)
	{
		if (string.IsNullOrWhiteSpace(inputServiceName))
		{
			return inputServiceName;
		}
		int num = inputServiceName.IndexOf('/');
		if (num < 0)
		{
			return inputServiceName;
		}
		string text = inputServiceName.Substring(0, num + 1);
		string text2 = inputServiceName.Substring(num + 1);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return inputServiceName;
		}
		string text3 = text2;
		string text4 = string.Empty;
		string text5 = string.Empty;
		UriHostNameType uriHostNameType = Uri.CheckHostName(text2);
		if (uriHostNameType == UriHostNameType.Unknown)
		{
			string text6 = text2;
			int num2 = text2.IndexOf('/');
			if (num2 >= 0)
			{
				text6 = text2.Substring(0, num2);
				text5 = text2.Substring(num2);
				text3 = text6;
			}
			int num3 = text6.LastIndexOf(':');
			if (num3 >= 0)
			{
				text3 = text6.Substring(0, num3);
				text4 = text6.Substring(num3 + 1);
				if (!ushort.TryParse(text4, NumberStyles.Integer, CultureInfo.InvariantCulture, out var _))
				{
					return inputServiceName;
				}
				text4 = text6.Substring(num3);
			}
			uriHostNameType = Uri.CheckHostName(text3);
		}
		if (uriHostNameType != UriHostNameType.Dns)
		{
			return inputServiceName;
		}
		if (!Uri.TryCreate(Uri.UriSchemeHttp + Uri.SchemeDelimiter + text3, UriKind.Absolute, out Uri result2))
		{
			return inputServiceName;
		}
		string components = result2.GetComponents(UriComponents.NormalizedHost, UriFormat.SafeUnescaped);
		string text7 = text + components + text4 + text5;
		if (inputServiceName.Equals(text7, StringComparison.OrdinalIgnoreCase))
		{
			return inputServiceName;
		}
		return text7;
	}

	private bool AddSingleServiceName(string spn)
	{
		spn = NormalizeServiceName(spn);
		if (Contains(spn))
		{
			return false;
		}
		_serviceNames.Add(spn);
		return true;
	}

	public bool Add(string uriPrefix)
	{
		string[] array = BuildServiceNames(uriPrefix);
		bool flag = false;
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (AddSingleServiceName(text))
			{
				flag = true;
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, System.SR.Format(System.SR.net_log_listener_spn_add, text, uriPrefix), "Add");
				}
			}
		}
		if (flag)
		{
			_serviceNameCollection = null;
		}
		else if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, System.SR.Format(System.SR.net_log_listener_spn_not_add, uriPrefix), "Add");
		}
		return flag;
	}

	public bool Remove(string uriPrefix)
	{
		string inputServiceName = BuildSimpleServiceName(uriPrefix);
		inputServiceName = NormalizeServiceName(inputServiceName);
		bool flag = Contains(inputServiceName);
		if (flag)
		{
			_serviceNames.Remove(inputServiceName);
			_serviceNameCollection = null;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			if (flag)
			{
				System.Net.NetEventSource.Info(this, System.SR.Format(System.SR.net_log_listener_spn_remove, inputServiceName, uriPrefix), "Remove");
			}
			else
			{
				System.Net.NetEventSource.Info(this, System.SR.Format(System.SR.net_log_listener_spn_not_remove, uriPrefix), "Remove");
			}
		}
		return flag;
	}

	private bool Contains(string newServiceName)
	{
		if (newServiceName == null)
		{
			return false;
		}
		foreach (string serviceName in _serviceNames)
		{
			if (serviceName.Equals(newServiceName, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	public void Clear()
	{
		_serviceNames.Clear();
		_serviceNameCollection = null;
	}

	private string ExtractHostname(string uriPrefix, bool allowInvalidUriStrings)
	{
		if (Uri.IsWellFormedUriString(uriPrefix, UriKind.Absolute))
		{
			Uri uri = new Uri(uriPrefix);
			return uri.Host;
		}
		if (allowInvalidUriStrings)
		{
			int num = uriPrefix.IndexOf("://", StringComparison.Ordinal) + 3;
			int i = num;
			for (bool flag = false; i < uriPrefix.Length && uriPrefix[i] != '/' && (uriPrefix[i] != ':' || flag); i++)
			{
				if (uriPrefix[i] == '[')
				{
					if (flag)
					{
						i = num;
						break;
					}
					flag = true;
				}
				if (flag && uriPrefix[i] == ']')
				{
					flag = false;
				}
			}
			return uriPrefix.Substring(num, i - num);
		}
		return null;
	}

	public string BuildSimpleServiceName(string uriPrefix)
	{
		string text = ExtractHostname(uriPrefix, allowInvalidUriStrings: false);
		if (text != null)
		{
			return "HTTP/" + text;
		}
		return null;
	}

	public string[] BuildServiceNames(string uriPrefix)
	{
		string text = ExtractHostname(uriPrefix, allowInvalidUriStrings: true);
		IPAddress address = null;
		if (string.Equals(text, "*", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "+", StringComparison.OrdinalIgnoreCase) || IPAddress.TryParse(text, out address))
		{
			try
			{
				string hostName = Dns.GetHostEntry(string.Empty).HostName;
				return new string[1] { "HTTP/" + hostName };
			}
			catch (SocketException)
			{
				return Array.Empty<string>();
			}
			catch (SecurityException)
			{
				return Array.Empty<string>();
			}
		}
		if (!text.Contains('.'))
		{
			try
			{
				string hostName2 = Dns.GetHostEntry(text).HostName;
				return new string[2]
				{
					"HTTP/" + text,
					"HTTP/" + hostName2
				};
			}
			catch (SocketException)
			{
				return new string[1] { "HTTP/" + text };
			}
			catch (SecurityException)
			{
				return new string[1] { "HTTP/" + text };
			}
		}
		return new string[1] { "HTTP/" + text };
	}
}
