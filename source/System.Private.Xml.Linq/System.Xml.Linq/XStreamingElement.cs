using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace System.Xml.Linq;

public class XStreamingElement
{
	internal XName name;

	internal object content;

	public XName Name
	{
		get
		{
			return name;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			name = value;
		}
	}

	public XStreamingElement(XName name)
	{
		if (name == null)
		{
			throw new ArgumentNullException("name");
		}
		this.name = name;
	}

	public XStreamingElement(XName name, object? content)
		: this(name)
	{
		this.content = ((!(content is List<object>)) ? content : new object[1] { content });
	}

	public XStreamingElement(XName name, params object?[] content)
		: this(name)
	{
		this.content = content;
	}

	public void Add(object? content)
	{
		if (content == null)
		{
			return;
		}
		List<object> list = this.content as List<object>;
		if (list == null)
		{
			list = new List<object>();
			if (this.content != null)
			{
				list.Add(this.content);
			}
			this.content = list;
		}
		list.Add(content);
	}

	public void Add(params object?[] content)
	{
		Add((object?)content);
	}

	public void Save(Stream stream)
	{
		Save(stream, SaveOptions.None);
	}

	public void Save(Stream stream, SaveOptions options)
	{
		XmlWriterSettings xmlWriterSettings = XNode.GetXmlWriterSettings(options);
		using XmlWriter writer = XmlWriter.Create(stream, xmlWriterSettings);
		Save(writer);
	}

	public void Save(TextWriter textWriter)
	{
		Save(textWriter, SaveOptions.None);
	}

	public void Save(TextWriter textWriter, SaveOptions options)
	{
		XmlWriterSettings xmlWriterSettings = XNode.GetXmlWriterSettings(options);
		using XmlWriter writer = XmlWriter.Create(textWriter, xmlWriterSettings);
		Save(writer);
	}

	public void Save(XmlWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		writer.WriteStartDocument();
		WriteTo(writer);
		writer.WriteEndDocument();
	}

	public void Save(string fileName)
	{
		Save(fileName, SaveOptions.None);
	}

	public void Save(string fileName, SaveOptions options)
	{
		XmlWriterSettings xmlWriterSettings = XNode.GetXmlWriterSettings(options);
		using XmlWriter writer = XmlWriter.Create(fileName, xmlWriterSettings);
		Save(writer);
	}

	public override string ToString()
	{
		return GetXmlString(SaveOptions.None);
	}

	public string ToString(SaveOptions options)
	{
		return GetXmlString(options);
	}

	public void WriteTo(XmlWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		new StreamingElementWriter(writer).WriteStreamingElement(this);
	}

	private string GetXmlString(SaveOptions o)
	{
		using StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
		XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
		xmlWriterSettings.OmitXmlDeclaration = true;
		if ((o & SaveOptions.DisableFormatting) == 0)
		{
			xmlWriterSettings.Indent = true;
		}
		if ((o & SaveOptions.OmitDuplicateNamespaces) != 0)
		{
			xmlWriterSettings.NamespaceHandling |= NamespaceHandling.OmitDuplicates;
		}
		using (XmlWriter writer = XmlWriter.Create(stringWriter, xmlWriterSettings))
		{
			WriteTo(writer);
		}
		return stringWriter.ToString();
	}
}
