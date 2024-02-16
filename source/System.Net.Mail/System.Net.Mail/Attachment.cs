using System.IO;
using System.Net.Mime;
using System.Text;

namespace System.Net.Mail;

public class Attachment : AttachmentBase
{
	private string _name;

	private Encoding _nameEncoding;

	public string? Name
	{
		get
		{
			return _name;
		}
		set
		{
			Encoding encoding = MimeBasePart.DecodeEncoding(value);
			if (encoding != null)
			{
				_nameEncoding = encoding;
				_name = MimeBasePart.DecodeHeaderValue(value);
				base.MimePart.ContentType.Name = value;
			}
			else
			{
				_name = value;
				SetContentTypeName(allowUnicode: true);
			}
		}
	}

	public Encoding? NameEncoding
	{
		get
		{
			return _nameEncoding;
		}
		set
		{
			_nameEncoding = value;
			if (_name != null && _name != string.Empty)
			{
				SetContentTypeName(allowUnicode: true);
			}
		}
	}

	public ContentDisposition? ContentDisposition => base.MimePart.ContentDisposition;

	internal Attachment()
	{
		base.MimePart.ContentDisposition = new ContentDisposition();
	}

	public Attachment(string fileName)
		: base(fileName)
	{
		Name = Path.GetFileName(fileName);
		base.MimePart.ContentDisposition = new ContentDisposition();
	}

	public Attachment(string fileName, string? mediaType)
		: base(fileName, mediaType)
	{
		Name = Path.GetFileName(fileName);
		base.MimePart.ContentDisposition = new ContentDisposition();
	}

	public Attachment(string fileName, ContentType contentType)
		: base(fileName, contentType)
	{
		if (string.IsNullOrEmpty(contentType.Name))
		{
			Name = Path.GetFileName(fileName);
		}
		else
		{
			Name = contentType.Name;
		}
		base.MimePart.ContentDisposition = new ContentDisposition();
	}

	public Attachment(Stream contentStream, string? name)
		: base(contentStream, null, null)
	{
		Name = name;
		base.MimePart.ContentDisposition = new ContentDisposition();
	}

	public Attachment(Stream contentStream, string? name, string? mediaType)
		: base(contentStream, null, mediaType)
	{
		Name = name;
		base.MimePart.ContentDisposition = new ContentDisposition();
	}

	public Attachment(Stream contentStream, ContentType contentType)
		: base(contentStream, contentType)
	{
		Name = contentType.Name;
		base.MimePart.ContentDisposition = new ContentDisposition();
	}

	internal void SetContentTypeName(bool allowUnicode)
	{
		if (!allowUnicode && _name != null && _name.Length != 0 && !MimeBasePart.IsAscii(_name, permitCROrLF: false))
		{
			Encoding encoding = NameEncoding ?? Encoding.GetEncoding("utf-8");
			base.MimePart.ContentType.Name = MimeBasePart.EncodeHeaderValue(_name, encoding, MimeBasePart.ShouldUseBase64Encoding(encoding));
		}
		else
		{
			base.MimePart.ContentType.Name = _name;
		}
	}

	internal override void PrepareForSending(bool allowUnicode)
	{
		if (_name != null && _name != string.Empty)
		{
			SetContentTypeName(allowUnicode);
		}
		base.PrepareForSending(allowUnicode);
	}

	public static Attachment CreateAttachmentFromString(string content, string? name)
	{
		Attachment attachment = new Attachment();
		attachment.SetContentFromString(content, null, string.Empty);
		attachment.Name = name;
		return attachment;
	}

	public static Attachment CreateAttachmentFromString(string content, string? name, Encoding? contentEncoding, string? mediaType)
	{
		Attachment attachment = new Attachment();
		attachment.SetContentFromString(content, contentEncoding, mediaType);
		attachment.Name = name;
		return attachment;
	}

	public static Attachment CreateAttachmentFromString(string content, ContentType contentType)
	{
		Attachment attachment = new Attachment();
		attachment.SetContentFromString(content, contentType);
		attachment.Name = contentType.Name;
		return attachment;
	}
}
