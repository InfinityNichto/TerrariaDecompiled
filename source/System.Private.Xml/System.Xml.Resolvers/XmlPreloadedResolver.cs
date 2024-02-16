using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace System.Xml.Resolvers;

public class XmlPreloadedResolver : XmlResolver
{
	private abstract class PreloadedData
	{
		internal abstract Stream AsStream();

		internal virtual TextReader AsTextReader()
		{
			throw new XmlException(System.SR.Xml_UnsupportedClass);
		}

		internal virtual bool SupportsType(Type type)
		{
			if (type == null || type == typeof(Stream))
			{
				return true;
			}
			return false;
		}
	}

	private sealed class XmlKnownDtdData : PreloadedData
	{
		internal string publicId;

		internal string systemId;

		private readonly string _resourceName;

		internal XmlKnownDtdData(string publicId, string systemId, string resourceName)
		{
			this.publicId = publicId;
			this.systemId = systemId;
			_resourceName = resourceName;
		}

		internal override Stream AsStream()
		{
			Assembly assembly = GetType().Assembly;
			return assembly.GetManifestResourceStream(_resourceName);
		}
	}

	private sealed class ByteArrayChunk : PreloadedData
	{
		private readonly byte[] _array;

		private readonly int _offset;

		private readonly int _length;

		internal ByteArrayChunk(byte[] array)
			: this(array, 0, array.Length)
		{
		}

		internal ByteArrayChunk(byte[] array, int offset, int length)
		{
			_array = array;
			_offset = offset;
			_length = length;
		}

		internal override Stream AsStream()
		{
			return new MemoryStream(_array, _offset, _length);
		}
	}

	private sealed class StringData : PreloadedData
	{
		private readonly string _str;

		internal StringData(string str)
		{
			_str = str;
		}

		internal override Stream AsStream()
		{
			return new MemoryStream(Encoding.Unicode.GetBytes(_str));
		}

		internal override TextReader AsTextReader()
		{
			return new StringReader(_str);
		}

		internal override bool SupportsType(Type type)
		{
			if (type == typeof(TextReader))
			{
				return true;
			}
			return base.SupportsType(type);
		}
	}

	private readonly XmlResolver _fallbackResolver;

	private readonly Dictionary<Uri, PreloadedData> _mappings;

	private readonly XmlKnownDtds _preloadedDtds;

	private static readonly XmlKnownDtdData[] s_xhtml10_Dtd = new XmlKnownDtdData[6]
	{
		new XmlKnownDtdData("-//W3C//DTD XHTML 1.0 Strict//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd", "xhtml1-strict.dtd"),
		new XmlKnownDtdData("-//W3C//DTD XHTML 1.0 Transitional//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd", "xhtml1-transitional.dtd"),
		new XmlKnownDtdData("-//W3C//DTD XHTML 1.0 Frameset//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml1-frameset.dtd", "xhtml1-frameset.dtd"),
		new XmlKnownDtdData("-//W3C//ENTITIES Latin 1 for XHTML//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml-lat1.ent", "xhtml-lat1.ent"),
		new XmlKnownDtdData("-//W3C//ENTITIES Symbols for XHTML//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml-symbol.ent", "xhtml-symbol.ent"),
		new XmlKnownDtdData("-//W3C//ENTITIES Special for XHTML//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml-special.ent", "xhtml-special.ent")
	};

	private static readonly XmlKnownDtdData[] s_rss091_Dtd = new XmlKnownDtdData[1]
	{
		new XmlKnownDtdData("-//Netscape Communications//DTD RSS 0.91//EN", "http://my.netscape.com/publish/formats/rss-0.91.dtd", "rss-0.91.dtd")
	};

	public override ICredentials Credentials
	{
		set
		{
			if (_fallbackResolver != null)
			{
				_fallbackResolver.Credentials = value;
			}
		}
	}

	public IEnumerable<Uri> PreloadedUris => _mappings.Keys;

	public XmlPreloadedResolver()
		: this(null)
	{
	}

	public XmlPreloadedResolver(XmlKnownDtds preloadedDtds)
		: this(null, preloadedDtds, null)
	{
	}

	public XmlPreloadedResolver(XmlResolver? fallbackResolver)
		: this(fallbackResolver, XmlKnownDtds.All, null)
	{
	}

	public XmlPreloadedResolver(XmlResolver? fallbackResolver, XmlKnownDtds preloadedDtds)
		: this(fallbackResolver, preloadedDtds, null)
	{
	}

	public XmlPreloadedResolver(XmlResolver? fallbackResolver, XmlKnownDtds preloadedDtds, IEqualityComparer<Uri>? uriComparer)
	{
		_fallbackResolver = fallbackResolver;
		_mappings = new Dictionary<Uri, PreloadedData>(16, uriComparer);
		_preloadedDtds = preloadedDtds;
		if (preloadedDtds != 0)
		{
			if ((preloadedDtds & XmlKnownDtds.Xhtml10) != 0)
			{
				AddKnownDtd(s_xhtml10_Dtd);
			}
			if ((preloadedDtds & XmlKnownDtds.Rss091) != 0)
			{
				AddKnownDtd(s_rss091_Dtd);
			}
		}
	}

	public override Uri ResolveUri(Uri? baseUri, string? relativeUri)
	{
		if (relativeUri != null && relativeUri.StartsWith("-//", StringComparison.CurrentCulture))
		{
			if ((_preloadedDtds & XmlKnownDtds.Xhtml10) != 0 && relativeUri.StartsWith("-//W3C//", StringComparison.CurrentCulture))
			{
				for (int i = 0; i < s_xhtml10_Dtd.Length; i++)
				{
					if (relativeUri == s_xhtml10_Dtd[i].publicId)
					{
						return new Uri(relativeUri, UriKind.Relative);
					}
				}
			}
			if ((_preloadedDtds & XmlKnownDtds.Rss091) != 0 && relativeUri == s_rss091_Dtd[0].publicId)
			{
				return new Uri(relativeUri, UriKind.Relative);
			}
		}
		return base.ResolveUri(baseUri, relativeUri);
	}

	public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
	{
		if (absoluteUri == null)
		{
			throw new ArgumentNullException("absoluteUri");
		}
		if (!_mappings.TryGetValue(absoluteUri, out var value))
		{
			if (_fallbackResolver != null)
			{
				return _fallbackResolver.GetEntity(absoluteUri, role, ofObjectToReturn);
			}
			throw new XmlException(System.SR.Format(System.SR.Xml_CannotResolveUrl, absoluteUri));
		}
		if (ofObjectToReturn == null || ofObjectToReturn == typeof(Stream) || ofObjectToReturn == typeof(object))
		{
			return value.AsStream();
		}
		if (ofObjectToReturn == typeof(TextReader))
		{
			return value.AsTextReader();
		}
		throw new XmlException(System.SR.Xml_UnsupportedClass);
	}

	public override bool SupportsType(Uri absoluteUri, Type? type)
	{
		if (absoluteUri == null)
		{
			throw new ArgumentNullException("absoluteUri");
		}
		if (!_mappings.TryGetValue(absoluteUri, out var value))
		{
			if (_fallbackResolver != null)
			{
				return _fallbackResolver.SupportsType(absoluteUri, type);
			}
			return base.SupportsType(absoluteUri, type);
		}
		return value.SupportsType(type);
	}

	public void Add(Uri uri, byte[] value)
	{
		if (uri == null)
		{
			throw new ArgumentNullException("uri");
		}
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		Add(uri, new ByteArrayChunk(value, 0, value.Length));
	}

	public void Add(Uri uri, byte[] value, int offset, int count)
	{
		if (uri == null)
		{
			throw new ArgumentNullException("uri");
		}
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (offset < 0)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if (value.Length - offset < count)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		Add(uri, new ByteArrayChunk(value, offset, count));
	}

	public void Add(Uri uri, Stream value)
	{
		if (uri == null)
		{
			throw new ArgumentNullException("uri");
		}
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		checked
		{
			if (value.CanSeek)
			{
				int num = (int)value.Length;
				byte[] array = new byte[num];
				value.Read(array, 0, num);
				Add(uri, new ByteArrayChunk(array));
				return;
			}
			MemoryStream memoryStream = new MemoryStream();
			byte[] array2 = new byte[4096];
			int count;
			while ((count = value.Read(array2, 0, array2.Length)) > 0)
			{
				memoryStream.Write(array2, 0, count);
			}
			int num2 = (int)memoryStream.Position;
			byte[] array3 = new byte[num2];
			Array.Copy(memoryStream.ToArray(), array3, num2);
			Add(uri, new ByteArrayChunk(array3));
		}
	}

	public void Add(Uri uri, string value)
	{
		if (uri == null)
		{
			throw new ArgumentNullException("uri");
		}
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		Add(uri, new StringData(value));
	}

	public void Remove(Uri uri)
	{
		if (uri == null)
		{
			throw new ArgumentNullException("uri");
		}
		_mappings.Remove(uri);
	}

	private void Add(Uri uri, PreloadedData data)
	{
		if (_mappings.ContainsKey(uri))
		{
			_mappings[uri] = data;
		}
		else
		{
			_mappings.Add(uri, data);
		}
	}

	private void AddKnownDtd(XmlKnownDtdData[] dtdSet)
	{
		foreach (XmlKnownDtdData xmlKnownDtdData in dtdSet)
		{
			_mappings.Add(new Uri(xmlKnownDtdData.publicId, UriKind.RelativeOrAbsolute), xmlKnownDtdData);
			_mappings.Add(new Uri(xmlKnownDtdData.systemId, UriKind.RelativeOrAbsolute), xmlKnownDtdData);
		}
	}

	public override Task<object> GetEntityAsync(Uri absoluteUri, string? role, Type? ofObjectToReturn)
	{
		if (absoluteUri == null)
		{
			throw new ArgumentNullException("absoluteUri");
		}
		if (!_mappings.TryGetValue(absoluteUri, out var value))
		{
			if (_fallbackResolver != null)
			{
				return _fallbackResolver.GetEntityAsync(absoluteUri, role, ofObjectToReturn);
			}
			throw new XmlException(System.SR.Format(System.SR.Xml_CannotResolveUrl, absoluteUri));
		}
		if (ofObjectToReturn == null || ofObjectToReturn == typeof(Stream) || ofObjectToReturn == typeof(object))
		{
			return Task.FromResult((object)value.AsStream());
		}
		if (ofObjectToReturn == typeof(TextReader))
		{
			return Task.FromResult((object)value.AsTextReader());
		}
		throw new XmlException(System.SR.Xml_UnsupportedClass);
	}
}
