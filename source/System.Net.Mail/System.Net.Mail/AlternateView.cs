using System.IO;
using System.Net.Mime;
using System.Text;

namespace System.Net.Mail;

public class AlternateView : AttachmentBase
{
	private LinkedResourceCollection _linkedResources;

	public LinkedResourceCollection LinkedResources
	{
		get
		{
			if (disposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}
			return _linkedResources ?? (_linkedResources = new LinkedResourceCollection());
		}
	}

	public Uri? BaseUri
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

	internal AlternateView()
	{
	}

	public AlternateView(string fileName)
		: base(fileName)
	{
	}

	public AlternateView(string fileName, string? mediaType)
		: base(fileName, mediaType)
	{
	}

	public AlternateView(string fileName, ContentType? contentType)
		: base(fileName, contentType)
	{
	}

	public AlternateView(Stream contentStream)
		: base(contentStream)
	{
	}

	public AlternateView(Stream contentStream, string? mediaType)
		: base(contentStream, mediaType)
	{
	}

	public AlternateView(Stream contentStream, ContentType? contentType)
		: base(contentStream, contentType)
	{
	}

	public static AlternateView CreateAlternateViewFromString(string content)
	{
		AlternateView alternateView = new AlternateView();
		alternateView.SetContentFromString(content, null, string.Empty);
		return alternateView;
	}

	public static AlternateView CreateAlternateViewFromString(string content, Encoding? contentEncoding, string? mediaType)
	{
		AlternateView alternateView = new AlternateView();
		alternateView.SetContentFromString(content, contentEncoding, mediaType);
		return alternateView;
	}

	public static AlternateView CreateAlternateViewFromString(string content, ContentType? contentType)
	{
		AlternateView alternateView = new AlternateView();
		alternateView.SetContentFromString(content, contentType);
		return alternateView;
	}

	protected override void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing && _linkedResources != null)
			{
				_linkedResources.Dispose();
			}
			base.Dispose(disposing);
		}
	}
}
