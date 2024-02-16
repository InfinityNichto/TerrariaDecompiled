using System.Net;
using System.Threading.Tasks;

namespace System.Xml;

public class XmlSecureResolver : XmlResolver
{
	private readonly XmlResolver _resolver;

	public override ICredentials Credentials
	{
		set
		{
			_resolver.Credentials = value;
		}
	}

	public XmlSecureResolver(XmlResolver resolver, string? securityUrl)
	{
		_resolver = resolver;
	}

	public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
	{
		return _resolver.GetEntity(absoluteUri, role, ofObjectToReturn);
	}

	public override Uri ResolveUri(Uri? baseUri, string? relativeUri)
	{
		return _resolver.ResolveUri(baseUri, relativeUri);
	}

	public override Task<object> GetEntityAsync(Uri absoluteUri, string? role, Type? ofObjectToReturn)
	{
		return _resolver.GetEntityAsync(absoluteUri, role, ofObjectToReturn);
	}
}
