using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Security.Authentication.ExtendedProtection;

public class ServiceNameCollection : ReadOnlyCollectionBase
{
	public ServiceNameCollection(ICollection items)
	{
		if (items == null)
		{
			throw new ArgumentNullException("items");
		}
		AddIfNew(items, expectStrings: true);
	}

	private ServiceNameCollection(IList list, string serviceName)
		: this(list, 1)
	{
		AddIfNew(serviceName);
	}

	private ServiceNameCollection(IList list, IEnumerable serviceNames)
		: this(list, GetCountOrOne(serviceNames))
	{
		AddIfNew(serviceNames, expectStrings: false);
	}

	private ServiceNameCollection(IList list, int additionalCapacity)
	{
		foreach (string item in list)
		{
			base.InnerList.Add(item);
		}
	}

	public bool Contains(string? searchServiceName)
	{
		string b = NormalizeServiceName(searchServiceName);
		foreach (string inner in base.InnerList)
		{
			if (string.Equals(inner, b, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	public ServiceNameCollection Merge(string serviceName)
	{
		return new ServiceNameCollection(base.InnerList, serviceName);
	}

	public ServiceNameCollection Merge(IEnumerable serviceNames)
	{
		return new ServiceNameCollection(base.InnerList, serviceNames);
	}

	private void AddIfNew(IEnumerable serviceNames, bool expectStrings)
	{
		if (serviceNames is List<string> serviceNames2)
		{
			AddIfNew(serviceNames2);
			return;
		}
		if (serviceNames is ServiceNameCollection serviceNameCollection)
		{
			AddIfNew(serviceNameCollection.InnerList);
			return;
		}
		foreach (object serviceName in serviceNames)
		{
			AddIfNew(expectStrings ? ((string)serviceName) : (serviceName as string));
		}
	}

	private void AddIfNew(List<string> serviceNames)
	{
		foreach (string serviceName in serviceNames)
		{
			AddIfNew(serviceName);
		}
	}

	private void AddIfNew(IList serviceNames)
	{
		foreach (string serviceName in serviceNames)
		{
			AddIfNew(serviceName);
		}
	}

	private void AddIfNew(string serviceName)
	{
		if (string.IsNullOrEmpty(serviceName))
		{
			throw new ArgumentException(System.SR.security_ServiceNameCollection_EmptyServiceName);
		}
		serviceName = NormalizeServiceName(serviceName);
		if (!Contains(serviceName))
		{
			base.InnerList.Add(serviceName);
		}
	}

	private static int GetCountOrOne(IEnumerable collection)
	{
		if (!(collection is ICollection<string> collection2))
		{
			return 1;
		}
		return collection2.Count;
	}

	[return: NotNullIfNotNull("inputServiceName")]
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
		if (!Uri.TryCreate("http://" + text3, UriKind.Absolute, out Uri result2))
		{
			return inputServiceName;
		}
		string components = result2.GetComponents(UriComponents.NormalizedHost, UriFormat.SafeUnescaped);
		string text7 = text + components + text4 + text5;
		if (string.Equals(inputServiceName, text7, StringComparison.OrdinalIgnoreCase))
		{
			return inputServiceName;
		}
		return text7;
	}
}
