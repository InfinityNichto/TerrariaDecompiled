using System.IO;
using System.Net.Mime;
using System.Text;

namespace System.Net.Mail;

public class LinkedResource : AttachmentBase
{
	public Uri? ContentLink
	{
		get
		{
			return base.ContentLocation;
		}
		set
		{
			base.ContentLocation = value;
		}
	}

	internal LinkedResource()
	{
	}

	public LinkedResource(string fileName)
		: base(fileName)
	{
	}

	public LinkedResource(string fileName, string? mediaType)
		: base(fileName, mediaType)
	{
	}

	public LinkedResource(string fileName, ContentType? contentType)
		: base(fileName, contentType)
	{
	}

	public LinkedResource(Stream contentStream)
		: base(contentStream)
	{
	}

	public LinkedResource(Stream contentStream, string? mediaType)
		: base(contentStream, mediaType)
	{
	}

	public LinkedResource(Stream contentStream, ContentType? contentType)
		: base(contentStream, contentType)
	{
	}

	public static LinkedResource CreateLinkedResourceFromString(string content)
	{
		LinkedResource linkedResource = new LinkedResource();
		linkedResource.SetContentFromString(content, null, string.Empty);
		return linkedResource;
	}

	public static LinkedResource CreateLinkedResourceFromString(string content, Encoding? contentEncoding, string? mediaType)
	{
		LinkedResource linkedResource = new LinkedResource();
		linkedResource.SetContentFromString(content, contentEncoding, mediaType);
		return linkedResource;
	}

	public static LinkedResource CreateLinkedResourceFromString(string content, ContentType? contentType)
	{
		LinkedResource linkedResource = new LinkedResource();
		linkedResource.SetContentFromString(content, contentType);
		return linkedResource;
	}
}
