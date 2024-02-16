using System.Collections.Specialized;
using System.Net.Mail;
using System.Text;

namespace System.Net.Mime;

internal sealed class HeaderCollection : NameValueCollection
{
	internal HeaderCollection()
		: base(StringComparer.OrdinalIgnoreCase)
	{
	}

	public override void Remove(string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_emptystringcall, "name"), "name");
		}
		base.Remove(name);
	}

	public override string Get(string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_emptystringcall, "name"), "name");
		}
		return base.Get(name);
	}

	public override string[] GetValues(string name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_emptystringcall, "name"), "name");
		}
		return base.GetValues(name);
	}

	internal void InternalRemove(string name)
	{
		base.Remove(name);
	}

	internal void InternalSet(string name, string value)
	{
		base.Set(name, value);
	}

	internal void InternalAdd(string name, string value)
	{
		if (MailHeaderInfo.IsSingleton(name))
		{
			base.Set(name, value);
		}
		else
		{
			base.Add(name, value);
		}
	}

	public override void Set(string name, string value)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_emptystringcall, "name"), "name");
		}
		if (value.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_emptystringcall, "value"), "value");
		}
		if (!MimeBasePart.IsAscii(name, permitCROrLF: false))
		{
			throw new FormatException(System.SR.InvalidHeaderName);
		}
		name = MailHeaderInfo.NormalizeCase(name);
		value = value.Normalize(NormalizationForm.FormC);
		base.Set(name, value);
	}

	public override void Add(string name, string value)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (name.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_emptystringcall, "name"), "name");
		}
		if (value.Length == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_emptystringcall, "value"), "value");
		}
		MailBnfHelper.ValidateHeaderName(name);
		name = MailHeaderInfo.NormalizeCase(name);
		value = value.Normalize(NormalizationForm.FormC);
		InternalAdd(name, value);
	}
}
